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

        [Title("Cảm giác nhảy")]
        [LabelText("Độ nén khi bật nhảy")]
        [Tooltip("Camera hạ nhẹ trước khi cơ thể rời đất, tạo cảm giác chân phát lực.")]
        [Range(0f, 0.15f)]
        [SerializeField] private float takeoffDip = 0.055f;

        [LabelText("Góc ngẩng khi bật nhảy")]
        [Range(0f, 4f)]
        [SerializeField] private float takeoffPitch = 1.1f;

        [LabelText("Thời gian bật nhảy")]
        [MinValue(0.05f)]
        [SerializeField] private float takeoffDuration = 0.16f;

        [LabelText("Độ trễ camera trên không")]
        [Tooltip("Mô phỏng quán tính của đầu so với cơ thể khi bay lên và rơi xuống.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float airborneVerticalLag = 0.09f;

        [LabelText("Góc camera trên không")]
        [Range(0f, 4f)]
        [SerializeField] private float airbornePitch = 0.8f;

        [LabelText("Độ hạ camera khi tiếp đất")]
        [Range(0f, 0.25f)]
        [SerializeField] private float landingDrop = 0.12f;

        [LabelText("Góc chúi khi tiếp đất")]
        [Range(0f, 6f)]
        [SerializeField] private float landingPitch = 2.2f;

        [LabelText("Thời gian hồi sau tiếp đất")]
        [MinValue(0.1f)]
        [SerializeField] private float landingDuration = 0.34f;

        [LabelText("Số nhịp rebound")]
        [Range(0.5f, 3f)]
        [SerializeField] private float landingOscillations = 1.35f;

        [LabelText("Độ tắt dần rebound")]
        [Range(1f, 12f)]
        [SerializeField] private float landingDamping = 6.5f;

        [LabelText("Rung camera khi chạm đất")]
        [Range(0f, 0.05f)]
        [SerializeField] private float landingShake = 0.014f;

        [LabelText("Vận tốc va chạm tham chiếu")]
        [Tooltip("Tốc độ rơi đạt mức này sẽ dùng đúng cường độ landing đã cấu hình.")]
        [MinValue(0.1f)]
        [SerializeField] private float landingReferenceSpeed = 9f;

        [LabelText("FOV khi đang bay")]
        [Range(0f, 5f)]
        [SerializeField] private float airborneFieldOfViewBoost = 1.2f;

        [LabelText("FOV punch khi tiếp đất")]
        [Range(0f, 5f)]
        [SerializeField] private float landingFieldOfViewPunch = 1.5f;

        [Title("Phản hồi hành động")]

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
        private float _takeoffElapsed;
        private float _landingElapsed;
        private float _landingImpactScale = 1f;

        private void Awake()
        {
            _initialLocalPosition = transform.localPosition;
            _initialLocalRotation = transform.localRotation;
            _controlledCamera = GetComponentInChildren<Camera>();
            if (_controlledCamera != null)
            {
                _initialFieldOfView = _controlledCamera.fieldOfView;
            }

            ResetJumpResponse();
        }

        private void OnEnable()
        {
            if (player != null)
            {
                player.ActionPerformed += HandlePlayerAction;
                player.JumpStarted += HandleJumpStarted;
                player.Landed += HandleLanded;
            }
        }

        private void OnDisable()
        {
            if (player != null)
            {
                player.ActionPerformed -= HandlePlayerAction;
                player.JumpStarted -= HandleJumpStarted;
                player.Landed -= HandleLanded;
            }

            ResetJumpResponse();
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
            UpdateJumpResponse(
                isGrounded,
                out float jumpVerticalOffset,
                out float jumpLateralOffset,
                out float jumpPitchOffset,
                out float jumpRollOffset,
                out float jumpFieldOfViewOffset);

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
                (_lateralImpulse + jumpLateralOffset) * motionIntensity;
            targetPosition.y +=
                verticalBob * _movementWeight +
                (_verticalImpulse + jumpVerticalOffset) * motionIntensity;
            targetPosition.z += _forwardImpulse * motionIntensity;

            Quaternion targetRotation =
                _initialLocalRotation *
                Quaternion.Euler(
                    pitchBob * _movementWeight +
                    (_pitchImpulse + jumpPitchOffset) * motionIntensity,
                    0f,
                    rollBob * _movementWeight +
                    (_rollImpulse + jumpRollOffset) * motionIntensity);

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

            UpdateFieldOfView(
                speedFactor,
                isGrounded,
                jumpFieldOfViewOffset);
            RecoverImpulses();
        }

        private void UpdateFieldOfView(
            float speedFactor,
            bool isGrounded,
            float jumpFieldOfViewOffset)
        {
            if (_controlledCamera == null)
            {
                return;
            }

            float speedWeight = Mathf.Clamp01(speedFactor);
            float targetFieldOfView =
                _initialFieldOfView +
                (runFieldOfViewBoost * speedWeight +
                 (isGrounded ? 0f : airborneFieldOfViewBoost) +
                 jumpFieldOfViewOffset) *
                motionIntensity;
            float fieldOfViewBlend =
                1f - Mathf.Exp(-fieldOfViewSharpness * Time.deltaTime);
            _controlledCamera.fieldOfView = Mathf.Lerp(
                _controlledCamera.fieldOfView,
                targetFieldOfView,
                fieldOfViewBlend);
        }

        private void UpdateJumpResponse(
            bool isGrounded,
            out float verticalOffset,
            out float lateralOffset,
            out float pitchOffset,
            out float rollOffset,
            out float fieldOfViewOffset)
        {
            verticalOffset = 0f;
            lateralOffset = 0f;
            pitchOffset = 0f;
            rollOffset = 0f;
            fieldOfViewOffset = 0f;

            if (_takeoffElapsed < takeoffDuration)
            {
                _takeoffElapsed += Time.deltaTime;
                float takeoffProgress = Mathf.Clamp01(
                    _takeoffElapsed / Mathf.Max(takeoffDuration, 0.01f));
                float takeoffEnvelope = Mathf.Sin(takeoffProgress * Mathf.PI);
                verticalOffset -= takeoffDip * takeoffEnvelope;
                pitchOffset -= takeoffPitch * takeoffEnvelope;
            }

            if (!isGrounded)
            {
                float normalizedVerticalVelocity = Mathf.Clamp(
                    player.VerticalVelocity /
                    Mathf.Max(landingReferenceSpeed, 0.1f),
                    -1f,
                    1f);
                verticalOffset -=
                    normalizedVerticalVelocity * airborneVerticalLag;
                pitchOffset -=
                    normalizedVerticalVelocity * airbornePitch;
            }

            if (_landingElapsed >= landingDuration)
            {
                return;
            }

            _landingElapsed += Time.deltaTime;
            float landingProgress = Mathf.Clamp01(
                _landingElapsed / Mathf.Max(landingDuration, 0.01f));
            float damping = Mathf.Exp(-landingDamping * landingProgress);
            float phase =
                landingProgress * landingOscillations * Mathf.PI * 2f;
            float rebound = Mathf.Cos(phase) * damping * _landingImpactScale;

            verticalOffset -= landingDrop * rebound;
            pitchOffset += landingPitch * rebound;

            float shakeEnvelope =
                Mathf.Exp(-12f * landingProgress) * _landingImpactScale;
            float shakeTime = _landingElapsed;
            lateralOffset +=
                Mathf.Sin(shakeTime * 83f) * landingShake * shakeEnvelope;
            verticalOffset +=
                Mathf.Sin(shakeTime * 107f) *
                landingShake *
                0.65f *
                shakeEnvelope;
            rollOffset +=
                Mathf.Sin(shakeTime * 71f) *
                landingPitch *
                0.2f *
                shakeEnvelope;
            fieldOfViewOffset =
                landingFieldOfViewPunch *
                Mathf.Exp(-8f * landingProgress) *
                _landingImpactScale;
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

                case WarmupActionType.Duck:
                    _verticalImpulse = -landingDrop * 0.35f;
                    break;

                case WarmupActionType.Punch:
                    _forwardImpulse = punchPush;
                    break;
            }
        }

        private void HandleJumpStarted()
        {
            _takeoffElapsed = 0f;
            _landingElapsed = landingDuration;
        }

        private void HandleLanded(float impactSpeed)
        {
            _landingImpactScale = Mathf.Clamp(
                impactSpeed / Mathf.Max(landingReferenceSpeed, 0.1f),
                0.65f,
                1.5f);
            _landingElapsed = 0f;
            _takeoffElapsed = takeoffDuration;
        }

        private void ResetJumpResponse()
        {
            _takeoffElapsed = takeoffDuration;
            _landingElapsed = landingDuration;
            _landingImpactScale = 1f;
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
