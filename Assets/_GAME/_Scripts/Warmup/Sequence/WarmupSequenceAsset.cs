using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameYT.Warmup
{
    [Serializable]
    public sealed class WarmupCue
    {
        [MinValue(0f)]
        public float StartTime;

        [MinValue(0.1f)]
        public float LeadTime = 1f;

        [MinValue(0.1f)]
        public float ActionWindow = 1f;

        public WarmupActionType Action;

        [ValidateInput(nameof(HasShortLabel), "Cue label nên ngắn và dễ đọc.")]
        public string Label;

        [MinValue(0f)]
        public float SpeedMultiplier = 1f;

        private bool HasShortLabel(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Length <= 16;
        }
    }

    [CreateAssetMenu(
        fileName = "WarmupSequence",
        menuName = "Game YT/Immersive Warmup/Sequence")]
    public sealed class WarmupSequenceAsset : ScriptableObject
    {
        [Title("Sequence")]
        [MinValue(1f)]
        [SerializeField] private float duration = 450f;

        [TableList(AlwaysExpanded = true)]
        [SerializeField] private WarmupCue[] cues = Array.Empty<WarmupCue>();

        public float Duration => duration;
        public int CueCount => cues.Length;

        public WarmupCue GetCue(int index)
        {
            return cues[index];
        }

#if UNITY_EDITOR
        public void SetData(float sequenceDuration, WarmupCue[] sequenceCues)
        {
            duration = Mathf.Max(1f, sequenceDuration);
            cues = sequenceCues ?? Array.Empty<WarmupCue>();
            SortCues();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            SortCues();
        }

        private void SortCues()
        {
            Array.Sort(cues, CompareCueTime);
        }

        private static int CompareCueTime(WarmupCue left, WarmupCue right)
        {
            return left.StartTime.CompareTo(right.StartTime);
        }
#endif
    }
}
