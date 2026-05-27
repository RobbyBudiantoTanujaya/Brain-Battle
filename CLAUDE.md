# Brain Battle — Unity 6 LTS (CLAUDE.md)

---

## 1. Project Overview

Brain Battle is a 2D mobile puzzle game (Android + iOS) built in Unity 6.0.75f1 LTS with 2D URP.
Game #1 is **Kings** — an NxN crown-placement puzzle (1 crown per row/col/region, no adjacency).
The project has 2 active scenes: **SampleScene** (gameplay) and **LevelSelect**.
Milestones 1 and 2 are complete; Milestone 3 (polish, audio, monetization) is next.

---

## 2. Scene Structures

### SampleScene — `Assets/Scenes/SampleScene.unity`
Built and owned by **KingsSceneBuilder** (`BrainBattle → Build Kings Scene`).
Canvas reference resolution: **1170 × 2532** (iPhone 13 Pro Max), matchWidthOrHeight 0.5.

```
[Canvas]                        Canvas, CanvasScaler, GraphicRaycaster
  Background                    Image (solid DesignSystem.Background), sibling 0
  GridContainer                 KingsGridRenderer, RectTransform
                                  anchor (0,0)–(1,1), offsetMin.y = 120 (above HUD)
  TimerBar                      Image (NavyBg), anchor top, height = TimerBarHeight (48px)
    TimerText                   TextMeshProUGUI
  TutorialOverlay               Image (dark overlay), active=true (TC hides it in Awake)
    StepCounter                 TextMeshProUGUI
    StepText                    TextMeshProUGUI
    SkipButton                  Button → Label (TMP)
    NextButton                  Button → Label (TMP)
  TipsPanel                     Image, active=true (TipsButton hides it in Awake)
    TipsText                    TextMeshProUGUI
    CloseButton                 Button
  RestartConfirmPanel           Image, active=true (RestartButton hides it in Awake)
    PromptText                  TextMeshProUGUI
    ConfirmButton               Button (onClick → RestartButton.ConfirmRestart)
    CancelButton                Button (onClick → RestartButton.CancelRestart)
  HUD                           Image, HUDController
    TopBorder                   Image (1px separator)
    HUDRow                      HorizontalLayoutGroup
      UndoButton                Image, Button, UndoButton, LayoutElement
        Label                   TextMeshProUGUI
      RestartButton             Image, Button, RestartButton, LayoutElement
        Label                   TextMeshProUGUI
      MoveCounter               Image, LayoutElement
        MoveCountText           TextMeshProUGUI (pink)
      MenuButton                Image, Button, MenuButton, LayoutElement
        Label                   TextMeshProUGUI
      TipsButton                Image, Button, TipsButton, LayoutElement
        Label                   TextMeshProUGUI
  VictoryPanel                  VictoryPanel (active=true; subscribes to OnGameComplete)
    VictoryContent              Image (victory_screen_bg.png), active=FALSE at scene save
      Title                     TextMeshProUGUI "VICTORY!"
      StarRatingText            TextMeshProUGUI (HUDIcons SDF font)
      TimeLabel / TimeText      TextMeshProUGUI
      MovesLabel / MoveCountText TextMeshProUGUI
      MainMenuButton            Button (pink)
      NextLevelButton           Button (pink)
      RestartButton             Button (pink)
[GameManager]                   KingsGameManager, LevelLoader, TutorialController
[SceneBootstrap]                KingsSceneBootstrap
[EventSystem]                   EventSystem, InputSystemUIInputModule (or StandaloneInputModule)
```

---

### LevelSelect — `Assets/_Project/Scenes/LevelSelect.unity`
Built and owned by **LevelSelectSceneBuilder** (`BrainBattle → Build Level Select Scene`).
Canvas reference resolution: **1170 × 2532** (iPhone 13 Pro Max), matchWidthOrHeight 0.5.
Build order: LevelSelect = index 0, SampleScene = index 1.

