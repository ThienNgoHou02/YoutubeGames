#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameYT.Warmup.Editor
{
    public sealed class WarmupObstacleTimelineWindow : EditorWindow
    {
        private const float MinimumRecommendedSpacing = 30f;
        private const float TimelineHorizontalPadding = 14f;
        private const float TimelineMarkerWidth = 14f;
        private const float TimelineMarkerHeight = 26f;
        private const float SnapInterval = 0.5f;
        private const float FloatComparisonTolerance = 0.001f;

        private static readonly int TimelineControlHint =
            "WarmupObstacleTimeline".GetHashCode();
        private static readonly string[] SpawnSideOptions =
            { "Left", "Center", "Right" };

        private static readonly Color HeaderColor =
            new Color32(30, 42, 64, 255);
        private static readonly Color AccentColor =
            new Color32(67, 176, 255, 255);
        private static readonly Color SuccessColor =
            new Color32(71, 184, 129, 255);
        private static readonly Color WarningColor =
            new Color32(246, 178, 73, 255);
        private static readonly Color TimelineColor =
            new Color32(28, 34, 45, 255);
        private static readonly Color SelectedMarkerBorderColor =
            new Color32(255, 255, 255, 255);

        private readonly GUIContent[] _stepLabels =
        {
            new GUIContent("STEP 1", "Run & Jump"),
            new GUIContent("STEP 2", "Pose Wall"),
            new GUIContent("STEP 3", "Jump + Pose + Duck"),
            new GUIContent("STEP 4", "Lane Dodge"),
            new GUIContent("STEP 5", "Boss Wall"),
            new GUIContent("STEP 6", "Dense Combo")
        };

        private int _selectedStep = 1;
        private int _selectedObstacleIndex = -1;
        private int _pendingScrollIndex = -1;
        private int _draggedObstacleIndex = -1;
        private bool _snapEnabled = true;
        private bool _dragChanged;
        private float _dragPreviewTime;
        private float _dragStartTime;
        private float _playheadTime;
        private bool[] _expandedObstacles = Array.Empty<bool>();
        private Vector2 _scrollPosition;

        private WarmupPhaseTimelineAsset _phase;
        private WarmupPlayerConfig _playerConfig;
        private WarmupObstaclePrefabSet _prefabSet;
        private WarmupObstacleEvent _draggedObstacleReference;
        private SerializedObject _phaseSerializedObject;
        private SerializedObject _playerSerializedObject;
        private WarmupTimelinePreviewController _previewController;

        private int _cachedTooltipIndex = -1;
        private float _cachedTooltipTime = float.MinValue;
        private float _cachedTooltipSpeed = float.MinValue;
        private string _cachedTooltipText = string.Empty;

        private GUIStyle _headerTitleStyle;
        private GUIStyle _headerSubtitleStyle;
        private GUIStyle _sectionTitleStyle;
        private GUIStyle _metricValueStyle;
        private GUIStyle _metricLabelStyle;
        private GUIStyle _panelStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _cardHeaderStyle;
        private GUIStyle _cardSpacingStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _validationButtonStyle;
        private GUIStyle _timelineTooltipStyle;
        private GUIStyle _summaryStyle;

        [MenuItem(
            "Tools/Immersive Warmup/Obstacle Timeline/Timeline Dashboard",
            priority = 0)]
        public static void Open()
        {
            WarmupObstacleTimelineWindow window =
                GetWindow<WarmupObstacleTimelineWindow>();
            window.titleContent = new GUIContent("Obstacle Timeline");
            window.minSize = new Vector2(760f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += HandleUndoRedo;
            _previewController?.Dispose();
            _previewController = new WarmupTimelinePreviewController();
            LoadData(false);
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedo;
            CancelMarkerDrag();
            _previewController?.Dispose();
            _previewController = null;
        }

        private void OnProjectChange()
        {
            StopScenePreview(
                "Preview đã dừng vì asset trong Project thay đổi.",
                MessageType.Info);
            LoadData(true);
            Repaint();
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawHeader();
            DrawStepSelector();

            if (_phase == null || _playerConfig == null)
            {
                _scrollPosition =
                    EditorGUILayout.BeginScrollView(_scrollPosition);
                DrawMissingData();
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawTimelinePreview();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawOverview();
            DrawQuickSettings();
            DrawPoseSpriteLibrary();
            DrawObstacleEditor();
            DrawValidation();
            DrawActions();
            EditorGUILayout.Space(12f);
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            Rect rect =
                GUILayoutUtility.GetRect(0f, 92f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, HeaderColor);
            EditorGUI.DrawRect(
                new Rect(rect.x, rect.yMax - 4f, rect.width, 4f),
                AccentColor);

            GUI.Label(
                new Rect(rect.x + 22f, rect.y + 17f, rect.width - 44f, 30f),
                "OBSTACLE TIMELINE STUDIO",
                _headerTitleStyle);
            GUI.Label(
                new Rect(rect.x + 23f, rect.y + 50f, rect.width - 46f, 24f),
                "Chỉnh obstacle, kéo marker và kiểm tra spacing ngay trong Dashboard.",
                _headerSubtitleStyle);
        }

        private void DrawStepSelector()
        {
            EditorGUILayout.Space(14f);
            EditorGUILayout.BeginVertical(_panelStyle);
            EditorGUILayout.LabelField("CHỌN STEP", _sectionTitleStyle);
            EditorGUILayout.Space(5f);

            int selectedIndex = GUILayout.Toolbar(
                _selectedStep - 1,
                _stepLabels,
                GUILayout.Height(42f));
            if (selectedIndex != _selectedStep - 1)
            {
                CancelMarkerDrag();
                StopScenePreview(
                    "Preview đã dừng vì Step thay đổi.",
                    MessageType.Info);
                _selectedStep = selectedIndex + 1;
                _selectedObstacleIndex = -1;
                _pendingScrollIndex = -1;
                LoadData(false);
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawOverview()
        {
            float speed = ResolveSpeed();
            float minimumSpacing = CalculateMinimumSpacing(speed);
            float averageSpacing = CalculateAverageSpacing(speed);
            float courseLength = _phase.Duration * speed;

            EditorGUILayout.Space(10f);
            EditorGUILayout.BeginVertical(_panelStyle);
            EditorGUILayout.LabelField(_phase.DisplayName, _sectionTitleStyle);
            EditorGUILayout.Space(8f);

            EditorGUILayout.BeginHorizontal();
            DrawMetric(speed.ToString("0.0") + " m/s", "RUN SPEED");
            DrawMetric(_phase.EventCount.ToString(), "OBSTACLES");
            DrawMetric(minimumSpacing.ToString("0") + " m", "MIN SPACING");
            DrawMetric(averageSpacing.ToString("0") + " m", "AVG SPACING");
            DrawMetric(courseLength.ToString("0") + " m", "COURSE LENGTH");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(7f);
            bool usesPlayerConfig =
                _phase.RunSpeedSource == WarmupRunSpeedSource.PlayerConfig;
            DrawStatus(
                usesPlayerConfig
                    ? "Nguồn tốc độ: WarmupPlayerConfig"
                    : "Nguồn tốc độ: Override riêng của Step",
                usesPlayerConfig ? SuccessColor : WarningColor);
            EditorGUILayout.EndVertical();
        }

        private void DrawQuickSettings()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.BeginVertical(_panelStyle);
            EditorGUILayout.LabelField("QUICK TUNING", _sectionTitleStyle);
            EditorGUILayout.Space(7f);

            _playerSerializedObject.UpdateIfRequiredOrScript();
            _phaseSerializedObject.UpdateIfRequiredOrScript();

            SerializedProperty playerSpeed =
                _playerSerializedObject.FindProperty("autoRunSpeed");
            SerializedProperty speedSource =
                _phaseSerializedObject.FindProperty("runSpeedSource");
            SerializedProperty overrideSpeed =
                _phaseSerializedObject.FindProperty("metersPerSecond");
            SerializedProperty duration =
                _phaseSerializedObject.FindProperty("duration");
            SerializedProperty startPadding =
                _phaseSerializedObject.FindProperty("courseStartPadding");
            SerializedProperty showBefore =
                _phaseSerializedObject.FindProperty("visibilityLeadTime");
            SerializedProperty hideAfter =
                _phaseSerializedObject.FindProperty("visibilityTailTime");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                playerSpeed,
                new GUIContent(
                    "Player Run Speed",
                    "Tốc độ chung từ WarmupPlayerConfig."));
            EditorGUILayout.PropertyField(
                speedSource,
                new GUIContent(
                    "Step Speed Source",
                    "Mặc định nên dùng Player Config."));

            if ((WarmupRunSpeedSource)speedSource.intValue ==
                WarmupRunSpeedSource.PhaseOverride)
            {
                EditorGUILayout.PropertyField(
                    overrideSpeed,
                    new GUIContent("Step Override Speed"));
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(duration, new GUIContent("Duration"));
            EditorGUILayout.PropertyField(
                startPadding,
                new GUIContent("Start Gap"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(
                showBefore,
                new GUIContent("Show Before"));
            EditorGUILayout.PropertyField(
                hideAfter,
                new GUIContent("Hide After"));
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                StopScenePreview(
                    "Preview đã dừng vì Timeline settings thay đổi.",
                    MessageType.Info);
                bool playerChanged =
                    _playerSerializedObject.ApplyModifiedProperties();
                bool phaseChanged =
                    _phaseSerializedObject.ApplyModifiedProperties();
                if (playerChanged)
                {
                    EditorUtility.SetDirty(_playerConfig);
                }

                if (phaseChanged)
                {
                    EditorUtility.SetDirty(_phase);
                    _playheadTime = Mathf.Clamp(
                        _playheadTime,
                        0f,
                        _phase.Duration);
                    ClampSelection();
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "Khi dùng Player Config, obstacle được đặt theo đúng tốc độ thực tế. " +
                "Đổi speed không làm lệch encounter time.",
                MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void DrawPoseSpriteLibrary()
        {
            if (_phase.StepNumber != 2 || !HasPoseWallEvents())
            {
                return;
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.BeginVertical(_panelStyle);
            EditorGUILayout.LabelField(
                "MIRROR POSE SPRITE LIST",
                _sectionTitleStyle);
            EditorGUILayout.Space(7f);

            _phaseSerializedObject.UpdateIfRequiredOrScript();
            SerializedProperty poseSprites =
                _phaseSerializedObject.FindProperty("poseSprites");
            SerializedProperty poseRandomSeed =
                _phaseSerializedObject.FindProperty("poseRandomSeed");
            poseSprites.isExpanded = true;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                poseSprites,
                new GUIContent(
                    "Pose Sprites",
                    "Pose Wall lấy sprite theo đúng thứ tự list."));
            EditorGUILayout.PropertyField(
                poseRandomSeed,
                new GUIContent(
                    "Overflow Random Seed",
                    "Đổi seed để reroll các obstacle vượt quá số sprite."));

            if (EditorGUI.EndChangeCheck())
            {
                StopScenePreview(
                    "Preview đã dừng vì Pose Sprite List thay đổi.",
                    MessageType.Info);
                if (_phaseSerializedObject.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(_phase);
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "Pose Wall #1 dùng Element 0, #2 dùng Element 1... " +
                "Khi obstacle nhiều hơn list, sprite được random ổn định theo seed. " +
                "Pose Override trên từng obstacle vẫn được ưu tiên.",
                MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private bool HasPoseWallEvents()
        {
            for (int i = 0; i < _phase.EventCount; i++)
            {
                if (_phase.GetEvent(i).IsPoseWall)
                {
                    return true;
                }
            }

            return false;
        }

        private void DrawTimelinePreview()
        {
            if (_previewController != null &&
                _previewController.IsActive)
            {
                _previewController.ValidateSources(
                    _phase,
                    _playerConfig,
                    _prefabSet);
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.BeginVertical(_panelStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                "TIMELINE PREVIEW",
                _sectionTitleStyle);
            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(
                       _previewController == null ||
                       _previewController.IsActive))
            {
                if (GUILayout.Button(
                    "Start Preview",
                    EditorStyles.miniButtonLeft,
                    GUILayout.Width(96f)))
                {
                    StartScenePreview();
                    GUI.FocusControl(null);
                }
            }

            using (new EditorGUI.DisabledScope(
                       _previewController == null ||
                       !_previewController.IsActive))
            {
                if (GUILayout.Button(
                    "Stop Preview",
                    EditorStyles.miniButtonRight,
                    GUILayout.Width(92f)))
                {
                    StopScenePreview(
                        "Scene Scrub Preview đã dừng.",
                        MessageType.Info);
                    GUI.FocusControl(null);
                }
            }

            GUILayout.Space(10f);
            _snapEnabled = EditorGUILayout.ToggleLeft(
                "Snap 0.5s",
                _snapEnabled,
                GUILayout.Width(92f));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                "PLAYHEAD",
                EditorStyles.miniBoldLabel,
                GUILayout.Width(68f));
            EditorGUI.BeginChangeCheck();
            float playheadTime = GUILayout.HorizontalSlider(
                _playheadTime,
                0f,
                Mathf.Max(0.01f, _phase.Duration));
            if (EditorGUI.EndChangeCheck())
            {
                SetPlayheadTime(playheadTime);
            }

            EditorGUILayout.LabelField(
                _playheadTime.ToString("0.0") + "s",
                EditorStyles.miniBoldLabel,
                GUILayout.Width(48f));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);

            Rect timelineRect =
                GUILayoutUtility.GetRect(100f, 142f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(timelineRect, TimelineColor);
            DrawTimelineGrid(timelineRect);
            DrawTimelineEvents(timelineRect);
            DrawTimelinePlayhead(timelineRect);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Click marker để chọn. Kéo ngang để đổi Encounter Time. " +
                "Ctrl+D: duplicate • Delete: xóa nhanh.",
                EditorStyles.miniLabel);

            if (_previewController != null &&
                !string.IsNullOrEmpty(
                    _previewController.StatusMessage))
            {
                EditorGUILayout.HelpBox(
                    _previewController.StatusMessage,
                    _previewController.StatusType);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTimelineGrid(Rect rect)
        {
            const int divisions = 4;
            for (int i = 0; i <= divisions; i++)
            {
                float normalized = i / (float)divisions;
                float x = Mathf.Lerp(
                    rect.x + TimelineHorizontalPadding,
                    rect.xMax - TimelineHorizontalPadding,
                    normalized);
                EditorGUI.DrawRect(
                    new Rect(x, rect.y + 24f, 1f, rect.height - 54f),
                    new Color(1f, 1f, 1f, 0.12f));

                GUI.Label(
                    new Rect(x - 18f, rect.y + 4f, 40f, 18f),
                    (_phase.Duration * normalized).ToString("0") + "s",
                    EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void DrawTimelinePlayhead(Rect rect)
        {
            float normalized = Mathf.Clamp01(
                _playheadTime /
                Mathf.Max(0.01f, _phase.Duration));
            float x = Mathf.Lerp(
                rect.x + TimelineHorizontalPadding,
                rect.xMax - TimelineHorizontalPadding,
                normalized);
            Color playheadColor =
                _previewController != null &&
                _previewController.IsActive
                    ? SuccessColor
                    : AccentColor;

            EditorGUI.DrawRect(
                new Rect(
                    x - 1f,
                    rect.y + 22f,
                    2f,
                    rect.height - 46f),
                playheadColor);
            EditorGUI.DrawRect(
                new Rect(x - 4f, rect.y + 22f, 8f, 5f),
                playheadColor);
        }

        private void DrawTimelineEvents(Rect rect)
        {
            int controlId = GUIUtility.GetControlID(
                TimelineControlHint,
                FocusType.Keyboard,
                rect);
            Event guiEvent = Event.current;

            HandleTimelineInput(rect, controlId, guiEvent);

            float speed = ResolveSpeed();
            int hoveredIndex = -1;
            float hoveredTime = 0f;

            for (int i = 0; i < _phase.EventCount; i++)
            {
                WarmupObstacleEvent obstacleEvent = _phase.GetEvent(i);
                float displayTime = GetDisplayEncounterTime(i);
                Rect markerRect = GetMarkerRect(rect, i, displayTime);
                Rect hitRect = markerRect;
                hitRect.xMin -= 5f;
                hitRect.xMax += 5f;
                hitRect.yMin -= 4f;
                hitRect.yMax += 4f;

                if (i == _selectedObstacleIndex)
                {
                    Rect borderRect = markerRect;
                    borderRect.xMin -= 3f;
                    borderRect.xMax += 3f;
                    borderRect.yMin -= 3f;
                    borderRect.yMax += 3f;
                    EditorGUI.DrawRect(
                        borderRect,
                        SelectedMarkerBorderColor);
                }

                EditorGUI.DrawRect(
                    markerRect,
                    GetEventColor(obstacleEvent.Type));
                EditorGUIUtility.AddCursorRect(
                    hitRect,
                    MouseCursor.SlideArrow);

                if (hitRect.Contains(guiEvent.mousePosition))
                {
                    hoveredIndex = i;
                    hoveredTime = displayTime;
                }
            }

            if (_draggedObstacleIndex >= 0 &&
                GUIUtility.hotControl == controlId)
            {
                hoveredIndex = _draggedObstacleIndex;
                hoveredTime = _dragPreviewTime;
            }

            if (hoveredIndex >= 0)
            {
                DrawTimelineTooltip(
                    rect,
                    hoveredIndex,
                    hoveredTime,
                    speed,
                    _draggedObstacleIndex == hoveredIndex &&
                    GUIUtility.hotControl == controlId);
            }
        }

        private void HandleTimelineInput(
            Rect timelineRect,
            int controlId,
            Event guiEvent)
        {
            switch (guiEvent.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (guiEvent.button != 0 ||
                        !timelineRect.Contains(guiEvent.mousePosition))
                    {
                        return;
                    }

                    int hitIndex = FindMarkerAtPosition(
                        timelineRect,
                        guiEvent.mousePosition);
                    if (hitIndex < 0)
                    {
                        return;
                    }

                    SelectObstacle(hitIndex, false);
                    GUIUtility.keyboardControl = controlId;
                    _draggedObstacleIndex = hitIndex;
                    _draggedObstacleReference = _phase.GetEvent(hitIndex);
                    _dragStartTime =
                        _draggedObstacleReference.EncounterTime;
                    _dragPreviewTime = _dragStartTime;
                    _dragChanged = false;
                    GUIUtility.hotControl = controlId;
                    guiEvent.Use();
                    Repaint();
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != controlId ||
                        _draggedObstacleIndex < 0)
                    {
                        return;
                    }

                    _dragPreviewTime = CalculateTimelineTime(
                        timelineRect,
                        guiEvent.mousePosition.x);
                    _dragChanged =
                        !Mathf.Approximately(
                            _dragPreviewTime,
                            _dragStartTime);
                    guiEvent.Use();
                    Repaint();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl != controlId ||
                        _draggedObstacleIndex < 0)
                    {
                        return;
                    }

                    GUIUtility.hotControl = 0;
                    CommitMarkerDrag();
                    GUIUtility.keyboardControl = controlId;
                    guiEvent.Use();
                    Repaint();
                    break;

                case EventType.KeyDown:
                    if (GUIUtility.hotControl == controlId &&
                        guiEvent.keyCode == KeyCode.Escape)
                    {
                        GUIUtility.hotControl = 0;
                        CancelMarkerDrag();
                        guiEvent.Use();
                        Repaint();
                        break;
                    }

                    HandleTimelineKeyDown(controlId, guiEvent);
                    break;

                case EventType.ValidateCommand:
                    ValidateTimelineCommand(controlId, guiEvent);
                    break;

                case EventType.ExecuteCommand:
                    ExecuteTimelineCommand(controlId, guiEvent);
                    break;

                case EventType.MouseMove:
                    if (timelineRect.Contains(guiEvent.mousePosition))
                    {
                        Repaint();
                    }
                    break;
            }
        }

        private void HandleTimelineKeyDown(int controlId, Event guiEvent)
        {
            if (GUIUtility.keyboardControl != controlId ||
                GUIUtility.hotControl == controlId ||
                _selectedObstacleIndex < 0)
            {
                return;
            }

            if (guiEvent.control && guiEvent.keyCode == KeyCode.D)
            {
                DuplicateSelectedTimelineMarker(controlId);
                guiEvent.Use();
                return;
            }

            if (guiEvent.keyCode == KeyCode.Delete ||
                guiEvent.keyCode == KeyCode.Backspace)
            {
                DeleteSelectedTimelineMarker(controlId);
                guiEvent.Use();
            }
        }

        private void ValidateTimelineCommand(int controlId, Event guiEvent)
        {
            if (GUIUtility.keyboardControl != controlId ||
                GUIUtility.hotControl == controlId ||
                _selectedObstacleIndex < 0 ||
                !IsTimelineEditCommand(guiEvent.commandName))
            {
                return;
            }

            guiEvent.Use();
        }

        private void ExecuteTimelineCommand(int controlId, Event guiEvent)
        {
            if (GUIUtility.keyboardControl != controlId ||
                GUIUtility.hotControl == controlId ||
                _selectedObstacleIndex < 0)
            {
                return;
            }

            switch (guiEvent.commandName)
            {
                case "Duplicate":
                    DuplicateSelectedTimelineMarker(controlId);
                    guiEvent.Use();
                    break;

                case "Delete":
                case "SoftDelete":
                    DeleteSelectedTimelineMarker(controlId);
                    guiEvent.Use();
                    break;
            }
        }

        private void DuplicateSelectedTimelineMarker(int controlId)
        {
            DuplicateObstacle(_selectedObstacleIndex);
            GUIUtility.keyboardControl = controlId;
            if (_selectedObstacleIndex >= 0 &&
                _selectedObstacleIndex < _phase.EventCount)
            {
                SetPlayheadTime(
                    _phase.GetEvent(_selectedObstacleIndex).EncounterTime);
            }
        }

        private void DeleteSelectedTimelineMarker(int controlId)
        {
            RemoveObstacle(_selectedObstacleIndex);
            GUIUtility.keyboardControl =
                _phase.EventCount > 0 ? controlId : 0;
            if (_selectedObstacleIndex >= 0 &&
                _selectedObstacleIndex < _phase.EventCount)
            {
                SetPlayheadTime(
                    _phase.GetEvent(_selectedObstacleIndex).EncounterTime);
            }
        }

        private static bool IsTimelineEditCommand(string commandName)
        {
            return commandName == "Duplicate" ||
                   commandName == "Delete" ||
                   commandName == "SoftDelete";
        }

        private int FindMarkerAtPosition(Rect timelineRect, Vector2 mousePosition)
        {
            for (int i = _phase.EventCount - 1; i >= 0; i--)
            {
                Rect markerRect = GetMarkerRect(
                    timelineRect,
                    i,
                    GetDisplayEncounterTime(i));
                markerRect.xMin -= 5f;
                markerRect.xMax += 5f;
                markerRect.yMin -= 4f;
                markerRect.yMax += 4f;
                if (markerRect.Contains(mousePosition))
                {
                    return i;
                }
            }

            return -1;
        }

        private Rect GetMarkerRect(Rect timelineRect, int index, float time)
        {
            float normalized = Mathf.Clamp01(
                time / Mathf.Max(0.01f, _phase.Duration));
            float x = Mathf.Lerp(
                timelineRect.x + TimelineHorizontalPadding,
                timelineRect.xMax - TimelineHorizontalPadding,
                normalized);
            float y = timelineRect.y + (index % 2 == 0 ? 58f : 88f);
            return new Rect(
                x - TimelineMarkerWidth * 0.5f,
                y - TimelineMarkerHeight * 0.5f,
                TimelineMarkerWidth,
                TimelineMarkerHeight);
        }

        private float CalculateTimelineTime(Rect timelineRect, float mouseX)
        {
            float startX = timelineRect.x + TimelineHorizontalPadding;
            float endX = timelineRect.xMax - TimelineHorizontalPadding;
            float normalized = Mathf.InverseLerp(startX, endX, mouseX);
            float time = normalized * Mathf.Max(0f, _phase.Duration);
            if (_snapEnabled)
            {
                time = Mathf.Round(time / SnapInterval) * SnapInterval;
            }

            return Mathf.Clamp(time, 0f, _phase.Duration);
        }

        private float GetDisplayEncounterTime(int index)
        {
            if (index == _draggedObstacleIndex)
            {
                return _dragPreviewTime;
            }

            return _phase.GetEvent(index).EncounterTime;
        }

        private void DrawTimelineTooltip(
            Rect timelineRect,
            int obstacleIndex,
            float time,
            float speed,
            bool isDragging)
        {
            string tooltip = GetTimelineTooltip(
                obstacleIndex,
                time,
                speed);
            float width = Mathf.Min(320f, timelineRect.width - 12f);
            Rect tooltipRect = new Rect(
                timelineRect.center.x - width * 0.5f,
                timelineRect.yMax - 27f,
                width,
                21f);
            GUI.Box(tooltipRect, tooltip, _timelineTooltipStyle);

            if (isDragging)
            {
                Rect timeRect = new Rect(
                    timelineRect.center.x - 54f,
                    timelineRect.y + 25f,
                    108f,
                    24f);
                GUI.Box(
                    timeRect,
                    _dragPreviewTime.ToString("0.0") + "s",
                    _timelineTooltipStyle);
            }
        }

        private string GetTimelineTooltip(
            int obstacleIndex,
            float time,
            float speed)
        {
            if (_cachedTooltipIndex == obstacleIndex &&
                Mathf.Approximately(_cachedTooltipTime, time) &&
                Mathf.Approximately(_cachedTooltipSpeed, speed))
            {
                return _cachedTooltipText;
            }

            WarmupObstacleEvent obstacleEvent =
                _phase.GetEvent(obstacleIndex);
            _cachedTooltipIndex = obstacleIndex;
            _cachedTooltipTime = time;
            _cachedTooltipSpeed = speed;
            _cachedTooltipText =
                time.ToString("0.0") + "s • " +
                GetTypeDisplayName(obstacleEvent.Type) + " • " +
                obstacleEvent.Lane + " • " +
                (time * speed).ToString("0.0") + "m";
            return _cachedTooltipText;
        }

        private void CommitMarkerDrag()
        {
            int draggedIndex = _draggedObstacleIndex;
            WarmupObstacleEvent draggedReference =
                _draggedObstacleReference;
            float targetTime = _dragPreviewTime;
            bool changed = _dragChanged;
            ResetDragState();

            if (!changed ||
                draggedIndex < 0 ||
                draggedIndex >= _phase.EventCount)
            {
                if (!changed &&
                    draggedIndex >= 0 &&
                    draggedIndex < _phase.EventCount)
                {
                    SelectObstacle(draggedIndex, true);
                    SetPlayheadTime(
                        _phase.GetEvent(draggedIndex).EncounterTime);
                }

                return;
            }

            StopScenePreview(
                "Preview đã dừng vì Encounter Time thay đổi.",
                MessageType.Info);
            Undo.RegisterCompleteObjectUndo(
                _phase,
                "Move Obstacle Marker");
            _phaseSerializedObject.Update();
            SerializedProperty eventsProperty =
                _phaseSerializedObject.FindProperty("events");
            SerializedProperty timeProperty =
                eventsProperty
                    .GetArrayElementAtIndex(draggedIndex)
                    .FindPropertyRelative("EncounterTime");
            timeProperty.floatValue =
                Mathf.Clamp(targetTime, 0f, _phase.Duration);
            _phaseSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(_phase);
            _phaseSerializedObject.Update();

            int selectedIndex = FindEventIndex(draggedReference);
            if (selectedIndex < 0)
            {
                selectedIndex = FindClosestEventIndex(targetTime);
            }

            SelectObstacle(selectedIndex, false);
            InvalidateTooltipCache();
        }

        private void CancelMarkerDrag()
        {
            if (_draggedObstacleIndex < 0)
            {
                return;
            }

            ResetDragState();
            Repaint();
        }

        private void ResetDragState()
        {
            _draggedObstacleIndex = -1;
            _draggedObstacleReference = null;
            _dragPreviewTime = 0f;
            _dragStartTime = 0f;
            _dragChanged = false;
        }

        private void DrawObstacleEditor()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.BeginVertical(_panelStyle);

            DrawObstacleToolbar();
            EditorGUILayout.Space(7f);
            DrawObstacleSummary();
            EditorGUILayout.Space(8f);

            _phaseSerializedObject.UpdateIfRequiredOrScript();
            SerializedProperty eventsProperty =
                _phaseSerializedObject.FindProperty("events");
            EnsureExpandedState(eventsProperty.arraySize, true);

            if (eventsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "Timeline chưa có obstacle. Bấm Add Obstacle để tạo event đầu tiên.",
                    MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            WarmupObstacleEvent selectedReference =
                GetSelectedEventReference();
            EditorGUI.BeginChangeCheck();

            for (int i = 0; i < eventsProperty.arraySize; i++)
            {
                DrawObstacleCard(eventsProperty, i);
            }

            if (EditorGUI.EndChangeCheck())
            {
                StopScenePreview(
                    "Preview đã dừng vì obstacle data thay đổi.",
                    MessageType.Info);
                bool changed =
                    _phaseSerializedObject.ApplyModifiedProperties();
                if (changed)
                {
                    EditorUtility.SetDirty(_phase);
                    _phaseSerializedObject.Update();
                    int selectedIndex =
                        FindEventIndex(selectedReference);
                    if (selectedIndex >= 0)
                    {
                        _selectedObstacleIndex = selectedIndex;
                        EnsureExpandedState(_phase.EventCount, true);
                        _expandedObstacles[selectedIndex] = true;
                    }
                    else
                    {
                        ClampSelection();
                    }

                    InvalidateTooltipCache();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawObstacleToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("OBSTACLES", _sectionTitleStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(
                "Add Obstacle",
                EditorStyles.miniButtonLeft,
                GUILayout.Width(96f)))
            {
                AddObstacle();
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button(
                "Sort By Time",
                EditorStyles.miniButtonMid,
                GUILayout.Width(90f)))
            {
                SortObstaclesByTime();
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button(
                "Expand All",
                EditorStyles.miniButtonMid,
                GUILayout.Width(82f)))
            {
                SetAllExpanded(true);
            }

            if (GUILayout.Button(
                "Collapse All",
                EditorStyles.miniButtonRight,
                GUILayout.Width(86f)))
            {
                SetAllExpanded(false);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawObstacleSummary()
        {
            int jumpCount = 0;
            int poseCount = 0;
            int duckCount = 0;
            int laneCount = 0;
            int bossCount = 0;

            for (int i = 0; i < _phase.EventCount; i++)
            {
                switch (_phase.GetEvent(i).Type)
                {
                    case WarmupObstacleType.Jump:
                        jumpCount++;
                        break;
                    case WarmupObstacleType.PoseWall:
                        poseCount++;
                        break;
                    case WarmupObstacleType.DuckBarrier:
                        duckCount++;
                        break;
                    case WarmupObstacleType.LaneBlocker:
                        laneCount++;
                        break;
                    case WarmupObstacleType.BossWall:
                        bossCount++;
                        break;
                }
            }

            EditorGUILayout.BeginHorizontal();
            DrawSummaryItem("Jump", jumpCount, GetEventColor(WarmupObstacleType.Jump));
            DrawSummaryItem("Pose", poseCount, GetEventColor(WarmupObstacleType.PoseWall));
            DrawSummaryItem("Duck", duckCount, GetEventColor(WarmupObstacleType.DuckBarrier));
            DrawSummaryItem("Lane", laneCount, GetEventColor(WarmupObstacleType.LaneBlocker));
            DrawSummaryItem("Boss", bossCount, GetEventColor(WarmupObstacleType.BossWall));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSummaryItem(string label, int count, Color color)
        {
            Rect rect = EditorGUILayout.GetControlRect(
                false,
                24f,
                GUILayout.MinWidth(80f));
            Color background = color;
            background.a = EditorGUIUtility.isProSkin ? 0.2f : 0.13f;
            EditorGUI.DrawRect(rect, background);
            EditorGUI.DrawRect(
                new Rect(rect.x, rect.y, 4f, rect.height),
                color);
            GUI.Label(
                new Rect(rect.x + 9f, rect.y, rect.width - 12f, rect.height),
                label + "  " + count,
                _summaryStyle);
        }

        private void DrawObstacleCard(
            SerializedProperty eventsProperty,
            int index)
        {
            bool cardMouseDown =
                Event.current.type == EventType.MouseDown &&
                Event.current.button == 0;
            Vector2 cardMousePosition = Event.current.mousePosition;
            SerializedProperty obstacleProperty =
                eventsProperty.GetArrayElementAtIndex(index);
            SerializedProperty timeProperty =
                obstacleProperty.FindPropertyRelative("EncounterTime");
            SerializedProperty typeProperty =
                obstacleProperty.FindPropertyRelative("Type");
            SerializedProperty laneProperty =
                obstacleProperty.FindPropertyRelative("Lane");

            bool isSelected = index == _selectedObstacleIndex;
            EditorGUILayout.BeginVertical(_cardStyle);
            Rect headerRect = GUILayoutUtility.GetRect(
                0f,
                32f,
                GUILayout.ExpandWidth(true));

            DrawObstacleCardHeader(
                headerRect,
                index,
                timeProperty.floatValue,
                (WarmupObstacleType)typeProperty.intValue,
                (WarmupLane)laneProperty.intValue,
                isSelected);

            if (_pendingScrollIndex == index &&
                Event.current.type == EventType.Repaint)
            {
                _scrollPosition.y =
                    Mathf.Max(0f, headerRect.y - 165f);
                _pendingScrollIndex = -1;
                Repaint();
            }

            if (_expandedObstacles[index])
            {
                DrawObstacleFields(obstacleProperty, typeProperty);
                DrawObstacleCardActions(index);
            }

            EditorGUILayout.EndVertical();
            Rect cardRect = GUILayoutUtility.GetLastRect();
            if (cardMouseDown &&
                cardRect.Contains(cardMousePosition) &&
                index != _selectedObstacleIndex)
            {
                _selectedObstacleIndex = index;
                EnsureExpandedState(_phase.EventCount, true);
                Repaint();
            }

            EditorGUILayout.Space(4f);
        }

        private void DrawObstacleCardHeader(
            Rect rect,
            int index,
            float encounterTime,
            WarmupObstacleType type,
            WarmupLane lane,
            bool isSelected)
        {
            Color background;
            if (isSelected)
            {
                background = GetEventColor(type);
                background.a = EditorGUIUtility.isProSkin ? 0.3f : 0.2f;
            }
            else
            {
                background = EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.045f)
                    : new Color(0f, 0f, 0f, 0.045f);
            }

            EditorGUI.DrawRect(rect, background);
            EditorGUI.DrawRect(
                new Rect(rect.x, rect.y, 4f, rect.height),
                GetEventColor(type));

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                SelectObstacle(index, false);
                _expandedObstacles[index] =
                    !_expandedObstacles[index];
                GUI.FocusControl(null);
                Repaint();
            }

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            GUI.Label(
                new Rect(rect.x + 10f, rect.y, 18f, rect.height),
                _expandedObstacles[index] ? "▼" : "▶",
                _cardHeaderStyle);
            GUI.Label(
                new Rect(rect.x + 31f, rect.y, rect.width - 190f, rect.height),
                encounterTime.ToString("00.0") + "s • " +
                GetTypeDisplayName(type) + " • " + lane,
                _cardHeaderStyle);

            if (index <= 0)
            {
                GUI.Label(
                    new Rect(rect.xMax - 130f, rect.y, 118f, rect.height),
                    "FIRST",
                    _cardSpacingStyle);
                return;
            }

            float previousTime =
                _phase.GetEvent(index - 1).EncounterTime;
            float spacing =
                (encounterTime - previousTime) * ResolveSpeed();
            Color previousColor =
                _cardSpacingStyle.normal.textColor;
            _cardSpacingStyle.normal.textColor =
                spacing < MinimumRecommendedSpacing
                    ? WarningColor
                    : previousColor;
            GUI.Label(
                new Rect(rect.xMax - 130f, rect.y, 118f, rect.height),
                "Δ " + spacing.ToString("0.0") + " m",
                _cardSpacingStyle);
            _cardSpacingStyle.normal.textColor = previousColor;
        }

        private void DrawObstacleFields(
            SerializedProperty obstacleProperty,
            SerializedProperty typeProperty)
        {
            EditorGUILayout.Space(7f);

            WarmupObstacleType obstacleType =
                (WarmupObstacleType)typeProperty.intValue;
            SerializedProperty laneProperty =
                obstacleProperty.FindPropertyRelative("Lane");
            SerializedProperty actionProperty =
                obstacleProperty.FindPropertyRelative("Action");
            SerializedProperty cueLabelProperty =
                obstacleProperty.FindPropertyRelative("CueLabel");
            bool usesSpawnSide =
                obstacleType == WarmupObstacleType.LaneBlocker;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(
                obstacleProperty.FindPropertyRelative("EncounterTime"),
                new GUIContent("Encounter Time"));
            EditorGUILayout.PropertyField(
                typeProperty,
                new GUIContent("Type"));
            if (usesSpawnSide)
            {
                DrawSpawnSideField(
                    laneProperty,
                    actionProperty,
                    cueLabelProperty);
            }
            else
            {
                EditorGUILayout.PropertyField(
                    laneProperty,
                    new GUIContent("Lane"));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (!usesSpawnSide)
            {
                EditorGUILayout.PropertyField(
                    actionProperty,
                    new GUIContent("Required Action"));
            }
            EditorGUILayout.PropertyField(
                cueLabelProperty,
                new GUIContent("HUD / Cue Label"));
            EditorGUILayout.PropertyField(
                obstacleProperty.FindPropertyRelative("CueLeadTime"),
                new GUIContent("Cue Lead Time"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(
                obstacleProperty.FindPropertyRelative("PrefabVariation"),
                new GUIContent("Prefab Variation"));
            EditorGUILayout.PropertyField(
                obstacleProperty.FindPropertyRelative("PrefabOverride"),
                new GUIContent("Prefab Override"));
            EditorGUILayout.EndHorizontal();

            if (obstacleType == WarmupObstacleType.PoseWall)
            {
                EditorGUILayout.PropertyField(
                    obstacleProperty.FindPropertyRelative("PoseSprite"),
                    new GUIContent(
                        "Mirror Human Pose Override",
                        "Override riêng cho mốc này. None = lấy theo Pose Sprite List."));
            }

            EditorGUILayout.PropertyField(
                obstacleProperty.FindPropertyRelative("PositionOffset"),
                new GUIContent("Position Offset"));
            EditorGUILayout.PropertyField(
                obstacleProperty.FindPropertyRelative("RotationOffset"),
                new GUIContent("Rotation Offset"));
            EditorGUILayout.PropertyField(
                obstacleProperty.FindPropertyRelative("ScaleMultiplier"),
                new GUIContent("Scale Multiplier"));
            EditorGUILayout.PropertyField(
                obstacleProperty.FindPropertyRelative("CollisionMode"),
                new GUIContent("Collision Mode"));

            if (obstacleType == WarmupObstacleType.BossWall)
            {
                EditorGUILayout.Space(3f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(
                    obstacleProperty.FindPropertyRelative("BossHitPoints"),
                    new GUIContent("Boss HP"));
                EditorGUILayout.PropertyField(
                    obstacleProperty.FindPropertyRelative("BossStopDistance"),
                    new GUIContent("Boss Stop Distance"));
                EditorGUILayout.EndHorizontal();
            }
        }

        private static void DrawSpawnSideField(
            SerializedProperty laneProperty,
            SerializedProperty actionProperty,
            SerializedProperty cueLabelProperty)
        {
            WarmupLane currentLane = (WarmupLane)laneProperty.intValue;
            int selectedIndex;

            switch (currentLane)
            {
                case WarmupLane.Left:
                    selectedIndex = 0;
                    break;
                case WarmupLane.Center:
                    selectedIndex = 1;
                    break;
                case WarmupLane.Right:
                    selectedIndex = 2;
                    break;
                default:
                    selectedIndex = 1;
                    ApplySpawnSide(
                        selectedIndex,
                        laneProperty,
                        actionProperty,
                        cueLabelProperty);
                    GUI.changed = true;
                    break;
            }

            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup(
                new GUIContent(
                    "Spawn Side",
                    "Left/Center/Right tính theo hướng nhìn của Player."),
                selectedIndex,
                SpawnSideOptions);
            if (EditorGUI.EndChangeCheck())
            {
                ApplySpawnSide(
                    selectedIndex,
                    laneProperty,
                    actionProperty,
                    cueLabelProperty);
            }
            else
            {
                SynchronizeSpawnSideData(
                    selectedIndex,
                    laneProperty,
                    actionProperty,
                    cueLabelProperty);
            }
        }

        private static void ApplySpawnSide(
            int selectedIndex,
            SerializedProperty laneProperty,
            SerializedProperty actionProperty,
            SerializedProperty cueLabelProperty)
        {
            switch (selectedIndex)
            {
                case 0:
                    laneProperty.intValue = (int)WarmupLane.Left;
                    actionProperty.intValue =
                        (int)WarmupActionType.MoveLeft;
                    cueLabelProperty.stringValue = "LEFT!";
                    break;

                case 2:
                    laneProperty.intValue = (int)WarmupLane.Right;
                    actionProperty.intValue =
                        (int)WarmupActionType.MoveRight;
                    cueLabelProperty.stringValue = "RIGHT!";
                    break;

                default:
                    laneProperty.intValue = (int)WarmupLane.Center;
                    actionProperty.intValue = (int)WarmupActionType.Run;
                    cueLabelProperty.stringValue = "CENTER!";
                    break;
            }
        }

        private static void SynchronizeSpawnSideData(
            int selectedIndex,
            SerializedProperty laneProperty,
            SerializedProperty actionProperty,
            SerializedProperty cueLabelProperty)
        {
            int expectedAction;
            switch (selectedIndex)
            {
                case 0:
                    expectedAction = (int)WarmupActionType.MoveLeft;
                    break;
                case 2:
                    expectedAction = (int)WarmupActionType.MoveRight;
                    break;
                default:
                    expectedAction = (int)WarmupActionType.Run;
                    break;
            }
            if (actionProperty.intValue == expectedAction)
            {
                return;
            }

            ApplySpawnSide(
                selectedIndex,
                laneProperty,
                actionProperty,
                cueLabelProperty);
            GUI.changed = true;
        }

        private void DrawObstacleCardActions(int index)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(index <= 0))
            {
                if (GUILayout.Button(
                    "Move Up",
                    EditorStyles.miniButtonLeft,
                    GUILayout.Width(76f)))
                {
                    MoveObstacle(index, -1);
                    GUIUtility.ExitGUI();
                }
            }

            using (new EditorGUI.DisabledScope(
                       index >= _phase.EventCount - 1))
            {
                if (GUILayout.Button(
                    "Move Down",
                    EditorStyles.miniButtonMid,
                    GUILayout.Width(82f)))
                {
                    MoveObstacle(index, 1);
                    GUIUtility.ExitGUI();
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(
                "Duplicate",
                EditorStyles.miniButtonMid,
                GUILayout.Width(78f)))
            {
                DuplicateObstacle(index);
                GUIUtility.ExitGUI();
            }

            Color previousBackground = GUI.backgroundColor;
            GUI.backgroundColor = new Color32(220, 82, 88, 255);
            if (GUILayout.Button(
                "Remove",
                EditorStyles.miniButtonRight,
                GUILayout.Width(72f)))
            {
                GUI.backgroundColor = previousBackground;
                if (ConfirmRemoveObstacle(index))
                {
                    RemoveObstacle(index);
                    GUIUtility.ExitGUI();
                }
            }

            GUI.backgroundColor = previousBackground;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(3f);
        }

        private void AddObstacle()
        {
            StopScenePreview(
                "Preview đã dừng vì Timeline thay đổi.",
                MessageType.Info);
            Undo.RegisterCompleteObjectUndo(_phase, "Add Obstacle");
            _phaseSerializedObject.Update();
            SerializedProperty eventsProperty =
                _phaseSerializedObject.FindProperty("events");
            int newIndex = eventsProperty.arraySize;
            float newTime = CalculateNewObstacleTime(eventsProperty);
            eventsProperty.arraySize++;
            SerializedProperty newObstacle =
                eventsProperty.GetArrayElementAtIndex(newIndex);
            InitializeObstacle(newObstacle, newTime);
            _phaseSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(_phase);
            _phaseSerializedObject.Update();

            int selectedIndex = FindClosestEventIndex(
                newTime,
                WarmupObstacleType.Jump);
            SelectObstacle(selectedIndex, true);
            InvalidateTooltipCache();
        }

        private float CalculateNewObstacleTime(
            SerializedProperty eventsProperty)
        {
            if (eventsProperty.arraySize == 0)
            {
                return Mathf.Min(1f, _phase.Duration);
            }

            float lastTime =
                eventsProperty
                    .GetArrayElementAtIndex(eventsProperty.arraySize - 1)
                    .FindPropertyRelative("EncounterTime")
                    .floatValue;
            float offset = CalculateDuplicateTimeOffset();
            return Mathf.Clamp(
                SnapTime(lastTime + offset),
                0f,
                _phase.Duration);
        }

        private static void InitializeObstacle(
            SerializedProperty obstacleProperty,
            float encounterTime)
        {
            obstacleProperty.FindPropertyRelative("EncounterTime").floatValue =
                encounterTime;
            obstacleProperty.FindPropertyRelative("Type").intValue =
                (int)WarmupObstacleType.Jump;
            obstacleProperty.FindPropertyRelative("Lane").intValue =
                (int)WarmupLane.Center;
            obstacleProperty.FindPropertyRelative("Action").intValue =
                (int)WarmupActionType.Jump;
            obstacleProperty.FindPropertyRelative("CueLabel").stringValue =
                "JUMP!";
            obstacleProperty.FindPropertyRelative("CueLeadTime").floatValue =
                1.4f;
            obstacleProperty.FindPropertyRelative("PrefabVariation").intValue =
                0;
            obstacleProperty.FindPropertyRelative("PrefabOverride")
                .objectReferenceValue = null;
            obstacleProperty.FindPropertyRelative("PoseSprite")
                .objectReferenceValue = null;
            obstacleProperty.FindPropertyRelative("PositionOffset")
                .vector3Value = Vector3.zero;
            obstacleProperty.FindPropertyRelative("RotationOffset")
                .vector3Value = Vector3.zero;
            obstacleProperty.FindPropertyRelative("ScaleMultiplier")
                .vector3Value = Vector3.one;
            obstacleProperty.FindPropertyRelative("CollisionMode").intValue =
                (int)WarmupObstacleCollisionMode.UsePrefab;
            obstacleProperty.FindPropertyRelative("BossHitPoints").intValue =
                4;
            obstacleProperty.FindPropertyRelative("BossStopDistance")
                .floatValue = 1.6f;
        }

        private void DuplicateObstacle(int index)
        {
            if (index < 0 || index >= _phase.EventCount)
            {
                return;
            }

            StopScenePreview(
                "Preview đã dừng vì Timeline thay đổi.",
                MessageType.Info);
            Undo.RegisterCompleteObjectUndo(_phase, "Duplicate Obstacle");
            _phaseSerializedObject.Update();
            SerializedProperty eventsProperty =
                _phaseSerializedObject.FindProperty("events");
            SerializedProperty sourceProperty =
                eventsProperty.GetArrayElementAtIndex(index);
            float duplicatedTime = Mathf.Clamp(
                SnapTime(
                    sourceProperty
                        .FindPropertyRelative("EncounterTime")
                        .floatValue +
                    CalculateDuplicateTimeOffset()),
                0f,
                _phase.Duration);

            eventsProperty.InsertArrayElementAtIndex(index);
            int duplicatedIndex =
                Mathf.Min(index + 1, eventsProperty.arraySize - 1);
            SerializedProperty duplicatedProperty =
                eventsProperty.GetArrayElementAtIndex(duplicatedIndex);
            duplicatedProperty
                .FindPropertyRelative("EncounterTime")
                .floatValue = duplicatedTime;

            WarmupObstacleType duplicatedType =
                (WarmupObstacleType)duplicatedProperty
                    .FindPropertyRelative("Type")
                    .intValue;
            _phaseSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(_phase);
            _phaseSerializedObject.Update();

            WarmupObstacleEvent duplicatedReference =
                duplicatedIndex < _phase.EventCount
                    ? _phase.GetEvent(duplicatedIndex)
                    : null;
            int selectedIndex = FindEventIndex(duplicatedReference);
            if (selectedIndex < 0)
            {
                selectedIndex = FindClosestEventIndex(
                    duplicatedTime,
                    duplicatedType);
            }

            SelectObstacle(selectedIndex, true);
            InvalidateTooltipCache();
        }

        private float CalculateDuplicateTimeOffset()
        {
            float speed = Mathf.Max(0.1f, ResolveSpeed());
            return Mathf.Max(
                SnapInterval,
                SnapTime(MinimumRecommendedSpacing / speed));
        }

        private bool ConfirmRemoveObstacle(int index)
        {
            WarmupObstacleEvent obstacleEvent = _phase.GetEvent(index);
            if (!HasImportantConfiguration(obstacleEvent))
            {
                return true;
            }

            return EditorUtility.DisplayDialog(
                "Remove Obstacle",
                "Obstacle này có prefab/transform/behaviour được cấu hình. " +
                "Xóa event sẽ mất toàn bộ cấu hình đó.",
                "Remove",
                "Cancel");
        }

        private static bool HasImportantConfiguration(
            WarmupObstacleEvent obstacleEvent)
        {
            return obstacleEvent.PrefabOverride != null ||
                   obstacleEvent.PrefabVariation != 0 ||
                   obstacleEvent.PoseSprite != null ||
                   obstacleEvent.PositionOffset != Vector3.zero ||
                   obstacleEvent.RotationOffset != Vector3.zero ||
                   obstacleEvent.ScaleMultiplier != Vector3.one ||
                   obstacleEvent.CollisionMode !=
                   WarmupObstacleCollisionMode.UsePrefab ||
                   obstacleEvent.IsBossWall;
        }

        private void RemoveObstacle(int index)
        {
            if (index < 0 || index >= _phase.EventCount)
            {
                return;
            }

            StopScenePreview(
                "Preview đã dừng vì Timeline thay đổi.",
                MessageType.Info);
            Undo.RegisterCompleteObjectUndo(_phase, "Remove Obstacle");
            _phaseSerializedObject.Update();
            SerializedProperty eventsProperty =
                _phaseSerializedObject.FindProperty("events");
            eventsProperty.DeleteArrayElementAtIndex(index);
            _phaseSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(_phase);
            _phaseSerializedObject.Update();

            int nextIndex = Mathf.Min(index, _phase.EventCount - 1);
            EnsureExpandedState(_phase.EventCount, false);
            SelectObstacle(nextIndex, nextIndex >= 0);
            InvalidateTooltipCache();
        }

        private void MoveObstacle(int index, int direction)
        {
            int otherIndex = index + direction;
            if (index < 0 ||
                index >= _phase.EventCount ||
                otherIndex < 0 ||
                otherIndex >= _phase.EventCount)
            {
                return;
            }

            StopScenePreview(
                "Preview đã dừng vì Timeline thay đổi.",
                MessageType.Info);
            WarmupObstacleEvent selectedReference =
                _phase.GetEvent(index);
            Undo.RegisterCompleteObjectUndo(
                _phase,
                direction < 0
                    ? "Move Obstacle Up"
                    : "Move Obstacle Down");
            _phaseSerializedObject.Update();
            SerializedProperty eventsProperty =
                _phaseSerializedObject.FindProperty("events");
            SerializedProperty selectedTime =
                eventsProperty
                    .GetArrayElementAtIndex(index)
                    .FindPropertyRelative("EncounterTime");
            SerializedProperty otherTime =
                eventsProperty
                    .GetArrayElementAtIndex(otherIndex)
                    .FindPropertyRelative("EncounterTime");
            float time = selectedTime.floatValue;
            selectedTime.floatValue = otherTime.floatValue;
            otherTime.floatValue = time;
            _phaseSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(_phase);
            _phaseSerializedObject.Update();

            int selectedIndex = FindEventIndex(selectedReference);
            if (selectedIndex < 0)
            {
                selectedIndex = otherIndex;
            }

            SelectObstacle(selectedIndex, true);
            InvalidateTooltipCache();
        }

        private void SortObstaclesByTime()
        {
            WarmupObstacleEvent selectedReference =
                GetSelectedEventReference();
            _phaseSerializedObject.Update();
            SerializedProperty eventsProperty =
                _phaseSerializedObject.FindProperty("events");
            if (IsSortedByTime(eventsProperty))
            {
                ShowNotification(new GUIContent("Timeline đã được sort"));
                return;
            }

            StopScenePreview(
                "Preview đã dừng vì Timeline được sort.",
                MessageType.Info);
            Undo.RegisterCompleteObjectUndo(
                _phase,
                "Sort Obstacles By Time");
            for (int i = 1; i < eventsProperty.arraySize; i++)
            {
                float time =
                    eventsProperty
                        .GetArrayElementAtIndex(i)
                        .FindPropertyRelative("EncounterTime")
                        .floatValue;
                int destination = i;
                while (destination > 0)
                {
                    float previousTime =
                        eventsProperty
                            .GetArrayElementAtIndex(destination - 1)
                            .FindPropertyRelative("EncounterTime")
                            .floatValue;
                    if (previousTime <= time)
                    {
                        break;
                    }

                    destination--;
                }

                if (destination != i)
                {
                    eventsProperty.MoveArrayElement(i, destination);
                }
            }

            _phaseSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(_phase);
            _phaseSerializedObject.Update();

            int selectedIndex = FindEventIndex(selectedReference);
            if (selectedIndex >= 0)
            {
                SelectObstacle(selectedIndex, true);
            }
            else
            {
                ClampSelection();
            }

            InvalidateTooltipCache();
        }

        private static bool IsSortedByTime(
            SerializedProperty eventsProperty)
        {
            for (int i = 1; i < eventsProperty.arraySize; i++)
            {
                float previous =
                    eventsProperty
                        .GetArrayElementAtIndex(i - 1)
                        .FindPropertyRelative("EncounterTime")
                        .floatValue;
                float current =
                    eventsProperty
                        .GetArrayElementAtIndex(i)
                        .FindPropertyRelative("EncounterTime")
                        .floatValue;
                if (current < previous)
                {
                    return false;
                }
            }

            return true;
        }

        private float SnapTime(float time)
        {
            if (!_snapEnabled)
            {
                return time;
            }

            return Mathf.Round(time / SnapInterval) * SnapInterval;
        }

        private void SetAllExpanded(bool expanded)
        {
            EnsureExpandedState(_phase.EventCount, true);
            for (int i = 0; i < _expandedObstacles.Length; i++)
            {
                _expandedObstacles[i] = expanded;
            }

            Repaint();
        }

        private void EnsureExpandedState(int count, bool preserveExisting)
        {
            if (_expandedObstacles.Length == count)
            {
                return;
            }

            bool[] newState = new bool[count];
            if (preserveExisting)
            {
                int copyCount = Mathf.Min(
                    count,
                    _expandedObstacles.Length);
                for (int i = 0; i < copyCount; i++)
                {
                    newState[i] = _expandedObstacles[i];
                }
            }

            _expandedObstacles = newState;
        }

        private void SelectObstacle(int index, bool requestScroll)
        {
            if (_phase == null || _phase.EventCount == 0)
            {
                _selectedObstacleIndex = -1;
                _pendingScrollIndex = -1;
                return;
            }

            _selectedObstacleIndex =
                Mathf.Clamp(index, 0, _phase.EventCount - 1);
            EnsureExpandedState(_phase.EventCount, true);
            _expandedObstacles[_selectedObstacleIndex] = true;
            if (requestScroll)
            {
                _pendingScrollIndex = _selectedObstacleIndex;
            }

            GUI.FocusControl(null);
            Repaint();
        }

        private WarmupObstacleEvent GetSelectedEventReference()
        {
            if (_phase == null ||
                _selectedObstacleIndex < 0 ||
                _selectedObstacleIndex >= _phase.EventCount)
            {
                return null;
            }

            return _phase.GetEvent(_selectedObstacleIndex);
        }

        private int FindEventIndex(WarmupObstacleEvent eventReference)
        {
            if (eventReference == null || _phase == null)
            {
                return -1;
            }

            for (int i = 0; i < _phase.EventCount; i++)
            {
                if (ReferenceEquals(
                        _phase.GetEvent(i),
                        eventReference))
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindClosestEventIndex(float time)
        {
            return FindClosestEventIndex(time, null);
        }

        private int FindClosestEventIndex(
            float time,
            WarmupObstacleType? preferredType)
        {
            if (_phase == null || _phase.EventCount == 0)
            {
                return -1;
            }

            int closestIndex = -1;
            float closestDifference = float.MaxValue;
            for (int i = 0; i < _phase.EventCount; i++)
            {
                WarmupObstacleEvent obstacleEvent = _phase.GetEvent(i);
                if (preferredType.HasValue &&
                    obstacleEvent.Type != preferredType.Value)
                {
                    continue;
                }

                float difference =
                    Mathf.Abs(obstacleEvent.EncounterTime - time);
                if (difference <= closestDifference)
                {
                    closestDifference = difference;
                    closestIndex = i;
                }
            }

            if (closestIndex >= 0)
            {
                return closestIndex;
            }

            return FindClosestEventIndex(time, null);
        }

        private void ClampSelection()
        {
            if (_phase == null || _phase.EventCount == 0)
            {
                _selectedObstacleIndex = -1;
                _pendingScrollIndex = -1;
                EnsureExpandedState(0, false);
                return;
            }

            if (_selectedObstacleIndex >= _phase.EventCount)
            {
                _selectedObstacleIndex = _phase.EventCount - 1;
            }

            EnsureExpandedState(_phase.EventCount, true);
        }

        private void DrawValidation()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.BeginVertical(_panelStyle);
            EditorGUILayout.LabelField("VALIDATION", _sectionTitleStyle);
            EditorGUILayout.Space(6f);

            int issueCount = 0;
            float speed = ResolveSpeed();

            if (_prefabSet == null)
            {
                issueCount++;
                DrawValidationIssue(
                    "Thiếu Video0ObstaclePrefabSet. Apply đã bị khóa để bảo vệ dữ liệu.",
                    -1);
            }

            if (_phase.EventCount == 0)
            {
                issueCount++;
                DrawValidationIssue(
                    "Timeline chưa có obstacle.",
                    -1);
            }

            for (int i = 0; i < _phase.EventCount; i++)
            {
                WarmupObstacleEvent obstacleEvent =
                    _phase.GetEvent(i);

                if (obstacleEvent.EncounterTime < 0f ||
                    obstacleEvent.EncounterTime > _phase.Duration)
                {
                    issueCount++;
                    DrawValidationIssue(
                        "Event " + (i + 1) +
                        ": Encounter Time nằm ngoài 0..Duration.",
                        i);
                }

                if (HasDuplicateTime(i, obstacleEvent.EncounterTime))
                {
                    issueCount++;
                    DrawValidationIssue(
                        "Event " + (i + 1) +
                        ": trùng Encounter Time " +
                        obstacleEvent.EncounterTime.ToString("0.###") +
                        "s.",
                        i);
                }

                if (i > 0)
                {
                    float spacing =
                        (obstacleEvent.EncounterTime -
                         _phase.GetEvent(i - 1).EncounterTime) *
                        speed;
                    if (spacing < MinimumRecommendedSpacing)
                    {
                        issueCount++;
                        DrawValidationIssue(
                            "Event " + (i + 1) +
                            ": spacing với obstacle trước chỉ " +
                            spacing.ToString("0.0") + "m (< " +
                            MinimumRecommendedSpacing.ToString("0") +
                            "m).",
                            i);
                    }
                }

                if (!HasResolvedPrefab(obstacleEvent))
                {
                    issueCount++;
                    DrawValidationIssue(
                        "Event " + (i + 1) +
                        ": thiếu prefab cho " +
                        GetTypeDisplayName(obstacleEvent.Type) + ".",
                        i);
                }

                if (obstacleEvent.IsBossWall &&
                    obstacleEvent.BossHitPoints <= 0)
                {
                    issueCount++;
                    DrawValidationIssue(
                        "Event " + (i + 1) +
                        ": Boss HP phải lớn hơn 0.",
                        i);
                }

                Vector3 scale = obstacleEvent.ScaleMultiplier;
                if (scale.x <= 0f ||
                    scale.y <= 0f ||
                    scale.z <= 0f)
                {
                    issueCount++;
                    DrawValidationIssue(
                        "Event " + (i + 1) +
                        ": Scale Multiplier có component bằng 0 hoặc âm.",
                        i);
                }
            }

            if (issueCount == 0)
            {
                DrawStatus(
                    "Sẵn sàng test: timing, spacing, prefab và config đều hợp lệ.",
                    SuccessColor);
            }
            else
            {
                EditorGUILayout.Space(3f);
                EditorGUILayout.LabelField(
                    issueCount + " issue(s). Click issue để mở đúng obstacle.",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private bool HasDuplicateTime(int index, float time)
        {
            for (int i = 0; i < index; i++)
            {
                if (Mathf.Abs(
                        _phase.GetEvent(i).EncounterTime - time) <=
                    FloatComparisonTolerance)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasResolvedPrefab(
            WarmupObstacleEvent obstacleEvent)
        {
            if (obstacleEvent.PrefabOverride != null)
            {
                return true;
            }

            return _prefabSet != null &&
                   _prefabSet.HasPrefab(obstacleEvent.Type);
        }

        private void DrawValidationIssue(string message, int obstacleIndex)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 26f);
            Color background = WarningColor;
            background.a = EditorGUIUtility.isProSkin ? 0.12f : 0.09f;
            EditorGUI.DrawRect(rect, background);
            EditorGUI.DrawRect(
                new Rect(rect.x, rect.y + 2f, 4f, rect.height - 4f),
                WarningColor);

            if (GUI.Button(
                    new Rect(
                        rect.x + 8f,
                        rect.y,
                        rect.width - 8f,
                        rect.height),
                    message,
                    _validationButtonStyle) &&
                obstacleIndex >= 0)
            {
                SelectObstacle(obstacleIndex, true);
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.BeginVertical(_panelStyle);
            EditorGUILayout.LabelField("ACTIONS", _sectionTitleStyle);
            EditorGUILayout.Space(7f);

            EditorGUILayout.BeginHorizontal();
            Color previousBackground = GUI.backgroundColor;
            GUI.backgroundColor = SuccessColor;
            if (GUILayout.Button(
                "APPLY STEP " + _selectedStep + " TO SCENE",
                GUILayout.Height(36f)))
            {
                if (_prefabSet == null)
                {
                    EditorUtility.DisplayDialog(
                        "Cannot Apply Step",
                        "Thiếu Video0ObstaclePrefabSet. Dashboard không tự rebuild " +
                        "template để tránh ghi đè dữ liệu Step 1–6.",
                        "OK");
                }
                else
                {
                    StopScenePreview(
                        "Preview đã dừng trước khi Apply Step.",
                        MessageType.Info);
                    SavePendingChanges();
                    WarmupObstacleTimelineSetup.ApplyStepToScene(
                        _selectedStep);
                    ShowNotification(
                        new GUIContent(
                            "Đã Apply Step " +
                            _selectedStep +
                            " vào scene"));
                }
            }

            GUI.backgroundColor = previousBackground;
            if (GUILayout.Button(
                "SELECT PHASE ASSET",
                GUILayout.Height(36f)))
            {
                Selection.activeObject = _phase;
                EditorGUIUtility.PingObject(_phase);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(
                "OPEN WARMUP SCENE",
                GUILayout.Height(30f)))
            {
                StopScenePreview(
                    "Preview đã dừng trước khi mở WarmUp scene.",
                    MessageType.Info);
                SavePendingChanges();
                EditorSceneManager.OpenScene(
                    "Assets/_GAME/_Scenes/WarnUp.unity",
                    OpenSceneMode.Single);
            }

            if (GUILayout.Button(
                "SELECT PLAYER CONFIG",
                GUILayout.Height(30f)))
            {
                Selection.activeObject = _playerConfig;
                EditorGUIUtility.PingObject(_playerConfig);
            }

            if (GUILayout.Button(
                "REBUILD VIDEO0 TEMPLATE",
                GUILayout.Height(30f)))
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Rebuild Video0 Template",
                    "Thao tác này ghi lại dữ liệu mẫu Step 1–6.",
                    "Rebuild",
                    "Hủy");
                if (confirmed)
                {
                    StopScenePreview(
                        "Preview đã dừng trước khi rebuild template.",
                        MessageType.Info);
                    WarmupObstacleTimelineSetup.BuildVideo0Demo();
                    LoadData(false);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawMissingData()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.BeginVertical(_panelStyle);
            EditorGUILayout.HelpBox(
                "Chưa tìm thấy Timeline hoặc WarmupPlayerConfig. " +
                "Dashboard sẽ không tự tạo hay ghi đè template.",
                MessageType.Warning);

            if (GUILayout.Button(
                "BUILD VIDEO0 DEMO (EXPLICIT)",
                GUILayout.Height(38f)))
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Build Video0 Demo",
                    "Thao tác này có thể ghi lại dữ liệu mẫu Step 1–6.",
                    "Build",
                    "Cancel");
                if (confirmed)
                {
                    WarmupObstacleTimelineSetup.BuildVideo0Demo();
                    LoadData(false);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMetric(string value, string label)
        {
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(100f));
            EditorGUILayout.LabelField(value, _metricValueStyle);
            EditorGUILayout.LabelField(label, _metricLabelStyle);
            EditorGUILayout.EndVertical();
        }

        private void DrawStatus(string message, Color color)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 24f);
            EditorGUI.DrawRect(
                new Rect(rect.x, rect.y + 2f, 4f, rect.height - 4f),
                color);
            GUI.Label(
                new Rect(rect.x + 11f, rect.y, rect.width - 11f, rect.height),
                message,
                _statusStyle);
        }

        private void LoadData(bool preserveSelection)
        {
            WarmupPhaseTimelineAsset previousPhase = _phase;
            int previousSelection = _selectedObstacleIndex;

            _phase = WarmupObstacleTimelineSetup.LoadPhase(_selectedStep);
            _playerConfig =
                WarmupObstacleTimelineSetup.LoadPlayerConfig();
            _prefabSet =
                WarmupObstacleTimelineSetup.LoadVideo0PrefabSet();
            _phaseSerializedObject =
                _phase != null ? new SerializedObject(_phase) : null;
            _playerSerializedObject =
                _playerConfig != null
                    ? new SerializedObject(_playerConfig)
                    : null;

            bool canPreserve =
                preserveSelection && previousPhase == _phase;
            _playheadTime =
                previousPhase == _phase && _phase != null
                    ? Mathf.Clamp(
                        _playheadTime,
                        0f,
                        _phase.Duration)
                    : 0f;
            _selectedObstacleIndex =
                canPreserve ? previousSelection : -1;
            EnsureExpandedState(
                _phase != null ? _phase.EventCount : 0,
                canPreserve);
            ClampSelection();
            InvalidateTooltipCache();
        }

        private void StartScenePreview()
        {
            if (_previewController == null)
            {
                _previewController =
                    new WarmupTimelinePreviewController();
            }

            _playheadTime = Mathf.Clamp(
                _playheadTime,
                0f,
                _phase.Duration);
            if (_previewController.StartPreview(
                    _phase,
                    _playerConfig,
                    _prefabSet,
                    _playheadTime))
            {
                _playheadTime = _previewController.CurrentTime;
            }

            Repaint();
        }

        private void StopScenePreview(
            string message,
            MessageType messageType)
        {
            if (_previewController == null ||
                !_previewController.IsActive)
            {
                return;
            }

            _previewController.StopPreview(message, messageType);
            Repaint();
        }

        private void SetPlayheadTime(float time)
        {
            float clampedTime = Mathf.Clamp(
                time,
                0f,
                _phase != null ? _phase.Duration : 0f);
            bool changed =
                !Mathf.Approximately(
                    _playheadTime,
                    clampedTime);
            _playheadTime = clampedTime;

            if (_previewController != null &&
                _previewController.IsActive)
            {
                _previewController.SetTime(_playheadTime);
            }

            if (changed)
            {
                Repaint();
            }
        }

        private void SavePendingChanges()
        {
            if (_playerSerializedObject != null &&
                _playerSerializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(_playerConfig);
            }

            if (_phaseSerializedObject != null &&
                _phaseSerializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(_phase);
            }

            AssetDatabase.SaveAssets();
        }

        private void HandleUndoRedo()
        {
            CancelMarkerDrag();
            StopScenePreview(
                "Preview đã dừng vì Undo/Redo thay đổi dữ liệu.",
                MessageType.Info);
            if (_phaseSerializedObject != null)
            {
                _phaseSerializedObject.Update();
            }

            if (_playerSerializedObject != null)
            {
                _playerSerializedObject.Update();
            }

            ClampSelection();
            InvalidateTooltipCache();
            Repaint();
        }

        private float ResolveSpeed()
        {
            return _phase.ResolveRunSpeed(_playerConfig.AutoRunSpeed);
        }

        private float CalculateMinimumSpacing(float speed)
        {
            if (_phase.EventCount < 2)
            {
                return 0f;
            }

            float minimum = float.MaxValue;
            float previousTime = _phase.GetEvent(0).EncounterTime;
            for (int i = 1; i < _phase.EventCount; i++)
            {
                float currentTime = _phase.GetEvent(i).EncounterTime;
                minimum = Mathf.Min(
                    minimum,
                    (currentTime - previousTime) * speed);
                previousTime = currentTime;
            }

            return Mathf.Max(0f, minimum);
        }

        private float CalculateAverageSpacing(float speed)
        {
            if (_phase.EventCount < 2)
            {
                return 0f;
            }

            float firstTime = _phase.GetEvent(0).EncounterTime;
            float lastTime =
                _phase.GetEvent(_phase.EventCount - 1).EncounterTime;
            return Mathf.Max(
                0f,
                (lastTime - firstTime) *
                speed /
                (_phase.EventCount - 1));
        }

        private void InvalidateTooltipCache()
        {
            _cachedTooltipIndex = -1;
            _cachedTooltipTime = float.MinValue;
            _cachedTooltipSpeed = float.MinValue;
            _cachedTooltipText = string.Empty;
        }

        private static string GetTypeDisplayName(
            WarmupObstacleType type)
        {
            switch (type)
            {
                case WarmupObstacleType.Jump:
                    return "Jump";
                case WarmupObstacleType.PoseWall:
                    return "Pose";
                case WarmupObstacleType.DuckBarrier:
                    return "Duck";
                case WarmupObstacleType.LaneBlocker:
                    return "Lane Blocker";
                case WarmupObstacleType.BossWall:
                    return "Boss";
                default:
                    return type.ToString();
            }
        }

        private static Color GetEventColor(WarmupObstacleType type)
        {
            switch (type)
            {
                case WarmupObstacleType.Jump:
                    return new Color32(64, 173, 255, 255);
                case WarmupObstacleType.PoseWall:
                    return new Color32(181, 112, 255, 255);
                case WarmupObstacleType.DuckBarrier:
                    return new Color32(77, 214, 148, 255);
                case WarmupObstacleType.LaneBlocker:
                    return new Color32(255, 180, 72, 255);
                case WarmupObstacleType.BossWall:
                    return new Color32(255, 82, 95, 255);
                default:
                    return Color.white;
            }
        }

        private void EnsureStyles()
        {
            if (_headerTitleStyle != null)
            {
                ApplyStylePalette();
                return;
            }

            _headerTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 22
            };

            _headerSubtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12
            };

            _sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };

            _metricValueStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 17,
                alignment = TextAnchor.MiddleCenter
            };
            _metricLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };

            _panelStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(14, 14, 12, 12)
            };
            _cardStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 7, 7)
            };
            _cardHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };
            _cardSpacingStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleRight,
                clipping = TextClipping.Clip
            };
            _statusStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft
            };
            _validationButtonStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };
            _timelineTooltipStyle = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            _summaryStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleLeft
            };

            ApplyStylePalette();
        }

        private void ApplyStylePalette()
        {
            Color primaryText = EditorGUIUtility.isProSkin
                ? new Color32(236, 241, 248, 255)
                : new Color32(32, 39, 48, 255);
            Color secondaryText = EditorGUIUtility.isProSkin
                ? new Color32(190, 201, 216, 255)
                : new Color32(70, 80, 94, 255);
            Color sectionText = EditorGUIUtility.isProSkin
                ? new Color32(91, 195, 255, 255)
                : new Color32(22, 105, 156, 255);

            SetStyleTextColor(
                _headerTitleStyle,
                Color.white,
                Color.white);
            SetStyleTextColor(
                _headerSubtitleStyle,
                new Color32(205, 220, 239, 255),
                Color.white);
            SetStyleTextColor(
                _sectionTitleStyle,
                sectionText,
                sectionText);
            SetStyleTextColor(
                _metricValueStyle,
                primaryText,
                primaryText);
            SetStyleTextColor(
                _metricLabelStyle,
                secondaryText,
                primaryText);
            SetStyleTextColor(
                _cardHeaderStyle,
                primaryText,
                Color.white);
            SetStyleTextColor(
                _cardSpacingStyle,
                secondaryText,
                primaryText);
            SetStyleTextColor(
                _statusStyle,
                primaryText,
                primaryText);
            SetStyleTextColor(
                _validationButtonStyle,
                primaryText,
                sectionText);
            SetStyleTextColor(
                _timelineTooltipStyle,
                Color.white,
                Color.white);
            SetStyleTextColor(
                _summaryStyle,
                primaryText,
                primaryText);
        }

        private static void SetStyleTextColor(
            GUIStyle style,
            Color normalColor,
            Color hoverColor)
        {
            style.normal.textColor = normalColor;
            style.hover.textColor = hoverColor;
            style.active.textColor = hoverColor;
            style.focused.textColor = hoverColor;
            style.onNormal.textColor = normalColor;
            style.onHover.textColor = hoverColor;
            style.onActive.textColor = hoverColor;
            style.onFocused.textColor = hoverColor;
        }
    }
}
#endif
