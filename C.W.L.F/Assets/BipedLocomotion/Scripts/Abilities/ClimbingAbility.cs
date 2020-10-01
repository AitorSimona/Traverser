using Unity.Kinematica;
using Unity.SnapshotDebugger;
using Unity.Mathematics;
using UnityEngine;

namespace CWLF
{
    [RequireComponent(typeof(AbilityController))]
    [RequireComponent(typeof(MovementController))]

    public partial class ClimbingAbility : SnapshotProvider, Ability
    {
        // --- Attributes ---
        [Header("Transition settings")]
        [Tooltip("Distance in meters for performing movement validity checks")]
        [Range(0.0f, 1.0f)]
        public float contactThreshold;

        [Tooltip("Maximum linear error for transition poses")]
        [Range(0.0f, 1.0f)]
        public float maximumLinearError;

        [Tooltip("Maximum angular error for transition poses.")]
        [Range(0.0f, 180.0f)]
        public float maximumAngularError;

        [Header("Ledge prediction settings")]
        [Tooltip("Desired speed in meters per second for ledge climbing.")]
        [Range(0.0f, 10.0f)]
        public float desiredSpeedLedge;

        [Tooltip("How fast or slow the target velocity is supposed to be reached.")]
        [Range(0.0f, 1.0f)]
        public float velocityPercentageLedge;

        // --------------------------------

        // --- Input wrapper ---
        public struct FrameCapture
        {
            public float stickHorizontal;
            public float stickVertical;
            public bool mountButton;
            public bool dismountButton;
            public bool pullUpButton;

            public void Update()
            {
                stickHorizontal = Input.GetAxis("Left Analog Horizontal");
                stickVertical = Input.GetAxis("Left Analog Vertical");

                mountButton = Input.GetButton("B Button") || Input.GetKey("b");
                dismountButton = Input.GetButton("B Button") || Input.GetKey("b");
                pullUpButton = Input.GetButton("A Button") || Input.GetKey("a");
            }
        }

        // --------------------------------

        // --- Climbing movement/state ---
        public enum State
        {
            Suspended,
            Mounting,
            Climbing,
            FreeClimbing,
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
            UpLeft,
            UpRight,
            DownLeft,
            DownRight,
            CornerRight,
            CornerLeft,
            None
        }

        // --------------------------------

        Kinematica kinematica; // system
        MovementController controller;
        CapsuleCollider capsule;

        State state; // Actual Climbing movement/state
        State previousState; // Previous Climbing movement/state

        ClimbingState climbingState; // Actual Climbing direction
        ClimbingState previousClimbingState; // Previous Climbing direction
        ClimbingState lastCollidingClimbingState;

        [Snapshot]  // Snapshot: compatible with snapshotdebugger
        FrameCapture capture; // input

        // --------------------------------

        // --- World interactable elements ---
        [Snapshot]
        LedgeGeometry ledgeGeometry;

        [Snapshot]
        WallGeometry wallGeometry;

        [Snapshot]
        LedgeAnchor ledgeAnchor;

        [Snapshot]
        WallAnchor wallAnchor;

        [Snapshot]
        AnchoredTransitionTask anchoredTransition;

        // --------------------------------

        // --- Basic Methods ---
        public override void OnEnable()
        {
            base.OnEnable();

            kinematica = GetComponent<Kinematica>();
            controller = GetComponent<MovementController>();
            capsule = GetComponent<CapsuleCollider>();

            state = State.Suspended;
            previousState = State.Suspended;

            climbingState = ClimbingState.Idle;
            previousClimbingState = ClimbingState.Idle;
            lastCollidingClimbingState = ClimbingState.None;

            ledgeGeometry = LedgeGeometry.Create();
            wallGeometry = WallGeometry.Create();

            ledgeAnchor = LedgeAnchor.Create();
            wallAnchor = WallAnchor.Create();

            anchoredTransition = AnchoredTransitionTask.Invalid;
        }

        public override void OnDisable()
        {
            base.OnDisable();

            ledgeGeometry.Dispose();
            anchoredTransition.Dispose();
        }

