using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using BrainBattle.Games.Kings.Logic;
using BrainBattle.Games.Kings.UI;
using BrainBattle.Kings;
using BrainBattle.Shared;

namespace BrainBattle.Editor
{
    public static class KingsSceneBuilder
    {
        [MenuItem("BrainBattle/Build Kings Scene")]
        static void Build()
        {
            if (!ConfirmRebuild()) return;

            Undo.SetCurrentGroupName("Build Kings Scene");
            int undoGroup = Undo.GetCurrentGroup();

            RemoveExisting();

            var r         = new Refs();
            r.Canvas      = CreateCanvas();
            r.GameMgrGO   = CreateGameManager();
            r.BootstrapGO = CreateSceneBootstrap();
            EnsureEventSystem();

            CreateGridContainer(r);
            CreateTutorialOverlay(r);
            CreateTipsPanel(r);
            CreateRestartConfirmPanel(r);
            CreateVictoryPanel(r);
            CreateHUD(r);

            WireAll(r);

            Undo.CollapseUndoOperations(undoGroup);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Selection.activeGameObject = r.Canvas;
            Debug.Log("[KingsSceneBuilder] Kings scene built and saved. Assign Crown/Dot sprites on GridContainer → KingsGridRenderer, then populate LevelLoader._allLevels.");
        }

        // ── Guards ─────────────────────────────────────────────────────────────────

        static bool ConfirmRebuild()
        {
            bool hasExisting = GameObject.Find("Canvas")        != null
                            || GameObject.Find("GameManager")    != null
                            || GameObject.Find("SceneBootstrap") != null;

            if (!hasExisting) return true;

            return EditorUtility.DisplayDialog(
                "Build Kings Scene",
                "Existing Kings scene objects (Canvas, GameManager, SceneBootstrap) " +
                "will be deleted and rebuilt from scratch. This action is undoable. Continue?",
                "Rebuild", "Cancel");
        }

        static void RemoveExisting()
        {
            foreach (string n in new[] { "Canvas", "GameManager", "SceneBootstrap" })
            {
                var go = GameObject.Find(n);
                if (go != null) Undo.DestroyObjectImmediate(go);
            }
        }

        // ── Top-level creators ─────────────────────────────────────────────────────

        static GameObject CreateCanvas()
        {
            var go = new GameObject("Canvas");
            Undo.RegisterCreatedObjectUndo(go, "Build Kings Scene");

            var canvas        = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler                 = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight  = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        static GameObject CreateGameManager()
        {
            var go = new GameObject("GameManager");
            Undo.RegisterCreatedObjectUndo(go, "Build Kings Scene");
            go.AddComponent<KingsGameManager>();
            go.AddComponent<LevelLoader>();
            go.AddComponent<TutorialController>();
            return go;
        }

        static GameObject CreateSceneBootstrap()
        {
            var go = new GameObject("SceneBootstrap");
            Undo.RegisterCreatedObjectUndo(go, "Build Kings Scene");
            go.AddComponent<KingsSceneBootstrap>();
            return go;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(go, "Build Kings Scene");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        // ── Canvas children ────────────────────────────────────────────────────────

        static void CreateGridContainer(Refs r)
        {
            r.GridContainer = MakeUIGO("GridContainer", r.Canvas.transform);
            r.GridContainer.AddComponent<KingsGridRenderer>();

            var rt              = r.GridContainer.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(900f, 900f);
        }

        static void CreateTutorialOverlay(Refs r)
        {
            // Root stays ACTIVE in hierarchy.
            // TutorialController (on GameManager) calls _overlayPanel.SetActive(false) in Awake.
            r.TutorialOverlayGO = MakeUIGO("TutorialOverlay", r.Canvas.transform);
            Stretch(r.TutorialOverlayGO);
            r.TutorialOverlayGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.80f);

            var root = r.TutorialOverlayGO.transform;

            r.StepCounter           = MakeTMP("StepCounter", root, "1 / 5");
            r.StepCounter.alignment = TextAlignmentOptions.Center;
            Anchor(r.StepCounter, 0.30f, 0.84f, 0.70f, 0.92f);

            r.StepText           = MakeTMP("StepText", root, "");
            r.StepText.alignment = TextAlignmentOptions.Center;
            r.StepText.fontSize  = 48f;
            Anchor(r.StepText, 0.06f, 0.35f, 0.94f, 0.78f);

            // SkipButton and NextButton — NextButton needs a TMP child so
            // TutorialController.Awake can swap its label via GetComponentInChildren.
            r.SkipButton = MakeButton("SkipButton", root, "Skip");
            Anchor(r.SkipButton, 0.05f, 0.06f, 0.32f, 0.14f);

            r.NextButton = MakeButton("NextButton", root, "Next");
            Anchor(r.NextButton, 0.68f, 0.06f, 0.95f, 0.14f);
        }

        static void CreateTipsPanel(Refs r)
        {
            // Stays ACTIVE — TipsButton.Awake hides it at runtime.
            r.TipsPanelGO = MakeUIGO("TipsPanel", r.Canvas.transform);
            Anchor(r.TipsPanelGO, 0.08f, 0.28f, 0.92f, 0.72f);
            r.TipsPanelGO.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.22f, 0.97f);

            var root = r.TipsPanelGO.transform;

            r.TipsText           = MakeTMP("TipsText", root, "");
            r.TipsText.alignment = TextAlignmentOptions.TopLeft;
            r.TipsText.fontSize  = 36f;
            Anchor(r.TipsText, 0.05f, 0.20f, 0.95f, 0.92f);

            r.CloseButton = MakeButton("CloseButton", root, "Close");
            Anchor(r.CloseButton, 0.35f, 0.04f, 0.65f, 0.16f);
        }

