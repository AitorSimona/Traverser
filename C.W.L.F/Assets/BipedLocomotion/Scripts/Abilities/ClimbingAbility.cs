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

    public class ClimbingAbility : SnapshotProvider
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

        // TODO: Missing definitions of world elements (wall, ledge)

        //[Snapshot]
        //LedgeGeometry ledgeGeometry;

        //[Snapshot]
        //WallGeometry wallGeometry;

        //[Snapshot]
        //LedgeAnchor ledgeAnchor;

        //[Snapshot]
        //WallAnchor wallAnchor;

        //[Snapshot]
        //AnchoredTransitionTask anchoredTransition;

        // --- Methods ---
        public override void OnEnable()
        {
            base.OnEnable();

            kinematica = GetComponent<Kinematica>();

            state = State.Suspended;
            previousState = State.Suspended;

            climbingState = ClimbingState.Idle;
            previousClimbingState = ClimbingState.Idle;
            lastCollidingClimbingState = ClimbingState.None;
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }

        public override void OnEarlyUpdate(bool rewind)
        {
            base.OnEarlyUpdate(rewind);

            if (!rewind)
            {
                capture.Update();
            }
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        

        // Update is called once per frame
        void Update()
        {
            ref var synthesizer = ref kinematica.Synthesizer.Ref;

        }


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
        //void ConfigureController(bool active)
        //{
        //    var controller = GetComponent<MovementController>();

        //    controller.collisionEnabled = !active;
        //    controller.groundSnap = !active;
        //    controller.resolveGroundPenetration = !active;
        //    controller.gravityEnabled = !active;
        //}
    }
}
