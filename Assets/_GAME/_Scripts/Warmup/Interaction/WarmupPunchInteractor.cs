using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameYT.Warmup
{
    [DisallowMultipleComponent]
    public sealed class WarmupPunchInteractor : MonoBehaviour
    {
        private const int HitBufferSize = 16;

        [Title("References")]
        [Required]
        [SerializeField] private Transform punchOrigin;

        [Title("Config")]
        [Required]
        [SerializeField] private WarmupPlayerConfig config;

        [SerializeField] private LayerMask punchableLayers = ~0;

        [Title("Runtime Debug")]
        [ShowInInspector, ReadOnly]
        public bool IsOnCooldown => Time.time < _nextPunchTime;

        public event Action<IPunchable> PunchConnected;

        private readonly Collider[] _hitBuffer = new Collider[HitBufferSize];
        private readonly HashSet<IPunchable> _processedTargets = new HashSet<IPunchable>();
        private float _nextPunchTime;

        public bool TryPunch()
        {
            if (config == null || punchOrigin == null || Time.time < _nextPunchTime)
            {
                return false;
            }

            _nextPunchTime = Time.time + config.PunchCooldown;
            _processedTargets.Clear();

            Vector3 center = punchOrigin.position + punchOrigin.forward * config.PunchRange;
            int hitCount = Physics.OverlapSphereNonAlloc(
                center,
                config.PunchRadius,
                _hitBuffer,
                punchableLayers,
                QueryTriggerInteraction.Collide);

            bool connected = false;
            PunchContext context = new PunchContext(
                center,
                punchOrigin.forward,
                config.PunchStrength);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _hitBuffer[i];
                IPunchable punchable = hit.GetComponentInParent<IPunchable>();
                if (punchable == null || !_processedTargets.Add(punchable))
                {
                    continue;
                }

                punchable.ReceivePunch(context);
                PunchConnected?.Invoke(punchable);
                connected = true;
            }

            return connected;
        }

#if UNITY_EDITOR
        public void SetupComponents(Transform origin, WarmupPlayerConfig playerConfig)
        {
            punchOrigin = origin;
            config = playerConfig;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        private void OnDrawGizmosSelected()
        {
            if (punchOrigin == null || config == null)
            {
                return;
            }

            Gizmos.color = Color.red;
            Vector3 center = punchOrigin.position + punchOrigin.forward * config.PunchRange;
            Gizmos.DrawWireSphere(center, config.PunchRadius);
        }
    }
}
