using Unity.Mathematics;
using UnityEngine;

namespace Traverser
{
    [RequireComponent(typeof(TraverserAbilityController))]
    [RequireComponent(typeof(TraverserCharacterController))]

    public class TraverserClimbingAbility : MonoBehaviour, TraverserAbility
    {
        // --- Attributes ---

        [Header("Ledge traversal settings")]
        [Tooltip("Desired speed in meters per second for ledge climbing.")]
        [Range(0.0f, 10.0f)]
        public float desiredSpeedLedge;

        [Tooltip("The closest the character will be able to get to a ledge corner, in meters.")]
        [Range(0.0f, 0.5f)]
        public float desiredCornerMinDistance;

        [Header("Feet IK settings")]
        [Tooltip("Activates or deactivates foot IK placement for the climbing ability.")]
        public bool fIKOn = true;
        [Tooltip("The character's foot length (size in meters).")]
        [Range(0.0f, 1.0f)]
        public float footLength = 1.0f;

        [Header("Hands IK settings")]
        [Tooltip("Activates or deactivates hand IK placement for the climbing ability.")]
        public bool hIKOn = true;
        [Tooltip("The Y distance to correct in order to place the hands on the ledge.")]
        [Range(0.0f, 1.0f)]
        public float handIKYDistance = 1.0f;
        [Tooltip("The character's hand length (size in meters) / correction in forward direction.")]
        [Range(-1.0f, 1.0f)]
        public float handLength = 1.0f;

        // --------------------------------

        // --- Climbing ability state ---
        public enum State
        {
            Suspended,
            Mounting,
            Climbing,
            Dismount,
            PullUp,
            DropDown
        }

        // --------------------------------

        // --- Climbing state directions ---
        public enum ClimbingState
        {
            Idle,
            Up,
            Down,
            Left,
            Right,
            UpRight,
            DownRight,
            UpLeft,
            DownLeft,
            CornerRight, 
            CornerLeft, 
            None
        }

        // --------------------------------

        // --- Private Variables ---

        private TraverserAbilityController abilityController;
        private TraverserCharacterController controller;
        private TraverserAnimationController animationController;
        private TraverserLocomotionAbility locomotionAbility;

        private State state; // Actual Climbing movement/state
        private ClimbingState climbingState; // Actual Climbing direction

        // --------------------------------

        // --- World interactable elements ---

        private TraverserLedgeObject.TraverserLedgeGeometry ledgeGeometry;
        private TraverserLedgeObject.TraverserLedgeGeometry auxledgeGeometry;
        private TraverserWallObject.TraverserWallGeometry wallGeometry;
        private TraverserLedgeObject.TraverserLedgeHook ledgeHook;
        private TraverserWallObject.TraverserWallAnchor wallAnchor;

        // --------------------------------

        // --- Basic Methods ---
        public void Start()
        {
            abilityController = GetComponent<TraverserAbilityController>();
            controller = GetComponent<TraverserCharacterController>();
            animationController = GetComponent<TraverserAnimationController>();
            locomotionAbility = GetComponent<TraverserLocomotionAbility>();

            state = State.Suspended;
            climbingState = ClimbingState.None;

            ledgeGeometry = TraverserLedgeObject.TraverserLedgeGeometry.Create();
            auxledgeGeometry = TraverserLedgeObject.TraverserLedgeGeometry.Create();
            wallGeometry = TraverserWallObject.TraverserWallGeometry.Create();

            ledgeHook = TraverserLedgeObject.TraverserLedgeHook.Create();
            wallAnchor = TraverserWallObject.TraverserWallAnchor.Create();
        }

        // --------------------------------

        // --- Ability class methods ---
        public void OnInputUpdate()
        {
            TraverserInputLayer.capture.UpdateClimbing();
        }

        public TraverserAbility OnUpdate(float deltaTime)
        {
            if (animationController.transition.isON)
                animationController.transition.UpdateTransition();

            return this;
        }

        public TraverserAbility OnFixedUpdate(float deltaTime)
        {
            TraverserAbility ret = this;

            // --- If character is not falling ---
            if (!IsState(State.Suspended))
            {
                // --- Handle current state ---
                switch (state)
                {
                    case State.Mounting:
                        HandleMountingState();
                        break;

                    case State.Dismount:
                        HandleDismountState();
                        break;

                    case State.PullUp:
                        HandlePullUpState();
                        break;

                    case State.DropDown:
                        HandleDropDownState();
                        break;

                    case State.Climbing:
                        HandleClimbingState(deltaTime);
                        break;

                    default:
                        break;
                }

                // --- Let other abilities handle the situation ---
                if (IsState(State.Suspended))
                {
                    ret = null;
                    SetClimbingState(ClimbingState.None);

                    // --- Turn off/on controller ---
                    controller.ConfigureController(true);
                }
            }

            return ret;
        }

