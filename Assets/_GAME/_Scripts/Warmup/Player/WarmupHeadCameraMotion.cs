using Sirenix.OdinInspector;
using UnityEngine;

namespace GameYT.Warmup
{
    [DisallowMultipleComponent]
    public sealed class WarmupHeadCameraMotion : MonoBehaviour
    {
        [Title("Tham chiếu")]
        [Required]
        [LabelText("Player")]
        [SerializeField] private WarmupPlayerController player;

        [Title("Chuyển động khi chạy")]
        [LabelText("Cường độ chuyển động")]
        [Tooltip("Cường độ tổng thể. 0 tắt camera motion, 1 dùng đầy đủ thiết lập bên dưới.")]
        [Range(0f, 1f)]
        [SerializeField] private float motionIntensity = 1f;

        [LabelText("Nhịp chạy cơ bản")]
        [Tooltip("Số chu kỳ sải chân mỗi giây ở tốc độ chạy mặc định.")]
        [MinValue(0.1f)]
        [SerializeField] private float baseFrequency = 1.7f;

        [LabelText("Độ nhún dọc")]
        [Tooltip("Biên độ camera lên xuống. Nên giữ thấp để tránh gây say khi xem video.")]
        [Range(0f, 0.08f)]
        [SerializeField] private float verticalAmplitude = 0.018f;

        [LabelText("Độ lắc ngang")]
        [Tooltip("Biên độ camera dịch nhẹ sang hai bên theo bước chân.")]
        [Range(0f, 0.08f)]
        [SerializeField] private float horizontalAmplitude = 0.009f;

        [LabelText("Góc nghiêng ngang")]
        [Tooltip("Góc roll theo bước chân. Giữ rất nhỏ để đường chân trời ổn định.")]
        [Range(0f, 1f)]
        [SerializeField] private float rollAmplitude = 0.12f;

        [LabelText("Góc chúi theo bước")]
        [Tooltip("Góc pitch nhẹ theo nhịp tiếp đất của từng bước.")]
        [Range(0f, 1f)]
        [SerializeField] private float pitchAmplitude = 0.1f;

        [Title("Phản hồi hành động")]
        [LabelText("Độ hạ camera khi tiếp đất")]
        [Range(0f, 0.15f)]
        [SerializeField] private float landingDrop = 0.03f;

        [LabelText("Góc chúi khi tiếp đất")]
        [Range(0f, 3f)]
        [SerializeField] private float landingPitch = 0.35f;

        [LabelText("Góc nghiêng khi chuyển làn")]
        [Range(0f, 2f)]
        [SerializeField] private float sideStepRoll = 0.85f;

        [LabelText("Độ lách vai khi chuyển làn")]
        [Tooltip("Camera dịch vai nhanh sang hướng né rồi hồi về tâm.")]
        [Range(0f, 0.1f)]
        [SerializeField] private float sideStepPush = 0.035f;

        [LabelText("Độ đẩy camera khi đấm")]
        [Range(0f, 0.05f)]
        [SerializeField] private float punchPush = 0.012f;

        [Title("Góc nhìn theo tốc độ")]
        [LabelText("FOV tăng thêm khi chạy")]
        [Tooltip("Tăng FOV nhẹ để tạo cảm giác tốc độ mà không rung camera.")]
        [Range(0f, 8f)]
        [SerializeField] private float runFieldOfViewBoost = 2.2f;

        [LabelText("Độ mượt thay đổi FOV")]
        [MinValue(0.1f)]
        [SerializeField] private float fieldOfViewSharpness = 4.5f;

        [Title("Làm mượt")]
        [LabelText("Độ mượt bắt đầu/dừng chạy")]
        [Tooltip("Làm camera motion vào/ra từ từ, tránh giật khi trạng thái grounded thay đổi.")]
        [MinValue(0.1f)]
        [SerializeField] private float movementBlendSharpness = 6f;

        [LabelText("Độ mượt vị trí")]
        [MinValue(0.1f)]
        [SerializeField] private float positionSharpness = 10f;

        [LabelText("Độ mượt góc xoay")]
        [MinValue(0.1f)]
        [SerializeField] private float rotationSharpness = 10f;

        [LabelText("Tốc độ hồi phản lực")]
        [MinValue(0.1f)]
        [SerializeField] private float impulseRecovery = 9f;

        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;
        private Camera _controlledCamera;
        private float _initialFieldOfView;
        private float _stridePhase;
        private float _movementWeight;
        private float _lateralImpulse;
        private float _verticalImpulse;
        private float _forwardImpulse;
        private float _pitchImpulse;
        private float _rollImpulse;
        private bool _wasGrounded;

        private void Awake()
        {
            _initialLocalPosition = transform.localPosition;
            _initialLocalRotation = transform.localRotation;
            _controlledCamera = GetComponentInChildren<Camera>();
            if (_controlledCamera != null)
            {
                _initialFieldOfView = _controlledCamera.fieldOfView;
            }

            _wasGrounded = player != null && player.IsGrounded;
        }

        private void OnEnable()
        {
            if (player != null)
            {
                player.ActionPerformed += HandlePlayerAction;
            }
        }

