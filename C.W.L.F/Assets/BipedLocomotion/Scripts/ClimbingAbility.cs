using Unity.Kinematica;
using Unity.SnapshotDebugger;
using Unity.Mathematics;
using Unity.Collections;

using UnityEngine;

namespace CWLF
{
    [RequireComponent(typeof(Kinematica))]
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

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
