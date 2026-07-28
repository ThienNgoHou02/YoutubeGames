using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameYT.Warmup
{
    [DisallowMultipleComponent]
    public sealed class InputManager : MonoBehaviour
    {
        private const string UpArrowPath = "<Keyboard>/upArrow";
        private const string DownArrowPath = "<Keyboard>/downArrow";
        private const string LeftArrowPath = "<Keyboard>/leftArrow";
        private const string RightArrowPath = "<Keyboard>/rightArrow";
        private const string PunchPath = "<Keyboard>/j";

        [Title("Runtime Debug")]
        [ShowInInspector, ReadOnly]
        public bool IsEnabled => _jumpAction != null && _jumpAction.enabled;

        public event Action<WarmupActionType> ActionTriggered;
        public event Action<bool> DuckStateChanged;

        public bool IsDuckHeld =>
            _duckAction != null && _duckAction.IsPressed();

        private InputAction _jumpAction;
        private InputAction _duckAction;
        private InputAction _leftAction;
        private InputAction _rightAction;
        private InputAction _punchAction;

        private void Awake()
        {
            CreateActions();
            Subscribe();
        }

        private void OnEnable()
        {
            EnableActions();
        }

        private void OnDisable()
        {
            DisableActions();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            DisposeActions();
        }

        private void CreateActions()
        {
            _jumpAction = new InputAction("Jump", InputActionType.Button, UpArrowPath);
            _duckAction = new InputAction("Duck", InputActionType.Button, DownArrowPath);
            _leftAction = new InputAction("Move Left", InputActionType.Button, LeftArrowPath);
            _rightAction = new InputAction("Move Right", InputActionType.Button, RightArrowPath);
            _punchAction = new InputAction("Punch", InputActionType.Button, PunchPath);
        }

        private void Subscribe()
        {
            _jumpAction.performed += HandleJump;
            _duckAction.performed += HandleDuckStarted;
            _duckAction.canceled += HandleDuckCanceled;
            _leftAction.performed += HandleLeft;
            _rightAction.performed += HandleRight;
            _punchAction.performed += HandlePunch;
        }

        private void Unsubscribe()
        {
            if (_jumpAction == null)
            {
                return;
            }

            _jumpAction.performed -= HandleJump;
            _duckAction.performed -= HandleDuckStarted;
            _duckAction.canceled -= HandleDuckCanceled;
            _leftAction.performed -= HandleLeft;
            _rightAction.performed -= HandleRight;
            _punchAction.performed -= HandlePunch;
        }

        private void EnableActions()
        {
            _jumpAction?.Enable();
            _duckAction?.Enable();
            _leftAction?.Enable();
            _rightAction?.Enable();
            _punchAction?.Enable();
        }

        private void DisableActions()
        {
            _jumpAction?.Disable();
            _duckAction?.Disable();
            _leftAction?.Disable();
            _rightAction?.Disable();
            _punchAction?.Disable();
        }

        private void DisposeActions()
        {
            _jumpAction?.Dispose();
            _duckAction?.Dispose();
            _leftAction?.Dispose();
            _rightAction?.Dispose();
            _punchAction?.Dispose();
        }

        private void HandleJump(InputAction.CallbackContext context)
        {
            ActionTriggered?.Invoke(WarmupActionType.Jump);
        }

        private void HandleDuckStarted(InputAction.CallbackContext context)
        {
            DuckStateChanged?.Invoke(true);
            ActionTriggered?.Invoke(WarmupActionType.Duck);
        }

        private void HandleDuckCanceled(InputAction.CallbackContext context)
        {
            DuckStateChanged?.Invoke(false);
        }

        private void HandleLeft(InputAction.CallbackContext context)
        {
            ActionTriggered?.Invoke(WarmupActionType.MoveLeft);
        }

        private void HandleRight(InputAction.CallbackContext context)
        {
            ActionTriggered?.Invoke(WarmupActionType.MoveRight);
        }

        private void HandlePunch(InputAction.CallbackContext context)
        {
            ActionTriggered?.Invoke(WarmupActionType.Punch);
        }
    }
}