        public bool OnContact(TraverserTransform contactTransform, float deltaTime)
        {
            bool ret = false;

            if (TraverserInputLayer.capture.mountButton)
            {
                //---If we make contact with a climbable surface and player issues climb order, mount ---         
                if (IsState(State.Suspended))
                {
                    BoxCollider collider = controller.current.collider as BoxCollider;

                    // --- Ensure we are not falling ---
                    if (collider != null && controller.isGrounded)
                    {
                        if (collider.gameObject.GetComponent<TraverserClimbingObject>() != null)
                        {
                            // --- Fill auxiliary ledge to prevent mounting too close to a corner ---
                            auxledgeGeometry.Initialize(collider);
                            TraverserLedgeObject.TraverserLedgeHook auxHook = auxledgeGeometry.GetHook(contactTransform.t);
                            float distance = 0.0f;
                            auxledgeGeometry.ClosestPointDistance(contactTransform.t, auxHook, ref distance);

                            // --- Make sure we are not too close to the corner (We may trigger an unwanted transition afterwards) ---
                            if (!collider.Equals(controller.current.ground.GetComponent<BoxCollider>()) && distance > 0.25)
                            {
                                Debug.Log("Climbing to ledge");

                                ledgeGeometry.Initialize(collider);
                                wallGeometry.Initialize(collider, contactTransform);

                                // --- We want to reach the pre climb position and then activate the animation ---

                                ledgeHook = ledgeGeometry.GetHook(transform.position);
                                float3 hookPosition = ledgeGeometry.GetPosition(ledgeHook);
                                Quaternion hookRotation = Quaternion.LookRotation(ledgeGeometry.GetNormal(ledgeHook), transform.up);

                                Vector3 targetPosition = hookPosition - ledgeGeometry.GetNormal(ledgeHook) * controller.capsuleRadius * 1.5f;
                                targetPosition.y = hookPosition.y - controller.capsuleHeight;
                                TraverserTransform target = TraverserTransform.Get(targetPosition, hookRotation);

                                // --- Require a transition ---
                                ret = animationController.transition.StartTransition("WalkTransition", "Mount",
                                    "WalkTransitionTrigger", "MountTrigger", 1.0f, 1.0f,
                                    ref contactTransform,
                                    ref target);

                                // --- If transition start is successful, change state ---
                                if (ret)
                                {
                                    SetState(State.Mounting);

                                    // --- Turn off/on controller ---
                                    controller.ConfigureController(false/*!IsState(State.Suspended)*/);
                                }
                            }
                        }
                    }
                }
            }

            return ret;
        }

        public bool OnDrop(float deltaTime)
        {
            bool ret = false;

            if (TraverserInputLayer.capture.dropDownButton)
            {
                BoxCollider collider = controller.previous.ground as BoxCollider;

                if (collider)
                {
                    //Debug.Log(collider.name);

                    ledgeGeometry.Initialize(collider);
                    ledgeHook = ledgeGeometry.GetHook(controller.previous.position);
                    Vector3 targetPosition = ledgeGeometry.GetPosition(ledgeHook);
                    Quaternion hookRotation = Quaternion.LookRotation(-ledgeGeometry.GetNormal(ledgeHook), transform.up);

                    TraverserTransform target = TraverserTransform.Get(targetPosition, hookRotation);
                    hookRotation = Quaternion.LookRotation(ledgeGeometry.GetNormal(ledgeHook), transform.up);

                    TraverserTransform target2 = TraverserTransform.Get(targetPosition, hookRotation);

                    // --- Require a transition ---
                    ret = animationController.transition.StartTransition("WalkTransition", "DropDown",
                        "WalkTransitionTrigger", "DropDownTrigger", 0.25f, 1.5f,
                        ref target,
                        ref target2);

                    if (ret)
                    {
                        SetState(State.DropDown);
                        // --- Turn off/on controller ---
                        controller.ConfigureController(false);
                    }
                }
            }

            return ret;
        }

        public bool IsAbilityEnabled()
        {
            return isActiveAndEnabled;
        }

