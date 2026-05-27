using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using BrainBattle.Kings;
using BrainBattle.Shared.UI;

namespace BrainBattle.Editor
{
    /// <summary>
    /// Creates Assets/_Project/Scenes/LevelSelect.unity and the LevelSelectButton prefab
    /// from scratch, fully wired. Run via BrainBattle ▶ Build Level Select Scene.
    /// </summary>
    public static class LevelSelectSceneBuilder
    {
        private const string ScenePath  = "Assets/_Project/Scenes/LevelSelect.unity";
        private const string PrefabPath = "Assets/_Project/Prefabs/LevelSelectButton.prefab";

        private static readonly Color ColBg         = DesignSystem.HUDBarGradientBottom;
        private static readonly Color ColPanel      = new Color(0.102f, 0.102f, 0.180f, 0.97f); // #1a1a2e
        private static readonly Color ColAccent     = new Color(1.00f, 0.176f, 0.471f, 1f);     // #ff2d78
        private static readonly Color ColTabInact   = new Color(0.165f, 0.165f, 0.243f, 1f);    // #2a2a3e
        private static readonly Color ColBtnBg      = new Color(0.18f, 0.22f, 0.45f, 1f);
        private static readonly Color ColTxtGray    = new Color(0.533f, 0.533f, 0.533f, 1f);    // #888888

        // ── Entry point ────────────────────────────────────────────────────────────

        [MenuItem("BrainBattle/Build Level Select Scene")]
        static void Build()
        {

            // Ensure directories exist.
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath)!);

            // Build prefab first (needs no open scene).
            var prefabGO = BuildLevelButtonPrefab();

            // NewSceneMode.Single closes ALL open scenes atomically (avoids "can't unload last scene" error).
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var r = new Refs();
            r.Prefab  = prefabGO;
            r.Canvas  = CreateCanvas();
            r.Control = CreateController();
            CreateCamera();
            CreateEventSystem(); // Always create — do NOT use FindFirstObjectByType (multi-scene!)

