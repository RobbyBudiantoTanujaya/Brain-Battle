# Brain Battle — Product Requirements Document (PRD)

**Version:** 1.0
**Last Updated:** 2026-05-25
**Target Platform:** Android + iOS (Unity 6.0.75f1 LTS, 2D URP)
**Audience:** Internal Dev Team

---

## Table of Contents

1. [Product Overview](#1-product-overview)
2. [Architecture Overview](#2-architecture-overview)
3. [Core Systems](#3-core-systems)
4. [Feature Specifications](#4-feature-specifications)
5. [API Reference](#5-api-reference)
6. [Asset Specifications](#6-asset-specifications)
7. [Development Guidelines](#7-development-guidelines)
8. [Roadmap](#8-roadmap)

---

## 1. Product Overview

### 1.1 Game Concept

Brain Battle is a 2D mobile puzzle game collection. The first game, **Kings**, is an N×N crown-placement puzzle where players must place exactly one crown per row, column, and colored region, with no two crowns touching (8-directional adjacency).

### 1.2 Core Mechanics (Kings)

| Rule | Description |
|------|-------------|
| **Row Constraint** | Exactly one crown per row |
| **Column Constraint** | Exactly one crown per column |
| **Region Constraint** | Exactly one crown per colored region |
| **Adjacency Constraint** | No two crowns may touch (Chebyshev distance > 1) |

### 1.3 Input Model

| Input | Action |
|-------|--------|
| **Single tap** | Cycle cell: Empty → Dot → (double-tap for Crown) |
| **Double tap** | Directly place Crown |
| **Tap on Crown** | Clear crown (and its auto-dots) |
| **Click + drag** | Place Dots continuously; never overwrites Crowns |

### 1.4 Auto-X Feature

When a crown is placed, the system automatically fills Dots in:
- Same row and column (excluding the crown cell)
- Same region (excluding the crown cell)
- 8 neighboring cells (adjacency)

Auto-dots are removed when their parent crown is cleared.

### 1.5 Tech Stack

| Component | Technology |
|-----------|------------|
| Engine | Unity 6.0.75f1 LTS |
| Render Pipeline | 2D URP |
| UI | Unity UI (Canvas) + TextMeshPro |
| Architecture | MVC-like (Models separate from Views/Controllers) |
| Data Storage | ScriptableObjects (levels), PlayerPrefs (progress) |
| Target | Android + iOS |

---

## 2. Architecture Overview

### 2.1 System Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         EDITOR TOOLS                            │
│  KingsSceneBuilder │ LevelSelectSceneBuilder │ KingsLevelGenerator│
│  KingsSceneValidator │ AudioManagerSetup                        │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼ generates/builds
┌─────────────────────────────────────────────────────────────────┐
│                       DATA LAYER                                │
│  LevelData (ScriptableObject) ← LevelLoader → GridData          │
│  PlayerPrefs: Kings_PendingLevel, Kings_Level_N_Stars, etc.     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼ loads
┌─────────────────────────────────────────────────────────────────┐
│                       LOGIC LAYER                               │
│  KingsGameManager ← ConstraintValidator (static)                │
│  TutorialController │ LevelSelectController                     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼ events
┌─────────────────────────────────────────────────────────────────┐
│                        UI LAYER                                 │
│  KingsGridRenderer │ VictoryPanel │ HUDController               │
│  LevelSelectButton │ UndoButton │ RestartButton │ TipsButton    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     SERVICES LAYER                              │
│  AudioManager (singleton, DontDestroyOnLoad)                    │
│  DesignSystem (static tokens)                                   │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Data Flow

```
App Launch
    │
    ▼
LevelSelect Scene (Build Index 0)
    │  LevelSelectController.Start()
    │  → BuildDiffPools() from LevelData[]
    │  → SelectTab(0) → RebuildGrid()
    │
    ▼ User taps level button
    │  PlayerPrefs.SetInt("Kings_PendingLevel", level)
    │  SceneManager.LoadScene("SampleScene")
    │
    ▼
SampleScene Loads
    │  KingsSceneBootstrap.Start() → BootDeferred()
    │  → Wait 1 frame (Canvas setup)
    │  → Read Kings_PendingLevel, delete key
    │  → LoadLevel(level)
    │      → LevelLoader.GetLevel(level)
    │      → LevelLoader.BuildGridFromLevel()
    │      → KingsGameManager.StartGame(grid)
    │
    ▼
Gameplay
    │  User taps cell → KingsGridRenderer.OnCellTapped
    │  → KingsGameManager processes move
    │  → ConstraintValidator.ValidateMove()
    │  → Apply state, render updates
    │
    ▼ Win condition
    │  ConstraintValidator.CheckWin() == true
    │  → OnGameComplete?.Invoke(time, moves)
    │  → VictoryPanel shows
    │  → Save stars to PlayerPrefs
    │
    ▼
Victory Screen
    ├── Next Level → Load next level or LevelSelect
    ├── Restart → KingsGameManager.RestartGame()
    └── Menu → SceneManager.LoadScene("LevelSelect")
```

### 2.3 Scene Structure

| Scene | Path | Build Index | Builder |
|-------|------|-------------|---------|
| LevelSelect | `Assets/_Project/Scenes/LevelSelect.unity` | 0 | LevelSelectSceneBuilder |
| SampleScene | `Assets/Scenes/SampleScene.unity` | 1 | KingsSceneBuilder |

---

## 3. Core Systems

### 3.1 Grid/Cell Models

**Location:** `Assets/_Project/Scripts/Core/Models/`

#### CellState (enum)
```csharp
public enum CellState {
    Empty = 0,
    Dot = 1,
    Crown = 2
}
```

#### CellData
```csharp
public class CellData {
    public int Row { get; }
    public int Col { get; }
    public CellState State { get; set; }
    public int RegionId { get; set; }
}
```

#### RegionData
```csharp
public class RegionData {
    public int RegionId { get; }
    public Color RegionColor { get; }
    public List<Vector2Int> Cells { get; }
}
```
**Convention:** `Vector2Int.x = col`, `Vector2Int.y = row`

#### GridData
```csharp
public class GridData : ISerializationCallbackReceiver {
    public int Size { get; }
    public CellData[,] Cells { get; }
    public List<RegionData> Regions { get; }

    public CellData GetCell(int row, int col);
    public void SetCellState(int row, int col, CellState state);
    public List<Vector2Int> GetCrownPositions();
    public void CycleState(int row, int col);
}
```
Implements `ISerializationCallbackReceiver` for 2D array serialization.

---

### 3.2 Constraint Validation

**File:** `Assets/_Project/Scripts/Core/Engine/ConstraintValidator.cs`

Static class with pure validation logic (no state, no MonoBehaviour).

#### ValidationResult
```csharp
public struct ValidationResult {
    public bool IsValid;
    public List<Vector2Int> ConflictPositions;
}
```

#### Public API
```csharp
public static class ConstraintValidator {
    // Returns all conflict positions if placing crown violates rules
    public static ValidationResult ValidateMove(GridData grid, int row, int col, CellState newState);

    // True when exactly N crowns, one per row/col/region, no adjacency
    public static bool CheckWin(GridData grid);

    // Returns all crown positions participating in any violation
    public static List<Vector2Int> GetAllConflicts(GridData grid);
}
```

#### Validation Rules

| Constraint | Check |
|------------|-------|
| Row | Only one crown per row |
| Column | Only one crown per column |
| Region | Only one crown per `regionId` |
| Adjacency | Chebyshev distance > 1 between all crowns |

---

### 3.3 Level Generation

**Pipeline:** `LevelGeneratorService.GenerateLevel()`

```
GenerateLevel(levelNumber, size, difficulty, seed)
  │
  ├─► KingsNQueensSolver.Solve(size, rng) → queens[]
  │     Backtracking with randomized column order
  │     Adjacency constraint: only check preceding row
  │
  ├─► KingsRegionBuilder.Build(size, queens, rng) → regionMap[,]
  │     Multi-source BFS flood-fill from queen positions
  │     Each queen seeds its own region
  │     Guarantees 4-connected regions
  │
  ├─► PassesRegionSizeConstraints(size, regionMap)
  │     No single-cell regions
  │     Max 2 two-cell regions for 8×8+
  │
  ├─► KingsUniquenessVerifier.Verify(size, regionMap, queens, rng)
  │     Solution counter (backtracking, early prune)
  │     Border mutation using Tarjan's articulation point algorithm
  │     Retry until exactly 1 solution
  │
  └─► BuildLevelData(...) → LevelData ScriptableObject
```

#### Region Size Constraints
- **No single-cell regions** — instantly reveals crown position
- **Max 2 two-cell regions** for 8×8 and larger — maintains puzzle difficulty

#### Uniqueness Verification Algorithm
1. Count all valid solutions (backtracking with early prune if count > 1)
2. Find border cells between regions
3. Use Tarjan's articulation point algorithm to find safe-to-move cells
4. Mutate region map by swapping non-AP border cells
5. Retry until exactly 1 solution exists

---

### 3.4 Game State Management

**File:** `Assets/_Project/Scripts/Games/Kings/Logic/KingsGameManager.cs`

#### State Fields
```csharp
private GridData _currentGrid;
private GridData _initialGrid;
private Stack<GridData> _undoStack;  // Max 50 entries
private Dictionary<Vector2Int, HashSet<Vector2Int>> _autoPlacedDots;
private int _moveCount;
private bool _timerActive;
private bool _gameActive;
```

#### Events
```csharp
public event Action OnWin;
public event Action<float, int> OnGameComplete;  // time, moves
public event Action<List<Vector2Int>> OnConflictDetected;
public event Action<bool> OnUndoStackChanged;
```

#### Public API
```csharp
public void StartGame(GridData grid);
public void DoUndo();
public void RestartGame();
public List<(string type, int index)> GetHints();  // Up to 3 hints
public int MoveCount { get; }
public float ElapsedSeconds { get; }
```

---

### 3.5 UI System

#### Design System

**File:** `Assets/_Project/Scripts/Shared/UI/BrainBattleDesignSystem.cs`

All colors, sizes, and spacings come from `DesignSystem.TokenName`.

| Category | Tokens |
|----------|--------|
| **Colors** | Primary, PrimaryDark, Background, Surface, TextPrimary, TextSecondary, Overlay, BorderRegion, BorderCell, HUDBarGradientTop/Bottom, HUDBorderSeparator, HUDButtonBg, HUDMoveCounterBg, HUDTipsBg, HUDLabelText |
| **Typography** | FontSizeSmall (12), FontSizeBody (16), FontSizeMedium (18), FontSizeLarge (20), FontSizeTitle (28), FontSizeHero (36) |
| **Spacing** | SpacingXS (4), SpacingS (8), SpacingM (12), SpacingL (16), SpacingXL (24), SpacingXXL (32) |
| **Radius** | RadiusS (8), RadiusM (12), RadiusL (16) |
| **Sizes** | HUDHeight (80), TimerBarHeight (48), ButtonHeight (64), LevelButtonSize (100), LevelButtonGap (12) |
| **Grid** | GridPadding (16), BorderRegionThickness (5), BorderCellThickness (3), CrownSizeRatio (0.65), DotSizeRatio (0.25) |

#### Key Controllers

| Controller | Responsibility |
|------------|---------------|
| **KingsGridRenderer** | Renders puzzle grid, handles tap/drag, manages conflict highlighting |
| **LevelSelectController** | Tab switching, level pool building, progress calculation, navigation |
| **VictoryPanel** | Displays win stats, saves stars, handles next level/restart/menu |
| **HUDController** | Updates timer and move counter displays |
| **TutorialController** | Shows 5-step tutorial on first play |

---

### 3.6 Audio System

**File:** `Assets/_Project/Scripts/Shared/Audio/AudioManager.cs`

Singleton pattern with `DontDestroyOnLoad`. Auto-created via `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)`.

#### Initialization Sequence

| Phase | Callback | Action |
|-------|----------|--------|
| Before scene load | `AutoCreate()` | Creates singleton GO |
| Awake | — | Loads settings, builds SFX pool, initializes BGM source |
| After scene load | `AutoStartBGM()` | Starts BGM playback |

#### SFX Pool
- 8 AudioSources for concurrent SFX
- `NextPooledSource()` finds idle or steals busy source

#### Public API
```csharp
public static AudioManager Instance { get; }

// SFX playback
public void PlayTap();           // Random: tap_dot or tap_dot_variant
public void PlayAutoDot();       // Random pitch 0.9-1.1, volume 0.4×
public void PlayButtonTap();
public void PlayInvalidPlace();  // Random: invalid_place or invalid_place_variant
public void PlayVictory();

// BGM control
public void PlayBGM();   // Idempotent, 1s fade-in
public void StopBGM();

// Volume/Mute (persisted via PlayerPrefs)
public void SetSFXVolume(float volume);  // 0-1
public void SetBGMVolume(float volume);
public void SetSFXMute(bool muted);
public void SetBGMMute(bool muted);
```

#### PlayerPrefs Keys
| Key | Default | Purpose |
|-----|---------|---------|
| Audio_SFXVolume | 1.0 | SFX volume |
| Audio_BGMVolume | 0.7 | BGM volume |
| Audio_SFXMuted | false | SFX mute state |
| Audio_BGMMuted | false | BGM mute state |

---

## 4. Feature Specifications

### Milestone 1: Core Game ✅ COMPLETE

| Feature | Status | Notes |
|---------|--------|-------|
| Grid/Cell Models | ✅ | CellData, CellState, GridData, RegionData |
| ConstraintValidator | ✅ | Row/col/region/adjacency validation |
| Unit Tests | ✅ | ConstraintValidator tests |
| KingsGridRenderer | ✅ | Dynamic grid rendering, tap/drag |
| KingsGameManager | ✅ | State, undo, auto-X, win detection |
| LevelData + LevelLoader | ✅ | ScriptableObject schema |
| LevelGeneratorService | ✅ | NQueensSolver, RegionBuilder, UniquenessVerifier |
| KingsLevelGenerator | ✅ | Editor tool: BrainBattle ▶ Generate Kings Levels |
| Undo/Restart/Tips UI | ✅ | UndoButton, RestartButton, TipsButton |
| Tutorial Overlay | ✅ | 5-step tutorial, PlayerPrefs flag |
| Victory Screen | ✅ | Time, moves, stars, next level |
| Scene Bootstrap | ✅ | KingsSceneBootstrap handles level loading |
| KingsSceneBuilder | ✅ | Editor tool: BrainBattle ▶ Build Kings Scene |
| KingsSceneValidator | ✅ | Editor tool: BrainBattle ▶ Validate Kings Scene |
| BrainBattleDesignSystem | ✅ | All design tokens centralized |

---

### Milestone 2: Level Generation + Progression ✅ COMPLETE

| Feature | Status | Notes |
|---------|--------|-------|
| Level Select Screen | ✅ | LevelSelectController, LevelSelectButton prefab |
| 3 Difficulty Tabs | ✅ | Beginner (1-12), Expert (13-24), Impossible (25-35) |
| Progress Bars | ✅ | Per-difficulty completion percentage |
| Level States | ✅ | Locked, Available, Completed (sprite-based) |
| Unlock Logic | ✅ | Per-tab independent, first level always available |
| Play Button | ✅ | Loads first available level in active tab |
| Scene Navigation | ✅ | PlayerPrefs-based level passing |
| 35 Level Assets | ✅ | Generated via KingsLevelGenerator |
| Next Level Navigation | ✅ | Stays within same difficulty, falls back to LevelSelect |
| Visual Polish | ✅ | Dark navy + hot pink design language |

---

### Milestone 3: Polish + Monetization + Launch v1.0 🔄 IN PROGRESS

| Feature | Status | Notes |
|---------|--------|-------|
| Audio System | ✅ | AudioManager singleton, SFX pool, BGM fade |
| BGM Auto-Start Fix | ✅ | Handles Unity 6 `isPlaying` quirk |
| Audio Lazy Loading | ✅ | Clips load on first PlayXxx() call |
| Haptic Feedback | ⬜ | Light tap, medium crown, strong victory |
| Animations | ⬜ | Crown pop-in, victory slide-up, star fill |
| Splash Screen | ⬜ | 1024×1024 app icon |
| AdMob Integration | ⬜ | Interstitial + rewarded ads |
| Hint System | ⬜ | Gated behind rewarded ad |
| Firebase Analytics | ⬜ | level_start, level_complete, ad_watched |
| Firebase Crashlytics | ⬜ | Crash reporting |
| Onboarding | ⬜ | Enforce level 1 first |
| Settings Screen | ⬜ | Sound/music/haptic toggles, reset progress |
| Store Assets | ⬜ | Screenshots, descriptions |
| Internal Build | ⬜ | Closed testing → production |

---

### Milestone 4: Content + Retention 📋 PLANNED

| Feature | Status | Notes |
|---------|--------|-------|
| Daily Challenge | ⬜ | Seeded RNG by date, global puzzle |
| Daily Streak | ⬜ | PlayerPrefs, reset on skip |
| Infinite Mode | ⬜ | Procedural 5-10 size |
| Level Rating | ⬜ | Optional 1-5 star user rating |
| Push Notifications | ⬜ | Daily challenge reminder |
| Leaderboard | ⬜ | Daily challenge top-10 |
| 35 Level Expansion | ⬜ | Impossible to level 50 |

---

### Milestone 5: Game #2 📋 PLANNED

**Puzzle Type:** TBD (Nonogram, Kakuro, or Sudoku variant)

| Feature | Status | Notes |
|---------|--------|-------|
| Game Design Doc | ⬜ | Constraint validator design |
| Grid Renderer | ⬜ | Reuse component pattern from Kings |
| Level Generator | ⬜ | 35 levels |
| Level Select Integration | ⬜ | New tab in existing controller |
| M3 Feature Parity | ⬜ | Audio, haptic, analytics, monetization |

---

### Milestone 6: Social + PvP 📋 PLANNED

| Feature | Status | Notes |
|---------|--------|-------|
| Account System | ⬜ | Firebase Auth (Google/Apple Sign-In) |
| Friend Challenge | ⬜ | Async puzzle sharing |
| Real-time PvP | ⬜ | Race mode (Photon or Firebase RTDB) |
| Matchmaking | ⬜ | ELO-based queue |
| PvP Rewards | ⬜ | Cosmetic crown skins (no pay-to-win) |

---

## 5. API Reference

### 5.1 ConstraintValidator (Static)

```csharp
// Validate a potential move
ValidationResult result = ConstraintValidator.ValidateMove(grid, row, col, CellState.Crown);
if (!result.IsValid) {
    // result.ConflictPositions contains all violating positions
}

// Check win condition
if (ConstraintValidator.CheckWin(grid)) {
    // Puzzle solved
}

// Get all conflicts for highlighting
List<Vector2Int> conflicts = ConstraintValidator.GetAllConflicts(grid);
```

### 5.2 KingsGameManager

```csharp
// Events
gameManager.OnGameComplete += (time, moves) => { /* handle win */ };
gameManager.OnConflictDetected += (conflicts) => { /* highlight conflicts */ };
gameManager.OnUndoStackChanged += (hasUndo) => { /* update undo button */ };

// Methods
gameManager.StartGame(grid);
gameManager.DoUndo();
gameManager.RestartGame();
var hints = gameManager.GetHints();  // Returns up to 3 hints

// Properties
int moves = gameManager.MoveCount;
float time = gameManager.ElapsedSeconds;
```

### 5.3 KingsGridRenderer

```csharp
// Events
gridRenderer.OnCellTapped += (row, col) => { /* handle tap */ };
gridRenderer.OnCellDragEntered += (row, col) => { /* handle drag */ };

// Methods
gridRenderer.RenderGrid(gridData);
gridRenderer.HighlightConflicts(positions);
gridRenderer.UpdateCell(row, col, cellData);
```

### 5.4 LevelLoader

```csharp
// Get level by number
LevelData level = levelLoader.GetLevel(5);

// Build runtime grid from level
GridData grid = levelLoader.BuildGridFromLevel(level);

// Get next level in same difficulty
int? next = levelLoader.GetNextLevelNumberInDifficulty(currentLevel);
```

### 5.5 AudioManager

```csharp
// SFX (via singleton)
AudioManager.Instance.PlayTap();
AudioManager.Instance.PlayAutoDot();
AudioManager.Instance.PlayButtonTap();
AudioManager.Instance.PlayInvalidPlace();
AudioManager.Instance.PlayVictory();

// BGM
AudioManager.Instance.PlayBGM();   // Idempotent
AudioManager.Instance.StopBGM();

// Volume/Mute
AudioManager.Instance.SetSFXVolume(0.8f);
AudioManager.Instance.SetBGMVolume(0.5f);
AudioManager.Instance.SetSFXMute(true);
```

---

## 6. Asset Specifications

### 6.1 Sprites

**Location:** `Assets/_Project/Resources/Sprites/`

| File | Type | Usage |
|------|------|-------|
| `dot.png` | Single sprite | Cell dot marker |
| `crown.png` | Multi-sprite sheet | Crown icons (use `crown_1` sub-asset, 347×224) |
| `main_menu_bg.png` | Multi-sprite | Scene backgrounds |
| `victory_screen_bg.png` | Single sprite | Victory panel background |
| `UIRoundedRect.png` | 9-sliced (128×128) | Button/panel backgrounds |
| `level_available.png` | Single sprite | Level button available state |
| `level_completed.png` | Single sprite | Level button completed state |
| `level_active.png` | Single sprite | Level button active state |
| `level_lock.png` | Single sprite | Level button locked state |

**Loading Pattern:**
```csharp
// Runtime (Resources.Load)
var sprites = Resources.LoadAll<Sprite>("Sprites/crown");
Sprite crown = sprites.First(s => s.name == "crown_1");

// Editor (AssetDatabase)
Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
    "Assets/_Project/Resources/Sprites/dot.png");
```

### 6.2 Fonts

**Location:** `Assets/_Project/Resources/Fonts/`

| File | Type | Usage |
|------|------|-------|
| `Outfit-Regular.ttf` | TTF source | Body font (must have `includeFontData = true`) |
| `Outfit SDF.asset` | TMP Static | Body text, 100 chars pre-baked (ASCII 32-126) |
| `HUDIcons SDF.asset` | TMP Static | Icons: ↩ ↺ ☰ ✦ ★ ☆ (10 glyphs pre-baked) |
| `SegoeSym.ttf` | TTF fallback | Icon font source |

**Critical Rules:**
- Both SDF assets MUST be `AtlasPopulationMode.Static`
- `TMP Settings.asset → m_ClearDynamicDataOnBuild` MUST be `0`
- Never call `TMP_FontAsset.CreateFontAsset()` without `BakeFullCharset()` first

### 6.3 Audio Clips

**Location:** `Assets/_Project/Resources/audio/`

| Path | Type | Usage |
|------|------|-------|
| `BGM/bgm.mp3` | BGM | Background music (Streaming on Android) |
| `SFX/tap_dot.ogg` | SFX | Tap sound |
| `SFX/tap_dot_variant.ogg` | SFX | Tap variant |
| `SFX/auto_dot.ogg` | SFX | Auto-dot placement |
| `SFX/auto_dot_variant.ogg` | SFX | Auto-dot variant |
| `SFX/button_tap.wav` | SFX | Button click |
| `SFX/invalid_place.ogg` | SFX | Invalid placement |
| `SFX/invalid_place_variant.ogg` | SFX | Invalid variant |
| `SFX/victory_sound.ogg` | SFX | Victory fanfare |

### 6.4 Level Data

**Location:** `Assets/_Project/ScriptableObjects/Kings/Levels/`

**Naming Convention:** `Kings_{Difficulty}_{Number:D2}.asset`
- Examples: `Kings_Beginner_01.asset`, `Kings_Expert_06.asset`

**Schema:**
```csharp
public sealed class LevelData : ScriptableObject {
    [SerializeField] private int _levelNumber;      // 1, 2, 3...
    [SerializeField] private string _difficulty;    // "Beginner", "Expert", "Impossible"
    [SerializeField] private int _gridSize;         // 4, 5, 6, 8, or 10
    [SerializeField] private RegionDefinition[] _regions;
    [SerializeField] private Vector2Int[] _solution;  // Queen positions

    [Serializable]
    public class RegionDefinition {
        [SerializeField] private int _regionId;
        [SerializeField] private Color _color;
        [SerializeField] private Vector2Int[] _cells;
    }
}
```

### 6.5 Prefabs

**Location:** `Assets/_Project/Prefabs/`

| Prefab | Usage |
|--------|-------|
| `LevelSelectButton.prefab` | Level grid button, 4 state sprites pre-assigned |

---

## 7. Development Guidelines

### 7.1 Coding Standards

- **No MonoBehaviour** on pure logic classes (`ConstraintValidator`, `GridData`, `LevelGeneratorService`)
- **`[SerializeField]`** for Inspector references; never `public` fields
- **Events via `System.Action`**, not `UnityEvent`
- **Coroutines or C# Task** for async; never mix both in same class
- **Namespace convention:** `BrainBattle.Core`, `BrainBattle.Kings`, `BrainBattle.Shared`

### 7.2 Scene Wiring Rules

| Rule | Reason |
|------|--------|
| **Never fix SerializeField by hand** | Always run scene builder |
| **Never run wrong builder** | KingsSceneBuilder → SampleScene only; LevelSelectSceneBuilder → LevelSelect only |
| **Never edit both scenes in one task** | Confirm scope first |
| **Never add SerializeField without re-running builder** | New field will be null at runtime |
| **Always run Validate Kings Scene after SampleScene change** | Catch wiring errors before play |

### 7.3 Design System Rules

| Rule | Reason |
|------|--------|
| **Never hardcode hex colors** | Use `DesignSystem.Primary` etc. |
| **Never hardcode pixel sizes with tokens** | Use `DesignSystem.HUDHeight` etc. |
| **Read tokens at call time** | Not in `[SerializeField]` defaults |

### 7.4 Editor Tools

| Menu Item | When to Run |
|-----------|-------------|
| `BrainBattle ▶ Build Kings Scene` | After adding SerializeField, before QA build |
| `BrainBattle ▶ Validate Kings Scene` | After any SampleScene change |
| `BrainBattle ▶ Build Level Select Scene` | After layout change, new SerializeField |
| `BrainBattle ▶ Generate Kings Levels` | To add new levels |
| `BrainBattle ▶ Setup Audio Manager` | In first-loaded scene (LevelSelect) |

### 7.5 PlayerPrefs Keys

| Key | Type | Purpose |
|-----|------|---------|
| `Kings_PendingLevel` | int | Level to load on scene transition |
| `Kings_Level_{N}_Stars` | int (0-3) | Best star rating for level N |
| `Kings_TutorialSeen` | int (0/1) | Tutorial completion flag |
| `Kings_Grid` | JSON | Auto-save current grid state |
| `Kings_Time` | float | Auto-save elapsed time |
| `Kings_Moves` | int | Auto-save move count |
| `Audio_SFXVolume` | float | SFX volume (0-1) |
| `Audio_BGMVolume` | float | BGM volume (0-1) |
| `Audio_SFXMuted` | int (0/1) | SFX mute state |
| `Audio_BGMMuted` | int (0/1) | BGM mute state |

---

## 8. Roadmap

### M3 Remaining (v1.0 Launch)

1. **Haptic Feedback** — Integrate Unity Vibration API
2. **Animations** — DOTween or custom coroutines for:
   - Crown pop-in scale tween
   - Victory panel slide-up
   - Star fill sequential animation
3. **Splash Screen** — Unity player settings + 1024×1024 icon
4. **AdMob** — Interstitial (between levels) + Rewarded (hint unlock)
5. **Firebase** — Analytics + Crashlytics integration
6. **Settings Screen** — Sound/music/haptic toggles, reset progress
7. **Store Assets** — Screenshots, descriptions for Google Play + App Store

### M4 Planning (Content + Retention)

- Daily Challenge seed generation
- Streak tracking logic
- Infinite mode procedural generation
- Push notification scheduling
- Leaderboard integration (Google Play Games / Game Center)

### M5 Decision Point

**Select Puzzle Type:** Nonogram, Kakuro, or Sudoku variant
- Consider: constraint complexity, visual rendering needs, level generation difficulty
- Decision needed before M5 work begins

### M6 Long-term (Social + PvP)

- Firebase Auth integration
- Real-time multiplayer architecture decision (Photon vs Firebase RTDB)
- ELO matchmaking system
- Cosmetic rewards (no pay-to-win)

---

## Appendix A: Known Issues & Fixes

| Issue | Root Cause | Fix |
|-------|------------|-----|
| VictoryContent visible on startup | Left Active in scene | Builder sets inactive; VictoryPanel.Start() re-hides |
| Grid rect zero | CanvasScaler not run | Bootstrap waits 1 frame |
| Crown icon tiny | Wrong sprite loaded | Use `LoadAllAssetsAtPath`, find `crown_1` |
| Cell taps not registering | Wrong input module | Builder adds InputSystemUIInputModule |
| BGM silent on boot | Unity 6 `isPlaying` quirk | Guard: `isPlaying && clip == _bgm` |
| Audio clips null | Early loading in Awake | Lazy load on first PlayXxx() |
| TMP text missing on Android | Dynamic atlas stripped | Static mode + pre-baked + `m_ClearDynamicDataOnBuild=0` |

---

## Appendix B: File Reference

| Category | Path |
|----------|------|
| **Models** | `Assets/_Project/Scripts/Core/Models/` |
| **Engine** | `Assets/_Project/Scripts/Core/Engine/` |
| **Generators** | `Assets/_Project/Scripts/Core/Generators/` |
| **Kings Logic** | `Assets/_Project/Scripts/Games/Kings/Logic/` |
| **Kings UI** | `Assets/_Project/Scripts/Games/Kings/UI/` |
| **Kings Data** | `Assets/_Project/Scripts/Games/Kings/Data/` |
| **Shared UI** | `Assets/_Project/Scripts/Shared/UI/` |
| **Shared Audio** | `Assets/_Project/Scripts/Shared/Audio/` |
| **Editor Tools** | `Assets/_Project/Editor/` |
| **Level Assets** | `Assets/_Project/ScriptableObjects/Kings/Levels/` |
| **Sprites** | `Assets/_Project/Resources/Sprites/` |
| **Fonts** | `Assets/_Project/Resources/Fonts/` |
| **Audio** | `Assets/_Project/Resources/audio/` |
| **Prefabs** | `Assets/_Project/Prefabs/` |
| **Documentation** | `Assets/_Project/Documentation/` |