```
[Canvas]                        Canvas, CanvasScaler, GraphicRaycaster
  Background                    Image (solid DesignSystem.Background)
  Header                        Image → Title (TMP "LEVEL SELECT")
  TabRow
    BeginnerTab                 Image, Button → Label (TMP)
    ExpertTab                   Image, Button → Label (TMP)
    ImpossibleTab               Image, Button → Label (TMP)
  ProgressRow
    ProgressGroup0/1/2
      Track → Fill             Image (fillMethod=Horizontal)
      PctText                  TextMeshProUGUI
  LevelScrollView               Image(transparent), ScrollRect
    Viewport                   Image(white), Mask
      Content                  GridLayoutGroup (3 cols, 359×359, gap 14), ContentSizeFitter
  PlayButton                    Image (pink), Button → Label (TMP "PLAY")
[LevelSelectController]         LevelSelectController
[Main Camera]                   Camera (orthographic, dark bg), AudioListener
[EventSystem]                   EventSystem, input module
```

---

## 3. SerializeField Wiring Map

### KingsGameManager (on `GameManager`)
| Field                | Type                  | Source GO/Component       | Null consequence                   |
|----------------------|-----------------------|---------------------------|------------------------------------|
| `_gridRenderer`      | KingsGridRenderer     | GridContainer             | StartGame() returns early, no grid |
| `_tutorialController`| TutorialController    | GameManager               | Tutorial silently skipped (warning)|

### KingsSceneBootstrap (on `SceneBootstrap`)
| Field           | Type              | Source GO           | Null consequence            |
|-----------------|-------------------|---------------------|-----------------------------|
| `_levelLoader`  | LevelLoader       | GameManager         | LoadLevel() LogError + abort|
| `_gameManager`  | KingsGameManager  | GameManager         | LoadLevel() LogError + abort|

### KingsGridRenderer (on `GridContainer`)
| Field          | Type   | Source                           | Null consequence                   |
|----------------|--------|----------------------------------|------------------------------------|
| `_dotSprite`   | Sprite | Resources/Sprites/dot.png        | Loaded at runtime in Awake fallback|
| `_crownSprite` | Sprite | crown.png sub-asset `crown_1`    | Loaded at runtime in Awake fallback|

### HUDController (on `HUD`)
| Field            | Type             | Source GO / Child    | Null consequence       |
|------------------|------------------|----------------------|------------------------|
| `_gameManager`   | KingsGameManager | GameManager          | Update() skips silently|
| `_timerText`     | TextMeshProUGUI  | TimerBar/TimerText   | Timer not displayed    |
| `_moveCountText` | TextMeshProUGUI  | MoveCounter/MoveCountText | Moves not displayed|

### UndoButton (on `HUD/.../UndoButton`)
| Field          | Type             | Source GO            | Null consequence          |
|----------------|------------------|----------------------|---------------------------|
| `_gameManager` | KingsGameManager | GameManager          | OnClick NPE               |
| `_button`      | Button           | same GO              | onClick never wired       |

### RestartButton (on `HUD/.../RestartButton`)
| Field           | Type             | Source GO             | Null consequence         |
|-----------------|------------------|-----------------------|--------------------------|
| `_gameManager`  | KingsGameManager | GameManager           | LogError + NPE on click  |
| `_button`       | Button           | same GO               | LogError                 |
| `_confirmPanel` | GameObject       | RestartConfirmPanel   | LogError + NPE           |

### TipsButton (on `HUD/.../TipsButton`)
| Field          | Type             | Source GO         | Null consequence              |
|----------------|------------------|-------------------|-------------------------------|
| `_gameManager` | KingsGameManager | GameManager       | LogError + NPE on click       |
| `_button`      | Button           | same GO           | LogError                      |
| `_tipsPanel`   | GameObject       | TipsPanel         | LogError + NPE                |
| `_tipsText`    | TextMeshProUGUI  | TipsPanel/TipsText| LogError                      |
| `_closeButton` | Button           | TipsPanel/CloseButton | LogError                  |

### TutorialController (on `GameManager`)
| Field           | Type             | Source GO          | Null consequence        |
|-----------------|------------------|--------------------|-------------------------|
| `_overlayPanel` | GameObject       | TutorialOverlay    | Tutorial overlay broken |
| `_stepText`     | TextMeshProUGUI  | TutorialOverlay/StepText | Steps not shown   |
| `_stepCounter`  | TextMeshProUGUI  | TutorialOverlay/StepCounter | Counter broken |
| `_nextButton`   | Button           | TutorialOverlay/NextButton | No navigation   |
| `_skipButton`   | Button           | TutorialOverlay/SkipButton | No skip         |

