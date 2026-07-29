using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace GameYT.Warmup
{
    [DisallowMultipleComponent]
    public sealed class WarmupBossWall : MonoBehaviour, IPunchable
    {
        [Title("References")]
        [SerializeField] private Renderer intactRenderer;
        [SerializeField] private WarmupPaperShardBurst shardBurst;
        [SerializeField] private ParticleSystem punchVfx;
        [SerializeField] private AudioSource punchAudio;

        [Title("Config")]
        [Range(1, 10)]
        [SerializeField] private int hitPoints = 4;

        [MinValue(0f)]
        [SerializeField] private float hitPunchScale = 0.08f;

        [MinValue(0.01f)]
        [SerializeField] private float hitFeedbackDuration = 0.16f;

        [Title("Events")]
        [SerializeField] private UnityEvent onFightStarted;
        [SerializeField] private UnityEvent onHit;
        [SerializeField] private UnityEvent onBroken;

        [ShowInInspector, ReadOnly]
        public int CurrentHitPoints { get; private set; }

        [ShowInInspector, ReadOnly]
        public bool IsFightActive { get; private set; }

        public int MaxHitPoints => hitPoints;
        public bool IsBroken => CurrentHitPoints <= 0;

        public event Action<WarmupBossWall, int, int> HealthChanged;
        public event Action<WarmupBossWall> Broken;

        private Collider[] _colliders;
        private Tween _hitTween;
        private Vector3 _initialScale;

        private void Awake()
        {
            CacheReferences();
            ResetWall();
        }

        private void OnDisable()
        {
            _hitTween?.Kill();
            _hitTween = null;
        }

        public void ConfigureRuntime(
            int requiredHits,
            Material paperShardMaterial = null)
        {
            hitPoints = Mathf.Clamp(requiredHits, 1, 10);
            shardBurst?.ConfigureMaterial(paperShardMaterial);
            ResetWall();
        }

        public void ActivateFight()
        {
            if (IsBroken || IsFightActive)
            {
                return;
            }

            IsFightActive = true;
            HealthChanged?.Invoke(this, CurrentHitPoints, hitPoints);
            onFightStarted?.Invoke();
        }

        public void ReceivePunch(PunchContext context)
        {
            if (!IsFightActive || IsBroken)
            {
                return;
            }

            CurrentHitPoints--;
            PlayHitFeedback(context);
            HealthChanged?.Invoke(this, CurrentHitPoints, hitPoints);
            onHit?.Invoke();

            if (CurrentHitPoints <= 0)
            {
                BreakWall();
            }
        }

#if UNITY_EDITOR
        [Button("Reset Wall")]
#endif
        public void ResetWall()
        {
            CacheReferences();
            _hitTween?.Kill();
            _hitTween = null;
            transform.localScale = _initialScale;
            CurrentHitPoints = hitPoints;
            IsFightActive = false;
            SetCollidersEnabled(true);
            shardBurst?.ResetBurst(intactRenderer);

            if (intactRenderer != null)
            {
                intactRenderer.enabled = true;
            }
        }

        private void BreakWall()
        {
            IsFightActive = false;
            SetCollidersEnabled(false);
            shardBurst?.Play(intactRenderer);

            if (shardBurst == null && intactRenderer != null)
            {
                intactRenderer.enabled = false;
            }

            onBroken?.Invoke();
            Broken?.Invoke(this);
        }

        private void PlayHitFeedback(PunchContext context)
        {
            _hitTween?.Kill();
            transform.localScale = _initialScale;
            _hitTween = transform
                .DOPunchScale(
                    Vector3.one * hitPunchScale,
                    hitFeedbackDuration,
                    4,
                    0.45f)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            if (punchVfx != null)
            {
                punchVfx.transform.SetPositionAndRotation(
                    context.Point,
                    Quaternion.LookRotation(-context.Direction));
                punchVfx.Play();
            }

            if (punchAudio != null)
            {
                punchAudio.Play();
            }
        }

        private void CacheReferences()
        {
            if (_initialScale == Vector3.zero)
            {
                _initialScale = transform.localScale;
            }

            if (intactRenderer == null)
            {
                intactRenderer = GetComponentInChildren<Renderer>(true);
            }

            if (shardBurst == null)
            {
                shardBurst = GetComponent<WarmupPaperShardBurst>();
                if (shardBurst == null)
                {
                    shardBurst = gameObject.AddComponent<WarmupPaperShardBurst>();
                }
            }

            if (_colliders == null)
            {
                _colliders = GetComponentsInChildren<Collider>(true);
            }
        }

        private void SetCollidersEnabled(bool isEnabled)
        {
            if (_colliders == null)
            {
                return;
            }

            for (int i = 0; i < _colliders.Length; i++)
            {
                _colliders[i].enabled = isEnabled;
            }
        }

#if UNITY_EDITOR
        [Button("Auto Assign References")]
        private void AutoAssignReferences()
        {
            intactRenderer = GetComponentInChildren<Renderer>(true);
            shardBurst = GetComponent<WarmupPaperShardBurst>();
            if (shardBurst == null)
            {
                shardBurst = gameObject.AddComponent<WarmupPaperShardBurst>();
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
