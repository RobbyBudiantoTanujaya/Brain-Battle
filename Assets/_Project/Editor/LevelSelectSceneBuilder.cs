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

        private static readonly Color ColBg         = new Color(0.06f, 0.06f, 0.10f, 1f);
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

            var bg    = UI("Background", canvas);
            Stretch(bg);
            var bgImg    = bg.AddComponent<Image>();
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Project/Resources/Sprites/main_menu_bg.png");
            if (bgSprite != null) { bgImg.sprite = bgSprite; bgImg.color = Color.white; }
            else bgImg.color = ColBg;

            var safeAreaRoot = UI("SafeAreaRoot", canvas);
            Stretch(safeAreaRoot);
            safeAreaRoot.AddComponent<SafeAreaFitter>();
            var layoutRoot = safeAreaRoot.transform;

            // ── Header ────────────────────────────────────────────────────────────
            var header = UI("Header", layoutRoot);
            Anchor(header, 0f, 0.89f, 1f, 1.00f);
            header.AddComponent<Image>().color = ColPanel;

            var eyebrowTmp = MakeTMP("Eyebrow", header.transform, "BRAIN BATTLE");
            eyebrowTmp.fontSize  = DesignSystem.FontSizeMedium;
            eyebrowTmp.fontStyle = FontStyles.Normal;
            eyebrowTmp.alignment = TextAlignmentOptions.Center;
            eyebrowTmp.color     = DesignSystem.TextSecondary;
            Anchor(eyebrowTmp.gameObject, 0f, 0.64f, 1f, 0.86f);

            var headerTmp   = MakeTMP("Title", header.transform, "Level Select");
            headerTmp.fontSize  = DesignSystem.FontSizeDisplay;
            headerTmp.fontStyle = FontStyles.Bold;
            headerTmp.alignment = TextAlignmentOptions.Center;
            headerTmp.color     = DesignSystem.TextPrimary;
            Anchor(headerTmp.gameObject, 0f, 0.18f, 1f, 0.66f);

            // ── Tab row: 3 flush equal-width tabs ─────────────────────────────────
            var tabRow = UI("TabRow", layoutRoot);
            var tabRowRt = tabRow.GetComponent<RectTransform>();
            tabRowRt.anchorMin = new Vector2(0f, 1f);
            tabRowRt.anchorMax = new Vector2(1f, 1f);
            tabRowRt.pivot = new Vector2(0.5f, 1f);
            tabRowRt.offsetMin = new Vector2(0f, -(DesignSystem.SpacingL + DesignSystem.LevelSelectTabHeight));
            tabRowRt.offsetMax = new Vector2(0f, -DesignSystem.SpacingL);

            string[] tabNames  = { "Beginner", "Expert", "Impossible" };
            var      tabButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                float x0   = i * (1f / 3f);
                float x1   = (i + 1) * (1f / 3f);
                bool  active = i == 0;

                var cell  = UI(tabNames[i] + "Tab", tabRow.transform);
                Anchor(cell, x0, 0f, x1, 1f);

                var img   = cell.AddComponent<Image>();
                img.color = active ? DesignSystem.Primary : DesignSystem.Surface;

                var btn   = cell.AddComponent<Button>();
                ApplyBtnColors(btn);

                var lbl       = MakeTMP("Label", cell.transform, tabNames[i]);
                lbl.alignment = TextAlignmentOptions.Center;
                lbl.fontSize  = DesignSystem.FontSizeMedium;
                lbl.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
                lbl.color     = active ? DesignSystem.TextPrimary : DesignSystem.TextSecondary;
                Stretch(lbl.gameObject);

                tabButtons[i] = btn;
            }
            r.TabButtons = tabButtons;

            // ── Progress row: active difficulty label + single progress bar ──────
            var progRow = UI("ProgressRow", layoutRoot);
            Anchor(progRow, 0f, 0.755f, 1f, 0.800f);

            var label = MakeTMP("ProgressLabel", progRow.transform, "Beginner progress");
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = DesignSystem.FontSizeMedium;
            label.color = DesignSystem.TextSecondary;
            Anchor(label.gameObject, 0.03f, 0f, 0.38f, 1f);

            var track = UI("Track", progRow.transform);
            var trackRt = track.GetComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0.38f, 0.5f);
            trackRt.anchorMax = new Vector2(0.86f, 0.5f);
            trackRt.pivot = new Vector2(0.5f, 0.5f);
            trackRt.anchoredPosition = Vector2.zero;
            trackRt.sizeDelta = new Vector2(0f, DesignSystem.ProgressBarHeight);
            track.AddComponent<Image>().color = DesignSystem.Surface;

            var fill = UI("Fill", track.transform);
            Stretch(fill);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = DesignSystem.Primary;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;
            fillImg.raycastTarget = false;

            var pct = MakeTMP("PctText", progRow.transform, "0%");
            pct.alignment = TextAlignmentOptions.MidlineRight;
            pct.fontSize = DesignSystem.FontSizeMedium;
            pct.color = DesignSystem.TextPrimary;
            Anchor(pct.gameObject, 0.87f, 0f, 0.97f, 1f);

            r.ProgressFill = fillImg;
            r.ProgressText = pct;
            r.ProgressLabel = label;

            // ── Scroll view ───────────────────────────────────────────────────────
            var scrollGO = UI("LevelScrollView", layoutRoot);
            Anchor(scrollGO, 0.03f, 0.06f, 0.97f, 0.775f);
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
            grid.padding         = new RectOffset(19, 19, 19, 19);
            grid.cellSize        = new Vector2(359f, 359f);
            grid.spacing         = new Vector2(14f, 14f);
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
            // Build in memory, save as prefab, destroy temp instance.
            var root    = new GameObject("LevelSelectButton");
            // Root Image is transparent — it only provides a raycast target for the Button.
            var rootImg = root.AddComponent<Image>();
            rootImg.color = Color.clear;
            var btn     = root.AddComponent<Button>();
            ApplyBtnColors(btn);
            root.AddComponent<LevelSelectButton>();

            // ── SpriteImage (index 0 — renders behind text) ───────────────────────
            // Stretches to fill the entire button; sprite is swapped per LevelState.
            var sprGO = new GameObject("SpriteImage", typeof(RectTransform));
            sprGO.transform.SetParent(root.transform, false);
            var sprRt       = sprGO.GetComponent<RectTransform>();
            sprRt.anchorMin = Vector2.zero;
            sprRt.anchorMax = Vector2.one;
            sprRt.offsetMin = Vector2.zero;
            sprRt.offsetMax = Vector2.zero;
            var sprImg              = sprGO.AddComponent<Image>();
            sprImg.color            = Color.white;
            sprImg.preserveAspect   = false;
            sprImg.raycastTarget    = false;

            // ── Level number label (centred, hidden when Locked) ──────────────────
            var lblGO = new GameObject("LevelLabel", typeof(RectTransform));
            lblGO.transform.SetParent(root.transform, false);
            var lblRt       = lblGO.GetComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = Vector2.zero;
            lblRt.offsetMax = Vector2.zero;
            var lblTmp       = lblGO.AddComponent<TextMeshProUGUI>();
            lblTmp.text      = "1";
            lblTmp.alignment = TextAlignmentOptions.Center;
            lblTmp.fontSize  = 67f;
            lblTmp.color     = Color.white;
            lblTmp.fontStyle = FontStyles.Bold;

            // ── Checkmark (top-right corner, hidden until Completed) ──────────────
            var ckGO = new GameObject("Checkmark", typeof(RectTransform));
            ckGO.transform.SetParent(root.transform, false);
            var ckRt       = ckGO.GetComponent<RectTransform>();
            ckRt.anchorMin = new Vector2(0.55f, 0.55f);
            ckRt.anchorMax = Vector2.one;
            ckRt.offsetMin = Vector2.zero;
            ckRt.offsetMax = Vector2.zero;
            var ckTmp       = ckGO.AddComponent<TextMeshProUGUI>();
            ckTmp.text      = "✓";
            ckTmp.alignment = TextAlignmentOptions.Center;
            ckTmp.fontSize  = 67f;
            ckTmp.color     = Color.white;
            ckGO.SetActive(false);

            // ── LockOverlay (legacy — always hidden; sprite handles locked state) ──
            var lkGO = new GameObject("LockOverlay", typeof(RectTransform));
            lkGO.transform.SetParent(root.transform, false);
            lkGO.SetActive(false);

            // ── Wire SerializeFields ───────────────────────────────────────────────
            var so = new SerializedObject(root.GetComponent<LevelSelectButton>());
            so.FindProperty("_background").objectReferenceValue  = rootImg;
            so.FindProperty("_spriteImage").objectReferenceValue = sprImg;
            so.FindProperty("_levelLabel").objectReferenceValue  = lblTmp;
            so.FindProperty("_checkmark").objectReferenceValue   = ckGO;
            so.FindProperty("_lockOverlay").objectReferenceValue = lkGO;
            so.ApplyModifiedPropertiesWithoutUndo();

            // ── Pre-assign state sprites ───────────────────────────────────────────
            AssignLevelButtonSpritesToSO(so);

            // Save as prefab asset.
            Directory.CreateDirectory("Assets/_Project/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[LevelSelectSceneBuilder] Prefab saved → " + PrefabPath);
            return prefab;
        }

        static void AssignLevelButtonSpritesToSO(SerializedObject so)
        {
            so.FindProperty("_spriteAvailable").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Resources/Sprites/level_available.png");
            so.FindProperty("_spriteCompleted").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Resources/Sprites/level_completed.png");
            so.FindProperty("_spriteActive").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Resources/Sprites/level_active.png");
            so.FindProperty("_spriteLocked").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Resources/Sprites/level_lock.png");
            so.ApplyModifiedPropertiesWithoutUndo();
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
