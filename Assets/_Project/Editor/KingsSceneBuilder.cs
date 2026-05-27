using System.Collections.Generic;
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
using BrainBattle.Shared.UI;

namespace BrainBattle.Editor
{
    public static class KingsSceneBuilder
    {
        [MenuItem("BrainBattle/Build Kings Scene")]
        static void Build()
        {
            // Builder must run in Edit mode — components' Awake() fire in Play mode
            // and NullRef before WireAll has a chance to assign references.
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                EditorUtility.DisplayDialog("Build Kings Scene",
                    "Play Mode stopped. Wait for it to exit, then run Build Kings Scene again.", "OK");
                return;
            }

            if (!ConfirmRebuild()) return;

            Undo.SetCurrentGroupName("Build Kings Scene");
            int undoGroup = Undo.GetCurrentGroup();

            RemoveExisting();

            var r         = new Refs();
            r.Canvas      = CreateCanvas();
            r.GameMgrGO   = CreateGameManager();
            r.BootstrapGO = CreateSceneBootstrap();
            EnsureEventSystem();

            CreateBackground(r);
            CreateGridContainer(r);
            CreateTimerBar(r);
            CreateTutorialOverlay(r);
            CreateTipsPanel(r);
            CreateRestartConfirmPanel(r);
            CreateHUD(r);
            CreateVictoryPanel(r);  // last = topmost, covers HUD during victory

            WireAll(r);

            // Apply Outfit body font to every TMP in the scene, then restore
            // the icon font on the star-rating label (needs special Unicode glyphs).
            var bodyFont = GetOrCreateBodyFont();
            if (bodyFont != null)
            {
                foreach (var tmp in r.Canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
                    tmp.font = bodyFont;
            }
            var iconFont = GetOrCreateHudIconFont();
            if (iconFont != null && r.VPStarRatingText != null)
                r.VPStarRatingText.font = iconFont;

            Undo.CollapseUndoOperations(undoGroup);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            if (!EditorApplication.isPlaying)
                EditorSceneManager.SaveOpenScenes();

            Selection.activeGameObject = r.Canvas;
            Debug.Log("[KingsSceneBuilder] Kings scene built and saved. All references, sprites, and level data auto-assigned — no manual steps required.");
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

            // Reset cached references — font creation functions reuse valid existing assets
            // or recreate only if the atlas texture is broken/missing.
            _hudIconFont       = null;
            _bodyFont          = null;
            _roundedRectSprite = null;
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
            scaler.referenceResolution = new Vector2(1170f, 2532f);
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
            var es = Object.FindFirstObjectByType<EventSystem>();

            if (es == null)
            {
                var go = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(go, "Build Kings Scene");
                es = go.AddComponent<EventSystem>();
            }

            // Prefer InputSystemUIInputModule when com.unity.inputsystem is present.
            // Type.GetType avoids a hard compile-time dependency on Unity.InputSystem (autoReferenced: false).
            System.Type inputModuleType =
                System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem")
                ?? typeof(StandaloneInputModule);

            // Remove legacy StandaloneInputModule if we're upgrading to InputSystemUIInputModule.
            if (inputModuleType != typeof(StandaloneInputModule))
            {
                var legacy = es.GetComponent<StandaloneInputModule>();
                if (legacy != null) Undo.DestroyObjectImmediate(legacy);
            }

            // Ensure exactly one input module of the correct type is present.
            if (es.GetComponent(inputModuleType) == null)
                Undo.AddComponent(es.gameObject, inputModuleType);
        }

        // ── Canvas children ────────────────────────────────────────────────────────

        static void CreateBackground(Refs r)
        {
            var bgGO = MakeUIGO("Background", r.Canvas.transform);
            Stretch(bgGO);
            bgGO.transform.SetSiblingIndex(0);

            var img = bgGO.AddComponent<Image>();
            img.color          = NavyBg;
            img.type           = Image.Type.Simple;
            img.preserveAspect = false;
        }

        static void CreateGridContainer(Refs r)
        {
            r.GridContainer = MakeUIGO("GridContainer", r.Canvas.transform);
            r.GridContainer.AddComponent<KingsGridRenderer>();

            // Full canvas minus HUD: offsetMin.y = 88 keeps the bottom edge flush
            // with the top of the HUD bar.  offsetMax = zero = canvas top.
            var rt       = r.GridContainer.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(0f, 120f);   // bottom = top of HUD
            rt.offsetMax = new Vector2(0f,   0f);  // top    = canvas top
        }

