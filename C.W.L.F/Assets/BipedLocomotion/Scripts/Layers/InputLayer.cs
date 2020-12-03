using Unity.Kinematica;
using Unity.Mathematics;
using Unity.SnapshotDebugger;
using UnityEngine;

// --- Wrapper for ability-input interactions ---

namespace CWLF
{
    public static class InputLayer
    {
        public struct FrameCapture
        {
            // --- Attributes ---
            public float3 movementDirection;
            public float moveIntensity;
            public bool run;
            public bool dropDownButton;
            // --------------------------------

            public bool parkourButton;

            // --------------------------------

            public float stickHorizontal;
            public float stickVertical;
            public bool mountButton;
            public bool dismountButton;
            public bool pullUpButton;

            // --------------------------------

            // --- Basic methods ---
            public void UpdateLocomotion()
            {
                Utility.GetInputMove(ref movementDirection, ref moveIntensity);
                run = Input.GetButton("Left Analog Button");
                mountButton = Input.GetButton("B Button") || Input.GetKey("b");
                dropDownButton = Input.GetButton("A Button") || Input.GetKey("a");
            }

            public void UpdateParkour()
            {
                parkourButton = Input.GetButton("A Button");
            }

            public void UpdateClimbing()
            {
                stickHorizontal = Input.GetAxis("Left Analog Horizontal");
                stickVertical = Input.GetAxis("Left Analog Vertical");

                //Debug.Log(stickVertical);

                dismountButton = Input.GetButton("B Button") || Input.GetKey("b");
                pullUpButton = Input.GetButton("A Button") || Input.GetKey("a");
            }

            // --------------------------------
        }

        // --- Attributes ---
        [Snapshot]
        public static FrameCapture capture;

        // --------------------------------

        // --- Utilities ---
        public static float2 GetStickInput()
        {
            float2 stickInput;
            stickInput.x = capture.stickHorizontal;
            stickInput.y = capture.stickVertical;

            if (math.length(stickInput) >= 0.1f)
            {
                if (math.length(stickInput) > 1.0f)
                    stickInput = math.normalize(stickInput);
            }
            else
                stickInput = float2.zero;

            return stickInput;
        }

        // --------------------------------
    }
}
