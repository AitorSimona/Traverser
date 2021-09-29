using UnityEngine;
using UnityEngine.InputSystem;

namespace Traverser
{
    public class TraverserInputController : MonoBehaviour
    {
        // --- Private Variables ---
        private Vector2 inputMovement;
        private Vector2 inputLook;

        private enum InputInteraction
        {                         
            None = 0,             
            WestButton = 1 << 1,  
            NorthButton = 1 << 2, 
            EastButton = 1 << 3,  
            SouthButton = 1 << 4, 
            RunButton = 1 << 5    
        }
  
        private InputInteraction inputInteraction;

        // --------------------------------

        // --- Basic Methods ---

        private void Awake()
        {
            inputMovement = Vector2.zero;
            inputLook = Vector2.zero;
            inputInteraction = InputInteraction.None;
        }

        // --------------------------------

        // --- Getters (names based on gamepad bindings) ---

        public Vector2 GetInputMovement()
        {
            return inputMovement;
        }

        public float GetMoveIntensity()
        {
            return Mathf.Clamp(inputMovement.magnitude, 0.0f, 1.0f);
        }

        public Vector2 GetInputLook()
        {
            return inputLook;
        }

        public bool GetInputButtonWest()
        {
            return (inputInteraction & InputInteraction.WestButton) != 0;
        }

        public bool GetInputButtonNorth()
        {
            return (inputInteraction & InputInteraction.NorthButton) != 0;
        }

        public bool GetInputButtonEast()
        {
            return (inputInteraction & InputInteraction.EastButton) != 0;
        }

        public bool GetInputButtonSouth()
        {
            return (inputInteraction & InputInteraction.SouthButton) != 0;
        }

        public bool GetInputButtonRun()
        {
            return (inputInteraction & InputInteraction.RunButton) != 0;
        }

        // --------------------------------

        // --- Events ---

        // These are called from the PlayerInput component, when the player uses any input

        public void OnMovement(InputAction.CallbackContext value)
        {
            inputMovement = value.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext value)
        {
            inputLook = value.ReadValue<Vector2>();
        }

        public void OnWestButton(InputAction.CallbackContext value)
        {
            if (value.performed)
                inputInteraction |= InputInteraction.WestButton;
            else if (value.canceled)
                inputInteraction &= ~InputInteraction.WestButton;
        }

        public void OnNorthButton(InputAction.CallbackContext value)
        {
            if (value.performed)
                inputInteraction |= InputInteraction.NorthButton;
            else if (value.canceled)
                inputInteraction &= ~InputInteraction.NorthButton;
        }
        public void OnEastButton(InputAction.CallbackContext value)
        {
            if (value.performed)
                inputInteraction |= InputInteraction.EastButton;
            else if (value.canceled)
                inputInteraction &= ~InputInteraction.EastButton;
        }

        public void OnSouthButton(InputAction.CallbackContext value)
        {
            if (value.performed)
                inputInteraction |= InputInteraction.SouthButton;
            else if (value.canceled)
                inputInteraction &= ~InputInteraction.SouthButton;
        }

        public void OnRunButton(InputAction.CallbackContext value)
        {
            if (value.performed)
                inputInteraction |= InputInteraction.RunButton;
            else if (value.canceled)
                inputInteraction &= ~InputInteraction.RunButton;
        }

        // --------------------------------
    }
}
