using System.Collections;
using System.Collections.Generic;
using Unity.Kinematica;
using Unity.Mathematics;
using Unity.SnapshotDebugger;
using UnityEngine;


namespace CWLF
{
    public static class InputLayer
    {
        public struct FrameCapture
        {
            // --- Attributes ---
            public float3 movementDirection;
            public float moveIntensity;
            public bool run; //run

            // --------------------------------

            public bool jumpButton;

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
                run = Input.GetButton("A Button");
            }

            public void UpdateParkour()
            {
                jumpButton = Input.GetButton("A Button");
            }

            public void UpdateClimbing()
            {
                stickHorizontal = Input.GetAxis("Left Analog Horizontal");
                stickVertical = Input.GetAxis("Left Analog Vertical");

                mountButton = Input.GetButton("B Button") || Input.GetKey("b");
                dismountButton = Input.GetButton("B Button") || Input.GetKey("b");
                pullUpButton = Input.GetButton("A Button") || Input.GetKey("a");
            }

            // --------------------------------
        }

        // --- Attributes ---
        [Snapshot]
        public static FrameCapture capture;

        // --------------------------------
    }
}
