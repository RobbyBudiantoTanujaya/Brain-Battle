> Folder: `Assets/_Project/Editor/`

# Editor — Editor-Only Tools

All files in this folder are `#if UNITY_EDITOR` or inside an `Editor` assembly.  
None of these classes exist in builds.  
Namespace: `BrainBattle.Editor`

---

## KingsSceneBuilder.cs

**Entry point:** `BrainBattle → Build Kings Scene`

Completely rebuilds `Assets/Scenes/SampleScene.unity` from scratch.  
This is the **single source of truth** for all SampleScene wiring.  
Never hand-wire SerializeFields in SampleScene — always edit the builder and re-run.

### What It Builds

```
SampleScene
├── Main Camera      (Camera + AudioListener + UniversalAdditionalCameraData)
├── Global Light 2D
├── EventSystem      (InputSystemUIInputModule)
├── Canvas (1170×2532 reference, ScaleWithScreenSize, match=0.5)
│   ├── Background   (Image, main_menu_bg.png sprite)
│   ├── GridContainer (KingsGridRenderer, anchored center)
│   ├── TimerBar     (Image + TimerText TMP)
│   ├── TutorialOverlay (TutorialController, 5-step overlay)
│   ├── TipsPanel    (TMP text + close button)
│   ├── RestartConfirmPanel (ConfirmButton / CancelButton)
│   ├── HUD          (HUDController + HUDLayoutController)
│   │   ├── TopBorder
│   │   └── HUDRow   (HorizontalLayoutGroup)
│   │       ├── UndoButton    (Image + Button + LayoutElement + UndoButton script)
│   │       ├── RestartButton (Image + Button + LayoutElement + RestartButton script)
│   │       ├── MoveCounter   (pink hero tile, MoveCountText + MovesLabel)
│   │       ├── MenuButton    (Image + Button + LayoutElement + MenuButton script)
│   │       └── TipsButton    (Image + Button + LayoutElement + TipsButton script)
│   └── VictoryPanel (VictoryPanel script, fullscreen overlay)
├── GameManager      (KingsGameManager + LevelLoader + TutorialController)
├── SceneBootstrap   (KingsSceneBootstrap)
└── AudioManager     (AudioManager — DontDestroyOnLoad singleton)
```

### Key Methods

```csharp
[MenuItem("BrainBattle/Build Kings Scene")]
static void Build();

static void WireAll(Refs r);           // wires all SerializeField references
static void WireAudioClips(AudioManager audioComp);
static void CreateAudioManager();
```

### Font Creation

The builder creates two TMP font assets if they don't exist or have broken materials:
- `Outfit SDF` from `Outfit-Regular.ttf` (body text, all labels)
- `HUDIcons SDF` from `SegoeSym.ttf` (star ★ glyphs in VictoryPanel)

Font creation uses `TMP_FontAsset.CreateFontAsset()` internally.

### Guards

- Refuses to run in Play mode (stops playback first).
- Shows a confirmation dialog if Canvas/GameManager/SceneBootstrap already exist.
- All operations are `Undo`-registered and collapsible.

---

## LevelSelectSceneBuilder.cs

**Entry point:** `BrainBattle → Build Level Select Scene`

Completely rebuilds `Assets/_Project/Scenes/LevelSelect.unity` and
`Assets/_Project/Prefabs/LevelSelectButton.prefab`.

### What It Builds

```
LevelSelect
├── Main Camera      (Camera + AudioListener, orthographic)
├── EventSystem
├── Canvas (1170×2532 reference, match=0.5)
│   ├── Background   (main_menu_bg.png)
│   ├── Header       (panel + "LEVEL SELECT" title)
│   ├── TabRow       (3 tab buttons: Beginner / Expert / Impossible)
│   ├── ProgressRow  (3 progress bars + "X/Y" texts)
│   ├── LevelGrid    (ScrollRect → Viewport → Content, populated at runtime)
│   └── PlayButton   (bottom CTA)
└── LevelSelectController (wired with all levels + refs)
└── AudioManager     (DontDestroyOnLoad singleton — starts BGM immediately)
```

### Key Difference from KingsSceneBuilder

Uses `NewSceneMode.Single` (opens a fresh blank scene), whereas KingsSceneBuilder
rebuilds objects in the currently-open scene.

---

## KingsLevelGenerator.cs

**Entry point:** `BrainBattle → Generate Kings Levels`

Batch-generates `LevelData` ScriptableObject assets.

```
BrainBattle → Generate Kings Levels
→ For each difficulty × count:
    1. LevelGeneratorService.Generate(gridSize, ...)
    2. Create LevelData asset at Assets/_Project/ScriptableObjects/Kings/Levels/
    3. Assign RegionDefinitions + Solution
    4. Run duplicate detection (fingerprint = sorted region cell counts + solution hash)
```

### Difficulty Settings (defaults)

| Difficulty | Grid Size | Count |
|---|---|---|
| Beginner | 5×5 | 11 |
| Expert | 7×7 | 11 |
| Impossible | 9×9 | 11 |

Levels are numbered sequentially per difficulty starting at 1.  
Existing levels with matching fingerprints are kept (not regenerated).  
Stale levels (excess count) are deleted.

---

## KingsSceneValidator.cs

Legacy editor validation helper for SampleScene SerializeField wiring.

```csharp
static void Validate();

// Checks: KingsGameManager, KingsGridRenderer, LevelLoader,
//         HUDController, UndoButton, RestartButton, TipsButton,
//         MenuButton, VictoryPanel, KingsSceneBootstrap, AudioManager
```

---

## SpriteGenerator.cs

Procedurally generates dot and crown textures at editor time.  
Called internally by `KingsSceneBuilder` if `Resources/Sprites/dot.png` or
`crown.png` do not exist. Not a menu item — called programmatically only.

```csharp
static Texture2D GenerateDot(int size, Color color);
static Texture2D GenerateCrown(int size, Color color);
```