        public override void OnEarlyUpdate(bool rewind)
        {
            base.OnEarlyUpdate(rewind);

            if (!rewind) // if we are not using snapshot debugger to rewind
            {
                capture.Update();
            }
        }

        // --------------------------------

        // --- Ability class methods ---
        public Ability OnUpdate(float deltaTime)
        {
            ref var synthesizer = ref kinematica.Synthesizer.Ref;

            // --- Turn off/on controller ---
            CollisionLayer.ConfigureController(!IsState(State.Suspended), ref controller);

            // --- If character is not falling ---
            if (!IsState(State.Suspended))
            {
                // --- Handle current state ---
                switch (state)
                {
                    case State.Mounting:
                        {
                            bool TransitionSuccess;

                            if (KinematicaLayer.IsAnchoredTransitionComplete(ref anchoredTransition, out TransitionSuccess))
                            {
                                if (!TransitionSuccess)
                                {
                                    SetState(State.Suspended);
                                    return null;
                                }

                                HandleMountingState(ref synthesizer);
                            }
                        }
                        break;

                    case State.Climbing:
                        HandleClimbingState(ref synthesizer, deltaTime);
                        break;

                    case State.FreeClimbing:
                        HandleFreeClimbingState(ref synthesizer, deltaTime);
                        break;

                    case State.DropDown:
                        {
                            bool TransitionSuccess;

                            if (KinematicaLayer.IsAnchoredTransitionComplete(ref anchoredTransition, out TransitionSuccess))
                            {
                                if (!TransitionSuccess)
                                {
                                    SetState(State.Suspended);
                                    return null;
                                }

                                HandleDropDownState(ref synthesizer);
                            }
                        }
                        break;

                    default:
                        break;
                }

                if (IsState(State.Dismount) || IsState(State.PullUp) || IsState(State.DropDown))
                {
                    bool bTransitionSucceeded;
                    if (KinematicaLayer.IsAnchoredTransitionComplete(ref anchoredTransition, out bTransitionSucceeded))
                    {
                        SetState(State.Suspended);
                    }
                }

                // TODO: Remove from here
                //if (anchoredTransition.isValid)
                //{
                //    if (!anchoredTransition.IsState(AnchoredTransitionTask.State.Complete) && !anchoredTransition.IsState(AnchoredTransitionTask.State.Failed))
                //    {
                //        anchoredTransition.synthesizer = MemoryRef<MotionSynthesizer>.Create(ref synthesizer);
                //        kinematica.AddJobDependency(AnchoredTransitionJob.Schedule(ref anchoredTransition));
                //    }
                //    else
                //    {
                //        anchoredTransition.Dispose();
                //    }
                //}

                KinematicaLayer.UpdateAnchoredTransition(ref anchoredTransition, ref kinematica);

                return this;
            }

            return null;
        }

        // --- Climbing states wrappers ---
        void HandleMountingState(ref MotionSynthesizer synthesizer)
        {
            bool freeClimbing = false; // ledgeDistance >= 0.1f;

            // --- Get ledge anchor point from root motion transform ---
            float3 rootPosition = synthesizer.WorldRootTransform.t;
            ledgeAnchor = ledgeGeometry.GetAnchor(rootPosition);

            // --- Depending on how far the anchor is, decide if we are hanging onto a ledge or climbing a wall ---

            Climbing climbingTrait = freeClimbing ? Climbing.Create(Climbing.Type.Wall) : Climbing.Create(Climbing.Type.Ledge);

            if (freeClimbing)
            {
                wallAnchor = wallGeometry.GetAnchor(synthesizer.WorldRootTransform.t); // rootposition
                SetState(State.FreeClimbing);
            }
            else
            {
                SetState(State.Climbing); // we are hanging onto a ledge 
            }

            SetClimbingState(ClimbingState.Idle);

            PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(Idle.Default));
        }