        // --- Climbing states wrappers ---
        void HandleMountingState()
        {
            // --- If transition is over and we were mounting change state to ledge idle ---
            if(!animationController.transition.isON)
            {
                SetState(State.Climbing); 
                SetClimbingState(ClimbingState.Idle);

                ledgeHook = ledgeGeometry.GetHook(transform.position);

                float3 hookPosition = ledgeGeometry.GetPosition(ledgeHook);
                Quaternion hookRotation = Quaternion.LookRotation(ledgeGeometry.GetNormal(ledgeHook), transform.up);
                Vector3 newPos = hookPosition - ledgeGeometry.GetNormal(ledgeHook) * controller.capsuleRadius * 1.3f;
                newPos.y = hookPosition.y - controller.capsuleHeight;

                controller.TeleportTo(newPos);
                transform.rotation = hookRotation;
            }
        }

        void HandleDismountState()
        {
            if (animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f)
            {
                animationController.fakeTransition = false;
                SetState(State.Suspended);
                // --- Get skeleton's current position and teleport controller ---
                float3 newTransform = animationController.skeleton.transform.position;
                newTransform.y -= controller.capsuleHeight / 2.0f;
                controller.TeleportTo(newTransform);
                locomotionAbility.ResetLocomotion();
                animationController.animator.Play("LocomotionON", 0, 0.0f);
            }
        }

        void HandlePullUpState()
        {
            if (animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f)
            {
                animationController.fakeTransition = false;
                SetState(State.Suspended);
                // --- Get skeleton's current position and teleport controller ---
                float3 newTransform = animationController.skeleton.transform.position;
                newTransform.y -= controller.capsuleHeight / 2.0f;
                controller.TeleportTo(newTransform);
                locomotionAbility.ResetLocomotion();
                animationController.animator.Play("LocomotionON", 0, 0.0f);
            }
        }

        void HandleDropDownState()
        {
            if (!animationController.transition.isON)
            {
                SetState(State.Climbing);
                SetClimbingState(ClimbingState.Idle);
                animationController.animator.Play("LedgeIdle", 0, 0.0f);

                ledgeHook = ledgeGeometry.GetHook(animationController.skeleton.position);
                float3 hookPosition = ledgeGeometry.GetPosition(ledgeHook);
                Quaternion hookRotation = Quaternion.LookRotation(ledgeGeometry.GetNormal(ledgeHook), transform.up);
                Vector3 newPos = hookPosition - ledgeGeometry.GetNormal(ledgeHook) * controller.capsuleRadius * 1.5f;
                newPos.y = hookPosition.y - controller.capsuleHeight;

                controller.ConfigureController(false);
                controller.TeleportTo(newPos);

                transform.rotation = hookRotation;
            }
        }

