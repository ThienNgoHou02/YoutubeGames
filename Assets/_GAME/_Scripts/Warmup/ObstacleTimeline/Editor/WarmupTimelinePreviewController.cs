#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GameYT.Warmup.Editor
{
    /// <summary>
    /// Owns temporary Scene View objects for manual timeline scrubbing.
    /// </summary>
    internal sealed class WarmupTimelinePreviewController : IDisposable
    {
        private const HideFlags PreviewHideFlags =
            HideFlags.HideInHierarchy |
            HideFlags.NotEditable |
            HideFlags.DontSaveInEditor |
            HideFlags.DontSaveInBuild;

        private sealed class PreviewObstacle
        {
            public WarmupObstacleEvent Data;
            public GameObject Instance;
        }

        private readonly List<PreviewObstacle> _obstacles =
            new List<PreviewObstacle>(32);

        private GUIStyle _previewLabelStyle;

        private WarmupPhaseTimelineAsset _phase;
        private WarmupPlayerConfig _playerConfig;
        private WarmupObstaclePrefabSet _prefabSet;
        private WarmupPlayerController _sourcePlayer;
        private GameObject _previewRoot;
        private GameObject _playerPreview;
        private Scene _previewScene;
        private WarmupTimelineCourseFrame _courseFrame;
        private Vector3 _sourcePlayerPosition;
        private Quaternion _sourcePlayerRotation;
        private Vector3 _sourcePlayerScale;
        private float _runSpeed;
        private float _currentTime;
        private int _phaseDirtyCount;
        private int _playerConfigDirtyCount;
        private int _prefabSetDirtyCount;
        private bool _disposed;

        public WarmupTimelinePreviewController()
        {
            AssemblyReloadEvents.beforeAssemblyReload += HandleBeforeAssemblyReload;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorSceneManager.sceneOpening += HandleSceneOpening;
            EditorSceneManager.activeSceneChangedInEditMode +=
                HandleActiveSceneChanged;
            EditorSceneManager.sceneClosed += HandleSceneClosed;
            SceneView.duringSceneGui += DrawScenePreviewOverlay;
        }

        public bool IsActive => _phase != null;

        public float CurrentTime => _currentTime;
        public string StatusMessage { get; private set; } = string.Empty;
        public MessageType StatusType { get; private set; } =
            MessageType.None;

        public bool StartPreview(
            WarmupPhaseTimelineAsset phase,
            WarmupPlayerConfig playerConfig,
            WarmupObstaclePrefabSet prefabSet,
            float playheadTime)
        {
            StopPreview();
            StatusMessage = string.Empty;
            StatusType = MessageType.None;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                StatusMessage =
                    "Không thể Start Preview khi Unity đang vào Play Mode.";
                StatusType = MessageType.Warning;
                return false;
            }

            if (phase == null || playerConfig == null || prefabSet == null)
            {
                StatusMessage =
                    "Preview cần Timeline, Player Config và Obstacle Prefab Set.";
                StatusType = MessageType.Warning;
                return false;
            }

            WarmupPlayerController sourcePlayer = FindScenePlayer();
            if (sourcePlayer == null)
            {
                StatusMessage =
                    "Không tìm thấy WarmupPlayerController trong scene đang mở. " +
                    "Hãy mở WarnUp scene hoặc Apply Step trước.";
                StatusType = MessageType.Warning;
                return false;
            }

            Scene scene = sourcePlayer.gameObject.scene;

            try
            {
                _phase = phase;
                _playerConfig = playerConfig;
                _prefabSet = prefabSet;
                _sourcePlayer = sourcePlayer;
                _previewScene = scene;
                _runSpeed = phase.ResolveRunSpeed(playerConfig.AutoRunSpeed);
                _courseFrame =
                    new WarmupTimelineCourseFrame(sourcePlayer.transform);
                _sourcePlayerPosition = sourcePlayer.transform.position;
                _sourcePlayerRotation = sourcePlayer.transform.rotation;
                _sourcePlayerScale = sourcePlayer.transform.lossyScale;

                CreatePreviewRoot();
                CreatePlayerPreview();
                CreateObstaclePreviews();

                _phaseDirtyCount = EditorUtility.GetDirtyCount(phase);
                _playerConfigDirtyCount =
                    EditorUtility.GetDirtyCount(playerConfig);
                _prefabSetDirtyCount =
                    EditorUtility.GetDirtyCount(prefabSet);

                _currentTime = -1f;
                UpdatePreview(playheadTime, true);
                return IsActive;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                StatusMessage =
                    "Scene Scrub Preview gặp lỗi và đã được dọn sạch. " +
                    "Xem Console để biết chi tiết.";
                StopPreview(StatusMessage, MessageType.Error);
                return false;
            }
        }

        public bool SetTime(float playheadTime)
        {
            if (!IsActive)
            {
                return false;
            }

            try
            {
                return UpdatePreview(playheadTime, false);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                StopPreview(
                    "Preview bị lỗi khi scrub và đã được dọn sạch.",
                    MessageType.Error);
                return false;
            }
        }

        public bool ValidateSources(
            WarmupPhaseTimelineAsset phase,
            WarmupPlayerConfig playerConfig,
            WarmupObstaclePrefabSet prefabSet)
        {
            if (!IsActive)
            {
                return false;
            }

            if (_previewRoot == null ||
                _playerPreview == null ||
                HasMissingObstacleReference())
            {
                StopPreview(
                    "Preview mất reference và đã được dọn sạch.",
                    MessageType.Error);
                return false;
            }

            bool changed =
                phase != _phase ||
                playerConfig != _playerConfig ||
                prefabSet != _prefabSet ||
                _sourcePlayer == null ||
                EditorUtility.GetDirtyCount(_phase) != _phaseDirtyCount ||
                EditorUtility.GetDirtyCount(_playerConfig) !=
                _playerConfigDirtyCount ||
                EditorUtility.GetDirtyCount(_prefabSet) !=
                _prefabSetDirtyCount;

            if (!changed && _sourcePlayer != null)
            {
                Transform playerTransform = _sourcePlayer.transform;
                changed =
                    playerTransform.position != _sourcePlayerPosition ||
                    playerTransform.rotation != _sourcePlayerRotation ||
                    playerTransform.lossyScale != _sourcePlayerScale;
            }

            if (!changed)
            {
                return true;
            }

            StopPreview(
                "Preview đã dừng vì Timeline, prefab hoặc Player source thay đổi.",
                MessageType.Info);
            return false;
        }

        private bool HasMissingObstacleReference()
        {
            for (int i = 0; i < _obstacles.Count; i++)
            {
                if (_obstacles[i].Instance == null)
                {
                    return true;
                }
            }

            return false;
        }

        public void StopPreview(
            string statusMessage = null,
            MessageType statusType = MessageType.None)
        {
            bool hadPreview = _previewRoot != null;

            if (_previewRoot != null)
            {
                Object.DestroyImmediate(_previewRoot);
            }

            _obstacles.Clear();
            _phase = null;
            _playerConfig = null;
            _prefabSet = null;
            _sourcePlayer = null;
            _previewRoot = null;
            _playerPreview = null;
            _previewScene = default;
            _currentTime = 0f;

            if (!string.IsNullOrEmpty(statusMessage))
            {
                StatusMessage = statusMessage;
                StatusType = statusType;
            }

            if (hadPreview)
            {
                SceneView.RepaintAll();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            StopPreview();
            AssemblyReloadEvents.beforeAssemblyReload -=
                HandleBeforeAssemblyReload;
            EditorApplication.playModeStateChanged -=
                HandlePlayModeStateChanged;
            EditorSceneManager.sceneOpening -= HandleSceneOpening;
            EditorSceneManager.activeSceneChangedInEditMode -=
                HandleActiveSceneChanged;
            EditorSceneManager.sceneClosed -= HandleSceneClosed;
            SceneView.duringSceneGui -= DrawScenePreviewOverlay;
        }

        private void CreatePreviewRoot()
        {
            _previewRoot =
                EditorUtility.CreateGameObjectWithHideFlags(
                    WarmupTimelinePreviewCleanup.PreviewRootName,
                    PreviewHideFlags);
            SceneManager.MoveGameObjectToScene(_previewRoot, _previewScene);
        }

        private void CreatePlayerPreview()
        {
            GameObject playerSource =
                PrefabUtility.GetCorrespondingObjectFromOriginalSource(
                    _sourcePlayer.gameObject);
            if (playerSource == null)
            {
                playerSource = _sourcePlayer.gameObject;
            }

            _playerPreview = Object.Instantiate(playerSource);
            _playerPreview.SetActive(false);
            _playerPreview.name = "[Preview] Player";
            _playerPreview.transform.SetParent(
                _previewRoot.transform,
                true);
            _playerPreview.transform.SetPositionAndRotation(
                _courseFrame.Origin,
                _courseFrame.Rotation);
            _playerPreview.transform.localScale = _sourcePlayerScale;
            DisableInteractiveComponents(_playerPreview);
            ApplyPreviewHideFlags(_playerPreview);
            _playerPreview.SetActive(true);
        }

        private void CreateObstaclePreviews()
        {
            int missingPrefabCount = 0;
            int poseOrderIndex = 0;
            var missingPrefabDetails = new StringBuilder(128);

            for (int i = 0; i < _phase.EventCount; i++)
            {
                WarmupObstacleEvent obstacleEvent = _phase.GetEvent(i);
                int currentPoseOrderIndex =
                    obstacleEvent.IsPoseWall ? poseOrderIndex++ : -1;
                GameObject prefab =
                    obstacleEvent.PrefabOverride != null
                        ? obstacleEvent.PrefabOverride
                        : _prefabSet.GetRandomPrefab(obstacleEvent.Type);

                if (prefab == null)
                {
                    missingPrefabCount++;
                    if (missingPrefabDetails.Length < 100)
                    {
                        if (missingPrefabDetails.Length > 0)
                        {
                            missingPrefabDetails.Append(", ");
                        }

                        missingPrefabDetails.Append(
                            obstacleEvent.Type);
                        missingPrefabDetails.Append(" @ ");
                        missingPrefabDetails.Append(
                            obstacleEvent.EncounterTime.ToString("0.0"));
                        missingPrefabDetails.Append('s');
                    }

                    continue;
                }

                GameObject instance = Object.Instantiate(prefab);
                instance.SetActive(false);
                instance.name =
                    "[Preview] " +
                    obstacleEvent.EncounterTime.ToString("000.0") +
                    "s - " + obstacleEvent.Type;
                instance.transform.SetParent(
                    _previewRoot.transform,
                    true);

                Vector3 position =
                    WarmupTimelinePositionCalculator
                        .CalculateObstaclePosition(
                            _courseFrame,
                            obstacleEvent.EncounterTime,
                            _runSpeed,
                            _phase.CourseStartPadding,
                            obstacleEvent.Lane,
                            _phase.LaneWidth,
                            obstacleEvent.PositionOffset);
                Quaternion rotation =
                    WarmupTimelinePositionCalculator
                        .CalculateObstacleRotation(
                            _courseFrame,
                            obstacleEvent.RotationOffset);
                instance.transform.SetPositionAndRotation(
                    position,
                    rotation);
                instance.transform.localScale = Vector3.Scale(
                    instance.transform.localScale,
                    obstacleEvent.ScaleMultiplier);

                ApplyPoseSprite(
                    instance,
                    obstacleEvent,
                    currentPoseOrderIndex);
                DisableInteractiveComponents(instance);
                ApplyPreviewHideFlags(instance);
                _obstacles.Add(new PreviewObstacle
                {
                    Data = obstacleEvent,
                    Instance = instance
                });
            }

            if (missingPrefabCount <= 0)
            {
                StatusMessage =
                    "Preview đang hoạt động. Object tạm không được lưu vào scene.";
                StatusType = MessageType.Info;
                return;
            }

            StatusMessage =
                "Đã bỏ qua " + missingPrefabCount +
                " obstacle thiếu prefab: " + missingPrefabDetails + ".";
            StatusType = MessageType.Warning;
            Debug.LogWarning(StatusMessage, _phase);
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

            Sprite poseSprite = _phase.ResolvePoseSprite(
                poseOrderIndex,
                obstacleEvent.PoseSprite);
            if (poseSprite == null)
            {
                return;
            }

            if (instance.TryGetComponent(
                    out WarmupPoseSpriteTarget poseTarget))
            {
                poseTarget.SetPose(poseSprite);
            }
        }

        private bool UpdatePreview(float playheadTime, bool force)
        {
            if (_previewRoot == null || _playerPreview == null)
            {
                StopPreview(
                    "Preview mất reference và đã được dọn sạch.",
                    MessageType.Error);
                return false;
            }

            float clampedTime =
                Mathf.Clamp(playheadTime, 0f, _phase.Duration);
            if (!force && Mathf.Approximately(_currentTime, clampedTime))
            {
                return false;
            }

            _currentTime = clampedTime;
            _playerPreview.transform.position =
                WarmupTimelinePositionCalculator.CalculatePlayerPosition(
                    _courseFrame,
                    _currentTime,
                    _runSpeed);

            for (int i = 0; i < _obstacles.Count; i++)
            {
                PreviewObstacle previewObstacle = _obstacles[i];
                if (previewObstacle.Instance == null)
                {
                    StopPreview(
                        "Preview mất obstacle reference và đã được dọn sạch.",
                        MessageType.Error);
                    return false;
                }

                bool shouldBeVisible =
                    WarmupTimelinePositionCalculator.IsObstacleVisible(
                        _currentTime,
                        previewObstacle.Data.EncounterTime,
                        _phase.VisibilityLeadTime,
                        _phase.VisibilityTailTime,
                        false);
                if (previewObstacle.Instance.activeSelf != shouldBeVisible)
                {
                    previewObstacle.Instance.SetActive(shouldBeVisible);
                }
            }

            SceneView.RepaintAll();
            return true;
        }

        private void DrawScenePreviewOverlay(SceneView sceneView)
        {
            if (!IsActive || Event.current.type != EventType.Repaint)
            {
                return;
            }

            try
            {
                Color previousColor = Handles.color;
                Handles.color = new Color32(60, 210, 255, 220);

                Vector3 playerPosition = _playerPreview.transform.position;
                Handles.DrawWireDisc(
                    playerPosition,
                    Vector3.up,
                    0.65f);
                Handles.DrawLine(
                    playerPosition,
                    playerPosition + _courseFrame.Forward * 1.5f);
                Handles.Label(
                    playerPosition + Vector3.up * 2.1f,
                    "PREVIEW PLAYER  " +
                    _currentTime.ToString("0.0") + "s",
                    GetPreviewLabelStyle());

                for (int i = 0; i < _obstacles.Count; i++)
                {
                    PreviewObstacle previewObstacle = _obstacles[i];
                    if (previewObstacle.Instance == null ||
                        !previewObstacle.Instance.activeInHierarchy)
                    {
                        continue;
                    }

                    Handles.Label(
                        previewObstacle.Instance.transform.position +
                        Vector3.up * 1.25f,
                        "PREVIEW " + previewObstacle.Data.Type + "  " +
                        previewObstacle.Data.EncounterTime.ToString("0.0") +
                        "s",
                        GetPreviewLabelStyle());
                }

                Handles.color = previousColor;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                StopPreview(
                    "Preview overlay gặp lỗi và đã được dọn sạch.",
                    MessageType.Error);
            }
        }

        private GUIStyle GetPreviewLabelStyle()
        {
            if (_previewLabelStyle != null)
            {
                return _previewLabelStyle;
            }

            _previewLabelStyle = new GUIStyle(EditorStyles.boldLabel);
            _previewLabelStyle.normal.textColor =
                new Color32(60, 210, 255, 255);
            return _previewLabelStyle;
        }

        private static WarmupPlayerController FindScenePlayer()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            WarmupPlayerController[] players =
                Object.FindObjectsOfType<WarmupPlayerController>(true);

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null &&
                    players[i].gameObject.scene == activeScene)
                {
                    return players[i];
                }
            }

            return null;
        }

        private static void DisableInteractiveComponents(GameObject root)
        {
            Behaviour[] behaviours =
                root.GetComponentsInChildren<Behaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                behaviours[i].enabled = false;
            }

            Collider[] colliders =
                root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private static void ApplyPreviewHideFlags(GameObject root)
        {
            Transform[] transforms =
                root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                GameObject gameObject = transforms[i].gameObject;
                gameObject.hideFlags = PreviewHideFlags;

                Component[] components =
                    gameObject.GetComponents<Component>();
                for (int componentIndex = 0;
                     componentIndex < components.Length;
                     componentIndex++)
                {
                    if (components[componentIndex] != null)
                    {
                        components[componentIndex].hideFlags =
                            PreviewHideFlags;
                    }
                }
            }
        }

        private void HandleBeforeAssemblyReload()
        {
            StopPreview();
        }

        private void HandlePlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                StopPreview(
                    "Preview đã dừng trước khi vào Play Mode.",
                    MessageType.Info);
            }
        }

        private void HandleSceneOpening(
            string path,
            OpenSceneMode mode)
        {
            StopPreview(
                "Preview đã dừng vì scene đang thay đổi.",
                MessageType.Info);
        }

        private void HandleActiveSceneChanged(
            Scene previousScene,
            Scene nextScene)
        {
            StopPreview(
                "Preview đã dừng vì active scene thay đổi.",
                MessageType.Info);
        }

        private void HandleSceneClosed(Scene scene)
        {
            if (scene == _previewScene)
            {
                StopPreview(
                    "Preview đã dừng vì scene bị đóng.",
                    MessageType.Info);
            }
        }
    }

    [InitializeOnLoad]
    internal static class WarmupTimelinePreviewCleanup
    {
        internal const string PreviewRootName =
            "[Scene Scrub Preview] TEMPORARY - DO NOT SAVE";

        static WarmupTimelinePreviewCleanup()
        {
            AssemblyReloadEvents.beforeAssemblyReload += DestroyOrphans;
            EditorApplication.playModeStateChanged +=
                HandlePlayModeStateChanged;
            EditorApplication.delayCall += DestroyOrphans;
        }

        internal static void DestroyOrphans()
        {
            GameObject[] gameObjects =
                Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < gameObjects.Length; i++)
            {
                GameObject gameObject = gameObjects[i];
                if (gameObject == null ||
                    gameObject.name != PreviewRootName)
                {
                    continue;
                }

                Object.DestroyImmediate(gameObject);
            }
        }

        private static void HandlePlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                DestroyOrphans();
            }
        }
    }
}
#endif