        void HandleClimbingState(ref MotionSynthesizer synthesizer, float deltaTime)
        {
            UpdateClimbing(ref synthesizer, deltaTime);

            var desiredState = GetDesiredClimbingState();
            if (desiredState == lastCollidingClimbingState)
            {
                desiredState = ClimbingState.Idle;
            }

            // --- Handle ledge climbing/movement direction ---
            if (!IsClimbingState(desiredState))
            {
                var climbingTrait = Climbing.Create(Climbing.Type.Ledge);

                if (desiredState == ClimbingState.Idle)
                {
                    PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(Idle.Default));
                }
                else if (desiredState == ClimbingState.Right)
                {
                    var direction = Direction.Create(Direction.Type.Right);
                    PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(direction).Except(Idle.Default));
                }
                else if (desiredState == ClimbingState.Left)
                {
                    var direction = Direction.Create(Direction.Type.Left);
                    PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(direction).Except(Idle.Default));
                }

                SetClimbingState(desiredState);
            }

            AffineTransform rootTransform = synthesizer.WorldRootTransform;
            wallGeometry.Initialize(rootTransform);
            wallAnchor = wallGeometry.GetAnchor(rootTransform.t);

            // --- React to pull up/dismount ---
            if (capture.pullUpButton && !CollisionLayer.IsCharacterCapsuleColliding(transform.position, ref capsule))
            {
                AffineTransform contactTransform = ledgeGeometry.GetTransform(ledgeAnchor);
                RequestTransition(ref synthesizer, contactTransform, Ledge.Type.PullUp);
                SetState(State.PullUp);
            }
            else if (capture.dismountButton /*&& closeToDrop*/)
            {
                RequestTransition(ref synthesizer, synthesizer.WorldRootTransform, Ledge.Type.Dismount);
                SetState(State.Dismount);
            }
        }

        void HandleFreeClimbingState(ref MotionSynthesizer synthesizer, float deltaTime)
        {
            // --- We are climbing a wall ---
            UpdateFreeClimbing(ref synthesizer, deltaTime);

            var desiredState = GetDesiredFreeClimbingState();

            if (!IsClimbingState(desiredState))
            {
                // --- Handle wall climbing state ---
                var climbingTrait = Climbing.Create(Climbing.Type.Wall);

                if (desiredState == ClimbingState.Idle)
                {
                    PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(Idle.Default));
                }
                else if (desiredState == ClimbingState.Down)
                {
                    var direction = Direction.Create(Direction.Type.Down);
                    PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(direction).Except(Idle.Default));
                }
                else if (desiredState == ClimbingState.UpRight)
                {
                    var direction = Direction.Create(Direction.Type.UpRight);
                    PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(direction).Except(Idle.Default));
                }
                else if (desiredState == ClimbingState.UpLeft)
                {
                    var direction = Direction.Create(Direction.Type.UpLeft);
                    PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(direction).Except(Idle.Default));
                }

                SetClimbingState(desiredState);
            }

            float height = wallGeometry.GetHeight(ref wallAnchor);
            float totalHeight = wallGeometry.GetHeight();
            bool closeToLedge = math.abs(totalHeight - height) <= 0.095f;
            bool closeToDrop = math.abs(height - 2.8f) <= 0.095f;

            // --- Check if we are close to hanging onto a ledge or almost on the ground ---
            if (closeToLedge && capture.stickVertical >= 0.9f)
            {
                ledgeAnchor = ledgeGeometry.GetAnchor(synthesizer.WorldRootTransform.t); // rootPosition
                SetState(State.Climbing);
            }
            else if (closeToDrop && capture.stickVertical <= -0.9f)
            {
                RequestTransition(ref synthesizer, synthesizer.WorldRootTransform, Ledge.Type.Dismount);
                SetState(State.Dismount);
            }
        }

        void HandleDropDownState(ref MotionSynthesizer synthesizer)
        {
            // --- From the top of a wall, drop down onto the ledge ---
            ledgeAnchor = ledgeGeometry.GetAnchor(synthesizer.WorldRootTransform.t);
            SetState(State.Climbing);
        }

        bool UpdateCollidingClimbingState(float desiredMoveOnLedge, float3 desiredPosition, float3 desiredForward)
        {
            bool bCollision = CollisionLayer.IsCharacterCapsuleColliding(desiredPosition - math.normalize(desiredForward) * 0.5f - new float3(0.0f, 1.5f, 0.0f), ref capsule);

            if (climbingState == ClimbingState.Idle)
            {
                lastCollidingClimbingState = ClimbingState.None;
            }
            else if (bCollision)
            {
                float currentMoveDirection = climbingState == ClimbingState.Left ? 1.0f : -1.0f;
                if (currentMoveDirection * desiredMoveOnLedge > 0.0f)
                {
                    lastCollidingClimbingState = climbingState;
                }
            }

            return bCollision;
        }

        void UpdateClimbing(ref MotionSynthesizer synthesizer, float deltaTime)
        {
            //
            // Smoothly adjust current root transform towards the anchor transform
            //

            AffineTransform deltaTransform = synthesizer.GetTrajectoryDeltaTransform(deltaTime);
            AffineTransform rootTransform = synthesizer.WorldRootTransform * deltaTransform;

            float linearDisplacement = -deltaTransform.t.x;

            LedgeAnchor desiredLedgeAnchor = ledgeGeometry.UpdateAnchor(ledgeAnchor, linearDisplacement);

            float3 position = ledgeGeometry.GetPosition(desiredLedgeAnchor);
            float3 desiredForward = ledgeGeometry.GetNormal(desiredLedgeAnchor);

            // --- Update current anchor if it is still on the ledge ---
            if (!UpdateCollidingClimbingState(linearDisplacement, position, desiredForward))
            {
                ledgeAnchor = desiredLedgeAnchor;
            }

            float distance = math.length(rootTransform.t - position);
            if (distance >= 0.01f)
            {
                float3 normal = math.normalize(position - rootTransform.t);
                rootTransform.t += normal * 0.5f * deltaTime;
            }
            rootTransform.t = position;

            float angle;
            float3 currentForward = Missing.zaxis(rootTransform.q);
            quaternion q = Missing.forRotation(currentForward, desiredForward);
            float maximumAngle = math.radians(90.0f) * deltaTime;
            float3 axis = Missing.axisAngle(q, out angle);
            angle = math.min(angle, maximumAngle);
            rootTransform.q = math.mul(quaternion.AxisAngle(axis, angle), rootTransform.q);

            // --- Update root motion transform ---
            synthesizer.WorldRootTransform = rootTransform;

            ledgeGeometry.DebugDraw();
            ledgeGeometry.DebugDraw(ref ledgeAnchor);
        }

        void UpdateFreeClimbing(ref MotionSynthesizer synthesizer, float deltaTime)
        {
            //
            // Smoothly adjust current root transform towards the anchor transform
            //
            // --- Update wall climbing ---

            AffineTransform deltaTransform = synthesizer.GetTrajectoryDeltaTransform(deltaTime);
            AffineTransform rootTransform = synthesizer.WorldRootTransform * deltaTransform;

            wallAnchor = wallGeometry.GetAnchor(rootTransform.t);
            float y = 1.0f - (2.8f / wallGeometry.GetHeight());
            wallAnchor.y = math.min(y, wallAnchor.y);

            float3 position = wallGeometry.GetPosition(wallAnchor);
            float distance = math.length(rootTransform.t - position);

            if (distance >= 0.01f)
            {
                float3 normal = math.normalize(position - rootTransform.t);
                rootTransform.t += normal * 0.5f * deltaTime;
            }
            rootTransform.t = position;

            float angle;
            float3 currentForward = Missing.zaxis(rootTransform.q);
            float3 desiredForward = -wallGeometry.GetNormalWorldSpace();

            quaternion q = Missing.forRotation(currentForward, desiredForward);
            float maximumAngle = math.radians(90.0f) * deltaTime;
            float3 axis = Missing.axisAngle(q, out angle);
            angle = math.min(angle, maximumAngle);
            rootTransform.q = math.mul(quaternion.AxisAngle(axis, angle), rootTransform.q);

            rootTransform *= deltaTransform.inverse();
            rootTransform.q = math.normalize(rootTransform.q);

            // --- Update root motion transform ---
            synthesizer.WorldRootTransform = rootTransform;

            wallGeometry.DebugDraw();
            wallGeometry.DebugDraw(ref wallAnchor);
        }

        public bool OnContact(ref MotionSynthesizer synthesizer, AffineTransform contactTransform, float deltaTime)
        {
            // --- If we make contact with a climbable surface and player issues climb order, mount ---
            if (capture.mountButton)
            {
                if (IsState(State.Suspended))
                {
                    BoxCollider collider = controller.current.collider as BoxCollider;

                    if (collider != null)
                    {
                        if (collider.gameObject.layer == LayerMask.NameToLayer("Wall"))
                        {
                            ledgeGeometry.Initialize(collider);
                            wallGeometry.Initialize(collider, contactTransform);

                            RequestTransition(ref synthesizer, contactTransform, Ledge.Type.Mount);

                            SetState(State.Mounting);

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public bool OnDrop(ref MotionSynthesizer synthesizer, float deltaTime)
        {
            return false;
        }

        public void RequestTransition(ref MotionSynthesizer synthesizer, AffineTransform contactTransform, Ledge.Type type)
        {
            // --- Require transition animation of the type given ---
            ref Binary binary = ref synthesizer.Binary; 
            Ledge trait = Ledge.Create(type);
            QueryResult sequence = TagExtensions.GetPoseSequence(ref binary, contactTransform, trait, contactThreshold);
            bool rootadjust = trait.type == Ledge.Type.PullUp ? false : true;

            anchoredTransition.Dispose();
            anchoredTransition = AnchoredTransitionTask.Create(ref synthesizer,
                    sequence, contactTransform, maximumLinearError,
                        maximumAngularError, rootadjust);
        }

        // --------------------------------

        // --- Utilities ---     
        //public bool IsTransitionComplete(out bool TransitionSuccess) // TODO: Remove from here
        //{
        //    bool ret = true;
        //    TransitionSuccess = false;
        //    bool active = anchoredTransition.isValid;

        //    // --- Check if current transition has been completed ---

        //    if (active)
        //    {
        //        if (anchoredTransition.IsState(AnchoredTransitionTask.State.Complete))
        //        {
        //            anchoredTransition.Dispose();
        //            anchoredTransition = AnchoredTransitionTask.Invalid;
        //            TransitionSuccess = true;
        //            ret = true;
        //        }
        //        else if (anchoredTransition.IsState(AnchoredTransitionTask.State.Failed))
        //        {
        //            anchoredTransition.Dispose();
        //            anchoredTransition = AnchoredTransitionTask.Invalid;
        //            ret = true;
        //        }
        //        else
        //        {
        //            ret = false;
        //        }
        //    }

        //    return ret;
        //}

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
        public void PlayFirstSequence(PoseSet poses) // TODO: Remove from here
        {
            kinematica.Synthesizer.Ref.PlayFirstSequence(poses);
            poses.Dispose();
        }

        float2 GetStickInput() // TODO: Remove from here
        {
            float2 stickInput =
                new float2(capture.stickHorizontal,
                    capture.stickVertical);

            if (math.length(stickInput) >= 0.1f)
            {
                if (math.length(stickInput) > 1.0f)
                    stickInput =
                        math.normalize(stickInput);

                return stickInput;
            }

            return float2.zero;
        }

        ClimbingState GetDesiredFreeClimbingState()
        {
            float2 stickInput = GetStickInput();

            if (math.length(stickInput) >= 0.1f)
            {
                if (stickInput.y < stickInput.x)
                {
                    return ClimbingState.Down;
                }

                if (stickInput.x > 0.0f)
                {
                    return ClimbingState.UpRight;
                }

                return ClimbingState.UpLeft;
            }

            return ClimbingState.Idle;
        }

        ClimbingState GetDesiredClimbingState()
        {
            float2 stickInput = GetStickInput();

            if (math.abs(stickInput.x) >= 0.5f)
            {
                if (stickInput.x > 0.0f)
                {
                    return ClimbingState.Right;
                }

                return ClimbingState.Left;
            }

            return ClimbingState.Idle;
        }

        //bool CanPullUp() // kill this
        //{
        //    return !CollisionLayer.IsCharacterCapsuleColliding(transform.position, ref capsule);
        //}

        // --------------------------------
    }
}

