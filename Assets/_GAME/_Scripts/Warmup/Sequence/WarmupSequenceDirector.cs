using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameYT.Warmup
{
    [DisallowMultipleComponent]
    public sealed class WarmupSequenceDirector : MonoBehaviour
    {
        [Title("References")]
        [Required]
        [SerializeField] private WarmupSequenceAsset sequence;

        [Required]
        [SerializeField] private WarmupPlayerController player;

        [Title("Playback")]
        [SerializeField] private bool playOnStart = true;

        [SerializeField] private bool loop;

        [Title("Runtime Debug")]
        [ShowInInspector, ReadOnly]
        public float ElapsedTime { get; private set; }

        [ShowInInspector, ReadOnly]
        public bool IsPlaying { get; private set; }

        public event Action<WarmupCue> CueStarted;
        public event Action<int> CountdownChanged;
        public event Action<WarmupCue, bool> CueFinished;
        public event Action<float> ProgressChanged;
        public event Action SequenceFinished;

        private int _nextCueIndex;
        private WarmupCue _activeCue;
        private int _lastCountdown = int.MinValue;
        private bool _activeCueSucceeded;

        private void OnEnable()
        {
            if (player != null)
            {
                player.ActionPerformed += HandlePlayerAction;
            }
        }

        private void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            if (player != null)
            {
                player.ActionPerformed -= HandlePlayerAction;
            }
        }

        private void Update()
        {
            if (!IsPlaying || sequence == null)
            {
                return;
            }

            ElapsedTime += Time.deltaTime;
            ProgressChanged?.Invoke(Mathf.Clamp01(ElapsedTime / sequence.Duration));

            TryStartDueCues();
            UpdateActiveCue();

            if (ElapsedTime >= sequence.Duration)
            {
                CompleteSequence();
            }
        }

        [Button("Play")]
        public void Play()
        {
            if (sequence == null)
            {
                Debug.LogError("WarmupSequenceDirector thiếu Sequence Asset.", this);
                return;
            }

            IsPlaying = true;
            if (player != null)
            {
                player.SetAutoRun(true);
            }
        }

        [Button("Pause")]
        public void Pause()
        {
            IsPlaying = false;
            if (player != null)
            {
                player.SetAutoRun(false);
            }
        }

        [Button("Restart")]
        public void Restart()
        {
            ElapsedTime = 0f;
            _nextCueIndex = 0;
            _activeCue = null;
            _lastCountdown = int.MinValue;
            _activeCueSucceeded = false;
            Play();
            ProgressChanged?.Invoke(0f);
        }

        private void TryStartDueCues()
        {
            while (_nextCueIndex < sequence.CueCount)
            {
                WarmupCue cue = sequence.GetCue(_nextCueIndex);
                if (ElapsedTime < cue.StartTime)
                {
                    break;
                }

                if (_activeCue != null)
                {
                    FinishActiveCue();
                }

                _activeCue = cue;
                _activeCueSucceeded = false;
                _lastCountdown = int.MinValue;
                _nextCueIndex++;

                if (player != null && cue.SpeedMultiplier > 0f)
                {
                    player.SetSpeedMultiplier(cue.SpeedMultiplier);
                }

                CueStarted?.Invoke(cue);
            }
        }

        private void UpdateActiveCue()
        {
            if (_activeCue == null)
            {
                return;
            }

            float cueElapsed = ElapsedTime - _activeCue.StartTime;
            float cueEnd = _activeCue.LeadTime + _activeCue.ActionWindow;

            if (cueElapsed >= cueEnd)
            {
                FinishActiveCue();
                return;
            }

            int countdown = CalculateCountdown(cueElapsed, _activeCue.LeadTime);
            if (countdown == _lastCountdown)
            {
                return;
            }

            _lastCountdown = countdown;
            CountdownChanged?.Invoke(countdown);
        }

        private static int CalculateCountdown(float cueElapsed, float leadTime)
        {
            if (cueElapsed >= leadTime)
            {
                return 0;
            }

            float normalizedRemaining = 1f - Mathf.Clamp01(cueElapsed / leadTime);
            return Mathf.Clamp(Mathf.CeilToInt(normalizedRemaining * 3f), 1, 3);
        }

        private void HandlePlayerAction(WarmupActionType action)
        {
            if (_activeCue == null || action != _activeCue.Action)
            {
                return;
            }

            float cueElapsed = ElapsedTime - _activeCue.StartTime;
            float earlyTolerance = Mathf.Min(0.25f, _activeCue.LeadTime);
            float windowStart = _activeCue.LeadTime - earlyTolerance;
            float windowEnd = _activeCue.LeadTime + _activeCue.ActionWindow;

            if (cueElapsed >= windowStart && cueElapsed <= windowEnd)
            {
                _activeCueSucceeded = true;
            }
        }

        private void FinishActiveCue()
        {
            WarmupCue finishedCue = _activeCue;
            _activeCue = null;
            CueFinished?.Invoke(finishedCue, _activeCueSucceeded);
        }

        private void CompleteSequence()
        {
            if (_activeCue != null)
            {
                FinishActiveCue();
            }

            IsPlaying = false;
            SequenceFinished?.Invoke();

            if (loop)
            {
                Restart();
            }
        }

#if UNITY_EDITOR
        public void SetupComponents(
            WarmupSequenceAsset sequenceAsset,
            WarmupPlayerController playerController)
        {
            sequence = sequenceAsset;
            player = playerController;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
