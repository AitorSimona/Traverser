using Unity.Kinematica;
using Unity.SnapshotDebugger;
using Unity.Mathematics;
using Unity.Collections;

using UnityEngine;

namespace CWLF
{
    [RequireComponent(typeof(Kinematica))]
    [RequireComponent(typeof(AbilityController))]
    [RequireComponent(typeof(MovementController))]

    public partial class ClimbingAbility : SnapshotProvider, Ability
    {
        // --- Inspector variables ---
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

        [Header("Debug settings")]
        [Tooltip("Enables debug display for this ability.")]
        public bool enableDebugging;

        [Tooltip("Determines the movement to debug.")]
        public int debugIndex;

        [Tooltip("Controls the pose debug display.")]
        [Range(0, 100)]
        public int debugPoseIndex;

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

        // --- Contact filters (what do we react to) ---
        public enum Layer
        {
            Wall = 8
        }

        // --- Internal variables ---
        Kinematica kinematica; // system

        State state; // Actual Climbing movement/state
        State previousState; // Previous Climbing movement/state

        ClimbingState climbingState; // Actual Climbing direction
        ClimbingState previousClimbingState; // Previous Climbing direction
        ClimbingState lastCollidingClimbingState;

        // Snapshot: compatible with snapshotdebugger

        [Snapshot]
        FrameCapture capture; // input

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

        // --- Basic Methods ---
        public override void OnEnable()
        {
            base.OnEnable();

            kinematica = GetComponent<Kinematica>();

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

            if (!rewind)
            {
                capture.Update();
            }
        }

        // --- Ability class methods ---
        public Ability OnUpdate(float deltaTime)
        {
            ref var synthesizer = ref kinematica.Synthesizer.Ref;

            // --- React to character falling ---
            ConfigureController(!IsState(State.Suspended));

            // --- If character is not falling ---
            if(!IsState(State.Suspended))
            {

                switch (state)
                {
                    case State.Mounting:
                        {
                            bool TransitionSuccess;

                            if (IsTransitionComplete(out TransitionSuccess))
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
                        HandleClimbingState(ref synthesizer);
                        break;

                    case State.FreeClimbing:
                        HandleFreeClimbingState(ref synthesizer, deltaTime);
                        break;

                    case State.Dismount:
                        //if (IsTransitionComplete())
                            HandleDismountState(ref synthesizer);
                        break;

                    case State.PullUp:
                        //if (IsTransitionComplete())
                            HandlePullUpState(ref synthesizer);
                        break;

                    case State.DropDown:
                        {
                            bool TransitionSuccess;

                            if (IsTransitionComplete(out TransitionSuccess))
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

                return this;
            }

            return null;
        }

        // --- Climbing states wrappers ---

        void HandleMountingState(ref MotionSynthesizer synthesizer)
        {
            float3 rootPosition = synthesizer.WorldRootTransform.t;

            ledgeAnchor = ledgeGeometry.GetAnchor(rootPosition);
            float3 ledgePosition = ledgeGeometry.GetPosition(ledgeAnchor);
            float ledgeDistance = math.length(rootPosition - ledgePosition);

            bool freeClimbing = false; // ledgeDistance >= 0.1f;

            Climbing climbingTrait = freeClimbing ? Climbing.Create(Climbing.Type.Wall) : Climbing.Create(Climbing.Type.Ledge);

            if (freeClimbing)
            {
                wallAnchor = wallGeometry.GetAnchor(rootPosition);
                SetState(State.FreeClimbing);
            }
            else
            {
                SetState(State.Climbing);
            }

            SetClimbingState(ClimbingState.Idle);

            PlayFirstSequence(synthesizer.Query.Where(climbingTrait).And(Idle.Default));
        }

        void HandleClimbingState(ref MotionSynthesizer synthesizer)
        {

        }

        void HandleFreeClimbingState(ref MotionSynthesizer synthesizer, float deltaTime)
        {
            UpdateFreeClimbing(ref synthesizer, deltaTime);

            var desiredState = GetDesiredFreeClimbingState();

            if (!IsClimbingState(desiredState))
            {
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

            if (closeToLedge && capture.stickVertical >= 0.9f)
            {
                float3 rootPosition = synthesizer.WorldRootTransform.t;

                ledgeAnchor = ledgeGeometry.GetAnchor(rootPosition);
                float3 ledgePosition = ledgeGeometry.GetPosition(ledgeAnchor);

                SetState(State.Climbing);
            }
            else if (closeToDrop && capture.stickVertical <= -0.9f)
            {
                //RequestDismountTransition(ref synthesizer, deltaTime);

                SetState(State.Dismount);
            }
        }

        void HandleDismountState(ref MotionSynthesizer synthesizer)
        {

        }

        void HandlePullUpState(ref MotionSynthesizer synthesizer)
        {

        }

        void HandleDropDownState(ref MotionSynthesizer synthesizer)
        {
            ledgeAnchor = ledgeGeometry.GetAnchor(synthesizer.WorldRootTransform.t);

            SetState(State.Climbing);
        }

        void UpdateFreeClimbing(ref MotionSynthesizer synthesizer, float deltaTime)
        {
            //
            // Smoothly adjust current root transform towards the anchor transform
            //

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

            synthesizer.WorldRootTransform = rootTransform;

            wallGeometry.DebugDraw();
            wallGeometry.DebugDraw(ref wallAnchor);
        }

        // --------------------------------

        public bool OnContact(ref MotionSynthesizer synthesizer, AffineTransform contactTransform, float deltaTime)
        {

            return false;
        }

        public bool OnDrop(ref MotionSynthesizer synthesizer, float deltaTime)
        {
            return false;
        }

        // --- Utilities ---     
        public bool IsTransitionComplete(out bool TransitionSuccess)
        {
            bool ret = true;
            TransitionSuccess = false;
            bool active = anchoredTransition.isValid;

            if (active)
            {
                if (anchoredTransition.IsState(AnchoredTransitionTask.State.Complete))
                {
                    anchoredTransition.Dispose();
                    anchoredTransition = AnchoredTransitionTask.Invalid;
                    TransitionSuccess = true;
                    ret = true;
                }
                else if (anchoredTransition.IsState(AnchoredTransitionTask.State.Failed))
                {
                    anchoredTransition.Dispose();
                    anchoredTransition = AnchoredTransitionTask.Invalid;
                    ret = true;
                }
                else
                {
                    ret = false;
                }
            }

            return ret;
        }

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
        public void PlayFirstSequence(PoseSet poses)
        {
            kinematica.Synthesizer.Ref.PlayFirstSequence(poses);
            poses.Dispose();
        }

        float2 GetStickInput()
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

        void ConfigureController(bool active)
        {
            var controller = GetComponent<MovementController>();

            controller.collisionEnabled = !active;
            controller.groundSnap = !active;
            controller.resolveGroundPenetration = !active;
            controller.gravityEnabled = !active;
        }
    }
}