        static void CreateRestartConfirmPanel(Refs r)
        {
            // Stays ACTIVE — RestartButton.Awake hides it at runtime.
            r.RestartConfirmPanel = MakeUIGO("RestartConfirmPanel", r.Canvas.transform);
            Anchor(r.RestartConfirmPanel, 0.12f, 0.36f, 0.88f, 0.64f);
            r.RestartConfirmPanel.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.22f, 0.97f);

            var root = r.RestartConfirmPanel.transform;

            var prompt = MakeTMP("PromptText", root, "Restart this level?");
            prompt.alignment = TextAlignmentOptions.Center;
            Anchor(prompt, 0.05f, 0.56f, 0.95f, 0.92f);

            // Confirm / Cancel wired to RestartButton.ConfirmRestart / CancelRestart in WireAll.
            r.ConfirmButton = MakeButton("ConfirmButton", root, "Restart");
            Anchor(r.ConfirmButton, 0.54f, 0.08f, 0.93f, 0.46f);

            r.CancelButton = MakeButton("CancelButton", root, "Cancel");
            Anchor(r.CancelButton, 0.07f, 0.08f, 0.46f, 0.46f);
        }

        static void CreateVictoryPanel(Refs r)
        {
            // Root stays ACTIVE so VictoryPanel.OnEnable can subscribe to OnGameComplete.
            // VictoryContent is the animated card; VictoryPanel.Awake hides it at runtime.
            r.VictoryPanelGO = MakeUIGO("VictoryPanel", r.Canvas.transform);
            Stretch(r.VictoryPanelGO);
            r.VictoryPanelGO.AddComponent<VictoryPanel>();

            r.VictoryContent = MakeUIGO("VictoryContent", r.VictoryPanelGO.transform);
            Anchor(r.VictoryContent, 0.10f, 0.22f, 0.90f, 0.78f);
            r.VictoryContent.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.20f, 1f);

            var content = r.VictoryContent.transform;

            r.VPTimeText           = MakeTMP("TimeText", content, "00:00");
            r.VPTimeText.alignment = TextAlignmentOptions.Center;
            r.VPTimeText.fontSize  = 72f;
            Anchor(r.VPTimeText, 0.10f, 0.76f, 0.90f, 0.94f);

            r.VPMoveCountText           = MakeTMP("MoveCountText", content, "0");
            r.VPMoveCountText.alignment = TextAlignmentOptions.Center;
            Anchor(r.VPMoveCountText, 0.10f, 0.60f, 0.90f, 0.76f);

            r.VPStarRatingText           = MakeTMP("StarRatingText", content, "★★★");
            r.VPStarRatingText.alignment = TextAlignmentOptions.Center;
            r.VPStarRatingText.fontSize  = 80f;
            Anchor(r.VPStarRatingText, 0.10f, 0.44f, 0.90f, 0.60f);

            r.VPNextLevelButton = MakeButton("NextLevelButton", content, "Next Level");
            Anchor(r.VPNextLevelButton, 0.06f, 0.06f, 0.56f, 0.22f);

            r.VPRestartButton = MakeButton("RestartButton", content, "Restart");
            Anchor(r.VPRestartButton, 0.58f, 0.06f, 0.94f, 0.22f);

