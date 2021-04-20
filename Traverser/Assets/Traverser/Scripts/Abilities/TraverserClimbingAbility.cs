using Unity.Mathematics;
using UnityEngine;

namespace Traverser
{
    [RequireComponent(typeof(TraverserAbilityController))]
    [RequireComponent(typeof(TraverserCharacterController))]

    public class TraverserClimbingAbility : MonoBehaviour, TraverserAbility
    {
        // --- Attributes ---
        //[Header("Transition settings")]
        //[Tooltip("Distance in meters for performing movement validity checks")]
        //[Range(0.0f, 1.0f)]
        //public float contactThreshold;

        //[Tooltip("Maximum linear error for transition poses")]
        //[Range(0.0f, 1.0f)]
        //public float maximumLinearError;

        //[Tooltip("Maximum angular error for transition poses.")]
        //[Range(0.0f, 180.0f)]
        //public float maximumAngularError;

        [Header("Ledge prediction settings")]
        [Tooltip("Desired speed in meters per second for ledge climbing.")]
        [Range(0.0f, 10.0f)]
        public float desiredSpeedLedge;

        //[Tooltip("How fast or slow the target velocity is supposed to be reached.")]
        //[Range(0.0f, 1.0f)]
        //public float velocityPercentageLedge;

        // --------------------------------

        // --- Climbing movement/state ---
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

        // --- Climbing directions ---
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

        private TraverserCharacterController controller;
        private TraverserAnimationController animationController;
        private TraverserLocomotionAbility locomotionAbility;

        private State state; // Actual Climbing movement/state
        private State previousState; // Previous Climbing movement/state

        private ClimbingState climbingState; // Actual Climbing direction
        private ClimbingState previousClimbingState; // Previous Climbing direction
        private ClimbingState lastCollidingClimbingState;

        // --------------------------------

        // --- World interactable elements ---

        TraverserLedgeObject.TraverserLedgeGeometry ledgeGeometry;
        TraverserLedgeObject.TraverserLedgeGeometry auxledgeGeometry;
        TraverserWallObject.TraverserWallGeometry wallGeometry;
        TraverserLedgeObject.TraverserLedgeHook ledgeHook;
        TraverserWallObject.TraverserWallAnchor wallAnchor;

        // --------------------------------

        // --- Basic Methods ---
        public void Start()
        {
            controller = GetComponent<TraverserCharacterController>();
            animationController = GetComponent<TraverserAnimationController>();
            locomotionAbility = GetComponent<TraverserLocomotionAbility>();

            state = State.Suspended;
            previousState = State.Suspended;
            climbingState = ClimbingState.Idle;
            previousClimbingState = ClimbingState.Idle;
            lastCollidingClimbingState = ClimbingState.None;

            ledgeGeometry = TraverserLedgeObject.TraverserLedgeGeometry.Create();
            auxledgeGeometry = TraverserLedgeObject.TraverserLedgeGeometry.Create();
            wallGeometry = TraverserWallObject.TraverserWallGeometry.Create();

            ledgeHook = TraverserLedgeObject.TraverserLedgeHook.Create();
            wallAnchor = TraverserWallObject.TraverserWallAnchor.Create();
        }

        // --------------------------------

        // --- Ability class methods ---
        public TraverserAbility OnUpdate(float deltaTime)
        {
            TraverserAbility ret = this;

            TraverserInputLayer.capture.UpdateClimbing();

            // --- Turn off/on controller ---
            controller.ConfigureController(!IsState(State.Suspended));

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

                    case State.Climbing:
                        HandleClimbingState(deltaTime);
                        break;

                    case State.DropDown:
                        HandleDropDownState();
                        break;

                    default:
                        break;
                }

                // --- Let other abilities handle the situation ---
                if (IsState(State.Suspended))
                    return null;;

                ret = this;
            }

            if(animationController.transition.isON)
                animationController.transition.UpdateTransition();

