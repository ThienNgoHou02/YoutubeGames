using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameYT.Warmup
{
    public enum WarmupObstacleType
    {
        Jump = 0,
        PoseWall = 1,
        DuckBarrier = 2,
        LaneBlocker = 3,
        BossWall = 4
    }

    public enum WarmupLane
    {
        Left = -1,
        Center = 0,
        Right = 1
    }

    public enum WarmupObstacleCollisionMode
    {
        UsePrefab = 0,
        DisableAll = 1,
        TriggerAll = 2
    }

    [Serializable]
    public sealed class WarmupObstacleEvent
    {
        [BoxGroup("Encounter")]
        [HorizontalGroup("Encounter/Row", Width = 120f)]
        [MinValue(0f)]
        [LabelText("Time")]
        [SuffixLabel("sec", Overlay = true)]
        public float EncounterTime;

        [HorizontalGroup("Encounter/Row")]
        [LabelText("Type")]
        public WarmupObstacleType Type;

        [HorizontalGroup("Encounter/Row", Width = 120f)]
        [LabelText("Lane")]
        public WarmupLane Lane;

        [BoxGroup("Viewer Cue")]
        [HorizontalGroup("Viewer Cue/Action Row")]
        public WarmupActionType Action;

        [HorizontalGroup("Viewer Cue/Action Row")]
        [ValidateInput(nameof(HasReadableLabel), "Label phải ngắn, rõ và không được rỗng.")]
        [LabelText("HUD Label")]
        public string CueLabel = "JUMP!";

        [BoxGroup("Viewer Cue")]
        [MinValue(0f)]
        [SuffixLabel("sec before obstacle", Overlay = true)]
        [LabelText("Show Before")]
        public float CueLeadTime = 1.4f;

        [BoxGroup("Prefab Source")]
        [HorizontalGroup("Prefab Source/Selection")]
        [MinValue(0)]
        [Tooltip("Index trong mảng prefab cùng loại của Video Prefab Set.")]
        [LabelText("Variation")]
        public int PrefabVariation;

        [HorizontalGroup("Prefab Source/Selection")]
        [Tooltip("Nếu có giá trị, prefab này được ưu tiên hơn Video Prefab Set.")]
        [AssetsOnly]
        [LabelText("Override")]
        public GameObject PrefabOverride;

        [FoldoutGroup("Transform Override")]
        [LabelText("Position")]
        public Vector3 PositionOffset;

        [FoldoutGroup("Transform Override")]
        [LabelText("Rotation")]
        public Vector3 RotationOffset;

        [FoldoutGroup("Transform Override")]
        [LabelText("Scale")]
        public Vector3 ScaleMultiplier = Vector3.one;

        [BoxGroup("Behaviour")]
        [LabelText("Collider")]
        public WarmupObstacleCollisionMode CollisionMode =
            WarmupObstacleCollisionMode.UsePrefab;

        [BoxGroup("Behaviour")]
        [ShowIf(nameof(IsBossWall))]
        [Range(1, 10)]
        public int BossHitPoints = 4;

        [BoxGroup("Behaviour")]
        [ShowIf(nameof(IsBossWall))]
        [MinValue(0.4f)]
        public float BossStopDistance = 1.6f;

        public bool IsBossWall => Type == WarmupObstacleType.BossWall;

        public string InspectorLabel
        {
            get
            {
                return EncounterTime.ToString("00.0") + "s  •  " +
                       Type + "  •  " + Lane;
            }
        }

        private bool HasReadableLabel(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Length <= 18;
        }
    }
}
