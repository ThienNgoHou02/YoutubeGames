using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameYT.Warmup
{
    [DisallowMultipleComponent]
    public sealed class WarmupObstacleTimelineDirector : MonoBehaviour
    {
        [Title("Data")]
        [Required]
        [SerializeField] private WarmupPhaseTimelineAsset phase;

        [Required]
        [SerializeField] private WarmupObstaclePrefabSet prefabSet;

        [Title("References")]
        [Required]
        [SerializeField] private WarmupPlayerController player;

        [SerializeField] private Transform obstacleRoot;

        [Tooltip("Material 2 mặt dùng cho các mảnh vỡ boss wall.")]
        [SerializeField] private Material bossShardMaterial;

        [Title("Playback")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool loop;
        [SerializeField] private bool pauseTimelineDuringBoss = true;

        [Title("Runtime Debug")]
        [ShowInInspector, ReadOnly]
        public float ElapsedTime { get; private set; }

        [ShowInInspector, ReadOnly]
        public float DistanceMeters { get; private set; }

        [ShowInInspector, ReadOnly]
        public bool IsPlaying { get; private set; }

        [ShowInInspector, ReadOnly]
        public bool IsBossFight { get; private set; }

        [ShowInInspector, ReadOnly]
        public float ActiveRunSpeed { get; private set; }

        public WarmupPhaseTimelineAsset Phase => phase;

        public event Action<WarmupPhaseTimelineAsset> PhaseStarted;
        public event Action<WarmupObstacleEvent> CueStarted;
        public event Action<float> ProgressChanged;
        public event Action<float> DistanceChanged;
        public event Action<WarmupBossWall> BossFightStarted;
        public event Action<int, int> BossHealthChanged;
        public event Action BossFightFinished;
        public event Action PhaseFinished;

        private sealed class RuntimeObstacle
        {
            public WarmupObstacleEvent Data;
            public GameObject Instance;
            public WarmupBossWall BossWall;
        }

        private readonly List<RuntimeObstacle> _runtimeObstacles =
            new List<RuntimeObstacle>(32);

        private int _nextCueIndex;
        private int _nextBossIndex;
        private WarmupTimelineCourseFrame _courseFrame;
        private WarmupBossWall _activeBossWall;
        private bool _courseBuilt;

        private void Awake()
        {
            if (obstacleRoot == null)
            {
                var root = new GameObject("Runtime Obstacles");
                root.transform.SetParent(transform, false);
                obstacleRoot = root.transform;
            }
        }

        private void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        private void Update()
        {
            if (!IsPlaying || phase == null)
            {
                return;
            }

            UpdateBossEncounter();
            if (IsBossFight && pauseTimelineDuringBoss)
            {
                return;
            }

            ElapsedTime += Time.deltaTime;
            DistanceMeters = Mathf.Min(
                ElapsedTime * ActiveRunSpeed,
                phase.Duration * ActiveRunSpeed);

            ProgressChanged?.Invoke(
                Mathf.Clamp01(ElapsedTime / phase.Duration));
            DistanceChanged?.Invoke(DistanceMeters);

            StartDueCues();
            UpdateObstacleVisibility();
            if (ElapsedTime >= phase.Duration)
            {
                CompletePhase();
            }
        }

        private void OnDisable()
        {
            UnsubscribeBossWalls();
            IsPlaying = false;
            IsBossFight = false;
            _activeBossWall = null;

            if (player != null)
            {
                player.SetAutoRun(false);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeBossWalls();
        }

#if UNITY_EDITOR
        [Button("Play")]
#endif
        public void Play()
        {
            if (!ValidateReferences())
            {
                return;
            }

            if (!_courseBuilt)
            {
                BuildCourse();
            }

            SubscribeBossWalls();
            float baseSpeed = Mathf.Max(0.01f, player.ConfiguredRunSpeed);
            ActiveRunSpeed = phase.ResolveRunSpeed(baseSpeed);
            player.SetSpeedMultiplier(ActiveRunSpeed / baseSpeed);
            player.SetAutoRun(true);
            IsPlaying = true;
            UpdateObstacleVisibility();
            PhaseStarted?.Invoke(phase);
            ProgressChanged?.Invoke(
                Mathf.Clamp01(ElapsedTime / phase.Duration));
            DistanceChanged?.Invoke(DistanceMeters);
        }

#if UNITY_EDITOR
        [Button("Pause")]
#endif
        public void Pause()
        {
            IsPlaying = false;
            player?.SetAutoRun(false);
        }

#if UNITY_EDITOR
        [Button("Restart")]
#endif
        public void Restart()
        {
            IsPlaying = false;
            IsBossFight = false;
            _activeBossWall = null;
            ElapsedTime = 0f;
            DistanceMeters = 0f;
            _nextCueIndex = 0;
            _nextBossIndex = 0;
            RebuildCourse();
            Play();
        }

#if UNITY_EDITOR
        [Button("Rebuild Course")]
#endif
        public void RebuildCourse()
        {
            ClearCourse();
            BuildCourse();
        }

        private bool ValidateReferences()
        {
            if (phase == null || prefabSet == null || player == null)
            {
                Debug.LogError(
                    "Obstacle Timeline cần Phase, Video Prefab Set và Player.",
                    this);
                return false;
            }

            return true;
        }

        private void BuildCourse()
        {
            if (!ValidateReferences())
            {
                return;
            }

            UnsubscribeBossWalls();
            _runtimeObstacles.Clear();
            ActiveRunSpeed = phase.ResolveRunSpeed(player.ConfiguredRunSpeed);
            _courseFrame =
                new WarmupTimelineCourseFrame(player.transform);
            int poseOrderIndex = 0;

            for (int i = 0; i < phase.EventCount; i++)
            {
                WarmupObstacleEvent obstacleEvent = phase.GetEvent(i);
                int currentPoseOrderIndex =
                    obstacleEvent.IsPoseWall ? poseOrderIndex++ : -1;
                GameObject prefab =
                    obstacleEvent.PrefabOverride != null
                        ? obstacleEvent.PrefabOverride
                        : prefabSet.GetRandomPrefab(obstacleEvent.Type);

                if (prefab == null)
                {
                    Debug.LogWarning(
                        "Thiếu prefab " + obstacleEvent.Type +
                        " tại " + obstacleEvent.EncounterTime + "s.",
                        phase);
                    _runtimeObstacles.Add(new RuntimeObstacle
                    {
                        Data = obstacleEvent
                    });
                    continue;
                }

                Vector3 position =
                    WarmupTimelinePositionCalculator
                        .CalculateObstaclePosition(
                            _courseFrame,
                            obstacleEvent.EncounterTime,
                            ActiveRunSpeed,
                            phase.CourseStartPadding,
                            obstacleEvent.Lane,
                            phase.LaneWidth,
                            obstacleEvent.PositionOffset);
                Quaternion rotation =
                    WarmupTimelinePositionCalculator
                        .CalculateObstacleRotation(
                            _courseFrame,
                            obstacleEvent.RotationOffset);

                GameObject instance = Instantiate(
                    prefab,
                    position,
                    rotation,
                    obstacleRoot);
                instance.name =
                    obstacleEvent.EncounterTime.ToString("000.0") +
                    "s - " + obstacleEvent.Type;
                instance.transform.localScale =
                    Vector3.Scale(
                        instance.transform.localScale,
                        obstacleEvent.ScaleMultiplier);
                ApplyPoseSprite(
                    instance,
                    obstacleEvent,
                    currentPoseOrderIndex);
                ApplyCollisionMode(instance, obstacleEvent.CollisionMode);

                WarmupBossWall bossWall = null;
                if (obstacleEvent.Type == WarmupObstacleType.BossWall)
                {
                    bossWall = instance.GetComponent<WarmupBossWall>();
                    if (bossWall == null)
                    {
                        bossWall = instance.AddComponent<WarmupBossWall>();
                    }

                    bossWall.ConfigureRuntime(
                        obstacleEvent.BossHitPoints,
                        bossShardMaterial);
                }

                _runtimeObstacles.Add(new RuntimeObstacle
                {
                    Data = obstacleEvent,
                    Instance = instance,
                    BossWall = bossWall
                });
                instance.SetActive(false);
            }

            SubscribeBossWalls();
            _courseBuilt = true;
        }

        private void ApplyPoseSprite(
            GameObject instance,
            WarmupObstacleEvent obstacleEvent,
            int poseOrderIndex)
        {
            if (!obstacleEvent.IsPoseWall)
            {
                return;
            }

            Sprite poseSprite = phase.ResolvePoseSprite(
                poseOrderIndex,
                obstacleEvent.PoseSprite);
            if (poseSprite == null)
            {
                return;
            }

            if (!instance.TryGetComponent(
                    out WarmupPoseSpriteTarget poseTarget))
            {
                Debug.LogWarning(
                    "Prefab Pose Wall thiếu WarmupPoseSpriteTarget tại " +
                    obstacleEvent.EncounterTime.ToString("0.0") + "s.",
                    instance);
                return;
            }

            poseTarget.SetPose(poseSprite);
        }

        private void ClearCourse()
        {
            UnsubscribeBossWalls();

            for (int i = 0; i < _runtimeObstacles.Count; i++)
            {
                GameObject instance = _runtimeObstacles[i].Instance;
                if (instance == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(instance);
                }
                else
                {
                    DestroyImmediate(instance);
                }
            }

            _runtimeObstacles.Clear();
            _courseBuilt = false;
        }

        private void StartDueCues()
        {
            while (_nextCueIndex < phase.EventCount)
            {
                WarmupObstacleEvent obstacleEvent =
                    phase.GetEvent(_nextCueIndex);
                float cueTime =
                    obstacleEvent.EncounterTime - obstacleEvent.CueLeadTime;
                if (ElapsedTime < cueTime)
                {
                    break;
                }

                _nextCueIndex++;
                CueStarted?.Invoke(obstacleEvent);
            }
        }

        private void UpdateBossEncounter()
        {
            if (IsBossFight || player == null)
            {
                return;
            }

            while (_nextBossIndex < _runtimeObstacles.Count)
            {
                RuntimeObstacle runtime = _runtimeObstacles[_nextBossIndex];
                if (runtime.Data.Type != WarmupObstacleType.BossWall ||
                    runtime.BossWall == null ||
                    runtime.BossWall.IsBroken)
                {
                    _nextBossIndex++;
                    continue;
                }

                float forwardDistance = Vector3.Dot(
                    runtime.Instance.transform.position - player.transform.position,
                    _courseFrame.Forward);
                if (forwardDistance < -0.5f)
                {
                    _nextBossIndex++;
                    continue;
                }

                if (forwardDistance > runtime.Data.BossStopDistance)
                {
                    return;
                }

                _activeBossWall = runtime.BossWall;
                IsBossFight = true;
                player.SetAutoRun(false);
                _activeBossWall.ActivateFight();
                BossFightStarted?.Invoke(_activeBossWall);
                BossHealthChanged?.Invoke(
                    _activeBossWall.CurrentHitPoints,
                    _activeBossWall.MaxHitPoints);
                return;
            }
        }

        private void UpdateObstacleVisibility()
        {
            for (int i = 0; i < _runtimeObstacles.Count; i++)
            {
                RuntimeObstacle runtime = _runtimeObstacles[i];
                if (runtime.Instance == null)
                {
                    continue;
                }

                bool shouldBeVisible =
                    WarmupTimelinePositionCalculator.IsObstacleVisible(
                        ElapsedTime,
                        runtime.Data.EncounterTime,
                        phase.VisibilityLeadTime,
                        phase.VisibilityTailTime,
                        runtime.BossWall != null &&
                        runtime.BossWall.IsFightActive);

                if (runtime.Instance.activeSelf != shouldBeVisible)
                {
                    runtime.Instance.SetActive(shouldBeVisible);
                }
            }
        }

        private void HandleBossHealthChanged(
            WarmupBossWall wall,
            int currentHealth,
            int maxHealth)
        {
            if (wall != _activeBossWall)
            {
                return;
            }

            BossHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void HandleBossBroken(WarmupBossWall wall)
        {
            if (wall != _activeBossWall)
            {
                return;
            }

            _activeBossWall = null;
            IsBossFight = false;
            _nextBossIndex++;
            player.SetAutoRun(true);
            BossFightFinished?.Invoke();
        }

        private void CompletePhase()
        {
            ElapsedTime = phase.Duration;
            DistanceMeters = phase.Duration * ActiveRunSpeed;
            ProgressChanged?.Invoke(1f);
            DistanceChanged?.Invoke(DistanceMeters);
            IsPlaying = false;
            player.SetAutoRun(false);
            PhaseFinished?.Invoke();

            if (loop)
            {
                Restart();
            }
        }

        private void UnsubscribeBossWalls()
        {
            for (int i = 0; i < _runtimeObstacles.Count; i++)
            {
                WarmupBossWall wall = _runtimeObstacles[i].BossWall;
                if (wall == null)
                {
                    continue;
                }

                wall.HealthChanged -= HandleBossHealthChanged;
                wall.Broken -= HandleBossBroken;
            }
        }

        private void SubscribeBossWalls()
        {
            for (int i = 0; i < _runtimeObstacles.Count; i++)
            {
                WarmupBossWall wall = _runtimeObstacles[i].BossWall;
                if (wall == null)
                {
                    continue;
                }

                wall.HealthChanged -= HandleBossHealthChanged;
                wall.Broken -= HandleBossBroken;
                wall.HealthChanged += HandleBossHealthChanged;
                wall.Broken += HandleBossBroken;
            }
        }

        private static void ApplyCollisionMode(
            GameObject instance,
            WarmupObstacleCollisionMode collisionMode)
        {
            if (collisionMode == WarmupObstacleCollisionMode.UsePrefab)
            {
                return;
            }

            Collider[] colliders =
                instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (collisionMode == WarmupObstacleCollisionMode.DisableAll)
                {
                    colliders[i].enabled = false;
                }
                else
                {
                    colliders[i].enabled = true;
                    colliders[i].isTrigger = true;
                }
            }
        }

#if UNITY_EDITOR
        public void SetupComponents(
            WarmupPhaseTimelineAsset phaseAsset,
            WarmupObstaclePrefabSet videoPrefabSet,
            WarmupPlayerController playerController,
            Transform runtimeObstacleRoot,
            Material paperShardMaterial)
        {
            phase = phaseAsset;
            prefabSet = videoPrefabSet;
            player = playerController;
            obstacleRoot = runtimeObstacleRoot;
            bossShardMaterial = paperShardMaterial;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
