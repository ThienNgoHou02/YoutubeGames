using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameYT.Warmup
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(InputManager))]
    [RequireComponent(typeof(WarmupPunchInteractor))]
    public sealed class WarmupPlayerController : MonoBehaviour
    {
        [Title("References")]
        [Required]
        [SerializeField] private WarmupPlayerConfig config;

        [Required]
        [SerializeField] private InputManager input;

        [Required]
        [SerializeField] private WarmupPunchInteractor punchInteractor;

        [Required]
        [SerializeField] private Transform cameraPivot;

        [Title("Runtime")]
        [SerializeField] private bool autoRun = true;

        [ShowInInspector, ReadOnly]
        public int CurrentLane { get; private set; }

        [ShowInInspector, ReadOnly]
        public bool IsDucking { get; private set; }

        [ShowInInspector, ReadOnly]
        public float SpeedMultiplier { get; private set; } = 1f;

        public bool IsGrounded =>
            _characterController != null && _characterController.isGrounded;

        public float CurrentRunSpeed =>
            config != null && autoRun
                ? config.AutoRunSpeed * SpeedMultiplier
                : 0f;

        public float ConfiguredRunSpeed =>
            config != null ? config.AutoRunSpeed : 0f;

        public event Action<WarmupActionType> ActionPerformed;

        private CharacterController _characterController;
        private float _verticalVelocity;
        private float _currentLaneOffset;
        private float _laneChangeStartOffset;
        private float _laneChangeTargetOffset;
        private float _laneChangeElapsed;
        private bool _isChangingLane;
        private float _cameraHeightVelocity;
        private Vector3 _standingCenter;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            if (input == null)
            {
                input = GetComponent<InputManager>();
            }

            if (punchInteractor == null)
            {
                punchInteractor = GetComponent<WarmupPunchInteractor>();
            }

            _standingCenter = _characterController.center;
            ApplyStandingDimensions();
        }

        private void OnEnable()
        {
            if (input != null)
            {
                input.ActionTriggered += HandleAction;
                input.DuckStateChanged += HandleDuckStateChanged;
                SetDuckState(input.IsDuckHeld);
            }
        }

        private void OnDisable()
        {
            if (input != null)
            {
                input.ActionTriggered -= HandleAction;
                input.DuckStateChanged -= HandleDuckStateChanged;
            }

            SetDuckState(false);
        }

        private void Update()
        {
            if (config == null)
            {
                return;
            }

            UpdateMovement();
            UpdateCameraHeight();
        }

        public void SetAutoRun(bool isEnabled)
        {
            autoRun = isEnabled;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            SpeedMultiplier = Mathf.Max(0f, multiplier);
        }

        private void HandleAction(WarmupActionType action)
        {
            switch (action)
            {
                case WarmupActionType.MoveLeft:
                    if (!TryStartLaneChange(-1))
                    {
                        return;
                    }
                    break;

                case WarmupActionType.MoveRight:
                    if (!TryStartLaneChange(1))
                    {
                        return;
                    }
                    break;

                case WarmupActionType.Jump:
                    TryJump();
                    break;

                case WarmupActionType.Duck:
                    break;

                case WarmupActionType.Punch:
                    if (!punchInteractor.IsOnCooldown)
                    {
                        punchInteractor.TryPunch();
                    }
                    break;
            }

            ActionPerformed?.Invoke(action);
        }

        private bool TryStartLaneChange(int direction)
        {
            if (config == null)
            {
                return false;
            }

            int targetLane = Mathf.Clamp(
                CurrentLane + direction,
                -config.MaximumLaneIndex,
                config.MaximumLaneIndex);
            if (targetLane == CurrentLane)
            {
                return false;
            }

            CurrentLane = targetLane;
            _laneChangeStartOffset = _currentLaneOffset;
            _laneChangeTargetOffset = targetLane * config.LaneWidth;
            _laneChangeElapsed = 0f;
            _isChangingLane = true;
            return true;
        }

        private void TryJump()
        {
            if (!_characterController.isGrounded)
            {
                return;
            }

            _verticalVelocity = Mathf.Sqrt(
                config.JumpHeight * -2f * config.Gravity);
        }

        private void HandleDuckStateChanged(bool isHeld)
        {
            SetDuckState(isHeld);
        }

        private void SetDuckState(bool isDucking)
        {
            if (_characterController == null || config == null)
            {
                return;
            }

            IsDucking = isDucking;
            if (!isDucking)
            {
                ApplyStandingDimensions();
                return;
            }

            _characterController.height = config.DuckHeight;
            _characterController.center =
                _standingCenter -
                Vector3.up *
                ((config.StandingHeight - config.DuckHeight) * 0.5f);
        }

        private void ApplyStandingDimensions()
        {
            if (_characterController == null || config == null)
            {
                return;
            }

            _characterController.height = config.StandingHeight;
            _characterController.center = _standingCenter;
        }

        private void UpdateMovement()
        {
            if (_characterController.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            _verticalVelocity += config.Gravity * Time.deltaTime;

            float previousLaneOffset = _currentLaneOffset;
            UpdateLaneChange();
            float lateralDelta = _currentLaneOffset - previousLaneOffset;

            Vector3 motion = transform.right * lateralDelta;
            if (autoRun)
            {
                motion += transform.forward *
                          (config.AutoRunSpeed * SpeedMultiplier * Time.deltaTime);
            }

            motion.y = _verticalVelocity * Time.deltaTime;
            _characterController.Move(motion);
        }

        private void UpdateLaneChange()
        {
            if (!_isChangingLane)
            {
                return;
            }

            _laneChangeElapsed += Time.deltaTime;
            float duration = Mathf.Max(config.LaneChangeSmoothTime, 0.01f);
            float progress = Mathf.Clamp01(_laneChangeElapsed / duration);
            float easedProgress = EaseInOutQuadratic(progress);

            _currentLaneOffset = Mathf.LerpUnclamped(
                _laneChangeStartOffset,
                _laneChangeTargetOffset,
                easedProgress);

            if (progress >= 1f)
            {
                _currentLaneOffset = _laneChangeTargetOffset;
                _isChangingLane = false;
            }
        }

        private static float EaseInOutQuadratic(float value)
        {
            return value < 0.5f
                ? 2f * value * value
                : 1f - Mathf.Pow(-2f * value + 2f, 2f) * 0.5f;
        }

        private void UpdateCameraHeight()
        {
            if (cameraPivot == null)
            {
                return;
            }

            float targetHeight = IsDucking
                ? config.CameraDuckHeight
                : config.CameraStandingHeight;

            Vector3 localPosition = cameraPivot.localPosition;
            localPosition.y = Mathf.SmoothDamp(
                localPosition.y,
                targetHeight,
                ref _cameraHeightVelocity,
                config.CameraHeightSmoothTime);
            cameraPivot.localPosition = localPosition;
        }

#if UNITY_EDITOR
        [Button("Auto Assign References")]
        private void AutoAssignReferences()
        {
            input = GetComponent<InputManager>();
            punchInteractor = GetComponent<WarmupPunchInteractor>();

            Transform pivot = transform.Find("Camera Pivot");
            if (pivot != null)
            {
                cameraPivot = pivot;
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void SetupComponents(
            WarmupPlayerConfig playerConfig,
            InputManager inputManager,
            WarmupPunchInteractor interactor,
            Transform pivot)
        {
            config = playerConfig;
            input = inputManager;
            punchInteractor = interactor;
            cameraPivot = pivot;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
