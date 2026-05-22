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
        private static readonly Color ColPanel      = new Color(0.10f, 0.10f, 0.18f, 1f);
        private static readonly Color ColAccent     = new Color(0.93f, 0.26f, 0.56f, 1f);
        private static readonly Color ColTabInact   = new Color(0.12f, 0.12f, 0.22f, 1f);
        private static readonly Color ColBtnBg      = new Color(0.18f, 0.22f, 0.45f, 1f);

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

            // Background
            var bg = UI("Background", canvas);
            Stretch(bg);
            bg.AddComponent<Image>().color = ColBg;

            // Header
            var header = UI("Header", canvas);
            Anchor(header, 0f, 0.90f, 1f, 1.00f);
            header.AddComponent<Image>().color = ColPanel;
            var headerTmp     = MakeTMP("Title", header.transform, "LEVEL SELECT");
            headerTmp.fontSize = 72f;
            headerTmp.fontStyle = FontStyles.Bold;
            headerTmp.alignment = TextAlignmentOptions.Center;
            Stretch(headerTmp.gameObject);

            // ── Tab section ───────────────────────────────────────────────────────
            // TabRow: 3 equal button columns
            var tabRow = UI("TabRow", canvas);
            Anchor(tabRow, 0f, 0.80f, 1f, 0.90f);

            string[] tabNames = { "Beginner", "Expert", "Impossible" };
            var tabButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                float x0 = i * (1f / 3f), x1 = (i + 1) * (1f / 3f);
                var cell = UI(tabNames[i] + "Tab", tabRow.transform);
                Anchor(cell, x0 + 0.005f, 0f, x1 - 0.005f, 1f);
                var img   = cell.AddComponent<Image>();
                img.color = i == 0 ? ColAccent : ColTabInact;
                var btn   = cell.AddComponent<Button>();
                ApplyBtnColors(btn);
                var lbl     = MakeTMP("Label", cell.transform, tabNames[i]);
                lbl.alignment = TextAlignmentOptions.Center;
                lbl.fontSize  = 38f;
                Stretch(lbl.gameObject);
                tabButtons[i] = btn;
            }
            r.TabButtons = tabButtons;

            // ── Progress row ──────────────────────────────────────────────────────
            var progRow = UI("ProgressRow", canvas);
            Anchor(progRow, 0f, 0.73f, 1f, 0.80f);

            var progFills = new Image[3];
            var progTexts = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                float x0 = i * (1f / 3f), x1 = (i + 1) * (1f / 3f);

                // Outer container
                var group = UI($"ProgressGroup{i}", progRow.transform);
                Anchor(group, x0 + 0.01f, 0f, x1 - 0.01f, 1f);

                // Track (dark background)
                var track = UI("Track", group.transform);
                Anchor(track, 0f, 0.45f, 0.78f, 1.00f);
                track.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.20f, 1f);

                // Fill image (Image.Type.Filled, Horizontal)
                var fill    = UI("Fill", track.transform);
                Stretch(fill);
                var fillImg         = fill.AddComponent<Image>();
                fillImg.color       = ColAccent;
                fillImg.type        = Image.Type.Filled;
                fillImg.fillMethod  = Image.FillMethod.Horizontal;
                fillImg.fillAmount  = 0f;
                fillImg.raycastTarget = false;
                progFills[i] = fillImg;

                // Percentage text
                var pct     = MakeTMP($"PctText{i}", group.transform, "0%");
                pct.alignment = TextAlignmentOptions.MidlineLeft;
                pct.fontSize  = 32f;
                Anchor(pct.gameObject, 0.80f, 0f, 1.00f, 1f);
                progTexts[i] = pct;
            }
            r.ProgressFills = progFills;
            r.ProgressTexts = progTexts;

            // ── Scroll view ───────────────────────────────────────────────────────
            var scrollGO = UI("LevelScrollView", canvas);
            Anchor(scrollGO, 0.03f, 0.14f, 0.97f, 0.73f);
            scrollGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f); // transparent

            var scroll              = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal       = false;
            scroll.vertical         = true;
            scroll.scrollSensitivity = 30f;

            // Viewport — Image MUST be opaque (Color.white) for the Mask stencil to clip correctly.
            // showMaskGraphic=false hides the image visually while still defining the clip region.
            var viewportGO = UI("Viewport", scrollGO.transform);
            Stretch(viewportGO);
            viewportGO.AddComponent<Image>().color = Color.white;
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

            var grid              = contentGO.AddComponent<GridLayoutGroup>();
            grid.padding          = new RectOffset(14, 14, 14, 14);
            grid.cellSize         = new Vector2(250f, 250f);
            grid.spacing          = new Vector2(14f, 14f);
            grid.startCorner      = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis        = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment   = TextAnchor.UpperLeft;
            grid.constraint       = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount  = 3;

            var csf               = contentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit       = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit     = ContentSizeFitter.FitMode.Unconstrained;

            scroll.viewport    = viewportGO.GetComponent<RectTransform>();
            scroll.content     = contentRt;
            r.GridContent      = contentGO.transform;

            // ── Play button ───────────────────────────────────────────────────────
            var play  = MakeButton("PlayButton", canvas, "PLAY");
            Anchor(play.gameObject, 0.05f, 0.02f, 0.95f, 0.12f);
            // Make it more prominent: accent color + bigger font.
            play.GetComponent<Image>().color = ColAccent;
            play.GetComponentInChildren<TextMeshProUGUI>().fontSize = 56f;
            r.PlayButton = play;
        }

        // ── LevelSelectButton prefab ───────────────────────────────────────────────

        static GameObject BuildLevelButtonPrefab()
        {
            // Build in memory, save as prefab, destroy temp instance.
            var root       = new GameObject("LevelSelectButton");
            var rootImg    = root.AddComponent<Image>();
            rootImg.color  = new Color(0.93f, 0.26f, 0.56f, 1f);
            var btn        = root.AddComponent<Button>();
            ApplyBtnColors(btn);
            root.AddComponent<LevelSelectButton>();

            // Level number label (bottom-centre)
            var lblGO  = new GameObject("LevelLabel", typeof(RectTransform));
            lblGO.transform.SetParent(root.transform, false);
            var lblRt        = lblGO.GetComponent<RectTransform>();
            lblRt.anchorMin  = new Vector2(0f, 0f);
            lblRt.anchorMax  = new Vector2(1f, 0.40f);
            lblRt.offsetMin  = Vector2.zero;
            lblRt.offsetMax  = Vector2.zero;
            var lblTmp       = lblGO.AddComponent<TextMeshProUGUI>();
            lblTmp.text      = "1";
            lblTmp.alignment = TextAlignmentOptions.Center;
            lblTmp.fontSize  = 48f;
            lblTmp.color     = Color.white;
            lblTmp.fontStyle = FontStyles.Bold;

            // Checkmark overlay (centre, hidden by default)
            var ckGO  = new GameObject("Checkmark", typeof(RectTransform));
            ckGO.transform.SetParent(root.transform, false);
            var ckRt        = ckGO.GetComponent<RectTransform>();
            ckRt.anchorMin  = new Vector2(0.1f, 0.4f);
            ckRt.anchorMax  = new Vector2(0.9f, 1.0f);
            ckRt.offsetMin  = Vector2.zero;
            ckRt.offsetMax  = Vector2.zero;
            var ckTmp       = ckGO.AddComponent<TextMeshProUGUI>();
            ckTmp.text      = "✓"; // ✓
            ckTmp.alignment = TextAlignmentOptions.Center;
            ckTmp.fontSize  = 96f;
            ckTmp.color     = Color.white;
            ckGO.SetActive(false); // hidden until Completed

            // Lock overlay (centre, hidden by default)
            var lkGO  = new GameObject("LockOverlay", typeof(RectTransform));
            lkGO.transform.SetParent(root.transform, false);
            var lkRt        = lkGO.GetComponent<RectTransform>();
            lkRt.anchorMin  = Vector2.zero;
            lkRt.anchorMax  = Vector2.one;
            lkRt.offsetMin  = Vector2.zero;
            lkRt.offsetMax  = Vector2.zero;
            var lkImg       = lkGO.AddComponent<Image>();
            lkImg.color     = new Color(0f, 0f, 0f, 0.55f);
            lkImg.raycastTarget = false;
            var lkTmp       = new GameObject("LockLabel", typeof(RectTransform));
            lkTmp.transform.SetParent(lkGO.transform, false);
            var lkLblRt          = lkTmp.GetComponent<RectTransform>();
            lkLblRt.anchorMin    = Vector2.zero;
            lkLblRt.anchorMax    = Vector2.one;
            lkLblRt.offsetMin    = Vector2.zero;
            lkLblRt.offsetMax    = Vector2.zero;
            var lkText           = lkTmp.AddComponent<TextMeshProUGUI>();
            lkText.text          = "🔒"; // will fallback to box; see note below
            lkText.text          = "X";            // ASCII fallback for TMP default font
            lkText.alignment     = TextAlignmentOptions.Center;
            lkText.fontSize      = 80f;
            lkText.color         = new Color(1f, 1f, 1f, 0.80f);
            lkGO.SetActive(false); // hidden until Locked

            // Wire LevelSelectButton SerializeFields via SerializedObject.
            var so = new SerializedObject(root.GetComponent<LevelSelectButton>());
            so.FindProperty("_background").objectReferenceValue   = rootImg;
            so.FindProperty("_levelLabel").objectReferenceValue   = lblTmp;
            so.FindProperty("_checkmark").objectReferenceValue    = ckGO;
            so.FindProperty("_lockOverlay").objectReferenceValue  = lkGO;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save as prefab asset.
            Directory.CreateDirectory("Assets/_Project/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[LevelSelectSceneBuilder] Prefab saved → " + PrefabPath);
            return prefab;
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