### VictoryPanel (on `VictoryPanel`)
| Field              | Type             | Source GO                | Null consequence              |
|--------------------|------------------|--------------------------|-------------------------------|
| `_panel`           | GameObject       | VictoryContent           | LogError + panel never shows  |
| `_hud`             | GameObject       | HUD                      | HUD stays visible during vic  |
| `_timeText`        | TextMeshProUGUI  | VictoryContent/TimeText  | Time not shown                |
| `_moveCountText`   | TextMeshProUGUI  | VictoryContent/MoveCountText | Moves not shown           |
| `_starRatingText`  | TextMeshProUGUI  | VictoryContent/StarRatingText | Stars not shown          |
| `_nextLevelButton` | Button           | VictoryContent/NextLevelButton | No next-level nav        |
| `_restartButton`   | Button           | VictoryContent/RestartButton | No restart                |
| `_mainMenuButton`  | Button           | VictoryContent/MainMenuButton | No menu nav              |
| `_gameManager`     | KingsGameManager | GameManager              | LogError + no OnGameComplete  |
| `_sceneBootstrap`  | KingsSceneBootstrap | SceneBootstrap        | Stars not saved, next level broken |

### LevelLoader (on `GameManager`)
| Field       | Type         | Source                                           | Null consequence                    |
|-------------|--------------|--------------------------------------------------|-------------------------------------|
| `_allLevels`| LevelData[]  | ScriptableObjects/Kings/Levels/ (all assets)     | Editor fallback; build crashes      |

### LevelSelectController (on `LevelSelectController`)
| Field               | Type             | Source                        | Null consequence          |
|---------------------|------------------|-------------------------------|---------------------------|
| `_allLevels`        | LevelData[]      | all LevelData assets          | No levels shown           |
| `_tabButtons`       | Button[3]        | BeginnerTab/ExpertTab/ImpossibleTab | Fallback by name search |
| `_progressFills`    | Image[3]         | ProgressGroup0/1/2 Fill       | Progress bars broken      |
| `_progressTexts`    | TextMeshProUGUI[3] | ProgressGroup0/1/2 PctText  | % text broken             |
| `_gridContent`      | Transform        | LevelScrollView/Viewport/Content | No buttons spawned     |
| `_levelButtonPrefab`| GameObject       | LevelSelectButton prefab      | No buttons spawned        |
| `_playButton`       | Button           | PlayButton                    | Play button non-functional|

### LevelSelectButton (prefab at `Assets/_Project/Prefabs/LevelSelectButton.prefab`)
| Field              | Type            | Source child     | Null consequence             |
|--------------------|-----------------|------------------|------------------------------|
| `_background`      | Image           | root Image       | Raycast target missing       |
| `_spriteImage`     | Image           | SpriteImage child | State sprite not shown      |
| `_levelLabel`      | TextMeshProUGUI | LevelLabel child | Number not displayed         |
| `_checkmark`       | GameObject      | Checkmark child  | Completed state broken       |
| `_spriteAvailable` | Sprite          | Resources/Sprites/level_available.png | Color fallback  |
| `_spriteCompleted` | Sprite          | Resources/Sprites/level_completed.png | Color fallback  |
| `_spriteLocked`    | Sprite          | Resources/Sprites/level_lock.png | Color fallback       |

---

## 4. Design System Rules

**File**: `Assets/_Project/Scripts/Shared/UI/BrainBattleDesignSystem.cs`
**Namespace**: `BrainBattle.Shared.UI`, accessed as `DesignSystem.TokenName`

**RULE**: ALL colors, sizes, and spacings MUST come from DesignSystem at runtime.
Never hardcode hex values or pixel values that have a DesignSystem token.
Read tokens in code (e.g., `BuildCells()`, `Start()`), not in `[SerializeField]` defaults.

### Available Tokens

