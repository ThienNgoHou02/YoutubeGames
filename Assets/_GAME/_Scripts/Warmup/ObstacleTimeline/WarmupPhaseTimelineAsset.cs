using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameYT.Warmup
{
    public enum WarmupRunSpeedSource
    {
        PlayerConfig = 0,
        PhaseOverride = 1
    }

    [CreateAssetMenu(
        fileName = "StepTimeline",
        menuName = "Game YT/Obstacle Timeline/Phase Timeline")]
    public sealed class WarmupPhaseTimelineAsset : ScriptableObject
    {
        [Title("Phase Setup", "Cấu hình dùng chung, không phụ thuộc prefab của từng video.")]
        [BoxGroup("Phase Setup/Identity")]
        [HorizontalGroup("Phase Setup/Identity/Main", Width = 120f)]
        [LabelText("Step")]
        [Range(1, 6)]
        [SerializeField] private int stepNumber = 1;

        [HorizontalGroup("Phase Setup/Identity/Main")]
        [LabelText("Name")]
        [SerializeField]
        [ValidateInput(nameof(HasDisplayName), "Tên phase không được để trống.")]
        private string displayName = "Step 1 - Run & Jump";

        [BoxGroup("Phase Setup/Timing & Distance")]
        [HorizontalGroup("Phase Setup/Timing & Distance/Row")]
        [LabelText("Duration")]
        [SuffixLabel("sec", Overlay = true)]
        [MinValue(1f)]
        [SerializeField] private float duration = 40f;

        [HorizontalGroup("Phase Setup/Timing & Distance/Row")]
        [LabelText("Speed Source")]
        [Tooltip("Player Config keeps one shared speed. Phase Override is only for a Step that needs its own speed.")]
        [SerializeField] private WarmupRunSpeedSource runSpeedSource =
            WarmupRunSpeedSource.PlayerConfig;

        [HorizontalGroup("Phase Setup/Timing & Distance/Row")]
        [LabelText("Run Speed")]
        [SuffixLabel("m/s", Overlay = true)]
        [MinValue(0.1f)]
        [ShowIf(nameof(UsesPhaseSpeedOverride))]
        [Tooltip("Tốc độ course và số mét mô phỏng trên HUD.")]
        [SerializeField] private float metersPerSecond = 6f;

        [HorizontalGroup("Phase Setup/Timing & Distance/Row")]
        [LabelText("Start Gap")]
        [SuffixLabel("m", Overlay = true)]
        [MinValue(0f)]
        [Tooltip("Khoảng trống trước obstacle đầu tiên.")]
        [SerializeField] private float courseStartPadding = 6f;

        [BoxGroup("Phase Setup/Lanes & Visibility")]
        [HorizontalGroup("Phase Setup/Lanes & Visibility/Row")]
        [LabelText("Lane Width")]
        [SuffixLabel("m", Overlay = true)]
        [MinValue(0.1f)]
        [Tooltip("Phải trùng Lane Width của Player Config.")]
        [SerializeField] private float laneWidth = 2.2f;

        [HorizontalGroup("Phase Setup/Lanes & Visibility/Row")]
        [LabelText("Show Before")]
        [SuffixLabel("sec", Overlay = true)]
        [MinValue(0.5f)]
        [Tooltip("Obstacle được bật trước thời điểm encounter bao nhiêu giây.")]
        [SerializeField] private float visibilityLeadTime = 6f;

        [HorizontalGroup("Phase Setup/Lanes & Visibility/Row")]
        [LabelText("Hide After")]
        [SuffixLabel("sec", Overlay = true)]
        [MinValue(0.5f)]
        [Tooltip("Obstacle được ẩn sau encounter để giảm draw call.")]
        [SerializeField] private float visibilityTailTime = 3f;

        [Title(
            "Obstacle Timeline",
            "Mỗi item có thể thu gọn. Kéo handle bên trái để đổi thứ tự.")]
        [ListDrawerSettings(
            Expanded = true,
            DraggableItems = true,
            ShowIndexLabels = false,
            ShowPaging = false,
            ShowItemCount = true,
            ListElementLabelName = nameof(WarmupObstacleEvent.InspectorLabel))]
        [SerializeField] private WarmupObstacleEvent[] events =
            Array.Empty<WarmupObstacleEvent>();

        public int StepNumber => stepNumber;
        public string DisplayName => displayName;
        public float Duration => duration;
        public WarmupRunSpeedSource RunSpeedSource => runSpeedSource;
        public float MetersPerSecond => metersPerSecond;
        public float CourseStartPadding => courseStartPadding;
        public float LaneWidth => laneWidth;
        public float VisibilityLeadTime => visibilityLeadTime;
        public float VisibilityTailTime => visibilityTailTime;
        public int EventCount => events.Length;

        public WarmupObstacleEvent GetEvent(int index)
        {
            return events[index];
        }

        public float ResolveRunSpeed(float playerConfigSpeed)
        {
            if (runSpeedSource == WarmupRunSpeedSource.PhaseOverride)
            {
                return Mathf.Max(0.1f, metersPerSecond);
            }

            return Mathf.Max(0.1f, playerConfigSpeed);
        }

        private bool UsesPhaseSpeedOverride()
        {
            return runSpeedSource == WarmupRunSpeedSource.PhaseOverride;
        }

        private bool HasDisplayName(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

#if UNITY_EDITOR
        public void SetData(
            int number,
            string phaseName,
            float phaseDuration,
            float phaseMetersPerSecond,
            WarmupObstacleEvent[] timelineEvents)
        {
            stepNumber = Mathf.Clamp(number, 1, 6);
            displayName = phaseName;
            duration = Mathf.Max(1f, phaseDuration);
            runSpeedSource = WarmupRunSpeedSource.PlayerConfig;
            metersPerSecond = Mathf.Max(0.1f, phaseMetersPerSecond);
            events = timelineEvents ?? Array.Empty<WarmupObstacleEvent>();
            SortEvents();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            SortEvents();
        }

        private void SortEvents()
        {
            Array.Sort(events, CompareEventTime);
        }

        private static int CompareEventTime(
            WarmupObstacleEvent left,
            WarmupObstacleEvent right)
        {
            return left.EncounterTime.CompareTo(right.EncounterTime);
        }
#endif
    }
}