        static void CreateTutorialOverlay(Refs r)
        {
            // Root stays ACTIVE in hierarchy.
            // TutorialController (on GameManager) calls _overlayPanel.SetActive(false) in Awake.
            r.TutorialOverlayGO = MakeUIGO("TutorialOverlay", r.Canvas.transform);
            Stretch(r.TutorialOverlayGO);
            r.TutorialOverlayGO.AddComponent<Image>().color = DesignSystem.BorderRegion;

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
            r.VictoryPanelGO = MakeUIGO("VictoryPanel", r.Canvas.transform);
            Stretch(r.VictoryPanelGO);
            r.VictoryPanelGO.AddComponent<VictoryPanel>();

            // Full-screen content — hidden until victory.
            r.VictoryContent = MakeUIGO("VictoryContent", r.VictoryPanelGO.transform);
            Stretch(r.VictoryContent);
            var vcImg           = r.VictoryContent.AddComponent<Image>();
            vcImg.color         = Color.white;
            vcImg.type          = Image.Type.Simple;
            vcImg.preserveAspect = false;
            // Load victory background sprite from Resources.
            const string VictoryBgPath = "Assets/_Project/Resources/Sprites/victory_screen_bg.png";
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(VictoryBgPath))
            {
                if (asset is Sprite s) { vcImg.sprite = s; break; }
            }
            if (vcImg.sprite == null)
            {
                Debug.LogWarning("[KingsSceneBuilder] victory_screen_bg sprite not found — using fallback color.");
                vcImg.color = new Color(0.05f, 0.05f, 0.15f, 0.97f);
            }
            r.VictoryContent.SetActive(false);

            var content = r.VictoryContent.transform;

            // ── VICTORY! title ────────────────────────────────────────────────────
            var title           = MakeTMP("Title", content, "VICTORY!");
            title.alignment     = TextAlignmentOptions.Center;
            title.fontStyle     = FontStyles.Bold;
            title.fontSize      = 96f;
            title.color         = PinkAccent;
            Anchor(title, 0.05f, 0.82f, 0.95f, 0.96f);

            // ── Star rating ───────────────────────────────────────────────────────
            r.VPStarRatingText           = MakeTMP("StarRatingText", content, "◆◆◆");
            r.VPStarRatingText.alignment = TextAlignmentOptions.Center;
            r.VPStarRatingText.fontSize  = 96f;
            r.VPStarRatingText.color     = PinkAccent;
            var starFont = GetOrCreateHudIconFont();
            if (starFont != null) r.VPStarRatingText.font = starFont;
            Anchor(r.VPStarRatingText, 0.05f, 0.68f, 0.95f, 0.84f);

            // ── Time ──────────────────────────────────────────────────────────────
            var timeLabel           = MakeTMP("TimeLabel", content, "TIME");
            timeLabel.alignment     = TextAlignmentOptions.Center;
            timeLabel.fontSize      = 30f;
            timeLabel.color         = new Color(0.70f, 0.70f, 0.90f, 1f);
            Anchor(timeLabel, 0.05f, 0.60f, 0.95f, 0.68f);

            r.VPTimeText            = MakeTMP("TimeText", content, "00:00");
            r.VPTimeText.alignment  = TextAlignmentOptions.Center;
            r.VPTimeText.fontStyle  = FontStyles.Bold;
            r.VPTimeText.fontSize   = 80f;
            Anchor(r.VPTimeText, 0.05f, 0.50f, 0.95f, 0.62f);

            // ── Moves ─────────────────────────────────────────────────────────────
            var movesLabel           = MakeTMP("MovesLabel", content, "MOVES");
            movesLabel.alignment     = TextAlignmentOptions.Center;
            movesLabel.fontSize      = 30f;
            movesLabel.color         = new Color(0.70f, 0.70f, 0.90f, 1f);
            Anchor(movesLabel, 0.05f, 0.42f, 0.95f, 0.50f);

            r.VPMoveCountText           = MakeTMP("MoveCountText", content, "0");
            r.VPMoveCountText.alignment = TextAlignmentOptions.Center;
            r.VPMoveCountText.fontSize  = 72f;
            Anchor(r.VPMoveCountText, 0.05f, 0.32f, 0.95f, 0.44f);

            // ── Buttons ───────────────────────────────────────────────────────────
            r.VPMainMenuButton = MakeButton("MainMenuButton", content, "Menu");
            StylePinkButton(r.VPMainMenuButton);
            Anchor(r.VPMainMenuButton, 0.08f, 0.20f, 0.92f, 0.31f);

            r.VPNextLevelButton = MakeButton("NextLevelButton", content, "Next Level");
            StylePinkButton(r.VPNextLevelButton);
            Anchor(r.VPNextLevelButton, 0.08f, 0.06f, 0.50f, 0.18f);

            r.VPRestartButton = MakeButton("RestartButton", content, "Restart");
            StylePinkButton(r.VPRestartButton);
            Anchor(r.VPRestartButton, 0.52f, 0.06f, 0.92f, 0.18f);
        }

