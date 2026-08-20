using Sirenix.OdinInspector;
using UnityEngine;

namespace GameYT.Warmup
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WarmupPlayerController))]
    [RequireComponent(typeof(AudioSource))]
    public sealed class WarmupPlayerSfx : MonoBehaviour
    {
        private const float MinimumRunSpeed = 0.01f;

        [Title("References")]
        [Required]
        [SerializeField] private WarmupPlayerController player;

        [Tooltip("Có thể để trống. Component sẽ tự tạo AudioSource khi chạy.")]
        [SerializeField] private AudioSource audioSource;

        [Title("SFX Clips")]
        [Tooltip("Tiếng bước chân khi chạy. Phát lặp theo Run Step Interval.")]
        [SerializeField] private AudioClip runClip;

        [SerializeField] private AudioClip punchClip;
        [SerializeField] private AudioClip jumpClip;
        [SerializeField] private AudioClip duckClip;

        [Tooltip("Dùng chung cho lách trái và lách phải.")]
        [SerializeField] private AudioClip dodgeClip;

        [Tooltip("Âm thanh phát mỗi lần nhặt Coin / Reward Item.")]
        [SerializeField] private AudioClip coinPickupClip;

        [Title("Volume")]
        [Range(0f, 1f)]
        [SerializeField] private float runVolume = 0.7f;

        [Range(0f, 1f)]
        [SerializeField] private float actionVolume = 1f;

        [Title("Run Settings")]
        [MinValue(0.05f)]
        [SerializeField] private float runStepInterval = 0.32f;

        [Tooltip("Nhịp chân thay đổi theo tốc độ chạy hiện tại.")]
        [SerializeField] private bool scaleRunIntervalWithSpeed = true;

        private float _nextRunStepTime;

        private void Awake()
        {
            if (player == null)
            {
                player = GetComponent<WarmupPlayerController>();
            }

            EnsureAudioSource();
        }

        private void OnEnable()
        {
            if (player != null)
            {
                player.ActionPerformed += HandleActionPerformed;
                player.JumpStarted += HandleJumpStarted;
            }

            _nextRunStepTime = Time.time;
        }

        private void OnDisable()
        {
            if (player != null)
            {
                player.ActionPerformed -= HandleActionPerformed;
                player.JumpStarted -= HandleJumpStarted;
            }

            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }

        private void Update()
        {
            if (!CanPlayRunStep())
            {
                _nextRunStepTime = Time.time;
                return;
            }

            if (Time.time < _nextRunStepTime)
            {
                return;
            }

            PlayOneShot(runClip, runVolume);
            _nextRunStepTime = Time.time + ResolveRunStepInterval();
        }

        private bool CanPlayRunStep()
        {
            return runClip != null &&
                   player != null &&
                   player.CurrentRunSpeed > MinimumRunSpeed &&
                   player.IsGrounded &&
                   !player.IsDucking;
        }

        private float ResolveRunStepInterval()
        {
            float interval = Mathf.Max(0.05f, runStepInterval);
            if (!scaleRunIntervalWithSpeed ||
                player.ConfiguredRunSpeed <= MinimumRunSpeed)
            {
                return interval;
            }

            float speedRatio = player.CurrentRunSpeed / player.ConfiguredRunSpeed;
            return interval / Mathf.Max(0.25f, speedRatio);
        }

        private void HandleActionPerformed(WarmupActionType action)
        {
            switch (action)
            {
                case WarmupActionType.MoveLeft:
                case WarmupActionType.MoveRight:
                    PlayOneShot(dodgeClip, actionVolume);
                    break;

                case WarmupActionType.Duck:
                    PlayOneShot(duckClip, actionVolume);
                    break;

                case WarmupActionType.Punch:
                    PlayOneShot(punchClip, actionVolume);
                    break;
            }
        }

        private void HandleJumpStarted()
        {
            PlayOneShot(jumpClip, actionVolume);
        }

        public void PlayCoinPickup()
        {
            PlayOneShot(coinPickupClip, actionVolume);
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null)
            {
                return;
            }

            EnsureAudioSource();
            audioSource.PlayOneShot(clip, volume);
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

#if UNITY_EDITOR
        [Button("Auto Assign References")]
        private void AutoAssignReferences()
        {
            player = GetComponent<WarmupPlayerController>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            EnsureAudioSource();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [Button("Validate References")]
        private void ValidateReferences()
        {
            if (player == null)
            {
                Debug.LogError("WarmupPlayerSfx thiếu Player Controller.", this);
            }

            if (runClip == null || punchClip == null || jumpClip == null ||
                duckClip == null || dodgeClip == null || coinPickupClip == null)
            {
                Debug.LogWarning(
                    "WarmupPlayerSfx còn thiếu AudioClip. Hãy kéo đủ SFX vào Inspector.",
                    this);
            }
        }

        public void SetupComponents(WarmupPlayerController playerController)
        {
            player = playerController;
            AutoAssignReferences();
        }
#endif
    }
}
