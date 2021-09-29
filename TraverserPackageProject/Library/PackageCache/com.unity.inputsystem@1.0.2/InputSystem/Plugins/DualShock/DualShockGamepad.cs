using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.Scripting;

////TODO: speaker, touchpad

////TODO: move gyro here

namespace UnityEngine.InputSystem.DualShock
{
    /// <summary>
    /// A Sony DualShock controller.
    /// </summary>
    [InputControlLayout(displayName = "PS4 Controller")]
    [Preserve]
    public class DualShockGamepad : Gamepad, IDualShockHaptics
    {
        /// <summary>
        /// Button that is triggered when the touchbar on the controller is pressed down.
        /// </summary>
        /// <value>Control representing the touchbar button.</value>
        [InputControl(name = "buttonWest", displayName = "Square", shortDisplayName = "Square")]
        [InputControl(name = "buttonNorth", displayName = "Triangle", shortDisplayName = "Triangle")]
        [InputControl(name = "buttonEast", displayName = "Circle", shortDisplayName = "Circle")]
        [InputControl(name = "buttonSouth", displayName = "Cross", shortDisplayName = "Cross")]
        [InputControl]
        public ButtonControl touchpadButton { get; private set; }

        /// <summary>
        /// The right side button in the middle section of the controller. Equivalent to
        /// <see cref="Gamepad.startButton"/>.
        /// </summary>
        /// <value>Same as <see cref="Gamepad.startButton"/>.</value>
        [InputControl(name = "start", displayName = "Options")]
        public ButtonControl optionsButton { get; private set; }

        /// <summary>
        /// The left side button in the middle section of the controller. Equivalent to
        /// <see cref="Gamepad.selectButton"/>
        /// </summary>
        /// <value>Same as <see cref="Gamepad.selectButton"/>.</value>
        [InputControl(name = "select", displayName = "Share")]
        public ButtonControl shareButton { get; private set; }

        /// <summary>
        /// The left shoulder button.
        /// </summary>
        /// <value>Equivalent to <see cref="Gamepad.leftShoulder"/>.</value>
        [InputControl(name = "leftShoulder", displayName = "L1", shortDisplayName = "L1")]
        public ButtonControl L1 { get; private set; }

        /// <summary>
        /// The right shoulder button.
        /// </summary>
        /// <value>Equivalent to <see cref="Gamepad.rightShoulder"/>.</value>
        [InputControl(name = "rightShoulder", displayName = "R1", shortDisplayName = "R1")]
        public ButtonControl R1 { get; private set; }

        /// <summary>
        /// The left trigger button.
        /// </summary>
        /// <value>Equivalent to <see cref="Gamepad.leftTrigger"/>.</value>
        [InputControl(name = "leftTrigger", displayName = "L2", shortDisplayName = "L2")]
        public ButtonControl L2 { get; private set; }

        /// <summary>
        /// The right trigger button.
        /// </summary>
        /// <value>Equivalent to <see cref="Gamepad.rightTrigger"/>.</value>
        [InputControl(name = "rightTrigger", displayName = "R2", shortDisplayName = "R2")]
        public ButtonControl R2 { get; private set; }

        /// <summary>
        /// The left stick press button.
        /// </summary>
        /// <value>Equivalent to <see cref="Gamepad.leftStickButton"/>.</value>
        [InputControl(name = "leftStickPress", displayName = "L3", shortDisplayName = "L3")]
        public ButtonControl L3 { get; private set; }

        /// <summary>
        /// The right stick press button.
        /// </summary>
        /// <value>Equivalent to <see cref="Gamepad.rightStickButton"/>.</value>
        [InputControl(name = "rightStickPress", displayName = "R3", shortDisplayName = "R3")]
        public ButtonControl R3 { get; private set; }

        /// <summary>
        /// The last used/added DualShock controller.
        /// </summary>
        public new static DualShockGamepad current { get; private set; }

        /// <inheritdoc />
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        /// <inheritdoc />
        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            base.FinishSetup();

            touchpadButton = GetChildControl<ButtonControl>("touchpadButton");
            optionsButton = startButton;
            shareButton = selectButton;

            L1 = leftShoulder;
            R1 = rightShoulder;
            L2 = leftTrigger;
            R2 = rightTrigger;
            L3 = leftStickButton;
            R3 = rightStickButton;
        }

        /// <inheritdoc />
        public virtual void SetLightBarColor(Color color)
        {
        }
    }
}