        // Applies hot-pink (#ff2d78) style to a Button created by MakeButton().
        static void StylePinkButton(Button btn)
        {
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = PinkAccent;

            var colors               = btn.colors;
            colors.normalColor       = Color.white;
            colors.highlightedColor  = new Color(1f, 0.4f, 0.6f, 1f);
            colors.pressedColor      = new Color(0.7f, 0.1f, 0.3f, 1f);
            colors.disabledColor     = new Color(1f, 1f, 1f, 0.40f);
            btn.colors               = colors;
        }

        static void CreateTimerBar(Refs r)
        {
            r.TimerBarGO = MakeUIGO("TimerBar", r.Canvas.transform);
            var rt              = r.TimerBarGO.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(0f, DesignSystem.TimerBarHeight);
            r.TimerBarGO.AddComponent<Image>().color = NavyBg;

            r.HudTimerText           = MakeTMP("TimerText", r.TimerBarGO.transform, "00:00");
            r.HudTimerText.alignment = TextAlignmentOptions.Center;
            r.HudTimerText.fontStyle = FontStyles.Bold;
            r.HudTimerText.fontSize  = 44f;
            Stretch(r.HudTimerText.gameObject);
        }

        static void CreateHUD(Refs r)
        {
            r.HudGO = MakeUIGO("HUD", r.Canvas.transform);
            var rt       = r.HudGO.GetComponent<RectTransform>();
            // Fixed 120 px bar anchored to the canvas bottom edge.
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(0f,   0f);
            rt.offsetMax = new Vector2(0f, 120f);
            r.HudGO.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.88f);
            r.HudGO.AddComponent<HUDController>();

            var hud = r.HudGO.transform;

            // 1px separator anchored to the very top edge of the HUD bar
            var sepGO              = MakeUIGO("TopBorder", hud);
            var sepRT              = sepGO.GetComponent<RectTransform>();
            sepRT.anchorMin        = new Vector2(0f, 1f);
            sepRT.anchorMax        = new Vector2(1f, 1f);
            sepRT.pivot            = new Vector2(0.5f, 1f);
            sepRT.anchoredPosition = Vector2.zero;
            sepRT.sizeDelta        = new Vector2(0f, 1f);
            sepGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

            // Full-area row container with HorizontalLayoutGroup
            var rowGO = MakeUIGO("HUDRow", hud);
            var rowRT = rowGO.GetComponent<RectTransform>();
            rowRT.anchorMin = Vector2.zero;
            rowRT.anchorMax = Vector2.one;
            rowRT.offsetMin = Vector2.zero;
            rowRT.offsetMax = Vector2.zero;

            var hlg                    = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing                = 5f;
            hlg.childAlignment         = TextAnchor.MiddleCenter;
            hlg.childControlWidth      = true;
            hlg.childControlHeight     = true;
            hlg.childForceExpandWidth  = true;
            hlg.childForceExpandHeight = true;
            hlg.padding                = new RectOffset(8, 8, 6, 6);

            var row = rowGO.transform;

            var grey = new Color(0.53f, 0.53f, 0.67f, 1f);   // #8888aa — muted labels
            var pink = new Color(1f,    0.18f, 0.47f, 1f);   // #ff2d78 — tips accent

            // 1. Undo
            r.UndoButtonGO = MakeHudBtn("UndoButton",   row, "UNDO",    new Color(1f, 1f, 1f, 0.12f), grey);
            r.UndoButtonGO.AddComponent<UndoButton>();

            // 2. Restart
            r.HudRestartGO = MakeHudBtn("RestartButton", row, "RESTART", new Color(1f, 1f, 1f, 0.12f), grey);
            r.HudRestartGO.AddComponent<RestartButton>();

            // 3. Move counter — pink hero element, no button interaction
            r.HudMoveText = MakeHudMoveCounter("MoveCounter", row);

            // 4. Menu
            r.MenuButtonGO = MakeHudBtn("MenuButton",  row, "MENU",    new Color(1f, 1f, 1f, 0.12f), grey);
            r.MenuButtonGO.AddComponent<MenuButton>();

