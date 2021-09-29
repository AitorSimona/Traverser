using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Scripting;

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A key on a <see cref="Keyboard"/>.
    /// </summary>
    /// <remarks>
    /// This is an extended button control which adds various features to account for the fact that keys
    /// have symbols associated with them which may change depending on keyboard layout as well as in combination
    /// with other keys.
    ///
    /// Note that there is no text input associated with individual keys as text composition is highly
    /// layout specific and does not need to be key-by-key. For general text input, see <see cref="Keyboard.onTextInput"/>.
    /// To find the text displayed on a key, use <see cref="KeyControl.displayName"/>.
    /// </remarks>
    [Preserve]
    public class KeyControl : ButtonControl
    {
        /// <summary>
        /// The code used in Unity to identify the key.
        /// </summary>
        /// <remarks>
        /// This property must be initialized by <see cref="InputControl.FinishSetup"/> of
        /// the device owning the control.
        /// </remarks>
        public Key keyCode { get; set; }

        ////REVIEW: rename this to something like platformKeyCode? We're not really dealing with scan code here.
        /// <summary>
        /// The code that the underlying platform uses to identify the key.
        /// </summary>
        public int scanCode
        {
            get
            {
                RefreshConfigurationIfNeeded();
                return m_ScanCode;
            }
        }

        protected override void RefreshConfiguration()
        {
            // Wipe our last cached set of data (if any).
            displayName = null;
            m_ScanCode = 0;

            var command = QueryKeyNameCommand.Create(keyCode);
            if (device.ExecuteCommand(ref command) > 0)
            {
                m_ScanCode = command.scanOrKeyCode;
                displayName = command.ReadKeyName();
            }
        }

        // Cached configuration data for the key. We fetch this from the
        // device on demand.
        private int m_ScanCode;
    }
}
