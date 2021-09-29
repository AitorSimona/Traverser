#if UNITY_INPUT_SYSTEM_ENABLE_XR && ENABLE_VR && !DISABLE_BUILTIN_INPUT_SYSTEM_OCULUS && !PACKAGE_DOCS_GENERATION
using UnityEngine.InputSystem;
using UnityEngine.Scripting;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

namespace Unity.XR.Oculus.Input
{
    /// <summary>
    /// An Oculus VR headset (such as the Oculus Rift series of devices).
    /// </summary>
    [InputControlLayout(displayName = "Oculus Headset")]
    [Preserve]
    public class OculusHMD : XRHMD
    {
        [Preserve]
        [InputControl]
        [InputControl(name = "trackingState", layout = "Integer", aliases = new[] { "devicetrackingstate" })]
        [InputControl(name = "isTracked", layout = "Button", aliases = new[] { "deviceistracked" })]
        public ButtonControl userPresence { get; private set; }
        [Preserve]
        [InputControl(noisy = true)]
        public Vector3Control deviceAngularVelocity { get; private set; }
        [Preserve]
        [InputControl(noisy = true)]
        public Vector3Control deviceAcceleration { get; private set; }
        [Preserve]
        [InputControl(noisy = true)]
        public Vector3Control deviceAngularAcceleration { get; private set; }
        [Preserve]
        [InputControl(noisy = true)]
        public Vector3Control leftEyeAngularVelocity { get; private set; }
        [Preserve]
        [InputControl(noisy = true)]
        public Vector3Control leftEyeAcceleration { get; private set; }
        [Preserve]
        [InputControl(noisy = true)]
        public Vector3Control leftEyeAngularAcceleration { get; private set; }
        [Preserve]
        [InputControl(noisy = true)]
        public Vector3Control rightEyeAngularVelocity { get; private set; }
        [Preserve]
        [InputControl(noisy = true)]
        public Vector3Control rightEyeAcceleration { get; private set; }
        [Preserve]
        [InputControl(noisy = true)]
        public Vector3Control rightEyeAngularAcceleration { get; private set; }
        [Preserve]
        [InputControl(noisy = true)]
        public Vector3Control centerEyeAngularVelocity { get; private set; }
        [Preserve]
        [InputControl(noisy = true)]
        public Vector3Control centerEyeAcceleration { get; private set; }
        [Preserve]
        [InputControl(noisy = true)]
        public Vector3Control centerEyeAngularAcceleration { get; private set; }


        protected override void FinishSetup()
        {
            base.FinishSetup();

            userPresence = GetChildControl<ButtonControl>("userPresence");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
            deviceAcceleration = GetChildControl<Vector3Control>("deviceAcceleration");
            deviceAngularAcceleration = GetChildControl<Vector3Control>("deviceAngularAcceleration");
            leftEyeAngularVelocity = GetChildControl<Vector3Control>("leftEyeAngularVelocity");
            leftEyeAcceleration = GetChildControl<Vector3Control>("leftEyeAcceleration");
            leftEyeAngularAcceleration = GetChildControl<Vector3Control>("leftEyeAngularAcceleration");
            rightEyeAngularVelocity = GetChildControl<Vector3Control>("rightEyeAngularVelocity");
            rightEyeAcceleration = GetChildControl<Vector3Control>("rightEyeAcceleration");
            rightEyeAngularAcceleration = GetChildControl<Vector3Control>("rightEyeAngularAcceleration");
            centerEyeAngularVelocity = GetChildControl<Vector3Control>("centerEyeAngularVelocity");
            centerEyeAcceleration = GetChildControl<Vector3Control>("centerEyeAcceleration");
            centerEyeAngularAcceleration = GetChildControl<Vector3Control>("centerEyeAngularAcceleration");
        }
    }

    /// <summary>
    /// An Oculus Touch controller.
    /// </summary>
    [Preserve]
    [InputControlLayout(displayName = "Oculus Touch Controller", commonUsages = new[] { "LeftHand", "RightHand" })]
    public class OculusTouchController : XRControllerWithRumble
    {
        [Preserve]
        [InputControl(aliases = new[] { "Primary2DAxis", "Joystick" })]
        public Vector2Control thumbstick { get; private set; }

        [Preserve]
        [InputControl]
        public AxisControl trigger { get; private set; }
        [Preserve]
        [InputControl]
        public AxisControl grip { get; private set; }