| Category   | Token                    | Value                       |
|------------|--------------------------|-----------------------------|
| **Colors** | `Primary`                | #ff2d78 (hot pink)          |
|            | `PrimaryDark`            | #c4006f                     |
|            | `Background`             | #1a1a1e (dark navy)         |
|            | `Surface`                | #2a2a3e                     |
|            | `TextPrimary`            | white                       |
|            | `TextSecondary`          | #888888                     |
|            | `Overlay`                | rgba(0,0,0,0.7)             |
|            | `BorderRegion`           | rgba(0,0,0,0.8)             |
|            | `BorderCell`             | rgba(0,0,0,0.2)             |
| **HUD**    | `HUDBarGradientTop/Bottom`| #13132a / #0d0d1a          |
|            | `HUDBorderSeparator`     | rgba(255,255,255,0.08)      |
|            | `HUDButtonBg/Border`     | rgba(255,255,255,0.05/0.08) |
|            | `HUDMoveCounterBg/Border`| rgba(255,45,120,0.12/0.30)  |
|            | `HUDTipsBg/Border`       | rgba(255,45,120,0.15/0.35)  |
|            | `HUDLabelText`           | #8888aa                     |
|            | `HUDMoveCounterLabel`    | rgba(255,45,120,0.55)       |
| **Type**   | `FontSizeSmall/Body/Medium/Large/Title/Hero` | 12/16/18/20/28/36 |
| **Spacing**| `SpacingXS/S/M/L/XL/XXL`| 4/8/12/16/24/32 px          |
| **Radius** | `RadiusS/M/L`            | 8/12/16 px                  |
| **Sizes**  | `HUDHeight`              | 80 px                       |
|            | `TimerBarHeight`         | 48 px                       |
|            | `ButtonHeight`           | 64 px                       |
|            | `LevelButtonSize/Gap`    | 100/12 px                   |
| **Grid**   | `GridPadding`            | 16 px                       |
|            | `BorderRegionThickness`  | 5 px (thick, between regions)|
|            | `BorderCellThickness`    | 3 px (thin, within region)  |
|            | `CrownSizeRatio`         | 0.65 (× cellSize)           |
|            | `DotSizeRatio`           | 0.25 (× cellSize)           |

**When to add a new token**: only if the value appears in ≥2 places or is a brand decision (color, typography). For one-off values in a single script, an inline constant is fine.

---

## 5. Critical Rules (MUST NOT BREAK)

1. **Never fix SerializeField wiring by hand** — always run `BrainBattle → Build Kings Scene` or `Build Level Select Scene`. If a field is missing from the builder, add it to the builder first.
2. **Never run the wrong builder** — KingsSceneBuilder owns SampleScene; LevelSelectSceneBuilder owns LevelSelect. They are not interchangeable.
3. **Never edit both scenes in one task** — confirm scope before starting if a task might touch both. Ask the user first.
3a. **Never use `manage_scene(action="load")` or `manage_scene(action="save")` via MCP** without explicit user instruction — loading a scene switches the editor's active scene without warning; saving writes to disk permanently. MCP scene/GO changes that are not explicitly requested must remain in-memory only (no save). If a scene inspection is needed, ask the user to open it first.
4. **Never add a SerializeField to a wired script** without re-running the scene builder — the new field will be null at runtime.
5. **Never hardcode a hex color or pixel size** that has a DesignSystem token. Use `DesignSystem.X` at call time.
6. **Never change logic when the task is visual** (and vice versa). Visual = rendering, colors, sizes, layout. Logic = game rules, constraints, state transitions.
7. **Never remove a component without checking all scripts** that hold a SerializeField reference to it.
8. **Never manually compute level data** (grids, region assignments, queen positions). Write algorithmic code, let Unity run it.
10. **Never hardcode level cell data** in a ScriptableObject by hand. Always generate via `BrainBattle → Generate Kings Levels`.
11. **Always read `KingsLevelGeneration.md`** before any level generation task.
12. **VictoryContent must be inactive at scene save** — KingsSceneBuilder sets `VictoryContent.SetActive(false)`. Never re-activate it in the editor.
13. **Vector2Int convention** throughout the project: `x = col`, `y = row`. `GetCell(row, col)` takes (row, col). Never mix these up.
14. **After completing any task**, update `Assets/_Project/Documentation/Milestones.md`.
15. **Never write `AtlasPopulationMode.Dynamic` anywhere in code** — Dynamic TMP fonts fail silently on Android. Always use `Static` + pre-baked atlas. All font creation goes through `KingsSceneBuilder.GetOrCreateBodyFont()` / `GetOrCreateHudIconFont()` which handle this correctly.
16. **Never call `TMP_FontAsset.CreateFontAsset()` without immediately calling `BakeFullCharset()` before switching to `Static`** — the order matters: bake while Dynamic → switch to Static. Reversing the order makes `TryAddCharacters` fail silently.
17. **Never "fix" missing Android text by only editing `TMP Settings.asset`** — `m_ClearDynamicDataOnBuild=0` is necessary but not sufficient. Font assets must also be `Static` with pre-baked atlas. Correct fix: run `BrainBattle → Build Kings Scene`.

