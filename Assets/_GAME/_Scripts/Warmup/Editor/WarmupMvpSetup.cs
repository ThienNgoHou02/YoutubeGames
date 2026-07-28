#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameYT.Warmup.Editor
{
    [InitializeOnLoad]
    public static class WarmupMvpSetup
    {
        private const string PlayerPrefabPath =
            "Assets/_GAME/_Scripts/Player/Player.prefab";
        private const string ScenePath =
            "Assets/_GAME/_Scenes/WarnUp.unity";
        private const string DataFolder =
            "Assets/_GAME/_Data/Warmup";
        private const string PlayerConfigPath =
            DataFolder + "/WarmupPlayerConfig.asset";
        private const string SequencePath =
            DataFolder + "/MonsterForestRunSequence.asset";
        private const string SetupKey =
            "GameYT.ImmersiveWarmup.MvpSetup.Version4";

        static WarmupMvpSetup()
        {
            EditorApplication.delayCall += TryRunAutomaticSetup;
        }

        [MenuItem("Tools/Immersive Warmup/Setup MVP")]
        public static void SetupMvpFromMenu()
        {
            RunSetup();
        }

        [MenuItem("Tools/Immersive Warmup/Select Player Config")]
        private static void SelectPlayerConfig()
        {
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<WarmupPlayerConfig>(PlayerConfigPath);
        }

        [MenuItem("Tools/Immersive Warmup/Select Sequence")]
        private static void SelectSequence()
        {
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<WarmupSequenceAsset>(SequencePath);
        }

        private static void TryRunAutomaticSetup()
        {
            if (EditorPrefs.GetBool(SetupKey, false) ||
                EditorApplication.isCompiling ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            RunSetup();
        }

        private static void RunSetup()
        {
            try
            {
                EnsureDataFolders();

                WarmupPlayerConfig playerConfig = GetOrCreatePlayerConfig();
                WarmupSequenceAsset sequence = GetOrCreateSequence();

                ConfigurePlayerPrefab(playerConfig);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                ConfigureWarmupScene(sequence);

                EditorPrefs.SetBool(SetupKey, true);
                Debug.Log(
                    "Immersive Warmup MVP setup hoàn tất: Player POV, controls, HUD, " +
                    "sequence 7:30 và punch gate mẫu.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void EnsureDataFolders()
        {
            EnsureFolder("Assets/_GAME", "_Data");
            EnsureFolder("Assets/_GAME/_Data", "Warmup");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static WarmupPlayerConfig GetOrCreatePlayerConfig()
        {
            WarmupPlayerConfig config =
                AssetDatabase.LoadAssetAtPath<WarmupPlayerConfig>(PlayerConfigPath);

            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<WarmupPlayerConfig>();
            AssetDatabase.CreateAsset(config, PlayerConfigPath);
            return config;
        }

        private static WarmupSequenceAsset GetOrCreateSequence()
        {
            WarmupSequenceAsset sequence =
                AssetDatabase.LoadAssetAtPath<WarmupSequenceAsset>(SequencePath);

            if (sequence != null)
            {
                return sequence;
            }

            sequence = ScriptableObject.CreateInstance<WarmupSequenceAsset>();
            AssetDatabase.CreateAsset(sequence, SequencePath);
            sequence.SetData(450f, CreateMonsterForestCues());
            return sequence;
        }

        private static WarmupCue[] CreateMonsterForestCues()
        {
            return new[]
            {
                Cue(1f, WarmupActionType.Run, "RUN!", 1.35f),
                Cue(3.2f, WarmupActionType.Jump, "JUMP!", 1.35f),
                Cue(5.1f, WarmupActionType.Punch, "PUNCH!", 1.35f),
                Cue(15f, WarmupActionType.Run, "RUN!", 0.85f),
                Cue(23f, WarmupActionType.MoveLeft, "LEFT!", 0.85f),
                Cue(32f, WarmupActionType.MoveRight, "RIGHT!", 0.9f),
                Cue(46f, WarmupActionType.Jump, "JUMP!", 0.95f),
                Cue(57f, WarmupActionType.Duck, "DUCK!", 0.95f),
                Cue(69f, WarmupActionType.Jump, "JUMP!", 1f),
                Cue(82f, WarmupActionType.MoveLeft, "LEFT!", 1f),
                Cue(94f, WarmupActionType.MoveRight, "RIGHT!", 1f),
                Cue(107f, WarmupActionType.Punch, "PUNCH!", 1f),
                Cue(121f, WarmupActionType.Freeze, "FREEZE!", 0.9f),
                Cue(134f, WarmupActionType.Duck, "DUCK!", 0.9f),
                Cue(148f, WarmupActionType.Freeze, "FREEZE!", 0.95f),
                Cue(163f, WarmupActionType.MoveRight, "RIGHT!", 1f),
                Cue(175f, WarmupActionType.MoveLeft, "LEFT!", 1f),
                Cue(187f, WarmupActionType.Jump, "JUMP!", 1.05f),
                Cue(203f, WarmupActionType.Run, "RUN!", 1.25f),
                Cue(211f, WarmupActionType.Duck, "DUCK!", 1.25f),
                Cue(220f, WarmupActionType.Jump, "JUMP!", 1.25f),
                Cue(237f, WarmupActionType.Punch, "PUNCH!", 1f),
                Cue(247f, WarmupActionType.Punch, "PUNCH!", 1f),
                Cue(257f, WarmupActionType.Punch, "PUNCH!", 1.05f),
                Cue(275f, WarmupActionType.MoveLeft, "LEFT!", 1.1f),
                Cue(289f, WarmupActionType.Freeze, "FREEZE!", 1.05f),
                Cue(304f, WarmupActionType.MoveRight, "RIGHT!", 1.15f),
                Cue(321f, WarmupActionType.Jump, "JUMP!", 1.2f),
                Cue(337f, WarmupActionType.Duck, "DUCK!", 1.2f),
                Cue(350f, WarmupActionType.Punch, "PUNCH!", 1.25f),
                Cue(362f, WarmupActionType.MoveLeft, "LEFT!", 1.3f),
                Cue(374f, WarmupActionType.Jump, "JUMP!", 1.3f),
                Cue(386f, WarmupActionType.MoveRight, "RIGHT!", 1.35f),
                Cue(398f, WarmupActionType.Duck, "DUCK!", 1.35f),
                Cue(409f, WarmupActionType.Punch, "PUNCH!", 1.4f),
                Cue(421f, WarmupActionType.Run, "RUN!", 0.7f),
                Cue(435f, WarmupActionType.Freeze, "BREATHE", 0.35f)
            };
        }

        private static WarmupCue Cue(
            float time,
            WarmupActionType action,
            string label,
            float speedMultiplier)
        {
            return new WarmupCue
            {
                StartTime = time,
                LeadTime = action == WarmupActionType.Freeze ? 1.2f : 1f,
                ActionWindow = 1.1f,
                Action = action,
                Label = label,
                SpeedMultiplier = speedMultiplier
            };
        }

        private static void ConfigurePlayerPrefab(WarmupPlayerConfig config)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

            try
            {
                BoxCollider boxCollider = root.GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    UnityEngine.Object.DestroyImmediate(boxCollider);
                }

                MeshRenderer placeholderRenderer = root.GetComponent<MeshRenderer>();
                if (placeholderRenderer != null)
                {
                    placeholderRenderer.enabled = false;
                }

                CharacterController characterController =
                    GetOrAddComponent<CharacterController>(root);
                characterController.height = config.StandingHeight;
                characterController.radius = 0.35f;
                characterController.center = Vector3.up * (config.StandingHeight * 0.5f);
                characterController.stepOffset = 0.25f;
                characterController.skinWidth = 0.05f;

                InputManager input = GetOrAddComponent<InputManager>(root);
                WarmupPunchInteractor punch =
                    GetOrAddComponent<WarmupPunchInteractor>(root);
                WarmupPlayerController player =
                    GetOrAddComponent<WarmupPlayerController>(root);

                Transform cameraPivot = GetOrCreateChild(root.transform, "Camera Pivot");
                cameraPivot.localPosition =
                    Vector3.up * config.CameraStandingHeight;
                cameraPivot.localRotation = Quaternion.identity;

                Transform headMotion =
                    GetOrCreateChild(cameraPivot, "Head Motion");
                headMotion.localPosition = Vector3.zero;
                headMotion.localRotation = Quaternion.identity;

                Transform cameraTransform =
                    headMotion.Find("First Person Camera");
                if (cameraTransform == null)
                {
                    cameraTransform = cameraPivot.Find("First Person Camera");
                    if (cameraTransform != null)
                    {
                        cameraTransform.SetParent(headMotion, false);
                    }
                    else
                    {
                        cameraTransform =
                            GetOrCreateChild(headMotion, "First Person Camera");
                    }
                }

                cameraTransform.localPosition = Vector3.zero;
                cameraTransform.localRotation = Quaternion.identity;
                cameraTransform.gameObject.tag = "MainCamera";

                Camera camera = GetOrAddComponent<Camera>(cameraTransform.gameObject);
                camera.fieldOfView = config.FieldOfView;
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 1000f;
                GetOrAddComponent<AudioListener>(cameraTransform.gameObject);

                Transform punchOrigin =
                    GetOrCreateChild(cameraPivot, "Punch Origin");
                punchOrigin.localPosition = new Vector3(0f, -0.15f, 0f);
                punchOrigin.localRotation = Quaternion.identity;

                Transform handsRoot =
                    cameraTransform.Find("First Person Hands");
                if (handsRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(handsRoot.gameObject);
                }

                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);

                punch.SetupComponents(punchOrigin, config);
                player.SetupComponents(config, input, punch, cameraPivot);

                WarmupHeadCameraMotion cameraMotion =
                    GetOrAddComponent<WarmupHeadCameraMotion>(
                        headMotion.gameObject);
                cameraMotion.SetupComponents(player);

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureWarmupScene(WarmupSequenceAsset sequence)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            GameObject playerObject = GameObject.Find("Player");
            if (playerObject == null)
            {
                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
                playerObject =
                    (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                playerObject.name = "Player";
            }

            WarmupPlayerController player =
                playerObject.GetComponent<WarmupPlayerController>();
            if (player == null)
            {
                PrefabUtility.RevertPrefabInstance(
                    playerObject,
                    InteractionMode.AutomatedAction);
                player = playerObject.GetComponent<WarmupPlayerController>();
            }

            DisableStandaloneCameras(playerObject.transform);

            GameObject gameRoot = FindOrCreateRoot("Warmup Game");
            WarmupSequenceDirector director =
                GetOrAddComponent<WarmupSequenceDirector>(gameRoot);
            director.SetupComponents(sequence, player);

            WarmupCuePresenter presenter = CreateOrConfigureHud(director);
            CreateFallbackGround(playerObject.transform);
            CreatePunchGateSample(playerObject.transform);

            EditorUtility.SetDirty(presenter);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static WarmupCuePresenter CreateOrConfigureHud(
            WarmupSequenceDirector director)
        {
            GameObject hudRoot = GameObject.Find("Warmup HUD");
            if (hudRoot == null)
            {
                hudRoot = new GameObject("Warmup HUD");
            }

            Canvas canvas = GetOrAddComponent<Canvas>(hudRoot);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(hudRoot);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            GetOrAddComponent<GraphicRaycaster>(hudRoot);

            GameObject panelObject = FindOrCreateUiChild(hudRoot.transform, "Cue Panel");
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            SetCenteredRect(panelRect, new Vector2(720f, 230f), new Vector2(0f, 230f));

            Image panelImage = GetOrAddComponent<Image>(panelObject);
            panelImage.sprite =
                AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);

            CanvasGroup cueGroup = GetOrAddComponent<CanvasGroup>(panelObject);
            cueGroup.blocksRaycasts = false;
            cueGroup.interactable = false;

            GameObject accentObject =
                FindOrCreateUiChild(panelObject.transform, "Accent");
            RectTransform accentRect = accentObject.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0.025f, 1f);
            accentRect.offsetMin = Vector2.zero;
            accentRect.offsetMax = Vector2.zero;
            Image accent = GetOrAddComponent<Image>(accentObject);
            accent.color = Color.red;

            TextMeshProUGUI actionText =
                CreateOrGetText(panelObject.transform, "Action", 86f);
            RectTransform actionRect = actionText.rectTransform;
            actionRect.anchorMin = new Vector2(0.08f, 0.15f);
            actionRect.anchorMax = new Vector2(0.7f, 0.85f);
            actionRect.offsetMin = Vector2.zero;
            actionRect.offsetMax = Vector2.zero;
            actionText.text = "RUN!";
            actionText.fontStyle = FontStyles.Bold;
            actionText.alignment = TextAlignmentOptions.Center;
            actionText.color = Color.white;

            TextMeshProUGUI countdownText =
                CreateOrGetText(panelObject.transform, "Countdown", 112f);
            RectTransform countdownRect = countdownText.rectTransform;
            countdownRect.anchorMin = new Vector2(0.7f, 0.1f);
            countdownRect.anchorMax = new Vector2(0.95f, 0.9f);
            countdownRect.offsetMin = Vector2.zero;
            countdownRect.offsetMax = Vector2.zero;
            countdownText.text = "3";
            countdownText.fontStyle = FontStyles.Bold;
            countdownText.alignment = TextAlignmentOptions.Center;
            countdownText.color = Color.white;

            GameObject progressBackgroundObject =
                FindOrCreateUiChild(hudRoot.transform, "Progress Background");
            RectTransform progressBackgroundRect =
                progressBackgroundObject.GetComponent<RectTransform>();
            progressBackgroundRect.anchorMin = new Vector2(0.2f, 0.06f);
            progressBackgroundRect.anchorMax = new Vector2(0.8f, 0.08f);
            progressBackgroundRect.offsetMin = Vector2.zero;
            progressBackgroundRect.offsetMax = Vector2.zero;
            Image progressBackground =
                GetOrAddComponent<Image>(progressBackgroundObject);
            progressBackground.color = new Color(0f, 0f, 0f, 0.55f);

            GameObject progressFillObject =
                FindOrCreateUiChild(progressBackgroundObject.transform, "Fill");
            RectTransform progressFillRect =
                progressFillObject.GetComponent<RectTransform>();
            progressFillRect.anchorMin = Vector2.zero;
            progressFillRect.anchorMax = Vector2.one;
            progressFillRect.offsetMin = Vector2.zero;
            progressFillRect.offsetMax = Vector2.zero;
            Image progressFill = GetOrAddComponent<Image>(progressFillObject);
            progressFill.sprite =
                AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = 0;
            progressFill.fillAmount = 0f;
            progressFill.color = new Color(0.2f, 0.9f, 0.35f, 1f);

            WarmupCuePresenter presenter =
                GetOrAddComponent<WarmupCuePresenter>(hudRoot);
            presenter.SetupComponents(
                director,
                cueGroup,
                actionText,
                countdownText,
                accent,
                progressFill);

            return presenter;
        }

        private static void CreatePunchGateSample(Transform player)
        {
            GameObject contentRoot = FindOrCreateRoot("Warmup Greybox Content");
            Transform gateTransform = contentRoot.transform.Find("Punch Gate Sample");

            GameObject gate;
            if (gateTransform == null)
            {
                gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gate.name = "Punch Gate Sample";
                gate.transform.SetParent(contentRoot.transform);
            }
            else
            {
                gate = gateTransform.gameObject;
            }

            gate.transform.position =
                player.position + player.forward * 22f + Vector3.up * 0.5f;
            gate.transform.rotation = player.rotation;
            gate.transform.localScale = new Vector3(3.2f, 3f, 0.45f);

            BoxCollider collider = gate.GetComponent<BoxCollider>();
            collider.isTrigger = true;

            if (gate.GetComponent<PunchableObstacle>() == null)
            {
                gate.AddComponent<PunchableObstacle>();
            }
        }

        private static void CreateFallbackGround(Transform player)
        {
            GameObject contentRoot = FindOrCreateRoot("Warmup Greybox Content");
            Transform groundTransform =
                contentRoot.transform.Find("Fallback Ground Collider");

            GameObject ground;
            if (groundTransform == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Fallback Ground Collider";
                ground.transform.SetParent(contentRoot.transform);
            }
            else
            {
                ground = groundTransform.gameObject;
            }

            const float forwardLength = 3000f;
            const float backwardPadding = 20f;
            const float groundLength = forwardLength + backwardPadding;
            const float groundWidth = 60f;
            const float groundThickness = 0.5f;

            ground.transform.position =
                player.position +
                player.forward *
                (groundLength * 0.5f - backwardPadding) -
                Vector3.up * (groundThickness * 0.5f + 0.02f);
            ground.transform.rotation = player.rotation;
            ground.transform.localScale =
                new Vector3(groundWidth, groundThickness, groundLength);

            BoxCollider collider = ground.GetComponent<BoxCollider>();
            collider.isTrigger = false;

            MeshRenderer renderer = ground.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        private static void DisableStandaloneCameras(Transform player)
        {
            Camera[] cameras =
                UnityEngine.Object.FindObjectsOfType<Camera>(true);

            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera.transform.IsChildOf(player))
                {
                    camera.enabled = true;
                    continue;
                }

                if (!camera.CompareTag("MainCamera") ||
                    camera.gameObject.name != "Main Camera")
                {
                    continue;
                }

                camera.enabled = false;
                AudioListener listener = camera.GetComponent<AudioListener>();
                if (listener != null)
                {
                    listener.enabled = false;
                }
            }
        }

        private static TextMeshProUGUI CreateOrGetText(
            Transform parent,
            string name,
            float fontSize)
        {
            GameObject textObject = FindOrCreateUiChild(parent, name);
            TextMeshProUGUI text =
                GetOrAddComponent<TextMeshProUGUI>(textObject);
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = fontSize * 0.55f;
            text.fontSizeMax = fontSize;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject FindOrCreateRoot(string name)
        {
            GameObject existing = GameObject.Find(name);
            return existing != null ? existing : new GameObject(name);
        }

        private static GameObject FindOrCreateUiChild(
            Transform parent,
            string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child;
        }

        private static Transform GetOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }

            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void SetCenteredRect(
            RectTransform rect,
            Vector2 size,
            Vector2 anchoredPosition)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
        }

        private static T GetOrAddComponent<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
#endif