            return ret;
        }

        public TraverserAbility OnFixedUpdate(float deltaTime)
        {
            //if (animationController.transition.isON)
                return this;


            //return null;
        }

        public TraverserAbility OnPostUpdate(float deltaTime)
        {
            return null;
        }

        // --- Climbing states wrappers ---
        void HandleMountingState()
        {
            // --- If transition is over and we were mounting change state to ledge idle ---
            if(!animationController.transition.isON)
            {
                SetState(State.Climbing); 
                SetClimbingState(ClimbingState.Idle);
                //ledgeHook = ledgeGeometry.GetHook(transform.position); // unneeded?
            }
        }

        void HandleClimbingState(float deltaTime)
        {
            bool bTransitionSucceeded;
            //KinematicaLayer.GetCurrentAnimationInfo(ref synthesizer, out bTransitionSucceeded);

            // --- Handle special corner transitions ---
            //if (climbingState == ClimbingState.CornerRight
            //    || climbingState == ClimbingState.CornerLeft
            //    )
            //{
            //    if (bTransitionSucceeded)
            //    {
            //        ledgeAnchor = ledgeGeometry.GetAnchor(synthesizer.WorldRootTransform.t);
            //        SetClimbingState(ClimbingState.None);
            //    }

            //    return;
            //}

            UpdateClimbing(deltaTime);

            ClimbingState desiredState = GetDesiredClimbingState();

            if (desiredState == lastCollidingClimbingState)
                desiredState = ClimbingState.Idle;

            // --- Handle ledge climbing/movement direction ---
            if (!IsClimbingState(desiredState) /*|| bTransitionSucceeded*/)
            {
                //Climbing climbingTrait = Climbing.Create(Climbing.Type.Ledge);

                if (desiredState == ClimbingState.Idle)
                {
                    animationController.animator.Play("LedgeIdle", 0, 0.0f);
                    //PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(Idle.Default));
                }
                else if (desiredState == ClimbingState.Right)
                {
                    animationController.animator.Play("LedgeRight", 0, 0.0f);
                    //Direction direction = Direction.Create(Direction.Type.Right);
                    //PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(direction).Except(Idle.Default));
                }
                else if (desiredState == ClimbingState.Left)
                {
                    animationController.animator.Play("LedgeLeft", 0, 0.0f);
                    //Direction direction = Direction.Create(Direction.Type.Left);
                    //PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(direction).Except(Idle.Default));
                }
                //else if (desiredState == ClimbingState.CornerRight)
                //{
                //    Direction direction = Direction.Create(Direction.Type.CornerRight);

                //    // --- We fake a play and check if at the segment's end there is a collision ---
                //    PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(direction).Except(Idle.Default));

                //    // --- If a collision is found play idle ---
                //    if (!KinematicaLayer.IsCurrentAnimationEndValid(ref synthesizer))
                //    {
                //        SetClimbingState(ClimbingState.Idle);
                //        PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(Idle.Default));
                //        Debug.Log("No sequences for right corner transition in climbing, collision found");
                //        return;
                //    }
                //}
                //else if (desiredState == ClimbingState.CornerLeft)
                //{
                //    Direction direction = Direction.Create(Direction.Type.CornerLeft);

                //    // --- We fake a play and check if at the segment's end there is a collision ---
                //    PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(direction).Except(Idle.Default));

                //    // --- If a collision is found play idle ---
                //    if (!KinematicaLayer.IsCurrentAnimationEndValid(ref synthesizer))
                //    {
                //        SetClimbingState(ClimbingState.Idle);
                //        PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(Idle.Default));
                //        Debug.Log("No sequences for left corner transition in climbing, collision found");
                //        return;
                //    }
                //}

                SetClimbingState(desiredState);
            }

            TraverserAffineTransform rootTransform = TraverserAffineTransform.Get(transform.position, transform.rotation);
            wallGeometry.Initialize(rootTransform);
            wallAnchor = wallGeometry.GetAnchor(rootTransform.t);
            float height = wallGeometry.GetHeight(ref wallAnchor);
            bool closeToDrop = math.abs(height - 2.8f) <= 0.095f;

            RaycastHit ray_hit;

            // --- Check if the ray hits a collider, allow dismount if true ---
            //if (Physics.Raycast(synthesizer.WorldRootTransform.t - synthesizer.WorldRootTransform.Forward, Vector3.down, out ray_hit, 2.8f, CollisionLayer.EnvironmentCollisionMask))
            //    closeToDrop = true;

            // --- React to pull up/dismount ---
            if (TraverserInputLayer.capture.pullUpButton /*&& !CollisionLayer.IsCharacterCapsuleColliding(transform.position, ref capsule)*/)
            {
                //AffineTransform contactTransform = ledgeGeometry.GetTransform(ledgeAnchor);
                //RequestTransition(ref synthesizer, contactTransform, Ledge.Type.PullUp);
                SetState(State.PullUp);
            }
            else if (closeToDrop && TraverserInputLayer.capture.dismountButton)
            {
                //Ledge trait = Ledge.Create(Ledge.Type.Dismount); // temporal
                //PlayFirstSequence(synthesizer.Query.Where("Ledge", trait).Except(Idle.Default)); // temporal
                SetState(State.Dismount);
            }
            //else if (!closeToDrop && -stickInput.y < -0.5)
            //{
            //    SetState(State.FreeClimbing);
            //}
        }

        void HandleDismountState()
        {
            //bool bTransitionSucceeded;
            //KinematicaLayer.GetCurrentAnimationInfo(ref synthesizer, out bTransitionSucceeded);

            //if (bTransitionSucceeded)
            //{
            //    SetState(State.Suspended);
            //    PlayFirstSequence(synthesizer.Query.Where("Idle", Locomotion.Default).And(Idle.Default));
            //}
        }

        void HandlePullUpState()
        {
            //bool bTransitionSucceeded;

            //if (KinematicaLayer.IsAnchoredTransitionComplete(ref anchoredTransition, out bTransitionSucceeded))
            //    SetState(State.Suspended);
        }

        void HandleDropDownState()
        {
            //if (!anchoredTransition.isValid && previousState != State.DropDown)
            //{
            //    BoxCollider collider = controller.current.ground.GetComponent<BoxCollider>();

            //    if (collider != null)
            //    {
            //        if (collider.gameObject.layer == LayerMask.NameToLayer("Wall"))
            //        {
            //            ledgeGeometry.Initialize(collider);
            //            ledgeAnchor = ledgeGeometry.GetAnchor(synthesizer.WorldRootTransform.t);
            //            wallGeometry.Initialize(collider, ledgeGeometry.GetTransform(ledgeAnchor));

            //            // --- Make sure we build the contact transform considering the wall's normal ---
            //            AffineTransform contactTransform = ledgeGeometry.GetTransformGivenNormal(ledgeAnchor, wallGeometry.GetNormalWorldSpace());
            //            RequestTransition(ref synthesizer, contactTransform, Ledge.Type.DropDown);
            //        }
            //    }
            //}

            //bool TransitionSuccess;

            //if (KinematicaLayer.IsAnchoredTransitionComplete(ref anchoredTransition, out TransitionSuccess))
            //{
            //    if(!TransitionSuccess)
            //    {
            //        SetState(State.Suspended);
            //        return;
            //    }

            //    // --- From the top of a wall, drop down onto the ledge ---
            //    ledgeAnchor = ledgeGeometry.GetAnchor(synthesizer.WorldRootTransform.t);

            //    // --- Depending on how far the anchor is, decide if we are hanging onto a ledge or climbing a wall ---
            //    SetState(State.Climbing);
            //    SetClimbingState(ClimbingState.Idle);
            //    Climbing climbingTrait = Climbing.Create(Climbing.Type.Ledge);
            //    PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(Idle.Default));
            //}
        }

        bool UpdateCollidingClimbingState(float desiredMoveOnLedge, float3 desiredPosition, float3 desiredForward)
        {
            bool bCollision = TraverserCollisionLayer.IsCharacterCapsuleColliding(desiredPosition - math.normalize(desiredForward) * 0.5f - new float3(0.0f, 1.5f, 0.0f), controller.capsuleCenter ,controller.capsuleHeight, controller.capsuleRadius);

            if (climbingState == ClimbingState.Idle)
            {
                lastCollidingClimbingState = ClimbingState.None;
            }
            else if (bCollision)
            {
                float currentMoveDirection = climbingState == ClimbingState.Left ? 1.0f : -1.0f;

                if (currentMoveDirection * desiredMoveOnLedge > 0.0f)
                    lastCollidingClimbingState = climbingState;
            }

            return bCollision;
        }

        void UpdateClimbing(float deltaTime)
        {
            //
            // Smoothly adjust current root transform towards the anchor transform
            //
            //Tra deltaTransform = synthesizer.GetTrajectoryDeltaTransform(deltaTime);
            //AffineTransform rootTransform = synthesizer.WorldRootTransform * deltaTransform;
            //float linearDisplacement = -deltaTransform.t.x;

            //TraverserLedgeObject.TraverserLedgeAnchor desiredLedgeAnchor = ledgeGeometry.UpdateAnchor(ledgeAnchor, linearDisplacement);
            //float3 position = ledgeGeometry.GetPosition(desiredLedgeAnchor);
            //float3 desiredForward = ledgeGeometry.GetNormal(desiredLedgeAnchor);

            //// --- Update current anchor if it is still on the ledge ---
            //if (!UpdateCollidingClimbingState(linearDisplacement, position, desiredForward))
            //    ledgeAnchor = desiredLedgeAnchor;

            //float distance = math.length(rootTransform.t - position);

            //if (distance >= 0.01f)
            //{
            //    float3 normal = math.normalize(position - rootTransform.t);
            //    rootTransform.t += normal * 0.5f * deltaTime;
            //}

            //rootTransform.t = position;

            //float angle;
            //float3 currentForward = Missing.zaxis(rootTransform.q);
            //quaternion q = Missing.forRotation(currentForward, desiredForward);
            //float maximumAngle = math.radians(90.0f) * deltaTime;
            //float3 axis = Missing.axisAngle(q, out angle);
            //angle = math.min(angle, maximumAngle);
            //rootTransform.q = math.mul(quaternion.AxisAngle(axis, angle), rootTransform.q);

            //// --- Update root motion transform ---
            //synthesizer.WorldRootTransform = rootTransform;

            // --- Given input, compute target position ---
            float2 stickInput;
            stickInput.x = TraverserInputLayer.capture.stickHorizontal;
            stickInput.y = TraverserInputLayer.capture.stickVertical;
            Vector3 target = transform.position + transform.right * stickInput.x * desiredSpeedLedge * deltaTime;

            // --- Obtain target displacement
            float linearDisplacement = -(target.x - transform.position.x);

            // --- Update 
            TraverserLedgeObject.TraverserLedgeHook desiredLedgeHook = ledgeGeometry.UpdateHook(ledgeHook, target);
            float3 position = ledgeGeometry.GetPosition(desiredLedgeHook);
            float3 desiredForward = ledgeGeometry.GetNormal(desiredLedgeHook);

            // --- Update current hook if it is still on the ledge ---
            if (!UpdateCollidingClimbingState(linearDisplacement, position, desiredForward))
                ledgeHook = desiredLedgeHook;

            controller.targetPosition = target;

            ledgeGeometry.DebugDraw();
            ledgeGeometry.DebugDraw(ref ledgeHook);
        }    

        public bool OnContact(TraverserAffineTransform contactTransform, float deltaTime)
        {
            bool ret = false;
            TraverserInputLayer.capture.UpdateClimbing();

            if(TraverserInputLayer.capture.mountButton)
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
                            float distance = auxledgeGeometry.ClosestPointDistance(contactTransform.t, auxHook);

                            // --- Make sure we are not too close to the corner (We may trigger an unwanted transition afterwards) ---
                            if (!collider.Equals(controller.current.ground.GetComponent<BoxCollider>()) && distance > 0.25)
                            {
                                Debug.Log("Climbing to ledge");

                                ledgeGeometry.Initialize(collider);
                                wallGeometry.Initialize(collider, contactTransform);

                                // --- We want to reach the pre climb position and then activate the animation ---
                                TraverserAffineTransform target = TraverserAffineTransform.Get(contactTransform.t, contactTransform.q);
                                target.t.y = ledgeGeometry.vertices[0].y;
                                target.t.z -= 1.0f;

                                // --- Require a transition ---
                                ret = animationController.transition.StartTransition("WalkTransition", "Mount",
                                    "WalkTransitionTrigger", "MountTrigger", 1.0f, 1.0f,
                                    ref contactTransform,
                                    ref target);
                                
                                // --- If transition start is successful, change state ---
                                if(ret)
                                    SetState(State.Mounting);
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


            //if (InputLayer.capture.dropDownButton && !IsState(State.DropDown))
            //{
            //    SetState(State.DropDown);
            //    ret = true;
            //}

            return ret;
        }

        public bool IsAbilityEnabled()
        {
            return isActiveAndEnabled;
        }

        // --------------------------------

        // --- Utilities ---    

        public void SetState(State newState)
        {
            previousState = state;
            state = newState;
            lastCollidingClimbingState = ClimbingState.None;
        }

        public bool IsState(State queryState)
        {
            return state == queryState;
        }

        public void SetClimbingState(ClimbingState climbingState)
        {
            previousClimbingState = this.climbingState;
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
            bool left = false;
            float distance = 0.0f; //ledgeGeometry.GetDistanceToClosestVertex(kinematica.Synthesizer.Ref.WorldRootTransform.t, ledgeGeometry.GetNormal(ledgeAnchor), ref left);

            if (stickInput.x > 0.5f)
            {
                //if (!left && distance < 0.1f)
                    //return ClimbingState.CornerRight;

                return ClimbingState.Right;
            }

            else if (stickInput.x < -0.5f)
            {
                //if (left && distance < 0.1f)
                    //return ClimbingState.CornerLeft;

                return ClimbingState.Left;
            }
            

            return ClimbingState.Idle;
        }

        // --------------------------------
    }
}

