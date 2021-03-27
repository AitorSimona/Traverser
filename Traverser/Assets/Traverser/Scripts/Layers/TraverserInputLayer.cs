using Unity.Mathematics;
using UnityEngine;

// --- Wrapper for ability-input interactions ---

namespace Traverser
{
    public static class TraverserInputLayer
    {
        public struct FrameCapture
        {
            // --- Attributes ---
            public float3 movementDirection;
            //public float moveIntensity;
            public bool run;
            public bool dropDownButton;
            // --------------------------------

            public bool parkourButton;
            public bool parkourDropDownButton;

            // --------------------------------

            public float stickHorizontal;
            public float stickVertical;
            public bool mountButton;
            public bool dismountButton;
            public bool pullUpButton;

            // --------------------------------

            // --- Basic methods ---

            // NOTE: Careful with overlapping input!! Another action may be activated due to sharing
            // the same input, and you won't notice it.

            public void UpdateLocomotion()
            {
                stickHorizontal = Input.GetAxis("Horizontal");
                stickVertical = Input.GetAxis("Vertical");
                movementDirection.x = stickHorizontal;
                movementDirection.y = 0.0f;
                movementDirection.z = stickVertical;

               //Debug.Log(stickHorizontal);

               run = Input.GetButton("Left Analog Button");
            }

            public void UpdateParkour()
            {
                parkourButton = Input.GetButton("A Button") || Input.GetKey("a");
                parkourDropDownButton = Input.GetButton("X Button") || Input.GetKey("c");
            }

            public void UpdateClimbing()
            {
                stickHorizontal = Input.GetAxis("Left Analog Horizontal");
                stickVertical = Input.GetAxis("Left Analog Vertical");

                //Debug.Log(stickVertical);
                mountButton = Input.GetButton("B Button") || Input.GetKey("b");
                dropDownButton = Input.GetButton("A Button") || Input.GetKey("a");
                dismountButton = Input.GetButton("B Button") || Input.GetKey("b");
                pullUpButton = Input.GetButton("A Button") || Input.GetKey("a");
            }

            // --------------------------------
        }

        // --- Attributes ---
        public static FrameCapture capture;

        // --------------------------------

        // --- Utilities ---

        public static float GetMoveIntensity()
        {
            return math.clamp(math.length(capture.movementDirection), 0.0f, 1.0f);
        }

        // --------------------------------
    }
}