            r.VPMainMenuButton = MakeButton("MainMenuButton", content, "Menu");
            Anchor(r.VPMainMenuButton, 0.24f, 0.26f, 0.76f, 0.40f);
        }

        static void CreateHUD(Refs r)
        {
            r.HudGO = MakeUIGO("HUD", r.Canvas.transform);
            var rt              = r.HudGO.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(0f, 120f);

            var hud = r.HudGO.transform;

            r.UndoButtonGO = MakeHudButton("UndoButton",    hud, "Undo",    0.02f, 0.17f);
            r.UndoButtonGO.AddComponent<UndoButton>();

            r.HudRestartGO = MakeHudButton("RestartButton", hud, "Restart", 0.20f, 0.38f);
            r.HudRestartGO.AddComponent<RestartButton>();

            r.HudTimerText           = MakeTMP("TimerText", hud, "00:00");
            r.HudTimerText.alignment = TextAlignmentOptions.Center;
            Anchor(r.HudTimerText, 0.39f, 0.08f, 0.61f, 0.92f);

            r.HudMoveText           = MakeTMP("MoveCountText", hud, "0");
            r.HudMoveText.alignment = TextAlignmentOptions.Center;
            Anchor(r.HudMoveText, 0.63f, 0.08f, 0.79f, 0.92f);

            r.TipsButtonGO = MakeHudButton("TipsButton", hud, "Tips", 0.81f, 0.98f);
            r.TipsButtonGO.AddComponent<TipsButton>();
        }

        // Creates a Button+Label inside the HUD strip at the given horizontal anchor range.
        static GameObject MakeHudButton(string name, Transform parent, string label, float anchorL, float anchorR)
        {
            var btn = MakeButton(name, parent, label);
            Anchor(btn, anchorL, 0.08f, anchorR, 0.92f);
            return btn.gameObject;
        }

        // ── SerializeField wiring ──────────────────────────────────────────────────

        static void WireAll(Refs r)
        {
            var mgr       = r.GameMgrGO.GetComponent<KingsGameManager>();
            var loader    = r.GameMgrGO.GetComponent<LevelLoader>();
            var tc        = r.GameMgrGO.GetComponent<TutorialController>();
            var bootstrap = r.BootstrapGO.GetComponent<KingsSceneBootstrap>();
            var renderer  = r.GridContainer.GetComponent<KingsGridRenderer>();
            var vp        = r.VictoryPanelGO.GetComponent<VictoryPanel>();
            var undoComp  = r.UndoButtonGO.GetComponent<UndoButton>();
            var restComp  = r.HudRestartGO.GetComponent<RestartButton>();
            var tipsComp  = r.TipsButtonGO.GetComponent<TipsButton>();

            // KingsGameManager
            Set(mgr, "_gridRenderer",       renderer);
            Set(mgr, "_tutorialController", tc);
            // _currentGrid is [Serializable] non-Object — set by code at runtime, skip here.

            // TutorialController (lives on GameManager, not TutorialOverlay — see SceneSetupGuide §7)
            Set(tc, "_overlayPanel", r.TutorialOverlayGO);
            Set(tc, "_stepText",     r.StepText);
            Set(tc, "_stepCounter",  r.StepCounter);
            Set(tc, "_nextButton",   r.NextButton);
            Set(tc, "_skipButton",   r.SkipButton);

            // KingsSceneBootstrap
            Set(bootstrap, "_levelLoader", loader);
            Set(bootstrap, "_gameManager", mgr);

            // UndoButton
            Set(undoComp, "_gameManager", mgr);
            Set(undoComp, "_button",      r.UndoButtonGO.GetComponent<Button>());

            // RestartButton (HUD)
            Set(restComp, "_gameManager",  mgr);
            Set(restComp, "_button",       r.HudRestartGO.GetComponent<Button>());
            Set(restComp, "_confirmPanel", r.RestartConfirmPanel);

            // TipsButton
            Set(tipsComp, "_gameManager", mgr);
            Set(tipsComp, "_button",      r.TipsButtonGO.GetComponent<Button>());
            Set(tipsComp, "_tipsPanel",   r.TipsPanelGO);
            Set(tipsComp, "_tipsText",    r.TipsText);
            Set(tipsComp, "_closeButton", r.CloseButton);

            // VictoryPanel
            Set(vp, "_panel",           r.VictoryContent);
            Set(vp, "_timeText",        r.VPTimeText);
            Set(vp, "_moveCountText",   r.VPMoveCountText);
            Set(vp, "_starRatingText",  r.VPStarRatingText);
            Set(vp, "_nextLevelButton", r.VPNextLevelButton);
            Set(vp, "_restartButton",   r.VPRestartButton);
            Set(vp, "_mainMenuButton",  r.VPMainMenuButton);
            Set(vp, "_gameManager",     mgr);
            Set(vp, "_sceneBootstrap",  bootstrap);

            // RestartConfirmPanel: ConfirmButton / CancelButton wired via persistent onClick
            // so they appear in the Inspector as if set by hand (serialized, survives domain reload).
            UnityEventTools.AddPersistentListener(
                r.ConfirmButton.onClick, (UnityAction)restComp.ConfirmRestart);
            UnityEventTools.AddPersistentListener(
                r.CancelButton.onClick,  (UnityAction)restComp.CancelRestart);
        }

        // ── Primitive helpers ──────────────────────────────────────────────────────

        static GameObject MakeUIGO(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Build Kings Scene");
            go.transform.SetParent(parent, false);
            return go;
        }

        // Creates a Button with an Image background and a centred TMP label child.
        // The TMP child is required by TutorialController.Awake (GetComponentInChildren).
        static Button MakeButton(string name, Transform parent, string label)
        {
            var go  = MakeUIGO(name, parent);
            go.AddComponent<Image>();
            var btn = go.AddComponent<Button>();

            var labelGo  = MakeUIGO("Label", go.transform);
            var tmp      = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.alignment = TextAlignmentOptions.Center;
            Stretch(labelGo);

            return btn;
        }

        static TextMeshProUGUI MakeTMP(string name, Transform parent, string text)
        {
            var go  = MakeUIGO(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            return tmp;
        }

        // Anchors a RectTransform using normalized parent coordinates; zeros all offsets.
        static void Anchor(RectTransform rt, float minX, float minY, float maxX, float maxY)
        {
            rt.anchorMin        = new Vector2(minX, minY);
            rt.anchorMax        = new Vector2(maxX, maxY);
            rt.offsetMin        = Vector2.zero;
            rt.offsetMax        = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        static void Anchor(Component c,  float minX, float minY, float maxX, float maxY)
            => Anchor(c.GetComponent<RectTransform>(), minX, minY, maxX, maxY);

        static void Anchor(GameObject go, float minX, float minY, float maxX, float maxY)
            => Anchor(go.GetComponent<RectTransform>(), minX, minY, maxX, maxY);

        static void Stretch(GameObject go) => Stretch(go.GetComponent<RectTransform>());

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.offsetMin        = Vector2.zero;
            rt.offsetMax        = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = Vector2.zero;
        }

        // Sets a [SerializeField] via SerializedObject so the assignment is serialized,
        // shows in the Inspector, and survives domain reload — identical to hand-wiring.
        static void Set(Component target, string field, Object value)
        {
            var so   = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[KingsSceneBuilder] '{field}' not found on {target.GetType().Name}. Check for renamed field.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Scene references container ─────────────────────────────────────────────

        private sealed class Refs
        {
            // Root GameObjects
            public GameObject Canvas;
            public GameObject GameMgrGO;
            public GameObject BootstrapGO;

            // Canvas → GridContainer
            public GameObject GridContainer;

            // Canvas → TutorialOverlay
            public GameObject      TutorialOverlayGO;
            public TextMeshProUGUI StepText;
            public TextMeshProUGUI StepCounter;
            public Button          NextButton;
            public Button          SkipButton;

            // Canvas → TipsPanel
            public GameObject      TipsPanelGO;
            public TextMeshProUGUI TipsText;
            public Button          CloseButton;

            // Canvas → RestartConfirmPanel
            public GameObject RestartConfirmPanel;
            public Button     ConfirmButton;
            public Button     CancelButton;

            // Canvas → VictoryPanel / VictoryContent
            public GameObject      VictoryPanelGO;
            public GameObject      VictoryContent;
            public TextMeshProUGUI VPTimeText;
            public TextMeshProUGUI VPMoveCountText;
            public TextMeshProUGUI VPStarRatingText;
            public Button          VPNextLevelButton;
            public Button          VPRestartButton;
            public Button          VPMainMenuButton;

            // Canvas → HUD
            public GameObject      HudGO;
            public GameObject      UndoButtonGO;
            public GameObject      HudRestartGO;
            public GameObject      TipsButtonGO;
            public TextMeshProUGUI HudTimerText;
            public TextMeshProUGUI HudMoveText;
        }
    }
}