---

## 6. Navigation Flow

- **App launch** → LevelSelect (index 0) → `LevelSelectController` builds difficulty pools, spawns `LevelSelectButton`s
- **Tap level** → `Kings_PendingLevel` set in PlayerPrefs → `LoadScene("SampleScene")`
- **SampleScene boot** → `KingsSceneBootstrap` waits `yield return null` → reads `Kings_PendingLevel` → `LoadLevel` → `StartGame` → `RenderGrid` → `ShowTutorial` (skipped if `Kings_TutorialSeen=1`)
- **Cold boot / no key** → redirect to LevelSelect
- **HUD Menu** → delete `Kings_PendingLevel` → `LoadScene("LevelSelect")`
- **Win** → `HandleWin()` fires `OnGameComplete` → `VictoryPanel` hides HUD, shows VictoryContent, saves `Kings_Level_N_Stars` (keeps best)
- **VictoryPanel Next** → set `Kings_PendingLevel = currentLevel+1` → `LoadScene("SampleScene")`
- **VictoryPanel Menu** → `LoadScene("LevelSelect")`
- **VictoryPanel Restart** → hide VictoryContent, show HUD, `RestartGame()`

---

## 7. Asset Conventions

### Sprites — `Assets/_Project/Resources/Sprites/`
| File                    | Notes                                                           |
|-------------------------|-----------------------------------------------------------------|
| `dot.png`               | Single sprite                                                   |
| `crown.png`             | Multi-sprite sheet. Use `crown_1` (347×224 actual crown). `crown_0` is a small circle. Use `LoadAllAssetsAtPath` to find by name. |
| `victory_screen_bg.png` | Single sprite (fullscreen victory background)                   |
| `UIRoundedRect.png`     | 9-sliced 128×128, radius=10px, pixelsPerUnit=1. Used on all Button/panel Images with `Image.Type.Sliced` |
| `level_available.png`   | LevelSelectButton — Available state                             |
| `level_completed.png`   | LevelSelectButton — Completed state                             |
| `level_active.png`      | LevelSelectButton — Active/highlighted state (reserved)         |
| `level_lock.png`        | LevelSelectButton — Locked state                                |

### Fonts — `Assets/_Project/Resources/Fonts/`
| File                 | Notes                                                      |
|----------------------|------------------------------------------------------------|
| `HUDIcons SDF.asset` | **Static** TMP font for HUD icons: ↩ ↺ ☰ ✦ ★ ☆. Source: Segoe UI Symbol (Windows) or LiberationSans fallback. 10 icon glyphs pre-baked. |
| `Outfit SDF.asset`   | **Static** TMP body font. Source: `Outfit-Regular.ttf`. 100 chars (full ASCII 32–126 + common symbols) pre-baked. |
| `Outfit-Regular.ttf` | Must have `includeFontData = true` (set by KingsSceneBuilder)|
| `SegoeSym.ttf`       | Copied from Windows Fonts once; not needed on Mac/Linux     |

**Font atlas rules (Android-critical):**
- Both SDF assets MUST be `AtlasPopulationMode.Static` — Dynamic mode fails silently on Android because GPU-side SDF atlas regeneration is unreliable in builds.
- `TMP Settings.asset → m_ClearDynamicDataOnBuild` MUST be `0` — value `1` strips all pre-baked atlas data from the APK before build.
- **Never manually edit font `.asset` files or switch them to Dynamic** — the next `Build Kings Scene` will detect the broken atlas and recreate them correctly.
- **Never "fix" missing text by only changing `TMP Settings`** — that is necessary but not sufficient. The font assets themselves must be Static with a pre-baked atlas.
- The correct fix for any missing-text bug on Android: run `BrainBattle → Build Kings Scene`. The builder's validity check (`Static` + `atlas.width > 1` + `characterTable.Count > 0`) will detect and rebuild broken assets automatically.
- `TryAddCharacters` only works while the font is **Dynamic**. The builder calls it before switching to Static — never swap those two steps.

### Level Assets — `Assets/_Project/ScriptableObjects/Kings/Levels/`
- Naming: `Kings_Beginner_01.asset`, `Kings_Expert_02.asset`, `Kings_Impossible_01.asset`
- Sorted alphabetically by name → determines display order
- Difficulty pool sizes: Beginner (4×4/5×5), Expert (6×6/8×8), Impossible (10×10)
- After regenerating levels, run `BrainBattle → Build Kings Scene` to re-sync `LevelLoader._allLevels`

