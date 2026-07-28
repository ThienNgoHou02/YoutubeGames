using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.InputSystem;

namespace GameYT
{
    public class InputManager : Singleton<InputManager>
    {
        private PlayerInputActions input;
        private new void Awake()
        {
            input = new PlayerInputActions();

            //JUMP
            input.Movements.Jump.performed += OnJumpPerformed;

            //CROUCH
            input.Movements.Crouch.performed += OnCrouchPerformed;
        }
        private void OnEnable()
        {
            input.Movements.Enable();
        }
        private void OnDisable()
        {
            input.Movements.Disable();
        }
        private void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            OnJump?.Invoke();
        }
        private void OnCrouchPerformed(InputAction.CallbackContext ctx)
        {
            OnCrouch?.Invoke();
        }

        private bool Register(ref Action action, Action register)
        {
            bool registered = action?
            .GetInvocationList()
            .Contains
            (
                (Action)register
            )
            ?? false;

            if (registered)
            {
                return false;
            }

            action += register;

            return true;
        }
        private bool Unregister(ref Action action, Action unregister)
        {
            bool registered = action?
            .GetInvocationList()
            .Contains
            (
                (Action)unregister
            )
            ?? false;

            if (registered)
            {
                return false;
            }

            action -= unregister;

            return true;
        }
        private void ClearAction(ref Action action)
        {
            action = null;
        }

        public event Action OnJump;
        public event Action OnCrouch;

        ///Jump
        public bool JumpActionRegister(Action action)
        {
            return Register(ref OnJump, action);
        }
        public bool JumpActionUnregister(Action action)
        {
            return Unregister(ref OnJump, action);
        }
        public void ResetJumpActionRegister()
        {
            ClearAction(ref OnJump);
        }

        ///Crouch
        public bool CrouchActionRegister(Action action)
        {
            return Register(ref OnCrouch, action);
        }
        public bool CrouchActionUnregister(Action action)
        {
            return Unregister(ref OnCrouch, action);
        }
        public void ResetCrouchActionRegister()
        {
            ClearAction(ref OnCrouch);
        }
    }
}