            BuildUI(r);
            WireController(r);
            AddToOrUpdateBuildSettings();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log("[LevelSelectSceneBuilder] Scene saved → " + ScenePath);
        }

        // ── Camera ─────────────────────────────────────────────────────────────────

        static void CreateCamera()
        {
            var go               = new GameObject("Main Camera");
            go.tag               = "MainCamera";
            var cam              = go.AddComponent<Camera>();
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = new Color(0.06f, 0.06f, 0.10f, 1f);
            cam.orthographic     = true;
            go.AddComponent<AudioListener>();
            go.transform.position = new Vector3(0f, 0f, -10f);
        }

        // ── EventSystem ────────────────────────────────────────────────────────────
        // Always create fresh — never use FindFirstObjectByType (finds objects in OTHER
        // loaded scenes when running additively, causing EventSystem to be skipped).

        static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();

            System.Type inputModuleType =
                System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem")
                ?? typeof(StandaloneInputModule);
            go.AddComponent(inputModuleType);
        }

        // ── Canvas ─────────────────────────────────────────────────────────────────

        static GameObject CreateCanvas()
        {
            var go        = new GameObject("Canvas");
            var canvas    = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler                 = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight  = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        // ── Controller GO ──────────────────────────────────────────────────────────

        static GameObject CreateController()
        {
            var go = new GameObject("LevelSelectController");
            go.AddComponent<LevelSelectController>();
            return go;
        }

        // ── Full UI hierarchy ──────────────────────────────────────────────────────

        static void BuildUI(Refs r)
        {
            var canvas = r.Canvas.transform;

            var bg = UI("Background", canvas);
            Stretch(bg);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = ColBg;

            var safeAreaRoot = UI("SafeAreaRoot", canvas);
            Stretch(safeAreaRoot);
            safeAreaRoot.AddComponent<SafeAreaFitter>();
            var layoutRoot = safeAreaRoot.transform;

            // ── Header ────────────────────────────────────────────────────────────
            var header = UI("Header", layoutRoot);
            var headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.offsetMin = new Vector2(0f, -DesignSystem.LevelSelectHeaderHeight);
            headerRt.offsetMax = Vector2.zero;
            header.AddComponent<Image>().color = ColPanel;

            var headerLayout = header.AddComponent<VerticalLayoutGroup>();
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.spacing = 4f;
            headerLayout.padding = new RectOffset(0, 0, 48, 16);

            var eyebrowTmp = MakeTMP("Eyebrow", header.transform, "BRAIN BATTLE");
            eyebrowTmp.text = "BRAIN BATTLE";
            eyebrowTmp.fontSize = 50f;
            eyebrowTmp.fontStyle = FontStyles.Normal;
            eyebrowTmp.characterSpacing = 8f;
            eyebrowTmp.color = new Color(1f, 0.18f, 0.47f, 0.65f);
            eyebrowTmp.alignment = TextAlignmentOptions.Top;
            eyebrowTmp.enableWordWrapping = false;
            Stretch(eyebrowTmp.gameObject);

            var headerTmp = MakeTMP("Title", header.transform, "Level Select");
            headerTmp.text = "Level Select";
            headerTmp.fontSize = 100f;
            headerTmp.fontStyle = FontStyles.Bold;
            headerTmp.color = new Color(1f, 1f, 1f, 1f);
            headerTmp.alignment = TextAlignmentOptions.Baseline;
            headerTmp.enableWordWrapping = false;
            Stretch(headerTmp.gameObject);

            // ── Tab row: segmented tabs below title ───────────────────────────────
            const float tabContainerHeight = 88f;
            var tabSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Resources/Sprites/UIRoundedRect.png");
            var tabRow = UI("TabRow", layoutRoot);
            var tabRowRt = tabRow.GetComponent<RectTransform>();
            tabRowRt.anchorMin = new Vector2(0f, 1f);
            tabRowRt.anchorMax = new Vector2(1f, 1f);
            tabRowRt.pivot = new Vector2(0.5f, 1f);
            tabRowRt.offsetMin = new Vector2(32f, -(DesignSystem.LevelSelectHeaderHeight + tabContainerHeight));
            tabRowRt.offsetMax = new Vector2(-32f, -DesignSystem.LevelSelectHeaderHeight);

            var tabContainerImage = tabRow.AddComponent<Image>();
            tabContainerImage.sprite = tabSprite;
            tabContainerImage.type = Image.Type.Sliced;
            tabContainerImage.color = new Color(1f, 1f, 1f, 0.05f);

            var tabLayout = tabRow.AddComponent<HorizontalLayoutGroup>();
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = true;
            tabLayout.padding = new RectOffset(4, 4, 4, 4);
            tabLayout.spacing = 4f;

            string[] tabNames  = { "Beginner", "Expert", "Impossible" };
            var      tabButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                bool active = i == 0;

                var cell = UI(tabNames[i] + "Tab", tabRow.transform);
                var layoutElement = cell.AddComponent<LayoutElement>();
                layoutElement.flexibleWidth = 1f;

                var img = cell.AddComponent<Image>();
                img.sprite = tabSprite;
                img.type = Image.Type.Sliced;
                img.color = active ? new Color(1f, 0.18f, 0.47f, 1f) : new Color(0f, 0f, 0f, 0f);

                var btn = cell.AddComponent<Button>();
                ApplyBtnColors(btn);

                var lbl = MakeTMP("Label", cell.transform, tabNames[i]);
                lbl.fontSize = 36f;
                lbl.alignment = TextAlignmentOptions.Center;
                lbl.enableWordWrapping = false;
                lbl.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
                lbl.color = active ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.35f);
                Stretch(lbl.gameObject);

                tabButtons[i] = btn;
            }
            r.TabButtons = tabButtons;

            // ── Progress row: active difficulty label + single progress bar ──────
            var progressSection = UI("ProgressSection", layoutRoot);
            var progressSectionRt = progressSection.GetComponent<RectTransform>();
            progressSectionRt.anchorMin = new Vector2(0f, 1f);
            progressSectionRt.anchorMax = new Vector2(1f, 1f);
            progressSectionRt.pivot = new Vector2(0.5f, 1f);
            var progressTop = DesignSystem.LevelSelectHeaderHeight + tabContainerHeight + DesignSystem.SpacingM;
            progressSectionRt.offsetMin = new Vector2(32f, -(progressTop + DesignSystem.LevelSelectProgressRowHeight));
            progressSectionRt.offsetMax = new Vector2(-32f, -progressTop);

            var progressSectionLayout = progressSection.AddComponent<VerticalLayoutGroup>();
            progressSectionLayout.spacing = 6f;
            progressSectionLayout.padding = new RectOffset(0, 0, 12, 8);
            progressSectionLayout.childControlWidth = true;
            progressSectionLayout.childControlHeight = true;
            progressSectionLayout.childForceExpandWidth = true;
            progressSectionLayout.childForceExpandHeight = false;

            var progRow = UI("ProgressRow", progressSection.transform);
            var progRowLayout = progRow.AddComponent<HorizontalLayoutGroup>();
            progRowLayout.childControlWidth = true;
            progRowLayout.childForceExpandWidth = true;
            progRowLayout.spacing = 0f;
            progRowLayout.padding = new RectOffset(0, 0, 0, 0);

            var progRowElement = progRow.AddComponent<LayoutElement>();
            progRowElement.preferredHeight = 32f;

            var label = MakeTMP("ProgressLabel", progRow.transform, "Beginner progress");
            label.fontSize = 30f;
            label.fontStyle = FontStyles.Normal;
            label.color = new Color(1f, 1f, 1f, 0.35f);
            label.alignment = TextAlignmentOptions.Left;
            label.enableWordWrapping = false;
            Stretch(label.gameObject);

            var pct = MakeTMP("ProgressPercent", progRow.transform, "0%");
            pct.fontSize = 30f;
            pct.fontStyle = FontStyles.Bold;
            pct.color = new Color(1f, 0.18f, 0.47f, 1f);
            pct.alignment = TextAlignmentOptions.Right;
            pct.enableWordWrapping = false;
            Stretch(pct.gameObject);

            var track = UI("ProgressTrack", progressSection.transform);
            var trackImage = track.AddComponent<Image>();
            trackImage.color = new Color(1f, 1f, 1f, 0.07f);
            trackImage.raycastTarget = false;
            var trackElement = track.AddComponent<LayoutElement>();
            trackElement.preferredHeight = 40f;

            var fill = UI("ProgressFill", track.transform);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(1f, 0.18f, 0.47f, 1f);
            fillImg.raycastTarget = false;

            r.ProgressFill = fillImg;
            r.ProgressText = pct;
            r.ProgressLabel = label;

            // ── Scroll view ───────────────────────────────────────────────────────
            var scrollGO = UI("LevelScrollView", layoutRoot);
            var scrollRt = scrollGO.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.03f, 0f);
            scrollRt.anchorMax = new Vector2(0.97f, 1f);
            scrollRt.pivot = new Vector2(0.5f, 0.5f);
            scrollRt.offsetMin = new Vector2(0f, 95f + DesignSystem.SpacingL);
            scrollRt.offsetMax = new Vector2(0f, -(progressTop + DesignSystem.LevelSelectProgressRowHeight + DesignSystem.SpacingL));
            scrollGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            var scroll               = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal        = false;
            scroll.vertical          = true;
            scroll.scrollSensitivity = 36f;

            // Viewport — MUST be Color.white for Mask stencil to clip children.
            var viewportGO = UI("Viewport", scrollGO.transform);
            Stretch(viewportGO);
            viewportGO.AddComponent<Image>().color      = Color.white;
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;

            // Content (GridLayoutGroup)
            var contentGO = UI("Content", viewportGO.transform);
            var contentRt        = contentGO.GetComponent<RectTransform>();
            contentRt.anchorMin  = new Vector2(0f, 1f);
            contentRt.anchorMax  = new Vector2(1f, 1f);
            contentRt.pivot      = new Vector2(0.5f, 1f);
            contentRt.offsetMin  = Vector2.zero;
            contentRt.offsetMax  = Vector2.zero;
            contentRt.sizeDelta  = new Vector2(0f, 0f);

            var grid             = contentGO.AddComponent<GridLayoutGroup>();
            grid.padding         = new RectOffset(32, 32, 8, 8);
            grid.cellSize        = new Vector2(300f, 300f);
            grid.spacing         = new Vector2(16f, 16f);
            grid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis       = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment  = TextAnchor.UpperCenter;
            grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            var csf              = contentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit      = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit    = ContentSizeFitter.FitMode.Unconstrained;

            scroll.viewport  = viewportGO.GetComponent<RectTransform>();
            scroll.content   = contentRt;
            r.GridContent    = contentGO.transform;

            // ── Play button: full width (−32 px), 64 px tall, 16 px from bottom ──
            var play   = MakeButton("PlayButton", layoutRoot, "PLAY");
            var playRt = play.gameObject.GetComponent<RectTransform>();
            playRt.anchorMin        = new Vector2(0f, 0f);
            playRt.anchorMax        = new Vector2(1f, 0f);
            playRt.pivot            = new Vector2(0.5f, 0f);
            playRt.offsetMin        = new Vector2(19f,  19f);
            playRt.offsetMax        = new Vector2(-19f, 95f); // 76 px height
            playRt.anchoredPosition = new Vector2(0f, 0f);

            StylePinkButton(play);
            var playLbl      = play.GetComponentInChildren<TextMeshProUGUI>();
            playLbl.fontSize  = 62f;
            playLbl.fontStyle = FontStyles.Bold;
            r.PlayButton = play;
        }

        // ── LevelSelectButton prefab ───────────────────────────────────────────────

        static GameObject BuildLevelButtonPrefab()
        {
            var roundedRect = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Resources/Sprites/UIRoundedRect.png");

            var root = new GameObject("LevelSelectButton");
            var rootImg = root.AddComponent<Image>();
            rootImg.sprite = roundedRect;
            rootImg.type = Image.Type.Sliced;
            var aspect = root.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            aspect.aspectRatio = 1f;
            var btn = root.AddComponent<Button>();
            ApplyBtnColors(btn);
            root.AddComponent<LevelSelectButton>();

            var bgGO = new GameObject("BG", typeof(RectTransform));
            bgGO.transform.SetParent(root.transform, false);
            var bgRt = bgGO.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(3f, 3f);
            bgRt.offsetMax = new Vector2(-3f, -3f);
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.sprite = roundedRect;
            bgImg.type = Image.Type.Sliced;
            bgImg.raycastTarget = false;

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(root.transform, false);
            var contentRt = contentGO.GetComponent<RectTransform>();
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            var contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.MiddleCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing = 4f;

            var primaryTextGO = new GameObject("PrimaryText", typeof(RectTransform));
            primaryTextGO.transform.SetParent(contentGO.transform, false);
            var primaryLayout = primaryTextGO.AddComponent<LayoutElement>();
            primaryLayout.preferredHeight = 80f;
            var primaryTmp = primaryTextGO.AddComponent<TextMeshProUGUI>();
            primaryTmp.alignment = TextAlignmentOptions.Center;
            primaryTmp.enableWordWrapping = false;
            primaryTmp.overflowMode = TextOverflowModes.Overflow;

            var secondaryTextGO = new GameObject("SecondaryText", typeof(RectTransform));
            secondaryTextGO.transform.SetParent(contentGO.transform, false);
            var secondaryLayout = secondaryTextGO.AddComponent<LayoutElement>();
            secondaryLayout.preferredHeight = 30f;
            var secondaryTmp = secondaryTextGO.AddComponent<TextMeshProUGUI>();
            secondaryTmp.alignment = TextAlignmentOptions.Center;
            secondaryTmp.enableWordWrapping = false;
            secondaryTmp.overflowMode = TextOverflowModes.Overflow;

            var so = new SerializedObject(root.GetComponent<LevelSelectButton>());
            so.FindProperty("_rootImage").objectReferenceValue = rootImg;
            so.FindProperty("_bgImage").objectReferenceValue = bgImg;
            so.FindProperty("_primaryText").objectReferenceValue = primaryTmp;
            so.FindProperty("_secondaryText").objectReferenceValue = secondaryTmp;
            so.FindProperty("_primaryLayout").objectReferenceValue = primaryLayout;
            so.FindProperty("_secondaryLayout").objectReferenceValue = secondaryLayout;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory("Assets/_Project/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[LevelSelectSceneBuilder] Prefab saved → " + PrefabPath);
            return prefab;
        }

        static void StylePinkButton(Button btn)
        {
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = ColAccent;
            var c               = btn.colors;
            c.normalColor       = Color.white;
            c.highlightedColor  = new Color(1f, 0.4f, 0.6f, 1f);
            c.pressedColor      = new Color(0.7f, 0.1f, 0.3f, 1f);
            c.disabledColor     = new Color(1f, 1f, 1f, 0.40f);
            btn.colors          = c;
        }

        // ── Wire LevelSelectController SerializeFields ─────────────────────────────

        static void WireController(Refs r)
        {
            var ctrl = r.Control.GetComponent<LevelSelectController>();
            var so   = new SerializedObject(ctrl);

            // Tab buttons array
            var tabProp = so.FindProperty("_tabButtons");
            tabProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
                tabProp.GetArrayElementAtIndex(i).objectReferenceValue = r.TabButtons[i];

            so.FindProperty("_progressFill").objectReferenceValue  = r.ProgressFill;
            so.FindProperty("_progressText").objectReferenceValue  = r.ProgressText;
            so.FindProperty("_progressLabel").objectReferenceValue = r.ProgressLabel;

            // Grid content + prefab
            so.FindProperty("_gridContent").objectReferenceValue        = r.GridContent;
            so.FindProperty("_levelButtonPrefab").objectReferenceValue  = r.Prefab;

            // Play button
            so.FindProperty("_playButton").objectReferenceValue = r.PlayButton;

            // All level assets — load from disk and assign so the controller has data at runtime
            const string levelsPath = "Assets/_Project/ScriptableObjects/Kings/Levels";
            var levelGuids = AssetDatabase.FindAssets("t:LevelData", new[] { levelsPath });
            var levelAssets = new List<LevelData>(levelGuids.Length);
            foreach (string guid in levelGuids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) levelAssets.Add(asset);
            }
            levelAssets.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            var allLevelsProp = so.FindProperty("_allLevels");
            allLevelsProp.arraySize = levelAssets.Count;
            for (int i = 0; i < levelAssets.Count; i++)
                allLevelsProp.GetArrayElementAtIndex(i).objectReferenceValue = levelAssets[i];

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Build settings ─────────────────────────────────────────────────────────

        static void AddToOrUpdateBuildSettings()
        {
            // Rebuild the list: LevelSelect at index 0, SampleScene at index 1.
            // Remove stale entries for both so we can re-insert in the correct order.
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path.Contains("LevelSelect") || s.path.Contains("SampleScene"));

            // LevelSelect MUST be index 0 (first scene loaded in a build).
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            scenes.Insert(1, new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true));

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[LevelSelectSceneBuilder] Build settings: LevelSelect=0, SampleScene=1.");
        }

        // ── Primitive helpers (same pattern as KingsSceneBuilder) ──────────────────

        static GameObject UI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static Button MakeButton(string name, Transform parent, string label)
        {
            var go  = UI(name, parent);
            var img = go.AddComponent<Image>();
            img.color = ColBtnBg;
            var btn = go.AddComponent<Button>();
            ApplyBtnColors(btn);

            var lbl      = UI("Label", go.transform);
            var tmp      = lbl.AddComponent<TextMeshProUGUI>();
            tmp.text     = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color    = Color.white;
            tmp.fontSize = 40f;
            Stretch(lbl);
            return btn;
        }

        static TextMeshProUGUI MakeTMP(string name, Transform parent, string text)
        {
            var go  = UI(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text  = text;
            tmp.color = Color.white;
            return tmp;
        }

        static void ApplyBtnColors(Button btn)
        {
            var c               = btn.colors;
            c.normalColor       = Color.white;
            c.highlightedColor  = new Color(0.85f, 0.85f, 1.00f, 1f);
            c.pressedColor      = new Color(0.65f, 0.65f, 0.90f, 1f);
            c.disabledColor     = new Color(1f, 1f, 1f, 0.40f);
            btn.colors          = c;
        }

        static void Anchor(RectTransform rt, float minX, float minY, float maxX, float maxY)
        {
            rt.anchorMin        = new Vector2(minX, minY);
            rt.anchorMax        = new Vector2(maxX, maxY);
            rt.offsetMin        = Vector2.zero;
            rt.offsetMax        = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        static void Anchor(GameObject go, float minX, float minY, float maxX, float maxY)
            => Anchor(go.GetComponent<RectTransform>(), minX, minY, maxX, maxY);

        static void Stretch(GameObject go)   => Stretch(go.GetComponent<RectTransform>());
        static void Stretch(RectTransform rt)
        {
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.offsetMin        = Vector2.zero;
            rt.offsetMax        = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = Vector2.zero;
        }

        // ── References container ───────────────────────────────────────────────────

        private sealed class Refs
        {
            public GameObject          Canvas;
            public GameObject          Control;
            public GameObject          Prefab;
            public Button[]            TabButtons;
            public Image               ProgressFill;
            public TextMeshProUGUI     ProgressText;
            public TextMeshProUGUI     ProgressLabel;
            public Transform           GridContent;
            public Button              PlayButton;
        }
    }
}
