#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameYT.Warmup.Editor
{
    public sealed class WarmupObstacleTimelineWindow : EditorWindow
    {
        private const float MinimumRecommendedSpacing = 30f;

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
        private Vector2 _scrollPosition;
        private WarmupPhaseTimelineAsset _phase;
        private WarmupPlayerConfig _playerConfig;
        private WarmupObstaclePrefabSet _prefabSet;
        private SerializedObject _phaseSerializedObject;
        private SerializedObject _playerSerializedObject;

        private GUIStyle _headerTitleStyle;
        private GUIStyle _headerSubtitleStyle;
        private GUIStyle _sectionTitleStyle;
        private GUIStyle _metricValueStyle;
        private GUIStyle _metricLabelStyle;
        private GUIStyle _panelStyle;
        private GUIStyle _statusStyle;

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
            LoadData();
        }

        private void OnProjectChange()
        {
            LoadData();
            Repaint();
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawHeader();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawStepSelector();

            if (_phase == null || _playerConfig == null)
            {
                DrawMissingData();
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawOverview();
            DrawQuickSettings();
            DrawTimelinePreview();
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
                "Tune tốc độ, kiểm tra spacing và Apply Step vào scene.",
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
                _selectedStep = selectedIndex + 1;
                LoadData();
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
            EditorGUILayout.LabelField(
                _phase.DisplayName,
                _sectionTitleStyle);
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

            _playerSerializedObject.Update();
            _phaseSerializedObject.Update();

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

            if ((WarmupRunSpeedSource)speedSource.enumValueIndex ==
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
                _playerSerializedObject.ApplyModifiedProperties();
                _phaseSerializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_playerConfig);
                EditorUtility.SetDirty(_phase);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "Khi dùng Player Config, obstacle được đặt theo đúng tốc độ thực tế. " +
                "Đổi speed không làm lệch encounter time.",
                MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void DrawTimelinePreview()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.BeginVertical(_panelStyle);
            EditorGUILayout.LabelField(
                "TIMELINE PREVIEW",
                _sectionTitleStyle);
            EditorGUILayout.Space(6f);

            Rect timelineRect =
                GUILayoutUtility.GetRect(100f, 118f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(timelineRect, TimelineColor);
            DrawTimelineGrid(timelineRect);
            DrawTimelineEvents(timelineRect);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Hover marker để xem loại obstacle, lane và khoảng cách.",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawTimelineGrid(Rect rect)
        {
            const int divisions = 4;
            for (int i = 0; i <= divisions; i++)
            {
                float normalized = i / (float)divisions;
                float x = Mathf.Lerp(
                    rect.x + 12f,
                    rect.xMax - 12f,
                    normalized);
                EditorGUI.DrawRect(
                    new Rect(x, rect.y + 22f, 1f, rect.height - 42f),
                    new Color(1f, 1f, 1f, 0.12f));

                GUI.Label(
                    new Rect(x - 18f, rect.y + 3f, 40f, 18f),
                    (_phase.Duration * normalized).ToString("0") + "s",
                    EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void DrawTimelineEvents(Rect rect)
        {
            float speed = ResolveSpeed();
            float startX = rect.x + 12f;
            float endX = rect.xMax - 12f;

            for (int i = 0; i < _phase.EventCount; i++)
            {
                WarmupObstacleEvent obstacleEvent = _phase.GetEvent(i);
                float normalized = Mathf.Clamp01(
                    obstacleEvent.EncounterTime /
                    Mathf.Max(0.01f, _phase.Duration));
                float x = Mathf.Lerp(startX, endX, normalized);
                float y = rect.center.y + (i % 2 == 0 ? -15f : 15f);
                var markerRect = new Rect(x - 6f, y - 11f, 12f, 22f);

                EditorGUI.DrawRect(
                    markerRect,
                    GetEventColor(obstacleEvent.Type));
                EditorGUIUtility.AddCursorRect(
                    markerRect,
                    MouseCursor.Link);

                if (!markerRect.Contains(Event.current.mousePosition))
                {
                    continue;
                }

                string tooltip =
                    obstacleEvent.EncounterTime.ToString("0.0") + "s • " +
                    obstacleEvent.Type + " • " +
                    obstacleEvent.Lane + " • " +
                    (obstacleEvent.EncounterTime * speed).ToString("0") + "m";
                float tooltipX = Mathf.Clamp(
                    x - 90f,
                    rect.x + 4f,
                    rect.xMax - 184f);
                GUI.Label(
                    new Rect(tooltipX, rect.yMax - 23f, 180f, 18f),
                    tooltip,
                    EditorStyles.centeredGreyMiniLabel);
                Repaint();
            }
        }

        private void DrawValidation()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.BeginVertical(_panelStyle);
            EditorGUILayout.LabelField("VALIDATION", _sectionTitleStyle);
            EditorGUILayout.Space(6f);

            int issueCount = 0;
            float minimumSpacing = CalculateMinimumSpacing(ResolveSpeed());

            if (_prefabSet == null)
            {
                issueCount++;
                DrawStatus("Thiếu Video0ObstaclePrefabSet.", WarningColor);
            }

            if (_phase.EventCount == 0)
            {
                issueCount++;
                DrawStatus("Timeline chưa có obstacle.", WarningColor);
            }

            if (_phase.EventCount > 1 &&
                minimumSpacing < MinimumRecommendedSpacing)
            {
                issueCount++;
                DrawStatus(
                    "Obstacle gần nhất chỉ cách " +
                    minimumSpacing.ToString("0.0") +
                    "m. Khuyến nghị tối thiểu " +
                    MinimumRecommendedSpacing.ToString("0") + "m.",
                    WarningColor);
            }

            for (int i = 0; i < _phase.EventCount; i++)
            {
                WarmupObstacleEvent obstacleEvent = _phase.GetEvent(i);
                if (obstacleEvent.EncounterTime >= 0f &&
                    obstacleEvent.EncounterTime <= _phase.Duration)
                {
                    continue;
                }

                issueCount++;
                DrawStatus(
                    "Event " + (i + 1) + " nằm ngoài Duration.",
                    WarningColor);
            }

            if (issueCount == 0)
            {
                DrawStatus(
                    "Sẵn sàng test: tốc độ, timing và spacing đều hợp lệ.",
                    SuccessColor);
            }

            EditorGUILayout.EndVertical();
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
                SavePendingChanges();
                WarmupObstacleTimelineSetup.ApplyStepToScene(_selectedStep);
                ShowNotification(
                    new GUIContent(
                        "Đã Apply Step " + _selectedStep + " vào scene"));
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
            if (GUILayout.Button("OPEN WARMUP SCENE", GUILayout.Height(30f)))
            {
                SavePendingChanges();
                EditorSceneManager.OpenScene(
                    "Assets/_GAME/_Scenes/WarnUp.unity",
                    OpenSceneMode.Single);
            }

            if (GUILayout.Button("SELECT PLAYER CONFIG", GUILayout.Height(30f)))
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
                    WarmupObstacleTimelineSetup.BuildVideo0Demo();
                    LoadData();
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
                "Chưa tìm thấy Timeline hoặc WarmupPlayerConfig.",
                MessageType.Warning);

            if (GUILayout.Button("BUILD VIDEO0 DEMO", GUILayout.Height(38f)))
            {
                WarmupObstacleTimelineSetup.BuildVideo0Demo();
                LoadData();
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

        private void LoadData()
        {
            _phase = WarmupObstacleTimelineSetup.LoadPhase(_selectedStep);
            _playerConfig = WarmupObstacleTimelineSetup.LoadPlayerConfig();
            _prefabSet = WarmupObstacleTimelineSetup.LoadVideo0PrefabSet();
            _phaseSerializedObject =
                _phase != null ? new SerializedObject(_phase) : null;
            _playerSerializedObject =
                _playerConfig != null
                    ? new SerializedObject(_playerConfig)
                    : null;
        }

        private void SavePendingChanges()
        {
            _playerSerializedObject?.ApplyModifiedProperties();
            _phaseSerializedObject?.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
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
                (lastTime - firstTime) * speed / (_phase.EventCount - 1));
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
                return;
            }

            _headerTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 22
            };
            _headerTitleStyle.normal.textColor = Color.white;

            _headerSubtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12
            };
            _headerSubtitleStyle.normal.textColor =
                new Color(0.78f, 0.84f, 0.92f);

            _sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };
            _sectionTitleStyle.normal.textColor = AccentColor;

            _metricValueStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 17,
                alignment = TextAnchor.MiddleCenter
            };
            _metricLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            _metricLabelStyle.normal.textColor =
                new Color(0.55f, 0.6f, 0.68f);

            _panelStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(14, 14, 12, 12)
            };
            _statusStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft
            };
        }
    }
}
#endif
