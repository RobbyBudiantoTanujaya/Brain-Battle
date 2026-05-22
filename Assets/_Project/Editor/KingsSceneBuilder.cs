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

            CreateBackground(r);
            CreateGridContainer(r);
            CreateTimerBar(r);
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
            img.color          = Color.white;
            img.type           = Image.Type.Simple;
            img.preserveAspect = false;

            // main_menu_bg.png is a multi-sprite asset; use LoadAllAssetsAtPath to get first Sprite sub-asset.
            const string BgPath = "Assets/_Project/Resources/Sprites/main_menu_bg.png";
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(BgPath))
            {
                if (asset is Sprite s) { img.sprite = s; break; }
            }
            if (img.sprite == null)
                Debug.LogWarning("[KingsSceneBuilder] main_menu_bg sprite not found — Background Image will be solid white.");
        }

        static void CreateGridContainer(Refs r)
        {
            r.GridContainer = MakeUIGO("GridContainer", r.Canvas.transform);
            r.GridContainer.AddComponent<KingsGridRenderer>();

            // Stretch to fill the canvas between the top TimerBar and the bottom HUD.
            var rt       = r.GridContainer.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(0f,  80f); // bottom inset = HUD height
            rt.offsetMax = new Vector2(0f, -60f); // top    inset = TimerBar height
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
            // Start hidden — VictoryPanel.Awake() also hides it, but setting it
            // inactive here means the scene file itself is correct even if Awake NPEs.
            r.VictoryContent.SetActive(false);

            var content = r.VictoryContent.transform;

            r.VPTimeText           = MakeTMP("TimeText", content, "00:00");
            r.VPTimeText.alignment = TextAlignmentOptions.Center;
            r.VPTimeText.fontSize  = 72f;
            Anchor(r.VPTimeText, 0.10f, 0.76f, 0.90f, 0.94f);

            r.VPMoveCountText           = MakeTMP("MoveCountText", content, "0");
            r.VPMoveCountText.alignment = TextAlignmentOptions.Center;
            Anchor(r.VPMoveCountText, 0.10f, 0.60f, 0.90f, 0.76f);

            r.VPStarRatingText           = MakeTMP("StarRatingText", content, "***");
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

        static void CreateTimerBar(Refs r)
        {
            r.TimerBarGO = MakeUIGO("TimerBar", r.Canvas.transform);
            var rt              = r.TimerBarGO.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(0f, 60f);
            r.TimerBarGO.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.16f, 0.95f);

            r.HudTimerText           = MakeTMP("TimerText", r.TimerBarGO.transform, "00:00");
            r.HudTimerText.alignment = TextAlignmentOptions.Center;
            r.HudTimerText.fontSize  = 52f;
            Stretch(r.HudTimerText.gameObject);
        }

        static void CreateHUD(Refs r)
        {
            r.HudGO = MakeUIGO("HUD", r.Canvas.transform);
            var rt              = r.HudGO.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(1f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(0f, 80f);
            // Dark background so HUD text and buttons are readable over any grid colour.
            r.HudGO.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.16f, 0.95f);

            var hud = r.HudGO.transform;

            // ── Bottom HUD: Undo | Restart | MoveCount | Tips ──────────────────────
            // Timer is in the TimerBar at the top — not in this strip.

            r.UndoButtonGO = MakeFixedHudButton("UndoButton",    hud, "Undo",    anchorLeft: true,  offsetX: 10f);
            r.UndoButtonGO.AddComponent<UndoButton>();

            r.HudRestartGO = MakeFixedHudButton("RestartButton", hud, "Restart", anchorLeft: true,  offsetX: 180f);
            r.HudRestartGO.AddComponent<RestartButton>();

            // Move count sits in the centre of the HUD strip.
            r.HudMoveText           = MakeTMP("MoveCountText", hud, "0 moves");
            r.HudMoveText.alignment = TextAlignmentOptions.Center;
            Anchor(r.HudMoveText, 0.38f, 0.08f, 0.62f, 0.92f);

            r.TipsButtonGO = MakeFixedHudButton("TipsButton",    hud, "Tips",    anchorLeft: false, offsetX: 10f);
            r.TipsButtonGO.AddComponent<TipsButton>();
        }

        // Creates a 160×80 Button pinned to the left or right edge of the HUD strip.
        // offsetX is the gap between the edge and the button's near side.
        static GameObject MakeFixedHudButton(string name, Transform parent, string label, bool anchorLeft, float offsetX)
        {
            var btn = MakeButton(name, parent, label);
            var rt  = btn.GetComponent<RectTransform>();

            float ax            = anchorLeft ? 0f : 1f;
            rt.anchorMin        = new Vector2(ax, 0.5f);
            rt.anchorMax        = new Vector2(ax, 0.5f);
            rt.pivot            = new Vector2(anchorLeft ? 0f : 1f, 0.5f);
            rt.sizeDelta        = new Vector2(160f, 80f);
            rt.anchoredPosition = new Vector2(anchorLeft ? offsetX : -offsetX, 0f);
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
            var img = go.AddComponent<Image>();
            img.color = BtnBg;
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

        // Shared palette — change once here to restyle all buttons.
        static readonly Color BtnBg = new Color(0.18f, 0.22f, 0.45f, 1f); // dark indigo

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

        // Pre-assigns the 4 level-button sprites to the LevelSelectButton prefab so they
        // appear in the Inspector and avoid a Resources.Load call at runtime.
        static void AssignLevelSelectButtonSprites()
        {
            const string PrefabPath = "Assets/_Project/Prefabs/LevelSelectButton.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[KingsSceneBuilder] LevelSelectButton prefab not found at {PrefabPath} — run Build Level Select Scene first.");
                return;
            }

            var lsb = prefab.GetComponent<BrainBattle.Shared.UI.LevelSelectButton>();
            if (lsb == null)
            {
                Debug.LogWarning("[KingsSceneBuilder] LevelSelectButton component missing from prefab.");
                return;
            }

            var so = new SerializedObject(lsb);
            so.FindProperty("_spriteAvailable").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Resources/Sprites/level_available.png");
            so.FindProperty("_spriteCompleted").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Resources/Sprites/level_completed.png");
            so.FindProperty("_spriteActive").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Resources/Sprites/level_active.png");
            so.FindProperty("_spriteLocked").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Resources/Sprites/level_lock.png");
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssetIfDirty(prefab);
            Debug.Log("[KingsSceneBuilder] LevelSelectButton prefab sprites assigned.");
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
            public GameObject      TipsButtonGO;
            public TextMeshProUGUI HudMoveText;
        }
    }
}