        void HandleClimbingState(float deltaTime)
        {
            // --- Handle special corner transitions ---
            if (climbingState == ClimbingState.CornerRight
                || climbingState == ClimbingState.CornerLeft
                )
            {
                if (animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >=  1.0f)
                {
                    ledgeHook = ledgeGeometry.GetHook(animationController.skeleton.position);
                    SetClimbingState(ClimbingState.None);
                    animationController.fakeTransition = false;

                    float3 hookPosition = ledgeGeometry.GetPosition(ledgeHook);
                    Quaternion hookRotation = Quaternion.LookRotation(ledgeGeometry.GetNormal(ledgeHook), transform.up);

                    Vector3 newPos = hookPosition - ledgeGeometry.GetNormal(ledgeHook)*controller.capsuleRadius*1.5f;
                    newPos.y = hookPosition.y - controller.capsuleHeight;

                    controller.TeleportTo(newPos);
                    transform.rotation = hookRotation;

                    animationController.animator.Play("LedgeIdle", 0, 0.0f);
                }

                return;
            }

            bool canMove = UpdateClimbing(deltaTime);

            ClimbingState desiredState = GetDesiredClimbingState();

            if (!canMove)
                desiredState = ClimbingState.Idle;

            // --- Handle ledge climbing/movement direction ---
            if (!IsClimbingState(desiredState))
            {

                if (desiredState == ClimbingState.Idle)
                    animationController.animator.Play("LedgeIdle", 0, 0.0f);
                else if (desiredState == ClimbingState.Right)
                    animationController.animator.Play("LedgeRight", 0, 0.0f);
                else if (desiredState == ClimbingState.Left)
                    animationController.animator.Play("LedgeLeft", 0, 0.0f);
                else if (desiredState == ClimbingState.CornerRight)
                {
                    // --- Leaving this here for reference, I just could not get the unity way of getting
                    // --- the position at the last frame working... Always got the current position... ---
                    //animationController.SetRootMotion(true);
                    //animationController.animator.Play("LedgeCornerRight", 0, 0.0f);
                    //animationController.animator.Update(1.0f);
                    //Vector3 position = animationController.animator.targetPosition;
                    //animationController.SetRootMotion(false);

                    // --- Compute the position of a capsule equal to the character's at the end of the corner transition ---
                    Vector3 position = transform.position;
                    position += transform.right;
                    position += transform.forward;

                    Vector3 start = position;
                    Vector3 end = position;
                    end.y += controller.capsuleHeight;

                    // --- If it collides against any relevant collider, do not start the corner transition ---
                    if (Physics.CheckCapsule(start, end, controller.capsuleRadius, TraverserCollisionLayer.EnvironmentCollisionMask))
                    {
                        desiredState = ClimbingState.Idle;
                        animationController.animator.Play("LedgeIdle", 0, 0.0f);
                    }
                    else
                    {
                        animationController.animator.Play("LedgeCornerRight", 0, 0.0f);
                        animationController.fakeTransition = true;
                    }
                }
                else if (desiredState == ClimbingState.CornerLeft)
                {
                    // --- Compute the position of a capsule equal to the character's at the end of the corner transition ---
                    Vector3 position = transform.position;
                    position -= transform.right;
                    position += transform.forward;

                    Vector3 start = position;
                    Vector3 end = position;
                    end.y += controller.capsuleHeight;

                    // --- If it collides against any relevant collider, do not start the corner transition ---
                    if (Physics.CheckCapsule(start, end, controller.capsuleRadius, TraverserCollisionLayer.EnvironmentCollisionMask))
                    {
                        desiredState = ClimbingState.Idle;
                        animationController.animator.Play("LedgeIdle", 0, 0.0f);
                    }
                    else
                    {
                        animationController.animator.Play("LedgeCornerLeft", 0, 0.0f);
                        animationController.fakeTransition = true;
                    }

                }

                SetClimbingState(desiredState);
            }

            TraverserTransform rootTransform = TraverserTransform.Get(transform.position, transform.rotation);
            rootTransform.t.y += controller.capsuleHeight;
            wallGeometry.Initialize(rootTransform);
            wallAnchor = wallGeometry.GetAnchor(rootTransform.t);
            float height = wallGeometry.GetHeight(ref wallAnchor);
            bool closeToDrop = math.abs(height - 2.8f) <= 0.095f;

            // --- React to pull up/dismount ---
            if (TraverserInputLayer.capture.pullUpButton)
            {
                animationController.animator.Play("PullUp", 0, 0.0f);
                animationController.fakeTransition = true;
                SetState(State.PullUp);
            }
            else if (closeToDrop && TraverserInputLayer.capture.dismountButton)
            {
                animationController.animator.Play("Dismount", 0, 0.0f);
                animationController.fakeTransition = true;
                SetState(State.Dismount);
            }
        }

        bool UpdateClimbing(float deltaTime)
        {
            bool ret = false;

            // --- Given input, compute target position ---
            float2 stickInput;
            stickInput.x = TraverserInputLayer.capture.stickHorizontal;
            stickInput.y = TraverserInputLayer.capture.stickVertical;

            // --- Since transform.position is only updated in fixed timestep we should use the controller's 
            // --- target position to accumulate movement and have framerate independent motion ---
            // --- Now we are performing this in fixes update so there is no problem, just as a reminder ---

            Vector3 target = controller.targetPosition; 
            target += transform.right * stickInput.x * desiredSpeedLedge * deltaTime;

            // --- Update hook ---
            TraverserLedgeObject.TraverserLedgeHook desiredLedgeHook = ledgeGeometry.UpdateHook(ledgeHook, target, desiredCornerMinDistance);

            // --- Update current hook if it is still on the ledge ---
            if (desiredLedgeHook.index == ledgeHook.index)
            {
                ret = true;
                ledgeHook = desiredLedgeHook;
                controller.targetPosition = target;
            }

            ledgeGeometry.DebugDraw();
            ledgeGeometry.DebugDraw(ref ledgeHook);

            return ret;
        }    

        

        // --------------------------------

        // --- Utilities ---    

        public void SetState(State newState)
        {
            state = newState;
        }

        public bool IsState(State queryState)
        {
            return state == queryState;
        }

