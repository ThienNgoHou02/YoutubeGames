#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameYT.Warmup.Editor
{
    public static class WarmupObstacleTimelineSetup
    {
        private const string DataFolder =
            "Assets/_GAME/_Data/ObstacleTimeline";
        private const string PrefabSetFolder =
            "Assets/_GAME/_Data/PrefabSet";
        private const string Video0SetPath =
            PrefabSetFolder + "/Video0.asset";
        private const string PhasePathFormat =
            DataFolder + "/Step{0}Timeline.asset";
        private const string ScenePath =
            "Assets/_GAME/_Scenes/WarnUp.unity";
        private const string PlayerPrefabPath =
            "Assets/_GAME/_Scripts/Player/Player.prefab";
        private const string HudPrefabPath =
            "Assets/_GAME/_Scripts/Warmup/UI/Warmup HUD.prefab";
        private const string Video0PrefabFolder =
            "Assets/_GAME/obstacle_Prefab/Video0";
        private const string DefaultPoseSpriteSheetPath =
            "Assets/_GAME/Art/DongtacWarnUP.png";
        private const string PaperShardMaterialPath =
            "Assets/_GAME/_Materials/WarmupPaperShard.mat";
        private const string PlayerConfigPath =
            "Assets/_GAME/_Data/Warmup/WarmupPlayerConfig.asset";

        [MenuItem("Tools/Immersive Warmup/Obstacle Timeline/Open Timeline Tool", priority = 1)]
        private static void OpenTimelineTool()
        {
            WarmupObstacleTimelineWindow.Open();
        }

        [MenuItem("Tools/Immersive Warmup/Obstacle Timeline/Build Video0 Demo")]
        public static void BuildVideo0Demo()
        {
            EnsureFolder(DataFolder);
            AssetDatabase.Refresh();

            WarmupObstaclePrefabSet prefabSet = CreateVideo0PrefabSet();
            WarmupPhaseTimelineAsset[] phases = CreatePhaseTemplates();
            CreatePaperShardMaterial();
            ConfigureHudPrefab();
            ConfigureScene(phases[0], prefabSet);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = phases[0];
            EditorGUIUtility.PingObject(phases[0]);
            Debug.Log(
                "Đã tạo Step 1-6, Video0 Prefab Set và nối Step 1 vào WarnUp scene.");
        }

        [MenuItem("Tools/Immersive Warmup/Obstacle Timeline/Select Video0 Prefab Set")]
        private static void SelectVideo0PrefabSet()
        {
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<WarmupObstaclePrefabSet>(
                    Video0SetPath);
        }

        [MenuItem("Tools/Immersive Warmup/Obstacle Timeline/Apply Step 1")]
        private static void ApplyStep1()
        {
            ApplyStepToScene(1);
        }

        [MenuItem("Tools/Immersive Warmup/Obstacle Timeline/Apply Step 2")]
        private static void ApplyStep2()
        {
            ApplyStepToScene(2);
        }

        [MenuItem("Tools/Immersive Warmup/Obstacle Timeline/Apply Step 3")]
        private static void ApplyStep3()
        {
            ApplyStepToScene(3);
        }

        [MenuItem("Tools/Immersive Warmup/Obstacle Timeline/Apply Step 4")]
        private static void ApplyStep4()
        {
            ApplyStepToScene(4);
        }

        [MenuItem("Tools/Immersive Warmup/Obstacle Timeline/Apply Step 5")]
        private static void ApplyStep5()
        {
            ApplyStepToScene(5);
        }

        [MenuItem("Tools/Immersive Warmup/Obstacle Timeline/Apply Step 6")]
        private static void ApplyStep6()
        {
            ApplyStepToScene(6);
        }

        public static void RunBatchSetup()
        {
            BuildVideo0Demo();
        }

        public static void ApplyStepToScene(int stepNumber)
        {
            WarmupPhaseTimelineAsset phase = LoadPhase(stepNumber);
            WarmupObstaclePrefabSet prefabSet = LoadVideo0PrefabSet();

            if (phase == null || prefabSet == null)
            {
                BuildVideo0Demo();
                phase = LoadPhase(stepNumber);
                prefabSet = LoadVideo0PrefabSet();
            }

            ConfigureScene(phase, prefabSet);
            AssetDatabase.SaveAssets();
            Selection.activeObject = phase;
            EditorGUIUtility.PingObject(phase);
        }

        public static WarmupPhaseTimelineAsset LoadPhase(int stepNumber)
        {
            return AssetDatabase.LoadAssetAtPath<WarmupPhaseTimelineAsset>(
                string.Format(PhasePathFormat, Mathf.Clamp(stepNumber, 1, 6)));
        }

        public static WarmupObstaclePrefabSet LoadVideo0PrefabSet()
        {
            return AssetDatabase.LoadAssetAtPath<WarmupObstaclePrefabSet>(
                Video0SetPath);
        }

        public static WarmupPlayerConfig LoadPlayerConfig()
        {
            return AssetDatabase.LoadAssetAtPath<WarmupPlayerConfig>(
                PlayerConfigPath);
        }

        private static WarmupObstaclePrefabSet CreateVideo0PrefabSet()
        {
            WarmupObstaclePrefabSet asset =
                AssetDatabase.LoadAssetAtPath<WarmupObstaclePrefabSet>(
                    Video0SetPath);
            if (asset != null)
            {
                return asset;
            }

            EnsureFolder(PrefabSetFolder);
            asset = LoadOrCreateAsset<WarmupObstaclePrefabSet>(Video0SetPath);
            GameObject cube = AssetDatabase.LoadAssetAtPath<GameObject>(
                Video0PrefabFolder + "/Cube.prefab");
            GameObject poseA = AssetDatabase.LoadAssetAtPath<GameObject>(
                Video0PrefabFolder + "/Boss.prefab");
            GameObject poseB = AssetDatabase.LoadAssetAtPath<GameObject>(
                Video0PrefabFolder + "/Boss1.prefab");

            asset.SetData(
                "Video0",
                ArrayOf(cube),
                CompactArray(poseA, poseB),
                ArrayOf(cube),
                ArrayOf(cube),
                ArrayOf(cube));
            return asset;
        }

        private static WarmupPhaseTimelineAsset[] CreatePhaseTemplates()
        {
            var phases = new WarmupPhaseTimelineAsset[6];
            phases[0] = CreatePhase(
                1,
                "Step 1 - Run & Jump",
                40f,
                new[]
                {
                    Jump(6f), Jump(11f), Jump(16.5f), Jump(23f),
                    Jump(29.5f), Jump(35.5f)
                });

            phases[1] = CreatePhase(
                2,
                "Step 2 - Hole in the Wall",
                60f,
                new[]
                {
                    MirrorPose(5f), MirrorPose(10.5f), MirrorPose(16f),
                    MirrorPose(21.5f), MirrorPose(27f), MirrorPose(32.5f),
                    MirrorPose(38f), MirrorPose(43.5f), MirrorPose(49f),
                    MirrorPose(55f)
                });
            phases[1].SetPoseLibrary(
                LoadDefaultPoseSprites(),
                20260730);

            phases[2] = CreatePhase(
                3,
                "Step 3 - Jump, Pose & Duck",
                72f,
                new[]
                {
                    Pose(5f, 0), Jump(10f), Duck(15f), Pose(20f, 1),
                    Jump(25f), Pose(31f, 0), Duck(37f), Jump(43f),
                    Pose(49f, 1), Duck(55f), Jump(61f), Pose(67f, 0)
                });

            phases[3] = CreatePhase(
                4,
                "Step 4 - Full Body & Lane Dodge",
                66f,
                new[]
                {
                    Jump(5f),
                    Pose(10f, 0),
                    LaneBlock(15f, WarmupLane.Left),
                    LaneBlock(20f, WarmupLane.Right),
                    Duck(25f),
                    Pose(30f, 1),
                    LaneBlock(35f, WarmupLane.Right),
                    Jump(40f),
                    LaneBlock(45f, WarmupLane.Left),
                    Duck(50f),
                    Pose(55f, 0),
                    Jump(61f)
                });

            phases[4] = CreatePhase(
                5,
                "Step 5 - Full Combo & Boss Wall",
                68f,
                new[]
                {
                    Jump(5f),
                    Pose(10f, 0),
                    LaneBlock(15f, WarmupLane.Left),
                    Duck(20f),
                    LaneBlock(25f, WarmupLane.Right),
                    Pose(30f, 1),
                    Jump(35f),
                    LaneBlock(40f, WarmupLane.Right),
                    Duck(45f),
                    LaneBlock(50f, WarmupLane.Left),
                    Boss(56f),
                    Pose(64f, 0)
                });

            phases[5] = CreatePhase(
                6,
                "Step 6 - Dense Full Combo",
                64f,
                new[]
                {
                    Jump(4f),
                    LaneBlock(8f, WarmupLane.Left),
                    Pose(12f, 0),
                    Duck(16f),
                    LaneBlock(20f, WarmupLane.Right),
                    Jump(24f),
                    Pose(28f, 1),
                    LaneBlock(32f, WarmupLane.Right),
                    Duck(36f),
                    LaneBlock(40f, WarmupLane.Left),
                    Jump(44f),
                    Pose(48f, 0),
                    Boss(53f),
                    Duck(59f)
                });

            return phases;
        }

        private static WarmupPhaseTimelineAsset CreatePhase(
            int stepNumber,
            string displayName,
            float duration,
            WarmupObstacleEvent[] events)
        {
            string path = string.Format(PhasePathFormat, stepNumber);
            WarmupPhaseTimelineAsset asset =
                LoadOrCreateAsset<WarmupPhaseTimelineAsset>(path);
            asset.SetData(stepNumber, displayName, duration, 6f, events);
            return asset;
        }

        private static WarmupObstacleEvent Jump(float time)
        {
            return CreateEvent(
                time,
                WarmupObstacleType.Jump,
                WarmupActionType.Jump,
                "JUMP!",
                WarmupLane.Center,
                0,
                new Vector3(0f, 0.45f, 0f),
                new Vector3(1f, 0.3f, 1f),
                WarmupObstacleCollisionMode.UsePrefab);
        }

        private static WarmupObstacleEvent Pose(float time, int variation)
        {
            return CreateEvent(
                time,
                WarmupObstacleType.PoseWall,
                WarmupActionType.Freeze,
                "POSE!",
                WarmupLane.Center,
                variation,
                new Vector3(0f, 3.7f, 0f),
                Vector3.one,
                WarmupObstacleCollisionMode.DisableAll);
        }

        private static WarmupObstacleEvent MirrorPose(float time)
        {
            WarmupObstacleEvent obstacleEvent = Pose(time, 0);
            obstacleEvent.PrefabOverride =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    Video0PrefabFolder + "/Mirror.prefab");
            obstacleEvent.PositionOffset = Vector3.zero;
            return obstacleEvent;
        }

        private static Sprite[] LoadDefaultPoseSprites()
        {
            UnityEngine.Object[] assets =
                AssetDatabase.LoadAllAssetRepresentationsAtPath(
                    DefaultPoseSpriteSheetPath);
            var sprites = new List<Sprite>(assets.Length);

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }

            sprites.Sort(ComparePoseSpriteNames);
            return sprites.ToArray();
        }

        private static int ComparePoseSpriteNames(Sprite left, Sprite right)
        {
            int indexComparison =
                GetPoseSpriteIndex(left).CompareTo(
                    GetPoseSpriteIndex(right));
            return indexComparison != 0
                ? indexComparison
                : string.CompareOrdinal(left.name, right.name);
        }

        private static int GetPoseSpriteIndex(Sprite sprite)
        {
            int separatorIndex = sprite.name.LastIndexOf('_');
            if (separatorIndex < 0 ||
                separatorIndex >= sprite.name.Length - 1)
            {
                return int.MaxValue;
            }

            return int.TryParse(
                sprite.name.Substring(separatorIndex + 1),
                out int index)
                ? index
                : int.MaxValue;
        }

        private static WarmupObstacleEvent Duck(float time)
        {
            return CreateEvent(
                time,
                WarmupObstacleType.DuckBarrier,
                WarmupActionType.Duck,
                "DUCK!",
                WarmupLane.Center,
                0,
                new Vector3(0f, 1.85f, 0f),
                new Vector3(1f, 0.2f, 1f),
                WarmupObstacleCollisionMode.UsePrefab);
        }

        private static WarmupObstacleEvent LaneBlock(
            float time,
            WarmupLane spawnSide)
        {
            WarmupActionType sideAction;
            string cueLabel;
            switch (spawnSide)
            {
                case WarmupLane.Left:
                    sideAction = WarmupActionType.MoveLeft;
                    cueLabel = "LEFT!";
                    break;
                case WarmupLane.Right:
                    sideAction = WarmupActionType.MoveRight;
                    cueLabel = "RIGHT!";
                    break;
                default:
                    sideAction = WarmupActionType.Run;
                    cueLabel = "CENTER!";
                    break;
            }

            return CreateEvent(
                time,
                WarmupObstacleType.LaneBlocker,
                sideAction,
                cueLabel,
                spawnSide,
                0,
                new Vector3(0f, 0.9f, 0f),
                new Vector3(0.28f, 0.6f, 1f),
                WarmupObstacleCollisionMode.UsePrefab);
        }

        private static WarmupObstacleEvent Boss(float time)
        {
            WarmupObstacleEvent obstacleEvent = CreateEvent(
                time,
                WarmupObstacleType.BossWall,
                WarmupActionType.Punch,
                "PUNCH!",
                WarmupLane.Center,
                0,
                new Vector3(0f, 1.5f, 0f),
                Vector3.one,
                WarmupObstacleCollisionMode.UsePrefab);
            obstacleEvent.CueLeadTime = 2f;
            obstacleEvent.BossHitPoints = 4;
            obstacleEvent.BossStopDistance = 1.6f;
            return obstacleEvent;
        }

        private static WarmupObstacleEvent CreateEvent(
            float time,
            WarmupObstacleType type,
            WarmupActionType action,
            string label,
            WarmupLane lane,
            int variation,
            Vector3 positionOffset,
            Vector3 scaleMultiplier,
            WarmupObstacleCollisionMode collisionMode)
        {
            return new WarmupObstacleEvent
            {
                EncounterTime = time,
                Type = type,
                Lane = lane,
                Action = action,
                CueLabel = label,
                CueLeadTime = 1.4f,
                PrefabVariation = variation,
                PositionOffset = positionOffset,
                RotationOffset = Vector3.zero,
                ScaleMultiplier = scaleMultiplier,
                CollisionMode = collisionMode
            };
        }

        private static void CreatePaperShardMaterial()
        {
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(PaperShardMaterialPath);
            Shader shader = Shader.Find("Game YT/Warmup/Paper Shard");
            if (shader == null)
            {
                return;
            }

            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "WarmupPaperShard"
                };
                AssetDatabase.CreateAsset(material, PaperShardMaterialPath);
            }
            else
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }
        }

        private static void ConfigureScene(
            WarmupPhaseTimelineAsset phase,
            WarmupObstaclePrefabSet prefabSet)
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            GameObject playerObject = GameObject.Find("Player");
            if (playerObject == null)
            {
                GameObject playerPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
                playerObject =
                    (GameObject)PrefabUtility.InstantiatePrefab(
                        playerPrefab,
                        scene);
                playerObject.name = "Player";
            }

            WarmupPlayerController player =
                playerObject.GetComponent<WarmupPlayerController>();
            GameObject gameRoot = GameObject.Find("Warmup Game");
            if (gameRoot == null)
            {
                gameRoot = new GameObject("Warmup Game");
            }

            WarmupSequenceDirector legacyDirector =
                gameRoot.GetComponent<WarmupSequenceDirector>();
            if (legacyDirector != null)
            {
                legacyDirector.enabled = false;
            }

            Transform runtimeRoot =
                FindOrCreateChild(gameRoot.transform, "Obstacle Timeline Content");
            Material paperShardMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(PaperShardMaterialPath);
            WarmupObstacleTimelineDirector director =
                GetOrAddComponent<WarmupObstacleTimelineDirector>(gameRoot);
            director.SetupComponents(
                phase,
                prefabSet,
                player,
                runtimeRoot,
                paperShardMaterial);
            director.enabled = true;

            GameObject sampleGate = GameObject.Find("Punch Gate Sample");
            if (sampleGate != null)
            {
                sampleGate.SetActive(false);
            }

            ConfigureHud(scene, director);
            EditorUtility.SetDirty(gameRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureHudPrefab()
        {
            GameObject hudRoot = PrefabUtility.LoadPrefabContents(HudPrefabPath);
            try
            {
                RectTransform rootRect = hudRoot.GetComponent<RectTransform>();
                if (rootRect != null)
                {
                    rootRect.localScale = Vector3.one;
                }

                WarmupCuePresenter[] legacyPresenters =
                    hudRoot.GetComponents<WarmupCuePresenter>();
                for (int i = 0; i < legacyPresenters.Length; i++)
                {
                    UnityEngine.Object.DestroyImmediate(legacyPresenters[i]);
                }

                Slider progressSlider =
                    FindComponentByName<Slider>(hudRoot.transform, "Slider");
                Slider healthSlider =
                    FindComponentByName<Slider>(hudRoot.transform, "SliderHp");
                Text kilometText =
                    FindComponentByName<Text>(hudRoot.transform, "KilometRun");
                Text healthText =
                    FindComponentByName<Text>(hudRoot.transform, "BossHealthText");

                if (progressSlider != null)
                {
                    progressSlider.minValue = 0f;
                    progressSlider.maxValue = 1f;
                    progressSlider.value = 0f;
                    progressSlider.interactable = false;
                }

                if (kilometText != null)
                {
                    kilometText.text = "0 m";
                }

                if (healthSlider != null)
                {
                    healthSlider.minValue = 0f;
                    healthSlider.maxValue = 4f;
                    healthSlider.value = 4f;
                    healthSlider.interactable = false;
                    healthSlider.gameObject.SetActive(false);
                }

                WarmupGameplayHud gameplayHud =
                    GetOrAddComponent<WarmupGameplayHud>(hudRoot);
                gameplayHud.SetupComponents(
                    null,
                    progressSlider,
                    kilometText,
                    healthSlider,
                    healthText,
                    healthSlider != null ? healthSlider.gameObject : null);

                PrefabUtility.SaveAsPrefabAsset(hudRoot, HudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(hudRoot);
            }
        }

        private static void ConfigureHud(
            Scene scene,
            WarmupObstacleTimelineDirector director)
        {
            GameObject hudRoot = GameObject.Find("Warmup HUD");
            if (hudRoot == null)
            {
                GameObject hudPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
                hudRoot =
                    (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab, scene);
                hudRoot.name = "Warmup HUD";
            }

            RectTransform rootRect = hudRoot.GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.localScale = Vector3.one;
            }

            WarmupCuePresenter[] legacyPresenters =
                hudRoot.GetComponents<WarmupCuePresenter>();
            for (int i = 0; i < legacyPresenters.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(legacyPresenters[i]);
            }

            Slider progressSlider =
                FindComponentByName<Slider>(hudRoot.transform, "Slider");
            Slider healthSlider =
                FindComponentByName<Slider>(hudRoot.transform, "SliderHp");
            Text kilometText =
                FindComponentByName<Text>(hudRoot.transform, "KilometRun");
            Text healthText =
                FindComponentByName<Text>(hudRoot.transform, "BossHealthText");

            WarmupGameplayHud[] gameplayHuds =
                hudRoot.GetComponents<WarmupGameplayHud>();
            WarmupGameplayHud gameplayHud = null;
            for (int i = 0; i < gameplayHuds.Length; i++)
            {
                bool comesFromPrefab =
                    PrefabUtility.GetCorrespondingObjectFromSource(
                        gameplayHuds[i]) != null;
                if (gameplayHud == null && comesFromPrefab)
                {
                    gameplayHud = gameplayHuds[i];
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(gameplayHuds[i]);
            }

            if (gameplayHud == null)
            {
                gameplayHud = hudRoot.AddComponent<WarmupGameplayHud>();
            }

            gameplayHud.SetupComponents(
                director,
                progressSlider,
                kilometText,
                healthSlider,
                healthText,
                healthSlider != null ? healthSlider.gameObject : null);

            if (healthSlider != null)
            {
                healthSlider.gameObject.SetActive(false);
            }

            EditorUtility.SetDirty(hudRoot);
        }

        private static T FindComponentByName<T>(
            Transform root,
            string objectName)
            where T : Component
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i].gameObject.name == objectName)
                {
                    return components[i];
                }
            }

            return null;
        }

        private static T LoadOrCreateAsset<T>(string path)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static GameObject[] ArrayOf(GameObject prefab)
        {
            return prefab != null
                ? new[] { prefab }
                : Array.Empty<GameObject>();
        }

        private static GameObject[] CompactArray(
            GameObject first,
            GameObject second)
        {
            var prefabs = new List<GameObject>(2);
            if (first != null)
            {
                prefabs.Add(first);
            }

            if (second != null)
            {
                prefabs.Add(second);
            }

            return prefabs.ToArray();
        }

        private static Transform FindOrCreateChild(
            Transform parent,
            string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                return child;
            }

            var childObject = new GameObject(childName);
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private static T GetOrAddComponent<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string currentPath = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = currentPath + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }

                currentPath = nextPath;
            }
        }
    }
}
#endif
