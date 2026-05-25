# Brain Battle — Product Requirements Document (PRD)

**Version:** 1.0
**Last Updated:** 2026-05-25
**Platform:** Android + iOS (Unity 6.0.75f1 LTS, 2D URP)
**Audience:** Internal Dev Team

---

## Contents

1. [Game Overview](#1-game-overview)
2. [Architecture](#2-architecture)
3. [Core Systems](#3-core-systems)
4. [Feature Status](#4-feature-status)
5. [Component Reference](#5-component-reference)
6. [Assets](#6-assets)
7. [Build & Release](#7-build--release)
8. [Testing](#8-testing)
9. [Dev Guidelines](#9-dev-guidelines)
10. [Roadmap](#10-roadmap)

---

## 1. Game Overview

### 1.1 Concept

Brain Battle is a 2D mobile puzzle game collection. **Game #1: Kings** — an N×N crown-placement puzzle.

**Rules:**
| Constraint | Description |
|------------|-------------|
| Row | Exactly 1 crown per row |
| Column | Exactly 1 crown per column |
| Region | Exactly 1 crown per colored region |
| Adjacency | No two crowns touch (8-directional) |

### 1.2 Input

| Input | Action |
|-------|--------|
| Single tap | Cycle: Empty → Dot → (double-tap for Crown) |
| Double tap | Place Crown directly |
| Tap Crown | Clear crown + auto-dots |
| Drag | Place Dots continuously (never overwrites Crowns) |

### 1.3 Auto-X

When a crown is placed, auto-fill Dots at:
- Same row/col (excluding crown cell)
- Same region (excluding crown cell)
- 8 adjacent cells

Auto-dots are removed when their parent crown is cleared.

### 1.4 Tech Stack

| Component | Technology |
|-----------|------------|
| Engine | Unity 6.0.75f1 LTS |
| Render | 2D URP |
| UI | Unity UI + TextMeshPro |
| Data | ScriptableObjects (levels), PlayerPrefs (progress) |
| Target | Android + iOS |

---

## 2. Architecture

### 2.1 Layers

```
┌─────────────────────────────────────┐
│ EDITOR TOOLS                        │
│ KingsSceneBuilder, LevelSelectBuilder│
│ KingsLevelGenerator, Validator      │
└─────────────────────────────────────┘
                │ generates
                ▼
┌─────────────────────────────────────┐
│ DATA                                │
│ LevelData (ScriptableObject)        │
│ PlayerPrefs (progress/state)        │
└─────────────────────────────────────┘
                │ loads
                ▼
┌─────────────────────────────────────┐
│ LOGIC                               │
│ KingsGameManager                    │
│ ConstraintValidator (static)        │
│ TutorialController                  │
└─────────────────────────────────────┘
                │ events
                ▼
┌─────────────────────────────────────┐
│ UI                                  │
│ KingsGridRenderer                   │
│ VictoryPanel, HUDController         │
│ LevelSelectController               │
└─────────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────┐
│ SERVICES                            │
│ AudioManager (singleton)            │
│ DesignSystem (static tokens)        │
└─────────────────────────────────────┘
```

### 2.2 Scene Flow

```
Launch → LevelSelect (index 0)
           │ tap level button
           │ PlayerPrefs.SetInt("Kings_PendingLevel", n)
           ▼
         SampleScene (index 1)
           │ KingsSceneBootstrap reads pending level
           │ LevelLoader.BuildGridFromLevel()
           ▼
         Gameplay
           │ win → VictoryPanel
           ▼
         Victory → Next Level / Menu / Restart
```

### 2.3 Scenes

| Scene | Path | Builder |
|-------|------|---------|
| LevelSelect | `Assets/_Project/Scenes/LevelSelect.unity` | LevelSelectSceneBuilder |
| SampleScene | `Assets/Scenes/SampleScene.unity` | KingsSceneBuilder |

---

## 3. Core Systems

### 3.1 Models

**Location:** `Scripts/Core/Models/`

| Class | Purpose |
|-------|---------|
| `CellState` | Enum: Empty, Dot, Crown |
| `CellData` | Position (row, col), state, regionId |
| `RegionData` | RegionId, color, cell positions |
| `GridData` | Size, 2D cell array, regions list |

**Convention:** `Vector2Int.x = col`, `Vector2Int.y = row`

### 3.2 Constraint Validation

**File:** `Scripts/Core/Engine/ConstraintValidator.cs`

Static class — pure validation logic, no MonoBehaviour.

```
ValidateMove(grid, row, col, state) → ValidationResult
  └─ Checks: row, col, region uniqueness + adjacency

CheckWin(grid) → bool
  └─ True if exactly N crowns with no conflicts

GetAllConflicts(grid) → List<Vector2Int>
  └─ All positions violating rules
```

### 3.3 Level Generation

**Pipeline:** `LevelGeneratorService.GenerateLevel()`

```
1. KingsNQueensSolver.Solve(size, rng)
   └─ Backtracking with adjacency constraint

2. KingsRegionBuilder.Build(size, queens, rng)
   └─ Multi-source BFS flood-fill from queens

3. KingsUniquenessVerifier.Verify(size, regionMap, queens, rng)
   └─ Count solutions, mutate borders until unique
   └─ Uses Tarjan's articulation point algorithm

4. BuildLevelData() → LevelData ScriptableObject
```

**Constraints:**
- No single-cell regions
- Max 2 two-cell regions for 8×8+

### 3.4 Game State

**File:** `Scripts/Games/Kings/Logic/KingsGameManager.cs`

| Field | Type | Purpose |
|-------|------|---------|
| `_currentGrid` | GridData | Active puzzle state |
| `_initialGrid` | GridData | For restart |
| `_undoStack` | Stack<GridData> | Max 50 entries |
| `_autoPlacedDots` | Dict<pos, set<pos>> | Track auto-dots per crown |
| `_moveCount` | int | Moves this game |
| `ElapsedSeconds` | float | Time tracking |

### 3.5 Audio

**File:** `Scripts/Shared/Audio/AudioManager.cs`

Singleton via `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`.

| Feature | Detail |
|---------|--------|
| SFX Pool | 8 AudioSources, no GC during gameplay |
| BGM | Dedicated source, 1s fade-in |
| Loading | Lazy — clips load on first `PlayXxx()` call |
| Persistence | Volume/mute via PlayerPrefs |

### 3.6 Design System

**File:** `Scripts/Shared/UI/BrainBattleDesignSystem.cs`

Static class — all design tokens.

| Category | Examples |
|----------|----------|
| Colors | Primary (#ff2d78), Background (#1a1a1e), Surface (#2a2a3e) |
| Sizes | HUDHeight (80), TimerBarHeight (48), ButtonHeight (64) |
| Grid | GridPadding (16), BorderRegionThickness (5), BorderCellThickness (3) |
| Ratios | CrownSizeRatio (0.65), DotSizeRatio (0.25) |

**Rule:** Never hardcode values that have a token. Read at call time.

---

## 4. Feature Status

### Milestone 1: Core Game ✅

- Grid/Cell models
- ConstraintValidator + tests
- KingsGridRenderer
- KingsGameManager
- LevelData + LevelLoader
- Level generation pipeline
- Undo/Restart/Tips UI
- Tutorial overlay
- Victory screen
- Scene bootstrap
- KingsSceneBuilder + Validator
- DesignSystem

### Milestone 2: Level Select ✅

- LevelSelect screen
- 3 difficulty tabs (Beginner/Expert/Impossible)
- Progress bars
- Level states (Locked/Available/Completed)
- 35 level assets
- Next level navigation

### Milestone 3: Polish + Launch 🔄

| Feature | Status |
|---------|--------|
| Audio system | ✅ |
| Haptic feedback | ⬜ |
| Animations | ⬜ |
| Splash screen | ⬜ |
| AdMob | ⬜ |
| Firebase Analytics | ⬜ |
| Settings screen | ⬜ |
| Store assets | ⬜ |

### Milestone 4: Content 📋

- Daily Challenge
- Infinite Mode
- Push notifications
- Leaderboard

### Milestone 5: Game #2 📋

TBD: Nonogram, Kakuro, or Sudoku variant

### Milestone 6: Social 📋

- Account system
- Friend challenge
- Real-time PvP

---

## 5. Component Reference

Quick reference for key MonoBehaviour components and static utilities.

### 5.1 KingsGameManager

**File:** `Scripts/Games/Kings/Logic/KingsGameManager.cs`

**Events:**
```csharp
event Action OnWin;
event Action<float, int> OnGameComplete;        // (time, moves)
event Action<List<Vector2Int>> OnConflictDetected;
event Action<bool> OnUndoStackChanged;          // (hasUndo)
```

**Methods:**
```csharp
void StartGame(GridData grid);
void DoUndo();
void RestartGame();
List<(string, int)> GetHints();    // Up to 3 hints
```

**Properties:**
```csharp
int MoveCount { get; }
float ElapsedSeconds { get; }
```

### 5.2 ConstraintValidator

**File:** `Scripts/Core/Engine/ConstraintValidator.cs`

Static — no instance needed.

```csharp
static ValidationResult ValidateMove(GridData grid, int row, int col, CellState newState);
static bool CheckWin(GridData grid);
static List<Vector2Int> GetAllConflicts(GridData grid);
```

**ValidationResult:**
```csharp
bool IsValid;
List<Vector2Int> ConflictPositions;
```

### 5.3 KingsGridRenderer

**File:** `Scripts/Games/Kings/UI/KingsGridRenderer.cs`

**Events:**
```csharp
event Action<int, int> OnCellTapped;        // (row, col)
event Action<int, int> OnCellDragEntered;   // (row, col)
```

**Methods:**
```csharp
void RenderGrid(GridData gridData);
void HighlightConflicts(List<Vector2Int> positions);
void UpdateCell(int row, int col, CellData cellData);
```

### 5.4 LevelLoader

**File:** `Scripts/Games/Kings/Data/LevelLoader.cs`

```csharp
LevelData GetLevel(int levelNumber);
GridData BuildGridFromLevel(LevelData level);
int? GetNextLevelNumberInDifficulty(int currentLevel);
```

### 5.5 AudioManager

**File:** `Scripts/Shared/Audio/AudioManager.cs`

**Access:** `AudioManager.Instance`

```csharp
// SFX
void PlayTap();
void PlayAutoDot();
void PlayButtonTap();
void PlayInvalidPlace();
void PlayVictory();

// BGM
void PlayBGM();    // Idempotent, 1s fade-in
void StopBGM();

// Settings
void SetSFXVolume(float volume);    // 0-1
void SetBGMVolume(float volume);
void SetSFXMute(bool muted);
void SetBGMMute(bool muted);
```

### 5.6 VictoryPanel

**File:** `Scripts/Shared/UI/VictoryPanel.cs`

Subscribes to `KingsGameManager.OnGameComplete`.

**Flow:**
1. Hide HUD, show VictoryContent
2. Calculate stars (based on time + hints)
3. Save `Kings_Level_{N}_Stars` (keep best)
4. Animate entrance

### 5.7 LevelSelectController

**File:** `Scripts/Shared/UI/LevelSelectController.cs`

```csharp
void SelectTab(int tabIndex);    // 0=Beginner, 1=Expert, 2=Impossible
void GoToLevel(int levelNumber);
void RebuildGrid();
```

---

## 6. Assets

### 6.1 Sprites

**Location:** `Resources/Sprites/`

| File | Usage |
|------|-------|
| `dot.png` | Cell dot marker |
| `crown.png` | Multi-sprite — use `crown_1` (347×224) |
| `main_menu_bg.png` | Scene backgrounds |
| `victory_screen_bg.png` | Victory panel background |
| `UIRoundedRect.png` | 9-sliced button/panel bg |
| `level_available.png` | Level button states |
| `level_completed.png` | |
| `level_lock.png` | |

**Load Pattern:**
```csharp
// Multi-sprite
var sprites = Resources.LoadAll<Sprite>("Sprites/crown");
Sprite crown = sprites.First(s => s.name == "crown_1");

// Single sprite
Sprite dot = Resources.Load<Sprite>("Sprites/dot");
```

### 6.2 Fonts

**Location:** `Resources/Fonts/`

| File | Usage |
|------|-------|
| `Outfit SDF.asset` | Body text (Static, pre-baked) |
| `HUDIcons SDF.asset` | Icons: ★ ☆ ↩ ↺ ☰ (Static) |
| `Outfit-Regular.ttf` | Source (must have `includeFontData=true`) |

**Android Critical:**
- Both SDF assets MUST be `AtlasPopulationMode.Static`
- `TMP Settings → m_ClearDynamicDataOnBuild` MUST be `0`
- Never switch Dynamic → Static without baking first

### 6.3 Audio

**Location:** `Resources/audio/`

| Path | Usage |
|------|-------|
| `BGM/bgm.mp3` | Background music |
| `SFX/tap_dot.ogg` | Tap sounds |
| `SFX/auto_dot.ogg` | Auto-dot placement |
| `SFX/button_tap.wav` | Button click |
| `SFX/invalid_place.ogg` | Invalid placement |
| `SFX/victory_sound.ogg` | Victory fanfare |

Each tap/auto/invalid has `_variant` clips for variety.

### 6.4 Level Data

**Location:** `ScriptableObjects/Kings/Levels/`

**Naming:** `Kings_{Difficulty}_{Number:D2}.asset`
- `Kings_Beginner_01.asset`
- `Kings_Expert_06.asset`
- `Kings_Impossible_01.asset`

**Schema:**
```csharp
LevelData {
    int LevelNumber;
    string Difficulty;      // "Beginner", "Expert", "Impossible"
    int GridSize;           // 4, 5, 6, 8, or 10
    RegionDefinition[] Regions;
    Vector2Int[] Solution;  // Queen positions
}
```

---

## 7. Build & Release

### 7.1 Platforms

| Platform | Backend | Status |
|----------|---------|--------|
| Android | IL2CPP (ARM64) | Primary |
| iOS | IL2CPP | Primary |
| Standalone | Mono | Dev only |

### 7.2 Pre-Build Checklist

1. Run `BrainBattle → Validate Kings Scene` (all errors = 0)
2. Run `BrainBattle → Generate Kings Levels` (if new levels)
3. Verify TMP Settings: `m_ClearDynamicDataOnBuild = 0`
4. Increment `AndroidBundleVersionCode` in ProjectSettings

### 7.3 Version

| Field | Location |
|-------|----------|
| Bundle Version | `ProjectSettings/ProjectSettings.asset` |
| Android Version Code | Same file |
| Unity Version | `ProjectSettings/ProjectVersion.txt` |

### 7.4 Packages

| Package | Version | Usage |
|---------|---------|-------|
| com.unity.render-pipelines.universal | 17.0.4 | 2D URP |
| com.unity.inputsystem | 1.19.0 | New input |
| com.unity.test-framework | 1.6.0 | Tests |
| com.coplaydev.unity-mcp | git | Editor tools |

---

## 8. Testing

### 8.1 Framework

Unity Test Framework (NUnit)

### 8.2 Location

```
Assets/_Project/Tests/EditMode/
├── BrainBattle.Tests.EditMode.asmdef
└── ConstraintValidatorTests.cs
```

### 8.3 Naming Convention

| Type | Pattern |
|------|---------|
| Class | `<ClassBeingTested>Tests` |
| Method | `<MethodName>_<Scenario>_<ExpectedBehavior>` |

**Example:**
```csharp
[TestFixture]
public class ConstraintValidatorTests
{
    [Test]
    public void ValidateMove_RowConflict_ReturnsInvalidAndReportsExistingCrown()
    {
        // Arrange-Act-Assert
    }
}
```

### 8.4 Running Tests

- **Editor:** Window → General → Test Runner → EditMode
- **CLI:** Unity batch mode with `-runTests`

---

## 9. Dev Guidelines

### 9.1 Coding Standards

- No MonoBehaviour on pure logic (`ConstraintValidator`, `GridData`)
- `[SerializeField]` for Inspector refs; never `public` fields
- Events via `System.Action`, not `UnityEvent`
- Async via Coroutines only (no async/await)
- Namespaces: `BrainBattle.Core`, `BrainBattle.Kings`, `BrainBattle.Shared`

### 9.2 Event Pattern

```csharp
// Publisher
public event Action<int> OnSomething;

// Subscriber
void OnEnable()  => _source.OnSomething += Handle;
void OnDisable() => _source.OnSomething -= Handle;
```

### 9.3 Logging Pattern

```csharp
Debug.LogError("[ClassName] Description. Run BrainBattle → Build Scene.", this);
//                                                             ↑ context for click-to-navigate
```

### 9.4 Critical Rules

1. Never fix SerializeField by hand — run scene builder
2. Never add SerializeField without re-running builder
3. Never hardcode values with DesignSystem tokens
4. Never change logic for visual tasks (and vice versa)
5. Always run Validate after SampleScene changes

### 9.5 Editor Tools

| Menu | When |
|------|------|
| Build Kings Scene | After SerializeField changes |
| Validate Kings Scene | After any scene change |
| Build Level Select Scene | After layout changes |
| Generate Kings Levels | To add new levels |

### 9.6 PlayerPrefs Keys

| Key | Type | Purpose |
|-----|------|---------|
| `Kings_PendingLevel` | int | Level to load |
| `Kings_Level_{N}_Stars` | int | Best stars (0-3) |
| `Kings_TutorialSeen` | int | Tutorial done |
| `Audio_SFXVolume` | float | SFX volume |
| `Audio_BGMVolume` | float | BGM volume |
| `Audio_SFXMuted` | int | SFX mute |
| `Audio_BGMMuted` | int | BGM mute |

---

## 10. Roadmap

### M3 Remaining (v1.0)

- Haptic feedback
- Animations (crown pop, victory slide, star fill)
- Splash screen + app icon
- AdMob (interstitial + rewarded)
- Firebase Analytics + Crashlytics
- Settings screen
- Store assets (screenshots, descriptions)

### M4

- Daily Challenge (seeded by date)
- Infinite Mode (procedural)
- Push notifications
- Leaderboard

### M5

**Decision needed:** Select Game #2 type
- Nonogram
- Kakuro
- Sudoku variant

### M6

- Firebase Auth
- Friend challenge
- Real-time PvP (Photon or Firebase RTDB)
- Cosmetic rewards

---

## Appendix: Known Issues

| Issue | Cause | Fix |
|-------|-------|-----|
| VictoryContent visible on startup | Left Active in scene | Builder sets inactive |
| Grid rect zero | CanvasScaler not run | Bootstrap waits 1 frame |
| Crown tiny | Wrong sprite loaded | Use `crown_1` sub-asset |
| BGM silent | Unity 6 `isPlaying` quirk | Guard: `isPlaying && clip == _bgm` |
| TMP missing on Android | Dynamic atlas stripped | Static + pre-baked + `m_ClearDynamicDataOnBuild=0` |

---

## Appendix: File Structure

```
Assets/_Project/
├── Scripts/
│   ├── Core/
│   │   ├── Models/          # CellData, GridData, RegionData
│   │   ├── Engine/          # ConstraintValidator
│   │   └── Generators/      # Level generation pipeline
│   ├── Games/Kings/
│   │   ├── Logic/           # KingsGameManager, Bootstrap
│   │   ├── UI/              # KingsGridRenderer, HUDController
│   │   └── Data/            # LevelData, LevelLoader
│   └── Shared/
│       ├── UI/              # DesignSystem, VictoryPanel
│       └── Audio/           # AudioManager
├── Editor/                  # Scene builders, validators
├── Tests/EditMode/          # NUnit tests
├── Resources/
│   ├── Sprites/
│   ├── Fonts/
│   └── audio/
├── ScriptableObjects/Kings/Levels/
├── Prefabs/
├── Scenes/
└── Documentation/
    ├── PRD.md
    └── Milestones.md
```