        public void SetClimbingState(ClimbingState climbingState)
        {
            this.climbingState = climbingState;
        }

        public bool IsClimbingState(ClimbingState climbingState)
        {
            return this.climbingState == climbingState;
        }
        
        ClimbingState GetDesiredClimbingState() 
        {
            float2 stickInput;
            stickInput.x = TraverserInputLayer.capture.stickHorizontal;
            stickInput.y = TraverserInputLayer.capture.stickVertical;

            // --- Use ledge definition to determine how close we are to the edges of the wall, also at which side the vertex is (bool left) ---
            float distance = 0.0f;
            bool left = ledgeGeometry.ClosestPointDistance(controller.targetPosition, ledgeHook, ref distance); 

            if (stickInput.x > 0.5f)
            {
                if (!left && distance < 0.05f + desiredCornerMinDistance)
                    return ClimbingState.CornerRight;

                return ClimbingState.Right;
            }

            else if (stickInput.x < -0.5f)
            {
                if (left && distance < 0.05f + desiredCornerMinDistance)
                    return ClimbingState.CornerLeft;

                return ClimbingState.Left;
            }
            

            return ClimbingState.Idle;
        }

        // --------------------------------

        // --- Events ---

        private void OnAnimatorIK(int layerIndex)
        {
            if (!abilityController.isCurrent(this) || climbingState == ClimbingState.None)
                return;

            // --- Set weights to 0 and return if IK is off ---
            if (!fIKOn)
            {
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0.0f);
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0.0f);
            }
            else
            {
                // --- Else, sample foot weight from the animator's parameters, which are set by the animations themselves through curves ---

                // --- Left foot ---
                float weight = animationController.animator.GetFloat("IKLeftFootWeight");

                if (weight > 0.0f)
                {
                    animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, weight);
                    Vector3 footPosition = ledgeGeometry.GetPosition(ledgeGeometry.GetHook(animationController.animator.GetIKPosition(AvatarIKGoal.LeftFoot)));
                    footPosition -= transform.forward * footLength;
                    footPosition.y = animationController.animator.GetIKPosition(AvatarIKGoal.LeftFoot).y;
                    animationController.animator.SetIKPosition(AvatarIKGoal.LeftFoot, footPosition);
                }

                weight = animationController.animator.GetFloat("IKRightFootWeight");

                // --- Right foot ---
                if (weight > 0.0f)
                {
                    animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, animationController.animator.GetFloat("IKRightFootWeight"));
                    Vector3 footPosition = ledgeGeometry.GetPosition(ledgeGeometry.GetHook(animationController.animator.GetIKPosition(AvatarIKGoal.RightFoot)));
                    footPosition -= transform.forward * footLength;
                    footPosition.y = animationController.animator.GetIKPosition(AvatarIKGoal.RightFoot).y;
                    animationController.animator.SetIKPosition(AvatarIKGoal.RightFoot, footPosition);
                }
            }

            // --- Set weights to 0 and return if IK is off ---
            if (!hIKOn)
            {
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0.0f);
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0.0f);
            }
            else
            {
                // --- Else, sample hand weight from the animator's parameters, which are set by the animations themselves through curves ---
                float weight = animationController.animator.GetFloat("IKLeftHandWeight");

                // --- Left hand ---
                if (weight > 0.0f)
                {
                    animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, animationController.animator.GetFloat("IKLeftHandWeight"));
                    Vector3 handPosition = ledgeGeometry.GetPosition(ledgeGeometry.GetHook(animationController.animator.GetIKPosition(AvatarIKGoal.LeftHand)));
                    handPosition -= transform.forward * handLength;
                    handPosition.y = ledgeGeometry.vertices[0].y + handIKYDistance;
                    animationController.animator.SetIKPosition(AvatarIKGoal.LeftHand, handPosition);
                }

                weight = animationController.animator.GetFloat("IKRightHandWeight");

                // --- Right hand ---
                if (weight > 0.0f)
                {
                    animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightHand, animationController.animator.GetFloat("IKRightHandWeight"));
                    Vector3 handPosition = ledgeGeometry.GetPosition(ledgeGeometry.GetHook(animationController.animator.GetIKPosition(AvatarIKGoal.RightHand)));
                    handPosition -= transform.forward * handLength;
                    handPosition.y = ledgeGeometry.vertices[0].y + handIKYDistance;
                    animationController.animator.SetIKPosition(AvatarIKGoal.RightHand, handPosition);
                }
            }
        }

        // -------------------------------------------------
    }
}

