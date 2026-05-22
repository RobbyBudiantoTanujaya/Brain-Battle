using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
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

            // Create a new empty scene, build the UI, save.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SetActiveScene(scene);

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

            // Close the temp scene – the saved asset is what we need.
            EditorSceneManager.CloseScene(scene, true);

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
            scaler.referenceResolution = new Vector2(1080f, 1920f);
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

            // ── Background: full-screen main_menu_bg sprite ───────────────────────
            var bg    = UI("Background", canvas);
            Stretch(bg);
            var bgImg    = bg.AddComponent<Image>();
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Project/Resources/Sprites/main_menu_bg.png");
            if (bgSprite != null) { bgImg.sprite = bgSprite; bgImg.color = Color.white; }
            else bgImg.color = ColBg;

            // ── Header ────────────────────────────────────────────────────────────
            var header = UI("Header", canvas);
            Anchor(header, 0f, 0.90f, 1f, 1.00f);
            header.AddComponent<Image>().color = ColPanel;
            var headerTmp   = MakeTMP("Title", header.transform, "LEVEL SELECT");
            headerTmp.fontSize  = 72f;
            headerTmp.fontStyle = FontStyles.Bold;
            headerTmp.alignment = TextAlignmentOptions.Center;
            Stretch(headerTmp.gameObject);

            // ── Tab row: 3 flush equal-width tabs ─────────────────────────────────
            var tabRow = UI("TabRow", canvas);
            Anchor(tabRow, 0f, 0.82f, 1f, 0.90f); // 60px approx in 1920 ref

            string[] tabNames  = { "Beginner", "Expert", "Impossible" };
            var      tabButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                float x0   = i * (1f / 3f);
                float x1   = (i + 1) * (1f / 3f);
                bool  active = i == 0;

                var cell  = UI(tabNames[i] + "Tab", tabRow.transform);
                Anchor(cell, x0, 0f, x1, 1f); // flush — no gap

                var img   = cell.AddComponent<Image>();
                img.color = active ? ColAccent : ColTabInact;

                var btn   = cell.AddComponent<Button>();
                ApplyBtnColors(btn);

                var lbl       = MakeTMP("Label", cell.transform, tabNames[i]);
                lbl.alignment = TextAlignmentOptions.Center;
                lbl.fontSize  = 38f;
                lbl.fontStyle = active ? FontStyles.Bold   : FontStyles.Normal;
                lbl.color     = active ? Color.white        : ColTxtGray;
                Stretch(lbl.gameObject);

                tabButtons[i] = btn;
            }
            r.TabButtons = tabButtons;

            // ── Progress row: one group per tab ───────────────────────────────────
            // Compact 48px strip — track is 6px, centred vertically.
            var progRow = UI("ProgressRow", canvas);
            Anchor(progRow, 0f, 0.775f, 1f, 0.820f);

            var progFills = new Image[3];
            var progTexts = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                float x0 = i * (1f / 3f), x1 = (i + 1) * (1f / 3f);

                var group = UI($"ProgressGroup{i}", progRow.transform);
                Anchor(group, x0 + 0.01f, 0f, x1 - 0.01f, 1f);

                // 6-px track — anchored to parent centre, width fills 80% of group.
                var track   = UI("Track", group.transform);
                var trackRt = track.GetComponent<RectTransform>();
                trackRt.anchorMin       = new Vector2(0f,   0.5f);
                trackRt.anchorMax       = new Vector2(0.80f, 0.5f);
                trackRt.pivot           = new Vector2(0.5f,  0.5f);
                trackRt.anchoredPosition = Vector2.zero;
                trackRt.sizeDelta       = new Vector2(0f, 6f);
                track.AddComponent<Image>().color = new Color(0.165f, 0.165f, 0.243f, 1f);

                // Fill (Image.Type.Filled, Horizontal)
                var fill   = UI("Fill", track.transform);
                Stretch(fill);
                var fillImg          = fill.AddComponent<Image>();
                fillImg.color        = ColAccent;
                fillImg.type         = Image.Type.Filled;
                fillImg.fillMethod   = Image.FillMethod.Horizontal;
                fillImg.fillAmount   = 0f;
                fillImg.raycastTarget = false;
                progFills[i]         = fillImg;

                // % text — right-aligned, takes the remaining 20% of the group.
                var pct       = MakeTMP($"PctText{i}", group.transform, "0%");
                pct.alignment = TextAlignmentOptions.MidlineRight;
                pct.fontSize  = 28f;
                pct.color     = Color.white;
                Anchor(pct.gameObject, 0.82f, 0f, 1.00f, 1f);
                progTexts[i]  = pct;
            }
            r.ProgressFills = progFills;
            r.ProgressTexts = progTexts;

            // ── Scroll view ───────────────────────────────────────────────────────
            var scrollGO = UI("LevelScrollView", canvas);
            Anchor(scrollGO, 0.03f, 0.06f, 0.97f, 0.775f);
            scrollGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            var scroll               = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal        = false;
            scroll.vertical          = true;
            scroll.scrollSensitivity = 30f;

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
            grid.padding         = new RectOffset(16, 16, 16, 16);
            grid.cellSize        = new Vector2(300f, 300f);
            grid.spacing         = new Vector2(12f, 12f);
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
            var play   = MakeButton("PlayButton", canvas, "PLAY");
            var playRt = play.gameObject.GetComponent<RectTransform>();
            playRt.anchorMin        = new Vector2(0f, 0f);
            playRt.anchorMax        = new Vector2(1f, 0f);
            playRt.pivot            = new Vector2(0.5f, 0f);
            playRt.offsetMin        = new Vector2(16f,  16f);
            playRt.offsetMax        = new Vector2(-16f, 80f); // 64 px height
            playRt.anchoredPosition = new Vector2(0f, 0f);

            StylePinkButton(play);
            var playLbl      = play.GetComponentInChildren<TextMeshProUGUI>();
            playLbl.fontSize  = 52f;
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
            lblTmp.fontSize  = 56f;
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
            ckTmp.fontSize  = 56f;
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

            // Progress fills array
            var fillProp = so.FindProperty("_progressFills");
            fillProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
                fillProp.GetArrayElementAtIndex(i).objectReferenceValue = r.ProgressFills[i];

            // Progress texts array
            var textProp = so.FindProperty("_progressTexts");
            textProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
                textProp.GetArrayElementAtIndex(i).objectReferenceValue = r.ProgressTexts[i];

            // Grid content + prefab
            so.FindProperty("_gridContent").objectReferenceValue        = r.GridContent;
            so.FindProperty("_levelButtonPrefab").objectReferenceValue  = r.Prefab;

            // Play button
            so.FindProperty("_playButton").objectReferenceValue = r.PlayButton;

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
            public Image[]             ProgressFills;
            public TextMeshProUGUI[]   ProgressTexts;
            public Transform           GridContent;
            public Button              PlayButton;
        }
    }
}
