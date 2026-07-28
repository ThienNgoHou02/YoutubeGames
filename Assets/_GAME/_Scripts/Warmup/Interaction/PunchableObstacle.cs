using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace GameYT.Warmup
{
    [DisallowMultipleComponent]
    public sealed class PunchableObstacle : MonoBehaviour, IPunchable
    {
        [Title("References")]
        [SerializeField] private GameObject intactVisual;
        [SerializeField] private GameObject brokenVisual;
        [SerializeField] private ParticleSystem impactVfx;
        [SerializeField] private AudioSource impactAudio;

        [Title("Feedback")]
        [MinValue(0f)]
        [SerializeField] private float punchScale = 0.18f;

        [MinValue(0.01f)]
        [SerializeField] private float punchDuration = 0.18f;

        [SerializeField] private UnityEvent onPunched;

        [Title("Runtime Debug")]
        [ShowInInspector, ReadOnly]
        public bool IsBroken { get; private set; }

        private Collider[] _colliders;
        private Tween _feedbackTween;
        private Vector3 _initialScale;

        private void Awake()
        {
            _initialScale = transform.localScale;
            _colliders = GetComponentsInChildren<Collider>(true);
            ResetObstacle();
        }

        private void OnDisable()
        {
            _feedbackTween?.Kill();
            _feedbackTween = null;
        }

        public void ReceivePunch(PunchContext context)
        {
            if (IsBroken)
            {
                return;
            }

            IsBroken = true;
            SetCollidersEnabled(false);

            _feedbackTween?.Kill();
            if (intactVisual == null && brokenVisual == null)
            {
                _feedbackTween = DOTween.Sequence()
                    .Append(transform.DOPunchScale(
                        Vector3.one * punchScale,
                        punchDuration,
                        5,
                        0.5f))
                    .Append(transform.DOScale(Vector3.zero, 0.12f))
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }
            else
            {
                _feedbackTween = transform
                    .DOPunchScale(Vector3.one * punchScale, punchDuration, 5, 0.5f)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }

            if (impactVfx != null)
            {
                impactVfx.transform.SetPositionAndRotation(
                    context.Point,
                    Quaternion.LookRotation(-context.Direction));
                impactVfx.Play();
            }

            if (impactAudio != null)
            {
                impactAudio.Play();
            }

            if (intactVisual != null)
            {
                intactVisual.SetActive(false);
            }

            if (brokenVisual != null)
            {
                brokenVisual.SetActive(true);
            }

            onPunched?.Invoke();
        }

        [Button("Reset Obstacle")]
        public void ResetObstacle()
        {
            if (_initialScale == Vector3.zero)
            {
                _initialScale = transform.localScale;
            }

            IsBroken = false;
            transform.localScale = _initialScale;

            if (_colliders == null)
            {
                _colliders = GetComponentsInChildren<Collider>(true);
            }

            SetCollidersEnabled(true);

            if (intactVisual != null)
            {
                intactVisual.SetActive(true);
            }

            if (brokenVisual != null)
            {
                brokenVisual.SetActive(false);
            }
        }

        private void SetCollidersEnabled(bool isEnabled)
        {
            for (int i = 0; i < _colliders.Length; i++)
            {
                _colliders[i].enabled = isEnabled;
            }
        }
    }
}
