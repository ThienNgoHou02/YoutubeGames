using UnityEngine;

namespace GameYT.Warmup
{
    [DisallowMultipleComponent]
    public sealed class WarmupCoinItem : MonoBehaviour
    {
        public bool IsCollected { get; private set; }

        private WarmupPlayerSfx _playerSfx;
        private Collider _trigger;

        private void Awake()
        {
            _trigger = GetComponent<Collider>();
            EnsureTrigger();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsCollected ||
                other.GetComponentInParent<WarmupPlayerController>() == null)
            {
                return;
            }

            IsCollected = true;
            _playerSfx?.PlayCoinPickup();
            gameObject.SetActive(false);
        }

        public void ConfigureRuntime(WarmupPlayerSfx playerSfx)
        {
            _playerSfx = playerSfx;
            if (_trigger == null)
            {
                _trigger = GetComponent<Collider>();
            }

            EnsureTrigger();
        }

        private void EnsureTrigger()
        {
            if (_trigger != null)
            {
                _trigger.enabled = true;
                _trigger.isTrigger = true;
            }
        }
    }
}
