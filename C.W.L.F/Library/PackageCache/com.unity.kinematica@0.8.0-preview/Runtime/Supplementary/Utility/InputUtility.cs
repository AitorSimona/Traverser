using System;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Kinematica
{
    public class InputUtility
    {
        public static int ActionButtonInput => 1 << 0;
        public static int MoveInput => 1 << 1;
        public static int CameraInput => 1 << 2;


        public static bool IsPressingActionButton()
        {
            try
            {
                return Input.GetButton(ActionButton);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static float GetMoveHorizontalInput()
        {
            return GetSafeAxisValue(MoveHorizontalAxis);
        }

        public static float GetMoveVerticalInput()
        {
            return GetSafeAxisValue(MoveVerticalAxis);
        }

        public static float GetCameraHorizontalInput()
        {
            float inputValue = GetSafeAxisValue(CameraHorizontalAxis);

            float mouseValue = Input.GetMouseButton(1) ? GetSafeAxisValue("Mouse X") * CameraMouseSpeed : 0.0f;
            OverrideByGreaterValue(ref inputValue, mouseValue);

            return inputValue;
        }

        public static float GetCameraVerticalInput()
        {
            float inputValue = GetSafeAxisValue(CameraVerticalAxis);

            float mouseValue = Input.GetMouseButton(1) ? GetSafeAxisValue("Mouse Y") * CameraMouseSpeed : 0.0f;
            OverrideByGreaterValue(ref inputValue, mouseValue);

            return inputValue;
        }

        public static void DisplayMissingInputs(int InputFlags)
        {
            float verticalPosition = 0;

            void DisplayText(string text)
            {
                GUI.Label(new Rect(0, verticalPosition, 900, 20), text);
                verticalPosition += 20.0f;
            }

            if ((InputFlags & ActionButtonInput) > 0 && !IsActionButtonSetup())
            {
                DisplayText($"Please configure '{ActionButton}' button in the Input Manager.");
            }

            if ((InputFlags & MoveInput) > 0 && !AreMoveInputsSetup())
            {
                DisplayText($"To control the character with joystick, please configure '{MoveHorizontalAxis}' and '{MoveVerticalAxis}' axes in the Input Manager.");
            }

            if ((InputFlags & CameraInput) > 0 && !AreCameraInputsSetup())
            {
                DisplayText($"To control the camera with joystick, please configure '{CameraHorizontalAxis}' and '{CameraVerticalAxis}' axes in the Input Manager.");
                DisplayText("You can still control the camera with the mouse (maintain left mouse button).");
            }
        }

        static bool IsActionButtonSetup()
        {
            try
            {
                Input.GetButton(ActionButton);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static bool AreMoveInputsSetup()
        {
            return IsAxisSetup(MoveHorizontalAxis) && IsAxisSetup(MoveVerticalAxis);
        }

        static bool AreCameraInputsSetup()
        {
            return IsAxisSetup(CameraHorizontalAxis) && IsAxisSetup(CameraVerticalAxis);
        }

        static float GetSafeAxisValue(string axisName)
        {
            try
            {
                return Input.GetAxis(axisName);
            }
            catch (Exception)
            {
                return 0.0f;
            }
        }

        static bool IsAxisSetup(string axisName)
        {
            try
            {
                Input.GetAxis(axisName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static void OverrideByGreaterValue(ref float inputValue, float candidate)
        {
            if (math.abs(candidate) > math.abs(inputValue))
            {
                inputValue = candidate;
            }
        }

        static string ActionButton => "Fire1";
        static string MoveHorizontalAxis => "Horizontal";
        static string MoveVerticalAxis => "Vertical";
        static string CameraHorizontalAxis => "Right Analog Horizontal";
        static string CameraVerticalAxis => "Right Analog Vertical";
        static float CameraMouseSpeed => 3.0f;
    }
}