### Prefabs — `Assets/_Project/Prefabs/`
| File                   | Notes                                     |
|------------------------|-------------------------------------------|
| `LevelSelectButton.prefab` | Built by LevelSelectSceneBuilder. All 4 state sprites pre-assigned. |

### Audio — `Assets/_Project/Resources/Audio/` — **NOT YET IMPLEMENTED** (Milestone 3)

---

## 8. Editor Tools

| Menu Path                                  | File                        | When to run                                                           | Side effects                                         |
|--------------------------------------------|-----------------------------|-----------------------------------------------------------------------|------------------------------------------------------|
| `BrainBattle → Build Kings Scene`          | `KingsSceneBuilder.cs`      | After adding SerializeField; after wiring bug; before QA build        | Deletes Canvas/GameManager/SceneBootstrap and rebuilds; saves scene |
| `BrainBattle → Build Level Select Scene`   | `LevelSelectSceneBuilder.cs`| After layout change; after adding new SerializeField to LevelSelectController | Creates new scene file, rebuilds prefab, updates Build Settings |
| `BrainBattle → Generate Kings Levels`      | `KingsLevelGenerator.cs`    | To add new levels (adds N per category on top of existing)            | Syncs `_allLevels` on LevelLoader + LevelSelectController in both scenes |
| `BrainBattle → Fix Duplicate Levels`       | `KingsLevelGenerator.cs`    | If duplicate puzzle content is suspected                              | Re-generates duplicate assets with new seeds         |
| `BrainBattle → Generate Placeholder Sprites` | `SpriteGenerator.cs`     | Dev/emergency only — production sprites already exist                 | Overwrites DotSprite.png/CrownSprite.png if run      |

---

## 9. Known Bug Patterns

Hanya bug yang bisa recur setelah builder dijalankan ulang atau perubahan development biasa.

| Bug                                | Root Cause                                              | Fix                                                                 |
|------------------------------------|---------------------------------------------------------|---------------------------------------------------------------------|
| VictoryContent visible on startup  | VictoryContent left Active in scene file                | KingsSceneBuilder sets `VictoryContent.SetActive(false)`. VictoryPanel.Start() also hides it as safety. |
| Grid not centered / rect is zero   | CanvasScaler hasn't run before RenderGrid               | KingsSceneBootstrap waits `yield return null` before LoadLevel(). GridContainer uses stretch anchors with offsetMin.y=120. |
| Grid rect zero → 320px fallback    | GridContainer layout not finalized                      | Check GridContainer anchor is stretch (0,0)–(1,1) and offsetMin.y=120. Re-run builder. |
| Any button throws NullReferenceException on click | SerializeField not wired | Re-run `BrainBattle → Build Kings Scene`. |
| Crown icon tiny (wrong sprite)     | `LoadAssetAtPath<Sprite>` on crown.png returns `crown_0` (35×33 circle) | Builder uses `LoadAllAssetsAtPath` to find sub-asset named `crown_1`. |
| Cell taps not registering          | EventSystem using wrong input module                    | KingsSceneBuilder's EnsureEventSystem adds InputSystemUIInputModule via reflection. Re-run builder. |
| Border widths inconsistent         | Stale serialized defaults, not reading DesignSystem at runtime | BuildCells() reads `DesignSystem.BorderRegionThickness` and `BorderCellThickness` directly. Never serialize these. |
| Tutorial shown every session       | `Kings_TutorialSeen` PlayerPrefs key missing            | Set to 1 on tutorial completion. Reset by clearing PlayerPrefs in dev. |
| UndoButton always greyed out       | `OnUndoStackChanged` event not subscribed               | Re-run builder; UndoButton subscribes in OnEnable/OnDisable. |
| LevelLoader empty in device build  | `_allLevels` not populated before build                 | Run `BrainBattle → Generate Kings Levels` then `Build Kings Scene` before building. |
| LevelSelectController tabs null    | `_tabButtons` not wired; fallback by name used          | Re-run `Build Level Select Scene`. Fallback logs a warning. |

---

## 10. Before You Start Checklist

Every time a new task arrives:

1. **Identify which scene(s) are affected** (SampleScene, LevelSelect, or both). If both, ask for scope confirmation before starting.
2. **Read relevant scripts first** via MCP tools before writing any code.
3. **Visual task** (colors, layout, sizes, sprites): touch only rendering code; never change game logic or constraints.
4. **Logic task** (rules, state, win conditions): touch only logic classes; never change hierarchy or component layout.
5. **If adding a SerializeField** to any script attached to a built scene: add it to the scene builder's WireAll() first, then re-run the builder.
6. **If fixing a wiring bug**: do not patch it directly in the scene via MCP — patch the builder, then re-run it.
7. **Never use Unity MCP screenshots as final evidence** — for any screenshot request, ask the user to capture the Unity Game view manually and send it back for review.
8. **After completing the task**: update `Assets/_Project/Documentation/Milestones.md`.

---

## Code Standards

- No MonoBehaviour on pure logic classes (`ConstraintValidator`, `GridData`, `LevelGeneratorService` etc.)
- `[SerializeField]` for Inspector references; never `public` fields
- Events via `System.Action`, not `UnityEvent`
- Async via Coroutine or C# Task — never mix both in the same class
- Namespace: `BrainBattle.Core`, `BrainBattle.Kings`, `BrainBattle.Shared`, `BrainBattle.Games.Kings.Logic/UI`
- All files under `Assets/_Project/`
- PlayerPrefs keys in use: `Kings_PendingLevel`, `Kings_Grid`, `Kings_Time`, `Kings_Moves`, `Kings_Level_{N}_Stars`, `Kings_TutorialSeen`

## 11. AudioManager — Setup Rules

**File**: `Assets/_Project/Scripts/Shared/Audio/AudioManager.cs`
**Pattern**: DontDestroyOnLoad singleton, auto-created via `RuntimeInitializeOnLoadMethod`.

### Boot sequence
| Phase | Callback | What happens |
|---|---|---|
| Before first scene | `BeforeSceneLoad` → `AutoCreate()` | Creates GO, builds 8-source SFX pool, creates `[BGMSource]` child |
| After first scene | `AfterSceneLoad` → `AutoStartBGM()` | Calls `PlayBGM()` |
| Every scene load | `SceneManager.sceneLoaded` → `OnSceneLoaded()` | Calls `PlayBGM()` — idempotent |

### Critical rules
1. **Never call `LoadClips()` in `Awake()`** — use lazy `EnsureClipsLoaded()` at top of every `PlayXxx()`. Audio engine not ready during `BeforeSceneLoad`.
2. **Never guard `PlayBGM()` with `isPlaying` alone** — correct guard: `if (_bgmSource.isPlaying && _bgmSource.clip == _bgm) return;`
3. **Never add `AudioListener` to `[AudioManager]` GO** — each scene provides one via its Camera.
4. **Never call `PlayBGM()` from scene controllers** — `AudioManager` handles BGM entirely via `sceneLoaded` + `AfterSceneLoad`.
5. **`PlayBGM()` is idempotent** — no-op if correct clip already playing.
6. **BGM fades in over 1 second** — `FadeBGMIn(1f)` with `Time.unscaledDeltaTime`.

### Audio clip paths (Resources)
| Clip | Path |
|---|---|
| `_bgm` | `audio/BGM/bgm` |
| `_tapClip` | `audio/SFX/tap_dot` |
| `_tapVariantClip` | `audio/SFX/tap_dot_variant` |
| `_autoDotClip` | `audio/SFX/auto_dot` |
| `_autoDotVariantClip` | `audio/SFX/auto_dot_variant` |
| `_buttonTapClip` | `audio/SFX/button_tap` |
| `_invalidPlaceClip` | `audio/SFX/invalid_place` |
| `_invalidPlaceVariantClip` | `audio/SFX/invalid_place_variant` |
| `_victoryClip` | `audio/SFX/victory_sound` |

### Android
- BGM must use `loadType: 2` (Streaming) on Android — set in `.meta` platform override.
- `m_ClearDynamicDataOnBuild: 0` in `TMP Settings.asset` — necessary but not sufficient (see §7 Font atlas rules).

---

## Anti-Stuck Rules

- **NEVER** manually compute, verify, or think through grid data, level content, or queen positions
- **NEVER** verify algorithm correctness in thinking — write the code, let Unity run it
- If a task needs >5 minutes of planning, STOP and ask for clarification
- Max one responsibility per prompt; if output will be >200 lines, ask to split first
- Write code immediately; no lengthy preamble; no explanation after code unless asked
- If stuck: output partial code with `// TODO` comments and stop
