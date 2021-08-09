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

        [Tooltip("The maximum distance the character will be able to cover when climbing to a ledge.")]
        [Range(0.0f, 5.0f)]
        public float maxClimbableHeight;

        [Tooltip("The maximum distance the character will be able to cover when jumping from ledge to ledge.")]
        [Range(0.0f, 5.0f)]
        public float maxJumpRadius = 1.0f;

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

        [Header("Debug")]
        [Tooltip("If active, debug utilities will be shown (information/geometry draw). Selecting the object may be required to show debug geometry.")]
        public bool debugDraw = false;

        [Tooltip("Indicates the point the character is aiming to, used to trigger a ledge to ledge transition.")]
        [Range(0.0f, 1.0f)]
        public float aimDebugSphereRadius = 0.25f;

        // --------------------------------

        // --- Ability-level state ---
        public enum ClimbingAbilityState
        {
            Suspended,
            Mounting,
            Dismount,
            PullUp,
            DropDown,
            LedgeToLedge,
            JumpBack,
            Climbing
        }

        // --------------------------------

        // --- Climbing-level state (directions) ---
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

        private ClimbingAbilityState state; // Actual Climbing movement/state
        private ClimbingState climbingState; // Actual Climbing direction

        // --- The position at which we are currently aiming to, determined by maxJumpRadius ---
        private Vector3 targetAimPosition;

        // --------------------------------

        // --- World interactable elements ---

        private TraverserLedgeObject.TraverserLedgeGeometry ledgeGeometry;
        private TraverserLedgeObject.TraverserLedgeGeometry auxledgeGeometry;
        private TraverserLedgeObject.TraverserLedgeHook ledgeHook;

        // --------------------------------

        // --- Basic Methods ---
        public void Start()
        {
            abilityController = GetComponent<TraverserAbilityController>();
            controller = GetComponent<TraverserCharacterController>();
            animationController = GetComponent<TraverserAnimationController>();
            locomotionAbility = GetComponent<TraverserLocomotionAbility>();

            state = ClimbingAbilityState.Suspended;
            climbingState = ClimbingState.None;

            ledgeGeometry = TraverserLedgeObject.TraverserLedgeGeometry.Create();
            auxledgeGeometry = TraverserLedgeObject.TraverserLedgeGeometry.Create();
            ledgeHook = TraverserLedgeObject.TraverserLedgeHook.Create();
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
            if (!IsState(ClimbingAbilityState.Suspended))
            {
                // --- Handle current state ---
                switch (state)
                {
                    case ClimbingAbilityState.Mounting:
                        HandleMountingState();
                        break;        

                    case ClimbingAbilityState.Dismount:
                        HandleDismountState();
                        break;

                    case ClimbingAbilityState.PullUp:
                        HandlePullUpState();
                        break;

                    case ClimbingAbilityState.DropDown:
                        HandleDropDownState();
                        break;

                    case ClimbingAbilityState.LedgeToLedge:
                        HandleLedgeToLedgeState();
                        break;

                    case ClimbingAbilityState.JumpBack:
                        HandleJumpBackState();
                        break;

                    case ClimbingAbilityState.Climbing:
                        HandleClimbingState(deltaTime);
                        break;

                    default:
                        break;
                }

                // --- Let other abilities handle the situation ---
                if (IsState(ClimbingAbilityState.Suspended))
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

            //---If we make contact with a climbable surface and player issues climb order, mount ---         
            BoxCollider collider = controller.current.collider as BoxCollider;

            if (TraverserInputLayer.capture.mountButton
                && IsState(ClimbingAbilityState.Suspended)
                && collider != null
                && controller.isGrounded             // --- Ensure we are not falling ---
                && collider.gameObject.GetComponent<TraverserClimbingObject>() != null)
            {

                // --- Fill auxiliary ledge to prevent mounting too close to a corner ---
                auxledgeGeometry.Initialize(collider);
                TraverserLedgeObject.TraverserLedgeHook auxHook = auxledgeGeometry.GetHook(contactTransform.t);
                float distance = 0.0f;
                auxledgeGeometry.ClosestPointDistance(contactTransform.t, auxHook, ref distance);

                // --- Make sure we are not too close to the corner (We may trigger an unwanted transition afterwards) ---
                if (!collider.Equals(controller.current.ground as BoxCollider) && distance > 0.25)
                {
                    // --- We want to reach the pre climb position and then activate the animation ---
                    ledgeGeometry.Initialize(collider);
                    ledgeHook = ledgeGeometry.GetHook(transform.position);
                    Vector3 hookPosition = ledgeGeometry.GetPosition(ledgeHook);
                    Quaternion hookRotation = Quaternion.LookRotation(ledgeGeometry.GetNormal(ledgeHook), transform.up);

                    Vector3 targetPosition = hookPosition - ledgeGeometry.GetNormal(ledgeHook) * controller.capsuleRadius;
                    targetPosition.y = hookPosition.y -
                        (controller.capsuleHeight - (animationController.skeleton.transform.position.y - transform.position.y));
                    TraverserTransform hangedTransform = TraverserTransform.Get(targetPosition, hookRotation);

                    float transitionOffset = 1.0f;
                    contactTransform.t -= transform.forward * transitionOffset;

                    // --- Require a transition ---
                    ret = animationController.transition.StartTransition("WalkTransition", "Mount",
                        "WalkTransitionTrigger", "MountTrigger", ref contactTransform, ref hangedTransform);

                    // --- If transition start is successful, change state ---
                    if (ret)
                    {
                        SetState(ClimbingAbilityState.Mounting);

                        // --- Turn off/on controller ---
                        controller.ConfigureController(false);
                    }
                }


            }
            else if (locomotionAbility.GetLocomotionState() == TraverserLocomotionAbility.LocomotionAbilityState.Falling
                && collider.gameObject.GetComponent<TraverserClimbingObject>() != null)
            {
                // ---  We are falling and colliding against a ledge
                ledgeGeometry.Initialize(collider);
                ledgeHook = ledgeGeometry.GetHook(transform.position);
                Vector3 hookPosition = ledgeGeometry.GetPosition(ledgeHook);
                Quaternion hookRotation = Quaternion.LookRotation(ledgeGeometry.GetNormal(ledgeHook), transform.up);
                
                Vector3 targetPosition = hookPosition - ledgeGeometry.GetNormal(ledgeHook) * controller.capsuleRadius;
                targetPosition.y = hookPosition.y -
                    (controller.capsuleHeight - (animationController.skeleton.transform.position.y - transform.position.y));
                TraverserTransform hangedTransform = TraverserTransform.Get(targetPosition, hookRotation);
                
                float transitionOffset = 1.0f;
                contactTransform.t -= transform.forward * transitionOffset;

                animationController.animator.Play("FallTransition", 0, 0.0f);

                // --- Require a transition ---
                ret = animationController.transition.StartTransition("FallTransition", "JumpHang",
                            "ClimbTransitionTrigger", "JumpHangTrigger", ref contactTransform, ref hangedTransform, true, true);


                // --- If transition start is successful, change state ---
                if (ret)
                {
                    SetState(ClimbingAbilityState.LedgeToLedge);
                
                    // --- Turn off/on controller ---
                    controller.ConfigureController(false);
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

                // --- If collider exists and it is a climbable object ---
                if (collider && collider.GetComponent<TraverserClimbingObject>())
                {
                    ledgeGeometry.Initialize(collider);
                    ledgeHook = ledgeGeometry.GetHook(controller.previous.position);
                    Vector3 hookPosition = ledgeGeometry.GetPosition(ledgeHook);
                    hookPosition.y += (animationController.skeleton.transform.position.y - transform.position.y);
                    Quaternion hookRotation = Quaternion.LookRotation(-ledgeGeometry.GetNormal(ledgeHook), transform.up);

                    float transitionOffset = 0.5f;
                    TraverserTransform contactTransform = TraverserTransform.Get(hookPosition, hookRotation);
                    contactTransform.t -= transform.forward * transitionOffset;

                    float targetOffset = 0.25f;
                    TraverserTransform hangedTransform = GetHangedTransform();
         
                    // --- If capsule collides against any relevant collider, do not start the dropDown transition ---
                    if (!Physics.CheckCapsule(hangedTransform.t, hangedTransform.t + Vector3.up * controller.capsuleHeight, controller.capsuleRadius, TraverserCollisionLayer.EnvironmentCollisionMask))
                    {
                        hangedTransform.t += ledgeGeometry.GetNormal(ledgeHook) * targetOffset;
                        hangedTransform.t.y += (animationController.skeleton.transform.position.y - transform.position.y);
                        hangedTransform.q = hookRotation;

                        // --- Require a transition ---
                        ret = animationController.transition.StartTransition("WalkTransition", "DropDown",
                            "WalkTransitionTrigger", "DropDownTrigger", ref contactTransform, ref hangedTransform, false);

                        if (ret)
                        {
                            SetState(ClimbingAbilityState.DropDown);
                            // --- Turn off/on controller ---
                            controller.ConfigureController(false);
                        }
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
                SetState(ClimbingAbilityState.Climbing); 
                SetClimbingState(ClimbingState.Idle);
            }
        }

        void HandleDismountState()
        {
            if (animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f)
            {
                animationController.fakeTransition = false;
                SetState(ClimbingAbilityState.Suspended);

                // --- Get skeleton's current position and teleport controller ---
                Vector3 newTransform = animationController.skeleton.transform.position;
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
                SetState(ClimbingAbilityState.Suspended);

                // --- Get skeleton's current position and teleport controller ---
                Vector3 newTransform = animationController.skeleton.transform.position;
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
                SetState(ClimbingAbilityState.Climbing);
                SetClimbingState(ClimbingState.Idle);
                animationController.animator.Play("LedgeIdle", 0, 0.0f);

                TraverserTransform hangedTransform = GetHangedTransform();

                controller.ConfigureController(false);
                controller.TeleportTo(hangedTransform.t);
                transform.rotation = hangedTransform.q;
            }
        }

        void HandleLedgeToLedgeState()
        {
            if (!animationController.transition.isON)
            {
                SetState(ClimbingAbilityState.Climbing);
                SetClimbingState(ClimbingState.Idle);
                //animationController.animator.Play("LedgeIdle", 0, 0.0f);

                TraverserTransform hangedTransform = GetHangedTransform();

                controller.ConfigureController(false);
                controller.TeleportTo(hangedTransform.t);
                transform.rotation = hangedTransform.q;
            }
        }

        void HandleJumpBackState()
        {
            if (animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f)
            {
                animationController.fakeTransition = false;
                SetState(ClimbingAbilityState.Suspended);
                //SetClimbingState(ClimbingState.Idle);
                animationController.animator.Play("FallLoop", 0, 0.0f);
                Vector3 newTransform = animationController.skeleton.transform.position;
                newTransform.y -= controller.capsuleHeight / 2.0f;
                controller.TeleportTo(newTransform);
                locomotionAbility.ResetLocomotion();
                locomotionAbility.SetLocomotionState(TraverserLocomotionAbility.LocomotionAbilityState.Falling);
                transform.rotation = Quaternion.LookRotation(-ledgeGeometry.GetNormal(ledgeHook),Vector3.up)/*animationController.skeleton.transform.rotation*/;
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
                    animationController.fakeTransition = false;
                    SetClimbingState(ClimbingState.None);
                    animationController.animator.Play("LedgeIdle", 0, 0.0f);

                    TraverserTransform hangedTransform = GetHangedTransform();

                    controller.TeleportTo(hangedTransform.t);
                    transform.rotation = hangedTransform.q;
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
                    // TODO: USE ANIMATION CONTROLLER'S NEW FUNCTIONALITY (end position)

                    // --- Compute the position of a capsule equal to the character's at the end of the corner transition ---
                    Vector3 position = transform.position;
                    position += transform.right * 0.75f;
                    position += transform.forward * 0.5f;

                    // --- If it collides against any relevant collider, do not start the corner transition ---
                    if (IsCapsuleColliding(ref position))
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
                    position -= transform.right * 0.75f;
                    position += transform.forward * 0.5f;

                    // --- If it collides against any relevant collider, do not start the corner transition ---
                    if (IsCapsuleColliding(ref position))
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

            bool closeToDrop = Physics.Raycast(transform.position, Vector3.down, maxClimbableHeight - controller.capsuleHeight, TraverserCollisionLayer.EnvironmentCollisionMask, QueryTriggerInteraction.Ignore);

            if(debugDraw)
                Debug.DrawRay(transform.position, Vector3.down * (maxClimbableHeight - controller.capsuleHeight), Color.yellow);

            // --- The position at which we perform a capsule check to prevent pulling up into a wall --- 
            Vector3 pullupPosition = transform.position;
            pullupPosition += transform.up * controller.capsuleHeight * 1.25f;
            pullupPosition += transform.forward * 0.5f;

            // --- React to pull up/dismount ---
            if (TraverserInputLayer.capture.leftStickVertical > 0.5f &&
                state != ClimbingAbilityState.LedgeToLedge && !IsCapsuleColliding(ref pullupPosition))
            {
                animationController.animator.Play("PullUp", 0, 0.0f);
                animationController.fakeTransition = true;
                SetState(ClimbingAbilityState.PullUp);
            }
            else if (closeToDrop && TraverserInputLayer.capture.dismountButton)
            {
                animationController.animator.Play("Dismount", 0, 0.0f);
                animationController.fakeTransition = true;
                SetState(ClimbingAbilityState.Dismount);
            }
        }

        bool UpdateClimbing(float deltaTime)
        {
            bool ret = false;

            // --- Given input, compute target position ---
            Vector2 leftStickInput;
            leftStickInput.x = TraverserInputLayer.capture.leftStickHorizontal;
            leftStickInput.y = TraverserInputLayer.capture.leftStickVertical;
            Vector3 targetPosition = transform.position + transform.right * leftStickInput.x * desiredSpeedLedge * deltaTime;

            // --- Given input, compute target aim Position (used to trigger ledge to ledge transitions) ---
            Vector3 aimDirection = transform.right * TraverserInputLayer.capture.leftStickHorizontal
                + transform.up * TraverserInputLayer.capture.leftStickVertical;
            aimDirection.Normalize();

            Vector3 rayOrigin = transform.position
                + transform.up * controller.capsuleHeight * 0.75f
                + transform.forward * controller.capsuleRadius;

            targetAimPosition = rayOrigin + aimDirection * maxJumpRadius * TraverserInputLayer.GetMoveIntensity(); 

            //GameObject.Find("dummy1").transform.position = rayOrigin;

            // --- Update hook ---
            TraverserLedgeObject.TraverserLedgeHook desiredLedgeHook = ledgeGeometry.UpdateHook(ledgeHook, targetPosition, desiredCornerMinDistance);

            // --- Update current hook if it is still on the ledge ---
            if (desiredLedgeHook.index == ledgeHook.index)
            {
                ret = true;
                ledgeHook = desiredLedgeHook;
                controller.targetDisplacement = targetPosition - transform.position;
            }
            else
                controller.targetDisplacement = Vector3.zero;

            RaycastHit hit;

            bool collided = Physics.SphereCast(rayOrigin, aimDebugSphereRadius, aimDirection, out hit, maxJumpRadius * TraverserInputLayer.GetMoveIntensity(), TraverserCollisionLayer.EnvironmentCollisionMask, QueryTriggerInteraction.Ignore);

            //collided = Physics.Raycast(targetAimPosition, transform.forward, out hit, maxJumpRadius, TraverserCollisionLayer.EnvironmentCollisionMask, QueryTriggerInteraction.Ignore);

            // --- Trigger a ledge to ledge transition if required by player ---
            if (TraverserInputLayer.GetMoveIntensity() > 0.1f && collided)
            {
                // --- Check if collided object is climbable ---
                if (hit.transform.GetComponent<TraverserClimbingObject>())
                {
                    // --- Trigger a transition and change state ---
                    ledgeGeometry.Initialize(hit.collider as BoxCollider);

                    // --- Adjust the aim position so we don't end in another ledge edge ---
                    ledgeHook = ledgeGeometry.GetHook(targetAimPosition - transform.forward * controller.capsuleRadius + aimDirection);

                    //ledgeHook = ledgeGeometry.UpdateHook(ledgeHook, targetAimPosition, desiredCornerMinDistance);
                    Vector3 matchPosition = ledgeGeometry.GetPositionAtDistance(ledgeHook.index, 0.5f);
                    matchPosition -= Vector3.up * (controller.capsuleHeight - (animationController.skeleton.transform.position.y - transform.position.y));
                    matchPosition -= transform.forward * controller.capsuleRadius;

                    TraverserTransform contactTransform = TraverserTransform.Get(animationController.skeleton.transform.position, transform.rotation);
                    TraverserTransform targetTransform = TraverserTransform.Get(matchPosition, transform.rotation);


                    float angle = Vector3.SignedAngle(transform.up, aimDirection, -transform.forward);

                    bool success = false;

                    if (Mathf.Abs(angle) < 45.0f)
                    {
                        success = animationController.transition.StartTransition("ClimbTransition", "HopUp",
                            "ClimbTransitionTrigger", "HopUpTrigger", ref contactTransform, ref targetTransform);
                    }
                    else if (angle >= 45.0f && angle <= 90.0f)
                    {
                        success = animationController.transition.StartTransition("ClimbTransition", "HopRight",
                            "ClimbTransitionTrigger", "HopRightTrigger", ref contactTransform, ref targetTransform);
                    }
                    else if (angle <= -45.0f && angle >= -90.0f)
                    {
                        success = animationController.transition.StartTransition("ClimbTransition", "HopLeft",
                            "ClimbTransitionTrigger", "HopLeftTrigger", ref contactTransform, ref targetTransform);
                    }
                    else
                    {
                        success = animationController.transition.StartTransition("ClimbTransition", "HopDown",
                            "ClimbTransitionTrigger", "HopDownTrigger", ref contactTransform, ref targetTransform);
                    }


                    //Debug.Log(angle);


                    if (success)
                    {
                        SetState(ClimbingAbilityState.LedgeToLedge);
                        // --- Turn off/on controller ---
                        controller.ConfigureController(false);
                    }
                }
            }

            // --- Trigger a jump back transition if required by player ---
            if(TraverserInputLayer.capture.pullUpButton)
            {
                SetState(ClimbingAbilityState.JumpBack);
                // --- Turn off/on controller ---
                controller.ConfigureController(false);

                animationController.animator.Play("JumpBack", 0, 0.0f);
                animationController.fakeTransition = true;
            }


            if (debugDraw)
            {
                ledgeGeometry.DebugDraw();
                ledgeGeometry.DebugDraw(ref ledgeHook);
            }

            return ret;
        }        

        // --------------------------------

        // --- Utilities ---    

        private bool IsCapsuleColliding(ref Vector3 start)
        {
            Vector3 end = start;
            end.y += controller.capsuleHeight;

            // DEBUG
            //GameObject.Find("dummy1").transform.position = start;
            //GameObject.Find("dummy2").transform.position = end;

            // --- Cast a capsule and return whether there has been a collision or not ---
            return Physics.CheckCapsule(start, end, controller.capsuleRadius, TraverserCollisionLayer.EnvironmentCollisionMask);        
        }

        private TraverserTransform GetHangedTransform()
        {
            // --- Compute the character's position and rotation when hanging on a ledge ---
            ledgeHook = ledgeGeometry.GetHook(animationController.skeleton.position);
            Vector3 hookPosition = ledgeGeometry.GetPosition(ledgeHook);
            Quaternion hangedRotation = Quaternion.LookRotation(ledgeGeometry.GetNormal(ledgeHook), transform.up);
            Vector3 hangedPosition = hookPosition - ledgeGeometry.GetNormal(ledgeHook) * controller.capsuleRadius;
            hangedPosition.y = hookPosition.y - controller.capsuleHeight;
            return TraverserTransform.Get(hangedPosition, hangedRotation);
        }

        public void SetState(ClimbingAbilityState newState)
        {
            state = newState;
        }

        public bool IsState(ClimbingAbilityState queryState)
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
            Vector2 stickInput;
            stickInput.x = TraverserInputLayer.capture.leftStickHorizontal;
            stickInput.y = TraverserInputLayer.capture.leftStickVertical;

            // --- Use ledge definition to determine how close we are to the edges of the wall, also at which side the vertex is (bool left) ---
            float distance = 0.0f;
            bool left = ledgeGeometry.ClosestPointDistance(transform.position, ledgeHook, ref distance);

            if (stickInput.x > 0.5f)
            {
                if (!left && distance <= desiredCornerMinDistance)
                    return ClimbingState.CornerRight;

                return ClimbingState.Right;
            }

            else if (stickInput.x < -0.5f)
            {
                if (left && distance <= desiredCornerMinDistance)
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

            // --- Ensure IK is not activated during a transition between two differently placed animations (mount-idle bug) ---
            if (animationController.animator.IsInTransition(0))
                return;

            // --- Set weights to 0 and return if IK is off ---
            if (!fIKOn)
            {
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0.0f);
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0.0f);
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0.0f);
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0.0f);
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
                    animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, weight);
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
                    animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
                    Vector3 handPosition = ledgeGeometry.GetPosition(ledgeGeometry.GetHook(animationController.animator.GetIKPosition(AvatarIKGoal.LeftHand)));
                    handPosition -= transform.forward * handLength;
                    handPosition.y = ledgeGeometry.vertices[0].y + handIKYDistance;
                    animationController.animator.SetIKPosition(AvatarIKGoal.LeftHand, handPosition);
                }

                weight = animationController.animator.GetFloat("IKRightHandWeight");

                // --- Right hand ---
                if (weight > 0.0f)
                {
                    animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightHand, weight);
                    Vector3 handPosition = ledgeGeometry.GetPosition(ledgeGeometry.GetHook(animationController.animator.GetIKPosition(AvatarIKGoal.RightHand)));
                    handPosition -= transform.forward * handLength;
                    handPosition.y = ledgeGeometry.vertices[0].y + handIKYDistance;
                    animationController.animator.SetIKPosition(AvatarIKGoal.RightHand, handPosition);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugDraw || abilityController == null)
                return;

            Gizmos.color = Color.cyan;

    //        Vector3 aimDirection = transform.right * TraverserInputLayer.capture.leftStickHorizontal
    //+ transform.up * TraverserInputLayer.capture.leftStickVertical;
    //        aimDirection.Normalize();

    //        Gizmos.DrawRay(rayOrigin, aimDirection * TraverserInputLayer.GetMoveIntensity());
    //        Gizmos.DrawRay(targetAimPosition, transform.forward*maxJumpRadius);

            Gizmos.DrawWireSphere(targetAimPosition, aimDebugSphereRadius);

        }

        // -------------------------------------------------
    }
}