            // 5. Tips — pink accent
            r.TipsButtonGO = MakeHudBtn("TipsButton",  row, "TIPS",    new Color(1f, 0.11f, 0.47f, 0.15f), pink);
            r.TipsButtonGO.AddComponent<TipsButton>();
        }

        // Creates a HUD button: single centred label TMP, no icon.
        static GameObject MakeHudBtn(string name, Transform parent, string label,
            Color bgColor, Color textColor)
        {
            var go  = MakeUIGO(name, parent);
            var img = go.AddComponent<Image>();
            img.sprite = GetOrCreateRoundedRectSprite();
            img.type   = Image.Type.Sliced;
            img.color  = bgColor;

            var btn                  = go.AddComponent<Button>();
            var colors               = btn.colors;
            colors.normalColor       = Color.white;
            colors.highlightedColor  = new Color(1f, 1f, 1f, 0.8f);
            colors.pressedColor      = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor     = new Color(1f, 1f, 1f, 0.4f);
            btn.colors               = colors;

            var le            = go.AddComponent<LayoutElement>();
            le.flexibleWidth  = 1f;
            le.preferredWidth = 110f;

            // Single stretched TMP label — no icon, no VLG needed.
            var labelGO               = MakeUIGO("Label", go.transform);
            Stretch(labelGO);
            var labelTMP              = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text             = label;
            labelTMP.alignment        = TextAlignmentOptions.Center;
            labelTMP.color            = textColor;
            labelTMP.fontSize         = 30f;
            labelTMP.fontStyle        = FontStyles.Bold;
            labelTMP.enableAutoSizing = false;

            return go;
        }

        // Creates the move counter: large pink count, centred.
        // Returns the TMP (wired to HUDController._moveCountText).
        static TextMeshProUGUI MakeHudMoveCounter(string name, Transform parent)
        {
            var go  = MakeUIGO(name, parent);
            var img = go.AddComponent<Image>();
            img.sprite = GetOrCreateRoundedRectSprite();
            img.type   = Image.Type.Sliced;
            img.color  = new Color(1f, 0.11f, 0.47f, 0.15f);

            var le            = go.AddComponent<LayoutElement>();
            le.flexibleWidth  = 1.5f;
            le.preferredWidth = 140f;

            var numGO               = MakeUIGO("MoveCountText", go.transform);
            Stretch(numGO);
            var numTMP              = numGO.AddComponent<TextMeshProUGUI>();
            numTMP.text             = "0";
            numTMP.alignment        = TextAlignmentOptions.Center;
            numTMP.color            = DesignSystem.Primary;
            numTMP.fontStyle        = FontStyles.Bold;
            numTMP.fontSize         = 28f;
            numTMP.enableAutoSizing = false;

            return numTMP;
        }

        static Sprite TryLoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        // ── SerializeField wiring ──────────────────────────────────────────────────

        static void WireAll(Refs r)
        {
            var mgr       = r.GameMgrGO.GetComponent<KingsGameManager>();
            var loader    = r.GameMgrGO.GetComponent<LevelLoader>();
            var tc        = r.GameMgrGO.GetComponent<TutorialController>();
            var bootstrap = r.BootstrapGO.GetComponent<KingsSceneBootstrap>();
            var renderer  = r.GridContainer.GetComponent<KingsGridRenderer>();
            var vp        = r.VictoryPanelGO.GetComponent<VictoryPanel>();
            var hudComp   = r.HudGO.GetComponent<HUDController>();
            var undoComp  = r.UndoButtonGO.GetComponent<UndoButton>();
            var restComp  = r.HudRestartGO.GetComponent<RestartButton>();
            var tipsComp  = r.TipsButtonGO.GetComponent<TipsButton>();

            // KingsGridRenderer — assign crown/dot sprites automatically.
            AssignGridRendererSprites(renderer);

            // LevelSelectButton prefab — pre-assign 4 level-state sprites.
            AssignLevelSelectButtonSprites();

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

            // LevelLoader — auto-find and assign all LevelData assets in the project.
            PopulateLevelLoader(loader);

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

            // HUDController — timer + moves display
            Set(hudComp, "_gameManager",   mgr);
            Set(hudComp, "_timerText",     r.HudTimerText);
            Set(hudComp, "_moveCountText", r.HudMoveText);

            // VictoryPanel
            Set(vp, "_panel",           r.VictoryContent);
            Set(vp, "_hud",             r.HudGO);
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
            var img = go.AddComponent<Image>();
            img.sprite = GetOrCreateRoundedRectSprite();
            img.type   = Image.Type.Sliced;
            img.color  = BtnBg;
            var btn = go.AddComponent<Button>();

            // Tint the button states so pressed/highlighted are visible.
            var colors               = btn.colors;
            colors.normalColor       = Color.white;
            colors.highlightedColor  = new Color(0.85f, 0.85f, 1.00f, 1f);
            colors.pressedColor      = new Color(0.65f, 0.65f, 0.90f, 1f);
            colors.disabledColor     = new Color(1f, 1f, 1f, 0.40f);
            btn.colors               = colors;

            var labelGo  = MakeUIGO("Label", go.transform);
            var tmp      = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;
            tmp.fontSize  = 36f;
            Stretch(labelGo);

            return btn;
        }

        static TextMeshProUGUI MakeTMP(string name, Transform parent, string text)
        {
            var go  = MakeUIGO(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text  = text;
            tmp.color = Color.white;
            return tmp;
        }

        // ── HUD icon font ─────────────────────────────────────────────────────────
        // LiberationSans SDF (pre-baked) covers ASCII only.  We create a Dynamic
        // TMP font asset from the bundled LiberationSans.ttf so that glyphs in the
        // Arrows (U+2190+) and Mathematical Operators (U+2200+) blocks are generated
        // on-demand.  The asset is saved to disk so it survives domain reload and
        // is included in Android/iOS builds (Assets/TextMesh Pro is always packaged).

        static TMP_FontAsset _hudIconFont;

        static TMP_FontAsset GetOrCreateHudIconFont()
        {
            const string AssetPath = "Assets/_Project/Resources/Fonts/HUDIcons SDF.asset";

            if (_hudIconFont != null) return _hudIconFont;

            var existingIcon = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
            bool isIconValid = false;
            try { isIconValid = existingIcon != null && existingIcon.atlasTextures != null
                             && existingIcon.atlasTextures.Length > 0 && existingIcon.atlasTextures[0] != null
                             && existingIcon.atlasTextures[0].width > 1  // atlas must have real pixel data
                             && existingIcon.atlasPopulationMode == AtlasPopulationMode.Static
                             && existingIcon.characterTable.Count > 0; }
            catch { }

            if (isIconValid) { _hudIconFont = existingIcon; return _hudIconFont; }

            // Existing asset is missing or has broken atlas — delete and recreate.
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(AssetPath)))
                AssetDatabase.DeleteAsset(AssetPath);

            // Prefer Segoe UI Symbol (ships with Windows 7+) — it covers Arrows, Dingbats, and
            // Miscellaneous Symbols, giving us ↩ ↺ ☰ ✦.  We copy the TTF into the project as a
            // proper Asset so TMP_FontAsset.CreateFontAsset can read its glyph data.
            // Fall back to bundled LiberationSans if Segoe isn't available.
            Font srcFont = null;
            const string SymAssetPath = "Assets/_Project/Resources/Fonts/SegoeSym.ttf";

            // Copy once from Windows Fonts if not already in project.
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(SymAssetPath)))
            {
                string winFonts = System.Environment.GetFolderPath(
                                      System.Environment.SpecialFolder.Fonts);
                foreach (var filename in new[] { "seguisym.ttf", "seguisym2.ttf" })
                {
                    string src = System.IO.Path.Combine(winFonts, filename);
                    if (!System.IO.File.Exists(src)) continue;
                    System.IO.Directory.CreateDirectory("Assets/_Project/Resources/Fonts");
                    System.IO.File.Copy(src, SymAssetPath, overwrite: true);
                    AssetDatabase.ImportAsset(SymAssetPath, ImportAssetOptions.ForceUpdate);
                    Debug.Log($"[KingsSceneBuilder] Copied '{filename}' → {SymAssetPath}");
                    break;
                }
            }
            srcFont = AssetDatabase.LoadAssetAtPath<Font>(SymAssetPath);

            if (srcFont == null)
            {
                const string TtfPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
                srcFont = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
                if (srcFont == null)
                {
                    Debug.LogWarning("[KingsSceneBuilder] No source font found — HUD icons will use default.");
                    return null;
                }
                Debug.LogWarning("[KingsSceneBuilder] HUD icon font: using LiberationSans fallback " +
                                 "(↩ ↺ ☰ ✦ may render as □ — Segoe UI Symbol not found on this machine).");
            }

            var fa = TMP_FontAsset.CreateFontAsset(srcFont);
            if (fa == null)
            {
                Debug.LogWarning("[KingsSceneBuilder] TMP_FontAsset.CreateFontAsset returned null.");
                return null;
            }

            fa.name = "HUDIcons SDF";

            // Pre-bake HUD icon glyphs while still Dynamic (TryAddCharacters only works in Dynamic mode),
            // then switch to Static so the atlas is preserved in Android/iOS builds.
            fa.TryAddCharacters(new uint[]
            {
                0x21A9, // ↩  LEFTWARDS ARROW WITH HOOK      — Undo
                0x21BA, // ↺  ANTICLOCKWISE OPEN CIRCLE      — Restart
                0x2630, // ☰  TRIGRAM FOR HEAVEN             — Menu
                0x2726, // ✦  BLACK FOUR POINTED STAR        — Tips
                0x2605, // ★  BLACK STAR                     — Victory stars
                0x2606, // ☆  WHITE STAR                     — Victory stars empty
                0x2190, // ←  LEFTWARDS ARROW (fallback)
                0x25CB, // ○  WHITE CIRCLE       (fallback)
                0x2261, // ≡  IDENTICAL TO       (fallback)
                0x25C6, // ◆  BLACK DIAMOND SUIT (fallback)
            });
            fa.atlasPopulationMode = AtlasPopulationMode.Static;

            System.IO.Directory.CreateDirectory("Assets/_Project/Resources/Fonts");
            _hudIconFont = SaveFontAssetWithSubAssets(fa, AssetPath);
            Debug.Log($"[KingsSceneBuilder] Created {AssetPath} (Static, {fa.characterTable.Count} icon chars baked)");
            return _hudIconFont;
        }

        // ── Font baking helper ────────────────────────────────────────────────────
        // Pre-bakes printable ASCII + common game chars into a Static TMP atlas.
        // Must be called immediately after TMP_FontAsset.CreateFontAsset() while
        // FontEngine still has the face loaded. Static mode + pre-baked atlas is
        // required for Android — Dynamic mode silently fails on many devices because
        // GPU-side SDF atlas regeneration is unreliable in Android builds.
        static void BakeFullCharset(TMP_FontAsset fa)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 32; i <= 126; i++) sb.Append((char)i);  // full printable ASCII
            sb.Append((char)0x2605); // ★ filled star
            sb.Append((char)0x2606); // ☆ empty star
            sb.Append((char)0x2022); // • bullet
            sb.Append((char)0x2013); // – en-dash
            sb.Append((char)0x2014); // — em-dash
            sb.Append((char)0x2026); // … ellipsis
            sb.Append((char)0x00B0); // ° degree
            string missingChars;
            bool ok = fa.TryAddCharacters(sb.ToString(), out missingChars, false);
            if (!ok && !string.IsNullOrEmpty(missingChars))
                Debug.LogWarning($"[KingsSceneBuilder] BakeFullCharset: {missingChars.Length} chars not in font: {missingChars}");
            else
                Debug.Log($"[KingsSceneBuilder] BakeFullCharset: {fa.characterTable.Count} chars baked into atlas.");
        }

        // ── Body font (Outfit) ────────────────────────────────────────────────────
        // Applied to every TMP in the scene for a consistent, modern look.
        // TTF downloaded to Assets/_Project/Resources/Fonts/Outfit-Regular.ttf.

        static TMP_FontAsset _bodyFont;

        static TMP_FontAsset GetOrCreateBodyFont()
        {
            const string AssetPath  = "Assets/_Project/Resources/Fonts/Outfit SDF.asset";
            const string TtfPath    = "Assets/_Project/Resources/Fonts/Outfit-Regular.ttf";

            if (_bodyFont != null) return _bodyFont;

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
            bool isValid = false;
            try { isValid = existing != null && existing.atlasTextures != null
                         && existing.atlasTextures.Length > 0 && existing.atlasTextures[0] != null
                         && existing.atlasTextures[0].width > 1  // atlas must have real pixel data
                         && existing.atlasPopulationMode == AtlasPopulationMode.Static
                         && existing.characterTable.Count > 0; }
            catch { }

            if (isValid) { _bodyFont = existing; return _bodyFont; }

            // Existing asset is missing or has broken atlas — delete and recreate.
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(AssetPath)))
                AssetDatabase.DeleteAsset(AssetPath);

            // Ensure font data is embedded so TMP can read the glyph data.
            AssetDatabase.ImportAsset(TtfPath, ImportAssetOptions.ForceUpdate);
            var fontImporter = AssetImporter.GetAtPath(TtfPath) as TrueTypeFontImporter;
            if (fontImporter != null && !fontImporter.includeFontData)
            {
                fontImporter.includeFontData = true;
                fontImporter.SaveAndReimport();
            }

            var srcFont = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (srcFont == null)
            {
                Debug.LogWarning($"[KingsSceneBuilder] {TtfPath} not found — body font skipped.");
                return null;
            }

            // Create with Static mode — Dynamic fails on Android because
            // runtime atlas regeneration requires GPU font rendering not available on all devices.
            // We pre-bake the full ASCII set + common game chars here so the atlas is
            // complete before the build. ClearDynamicDataOnBuild=0 keeps this atlas in APK.
            var fa = TMP_FontAsset.CreateFontAsset(srcFont);
            if (fa == null)
            {
                Debug.LogWarning("[KingsSceneBuilder] TMP_FontAsset.CreateFontAsset returned null for Outfit.");
                return null;
            }

            // Bake full charset BEFORE switching to Static — TryAddCharacters only
            // works on Dynamic fonts. After baking, switch to Static to lock the atlas
            // so it survives Android builds without runtime regeneration.
            BakeFullCharset(fa);
            fa.atlasPopulationMode = AtlasPopulationMode.Static;

            fa.name = "Outfit SDF";
            System.IO.Directory.CreateDirectory("Assets/_Project/Resources/Fonts");
            _bodyFont = SaveFontAssetWithSubAssets(fa, AssetPath);
            Debug.Log($"[KingsSceneBuilder] Created {AssetPath} (Static, {fa.characterTable.Count} chars baked)");
            return _bodyFont;
        }

        // Saves a TMP_FontAsset plus its atlas textures and material as sub-assets,
        // then wires m_AtlasTextures via SerializedObject so the reference is stable
        // on disk and survives domain reload.
        static TMP_FontAsset SaveFontAssetWithSubAssets(TMP_FontAsset fa, string assetPath)
        {
            AssetDatabase.CreateAsset(fa, assetPath);

            // Add atlas textures and material as named sub-assets.
            if (fa.atlasTextures != null)
                foreach (var t in fa.atlasTextures)
                    if (t != null) { t.name = fa.name + " Atlas"; AssetDatabase.AddObjectToAsset(t, fa); }
            if (fa.material != null)
                { fa.material.name = fa.name + " Material"; AssetDatabase.AddObjectToAsset(fa.material, fa); }

            AssetDatabase.SaveAssets();
            // ForceUpdate re-resolves sub-asset FileIDs on disk.
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            // Reload from disk and wire m_AtlasTextures to the saved sub-asset texture
            // via SerializedObject.  Without this step, the on-disk reference still points
            // to the old in-memory instanceID (which is destroyed after domain reload).
            var loaded = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            Texture2D savedAtlas = null;
            Material  savedMat   = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (obj is Texture2D tex && savedAtlas == null) savedAtlas = tex;
                if (obj is Material   mat && savedMat   == null) savedMat   = mat;
            }

            if (loaded != null && savedAtlas != null)
            {
                var so        = new SerializedObject(loaded);
                var atlasProp = so.FindProperty("m_AtlasTextures");
                if (atlasProp != null)
                {
                    atlasProp.arraySize = 1;
                    atlasProp.GetArrayElementAtIndex(0).objectReferenceValue = savedAtlas;
                }
                var matProp = so.FindProperty("m_Material");
                if (matProp != null && savedMat != null) matProp.objectReferenceValue = savedMat;
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }
            else Debug.LogWarning($"[KingsSceneBuilder] Could not wire atlas sub-asset for {assetPath}.");

            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        }

        // Shared palette — sourced from DesignSystem; BtnBg has no matching token.
        // ── Rounded-rect UI sprite ────────────────────────────────────────────────
        // White 128×128 texture with 28 px corner radius, saved once as a PNG.
        // Applied to every button/panel Image with Type.Sliced so corners stay
        // sharp at any RectTransform size.  pixelsPerUnit = 1 so the border value
        // (28) maps directly to 28 canvas pixels — nice ~14 % radius on a 200 px button.

        static Sprite _roundedRectSprite;

        static Sprite GetOrCreateRoundedRectSprite()
        {
            const string AssetPath = "Assets/_Project/Resources/Sprites/UIRoundedRect.png";
            const int    TexSize   = 128;
            const int    Radius    = 10;

            if (_roundedRectSprite != null) return _roundedRectSprite;

            _roundedRectSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetPath);
            if (_roundedRectSprite != null) return _roundedRectSprite;

            // Generate white rounded-rect texture.
            var tex    = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            var pixels = new Color32[TexSize * TexSize];
            var white  = new Color32(255, 255, 255, 255);
            var clear  = new Color32(0, 0, 0, 0);

            for (int y = 0; y < TexSize; y++)
                for (int x = 0; x < TexSize; x++)
                    pixels[y * TexSize + x] = RRectInside(x, y, TexSize, Radius) ? white : clear;

            tex.SetPixels32(pixels);
            tex.Apply();

            string fullPath = System.IO.Path.Combine(
                Application.dataPath, "_Project/Resources/Sprites/UIRoundedRect.png");
            System.IO.File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);

            // Configure 9-slice import: pixelsPerUnit=1 → border of 28 = 28 canvas px.
            var imp = AssetImporter.GetAtPath(AssetPath) as TextureImporter;
            if (imp != null)
            {
                imp.textureType          = TextureImporterType.Sprite;
                imp.spriteImportMode     = SpriteImportMode.Single;
                imp.alphaIsTransparency  = true;
                imp.filterMode           = FilterMode.Bilinear;
                imp.spriteBorder         = new Vector4(Radius, Radius, Radius, Radius);
                imp.spritePixelsPerUnit  = 1f;
                imp.SaveAndReimport();
            }

            _roundedRectSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetPath);
            Debug.Log($"[KingsSceneBuilder] Created {AssetPath}");
            return _roundedRectSprite;
        }

        static bool RRectInside(int x, int y, int size, int r)
        {
            int s = size - 1;
            if (x <= r   && y <= r)   return RRectDist(x, y, r,   r)   <= r;
            if (x >= s-r && y <= r)   return RRectDist(x, y, s-r, r)   <= r;
            if (x <= r   && y >= s-r) return RRectDist(x, y, r,   s-r) <= r;
            if (x >= s-r && y >= s-r) return RRectDist(x, y, s-r, s-r) <= r;
            return true;
        }

        static float RRectDist(int x, int y, int cx, int cy)
        {
            float dx = x - cx, dy = y - cy;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        // Shared palette — sourced from DesignSystem; BtnBg has no matching token.
        static readonly Color BtnBg      = new Color(0.18f, 0.22f, 0.45f, 1f); // dark indigo (no DesignSystem token)
        static readonly Color NavyBg     = DesignSystem.Background;
        static readonly Color PinkAccent = DesignSystem.Primary;

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

        // Loads dot and crown sprites from the project's Resources folder and assigns
        // them to KingsGridRenderer._dotSprite / _crownSprite via SerializedObject.
        static void AssignGridRendererSprites(KingsGridRenderer renderer)
        {
            const string DotPath   = "Assets/_Project/Resources/Sprites/dot.png";
            const string CrownPath = "Assets/_Project/Resources/Sprites/crown.png";

            // dot.png is a single sprite — LoadAssetAtPath<Sprite> works fine.
            var dotSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DotPath);
            if (dotSprite == null)
                Debug.LogWarning($"[KingsSceneBuilder] dot sprite not found at {DotPath}");

            // crown.png is a multi-sprite sheet (crown_0 = small circle, crown_1 = crown icon, crown_2 = bar).
            // LoadAssetAtPath<Sprite> only returns crown_0. Use LoadAllAssetsAtPath to find crown_1.
            Sprite crownSprite = null;
            Sprite crownFallback = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(CrownPath))
            {
                var s = asset as Sprite;
                if (s == null) continue;
                if (s.name == "crown_1") { crownSprite = s; break; }
                crownFallback = s; // remember last sprite as fallback
            }
            if (crownSprite == null) crownSprite = crownFallback;
            if (crownSprite == null)
                Debug.LogWarning($"[KingsSceneBuilder] crown sprite not found at {CrownPath}");

            var so = new SerializedObject(renderer);
            so.FindProperty("_dotSprite").objectReferenceValue   = dotSprite;
            so.FindProperty("_crownSprite").objectReferenceValue = crownSprite;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[KingsSceneBuilder] Grid sprites assigned — dot={(dotSprite != null ? dotSprite.name : "MISSING")}, crown={(crownSprite != null ? crownSprite.name : "MISSING")}");
        }

        static void AssignLevelSelectButtonSprites()
        {
            const string PrefabPath = "Assets/_Project/Prefabs/LevelSelectButton.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[KingsSceneBuilder] LevelSelectButton prefab not found at {PrefabPath} — run Build Level Select Scene first.");
                return;
            }

            if (prefab.GetComponent<BrainBattle.Shared.UI.LevelSelectButton>() == null)
            {
                Debug.LogWarning("[KingsSceneBuilder] LevelSelectButton component missing from prefab.");
                return;
            }

            Debug.Log("[KingsSceneBuilder] LevelSelectButton prefab uses scene-builder-driven visual layout.");
        }

        // Finds every LevelData asset in the project and assigns them (sorted by LevelNumber)
        // to LevelLoader._allLevels via SerializedObject so it survives domain reload.
        static void PopulateLevelLoader(LevelLoader loader)
        {
            var guids  = AssetDatabase.FindAssets("t:LevelData",
                             new[] { "Assets/_Project/ScriptableObjects/Kings/Levels" });
            var levels = new List<LevelData>(guids.Length);
            foreach (var guid in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<LevelData>(
                                AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) levels.Add(asset);
            }
            levels.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            var so   = new SerializedObject(loader);
            var prop = so.FindProperty("_allLevels");
            prop.arraySize = levels.Count;
            for (int i = 0; i < levels.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[KingsSceneBuilder] LevelLoader._allLevels populated with {levels.Count} level(s).");
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

            // Canvas → TimerBar (top)
            public GameObject      TimerBarGO;
            public TextMeshProUGUI HudTimerText;

            // Canvas → HUD (bottom)
            public GameObject      HudGO;
            public GameObject      UndoButtonGO;
            public GameObject      HudRestartGO;
            public GameObject      MenuButtonGO;
            public GameObject      TipsButtonGO;
            public TextMeshProUGUI HudMoveText;   // MoveCounter → HUDController._moveCountText
        }
    }
}