        private void OnDisable()
        {
            if (player != null)
            {
                player.ActionPerformed -= HandlePlayerAction;
            }

            transform.localPosition = _initialLocalPosition;
            transform.localRotation = _initialLocalRotation;
            if (_controlledCamera != null)
            {
                _controlledCamera.fieldOfView = _initialFieldOfView;
            }
        }

        private void LateUpdate()
        {
            if (player == null)
            {
                return;
            }

            bool isGrounded = player.IsGrounded;
            DetectLanding(isGrounded);

            float configuredSpeed = Mathf.Max(player.ConfiguredRunSpeed, 0.01f);
            float speedFactor = Mathf.Clamp(
                player.CurrentRunSpeed / configuredSpeed,
                0f,
                1.6f);
            float targetMovementWeight = isGrounded && speedFactor > 0.01f
                ? Mathf.Clamp01(speedFactor) * motionIntensity
                : 0f;
            float movementBlend =
                1f - Mathf.Exp(-movementBlendSharpness * Time.deltaTime);
            _movementWeight = Mathf.Lerp(
                _movementWeight,
                targetMovementWeight,
                movementBlend);

            if (_movementWeight > 0.001f)
            {
                _stridePhase +=
                    Time.deltaTime * baseFrequency * speedFactor * Mathf.PI * 2f;
            }

            float verticalBob =
                -Mathf.Cos(_stridePhase * 2f) * verticalAmplitude;
            float horizontalBob =
                Mathf.Sin(_stridePhase) * horizontalAmplitude;
            float rollBob =
                -Mathf.Sin(_stridePhase) * rollAmplitude;
            float pitchBob =
                Mathf.Cos(_stridePhase * 2f) * pitchAmplitude;

            Vector3 targetPosition = _initialLocalPosition;
            targetPosition.x +=
                horizontalBob * _movementWeight +
                _lateralImpulse * motionIntensity;
            targetPosition.y +=
                verticalBob * _movementWeight +
                _verticalImpulse * motionIntensity;
            targetPosition.z += _forwardImpulse * motionIntensity;

            Quaternion targetRotation =
                _initialLocalRotation *
                Quaternion.Euler(
                    pitchBob * _movementWeight +
                    _pitchImpulse * motionIntensity,
                    0f,
                    rollBob * _movementWeight +
                    _rollImpulse * motionIntensity);

            float positionBlend =
                1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
            float rotationBlend =
                1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);

            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                targetPosition,
                positionBlend);
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                targetRotation,
                rotationBlend);

            UpdateFieldOfView(speedFactor);
            RecoverImpulses();
            _wasGrounded = isGrounded;
        }

        private void UpdateFieldOfView(float speedFactor)
        {
            if (_controlledCamera == null)
            {
                return;
            }

            float speedWeight = Mathf.Clamp01(speedFactor);
            float targetFieldOfView =
                _initialFieldOfView +
                runFieldOfViewBoost * speedWeight * motionIntensity;
            float fieldOfViewBlend =
                1f - Mathf.Exp(-fieldOfViewSharpness * Time.deltaTime);
            _controlledCamera.fieldOfView = Mathf.Lerp(
                _controlledCamera.fieldOfView,
                targetFieldOfView,
                fieldOfViewBlend);
        }

        private void DetectLanding(bool isGrounded)
        {
            if (!_wasGrounded && isGrounded)
            {
                _verticalImpulse = -landingDrop;
                _pitchImpulse = landingPitch;
            }
        }

        private void HandlePlayerAction(WarmupActionType action)
        {
            switch (action)
            {
                case WarmupActionType.MoveLeft:
                    _rollImpulse = sideStepRoll;
                    _lateralImpulse = -sideStepPush;
                    break;

                case WarmupActionType.MoveRight:
                    _rollImpulse = -sideStepRoll;
                    _lateralImpulse = sideStepPush;
                    break;

                case WarmupActionType.Jump:
                    _pitchImpulse = -landingPitch * 0.35f;
                    break;

                case WarmupActionType.Duck:
                    _verticalImpulse = -landingDrop * 0.35f;
                    break;

                case WarmupActionType.Punch:
                    _forwardImpulse = punchPush;
                    break;
            }
        }

        private void RecoverImpulses()
        {
            float recoveryBlend =
                1f - Mathf.Exp(-impulseRecovery * Time.deltaTime);
            _lateralImpulse = Mathf.Lerp(
                _lateralImpulse,
                0f,
                recoveryBlend);
            _verticalImpulse = Mathf.Lerp(
                _verticalImpulse,
                0f,
                recoveryBlend);
            _forwardImpulse = Mathf.Lerp(
                _forwardImpulse,
                0f,
                recoveryBlend);
            _pitchImpulse = Mathf.Lerp(
                _pitchImpulse,
                0f,
                recoveryBlend);
            _rollImpulse = Mathf.Lerp(
                _rollImpulse,
                0f,
                recoveryBlend);
        }

#if UNITY_EDITOR
        public void SetupComponents(WarmupPlayerController playerController)
        {
            player = playerController;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