        [Preserve]
        [InputControl(aliases = new[] { "A", "X", "Alternate" })]
        public ButtonControl primaryButton { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "B", "Y", "Primary" })]
        public ButtonControl secondaryButton { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "GripButton" })]
        public ButtonControl gripPressed { get; private set; }
        [Preserve]
        [InputControl]
        public ButtonControl start { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "JoystickOrPadPressed", "thumbstickClick" })]
        public ButtonControl thumbstickClicked { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "ATouched", "XTouched", "ATouch", "XTouch" })]
        public ButtonControl primaryTouched { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "BTouched", "YTouched", "BTouch", "YTouch" })]
        public ButtonControl secondaryTouched { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "indexTouch", "indexNearTouched" })]
        public AxisControl triggerTouched { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "indexButton", "indexTouched" })]
        public ButtonControl triggerPressed { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "JoystickOrPadTouched", "thumbstickTouch" })]
        [InputControl(name = "trackingState", layout = "Integer", aliases = new[] { "controllerTrackingState" })]
        [InputControl(name = "isTracked", layout = "Button", aliases = new[] { "ControllerIsTracked" })]
        [InputControl(name = "devicePosition", layout = "Vector3", aliases = new[] { "controllerPosition" })]
        [InputControl(name = "deviceRotation", layout = "Quaternion", aliases = new[] { "controllerRotation" })]
        public ButtonControl thumbstickTouched { get; private set; }
        [Preserve]
        [InputControl(noisy = true, aliases = new[] { "controllerVelocity" })]
        public Vector3Control deviceVelocity { get; private set; }
        [Preserve]
        [InputControl(noisy = true, aliases = new[] { "controllerAngularVelocity" })]
        public Vector3Control deviceAngularVelocity { get; private set; }
        [Preserve]
        [InputControl(noisy = true, aliases = new[] { "controllerAcceleration" })]
        public Vector3Control deviceAcceleration { get; private set; }
        [Preserve]
        [InputControl(noisy = true, aliases = new[] { "controllerAngularAcceleration" })]
        public Vector3Control deviceAngularAcceleration { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            thumbstick = GetChildControl<Vector2Control>("thumbstick");
            trigger = GetChildControl<AxisControl>("trigger");
            triggerTouched = GetChildControl<AxisControl>("triggerTouched");
            grip = GetChildControl<AxisControl>("grip");

            primaryButton = GetChildControl<ButtonControl>("primaryButton");
            secondaryButton = GetChildControl<ButtonControl>("secondaryButton");
            gripPressed = GetChildControl<ButtonControl>("gripPressed");
            start = GetChildControl<ButtonControl>("start");
            thumbstickClicked = GetChildControl<ButtonControl>("thumbstickClicked");
            primaryTouched = GetChildControl<ButtonControl>("primaryTouched");
            secondaryTouched = GetChildControl<ButtonControl>("secondaryTouched");
            thumbstickTouched = GetChildControl<ButtonControl>("thumbstickTouched");
            triggerPressed = GetChildControl<ButtonControl>("triggerPressed");

            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
            deviceAcceleration = GetChildControl<Vector3Control>("deviceAcceleration");
            deviceAngularAcceleration = GetChildControl<Vector3Control>("deviceAngularAcceleration");
        }
    }

    [Preserve]
    public class OculusTrackingReference : TrackedDevice
    {
        [Preserve]
        [InputControl(aliases = new[] { "trackingReferenceTrackingState" })]
        public new IntegerControl trackingState { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "trackingReferenceIsTracked" })]
        public new ButtonControl isTracked { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
        }
    }

    /// <summary>
    /// An Oculus Remote controller.
    /// </summary>
    [InputControlLayout(displayName = "Oculus Remote")]
    [Preserve]
    public class OculusRemote : InputDevice
    {
        [Preserve]
        [InputControl]
        public ButtonControl back { get; private set; }
        [Preserve]
        [InputControl]
        public ButtonControl start { get; private set; }
        [Preserve]
        [InputControl]
        public Vector2Control touchpad { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            back = GetChildControl<ButtonControl>("back");
            start = GetChildControl<ButtonControl>("start");
            touchpad = GetChildControl<Vector2Control>("touchpad");
        }
    }

    /// <summary>
    /// A Standalone VR headset that includes on-headset controls.
    /// </summary>
    [InputControlLayout(displayName = "Oculus Headset (w/ on-headset controls)")]
    [Preserve]
    public class OculusHMDExtended : OculusHMD
    {
        [Preserve]
        [InputControl]
        public ButtonControl back { get; private set; }
        [Preserve]
        [InputControl]
        public Vector2Control touchpad { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            back = GetChildControl<ButtonControl>("back");
            touchpad = GetChildControl<Vector2Control>("touchpad");
        }
    }

    /// <summary>
    /// A Gear VR controller.
    /// </summary>
    [Preserve]
    [InputControlLayout(displayName = "GearVR Controller", commonUsages = new[] { "LeftHand", "RightHand" })]
    public class GearVRTrackedController : XRController
    {
        [Preserve]
        [InputControl]
        public Vector2Control touchpad { get; private set; }
        [Preserve]
        [InputControl]
        public AxisControl trigger { get; private set; }
        [Preserve]
        [InputControl]
        public ButtonControl back { get; private set; }
        [Preserve]
        [InputControl]
        public ButtonControl triggerPressed { get; private set; }
        [Preserve]
        [InputControl]
        public ButtonControl touchpadClicked { get; private set; }
        [Preserve]
        [InputControl]
        public ButtonControl touchpadTouched { get; private set; }
        [Preserve]
        [InputControl(noisy = true)]
        public Vector3Control deviceAngularVelocity { get; private set; }
        [Preserve]
        [InputControl(noisy = true)]
        public Vector3Control deviceAcceleration { get; private set; }
        [Preserve]
        [InputControl(noisy = true)]
        public Vector3Control deviceAngularAcceleration { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            touchpad = GetChildControl<Vector2Control>("touchpad");
            trigger = GetChildControl<AxisControl>("trigger");
            back = GetChildControl<ButtonControl>("back");
            triggerPressed = GetChildControl<ButtonControl>("triggerPressed");
            touchpadClicked = GetChildControl<ButtonControl>("touchpadClicked");
            touchpadTouched = GetChildControl<ButtonControl>("touchpadTouched");

            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
            deviceAcceleration = GetChildControl<Vector3Control>("deviceAcceleration");
            deviceAngularAcceleration = GetChildControl<Vector3Control>("deviceAngularAcceleration");
        }
    }
}
#endif
