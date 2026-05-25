# Data Model: LinkedIn Queens Game

**Feature**: `linkedin-queens-game`
**Date**: 2026-05-25

---

## Existing Entities (M1-M2 Complete)

These entities are already implemented. No changes required.

### CellState (enum)

| Value | Description |
|-------|-------------|
| `Empty` | No player mark |
| `Dot` | Player marked as "not crown" |
| `Crown` | Player placed crown |

**File**: `Assets/_Project/Scripts/Core/Models/CellState.cs`

---

### CellData (class)

| Field | Type | Mutable | Description |
|-------|------|---------|-------------|
| `Row` | int | No | Grid row index |
| `Col` | int | No | Grid column index |
| `State` | CellState | Yes | Current cell state |
| `RegionId` | int | Yes | Region membership |

**File**: `Assets/_Project/Scripts/Core/Models/CellData.cs`

---

### RegionData (class)

| Field | Type | Description |
|-------|------|-------------|
| `RegionId` | int | Unique region identifier |
| `Color` | Color | Display color |
| `Cells` | List\<Vector2Int\> | Member cell positions (col, row convention) |

**File**: `Assets/_Project/Scripts/Core/Models/RegionData.cs`

---

### GridData (class)

| Field | Type | Description |
|-------|------|-------------|
| `Size` | int | Grid dimension (N×N) |
| `Cells` | CellData[,] | 2D cell matrix |
| `Regions` | List\<RegionData\> | Region definitions |

**Methods**:
- `GetCell(row, col)` → CellData
- `SetCellState(row, col, state)` → void
- `CycleState(row, col)` → CellState (new state)
- `GetCrownPositions()` → List\<Vector2Int\>

**File**: `Assets/_Project/Scripts/Core/Models/GridData.cs`

---

### LevelData (ScriptableObject)

| Field | Type | Description |
|-------|------|-------------|
| `LevelNumber` | int | Display level number |
| `Difficulty` | string | "Beginner" / "Expert" / "Impossible" |
| `GridSize` | int | Grid dimension |
| `Regions` | List\<RegionDefinition\> | Serialized region data |
| `Solution` | List\<Vector2Int\> | Crown positions (for validation) |

**File**: `Assets/_Project/Scripts/Games/Kings/Data/LevelData.cs`

---

## New Entities (M3-M6)

### PlayerProgress (PlayerPrefs schema)

| Key | Type | Description |
|-----|------|-------------|
| `Kings_PendingLevel` | int | Level to load on scene transition |
| `Kings_Level_{N}_Stars` | int | Best star rating for level N (0-3) |
| `Kings_TutorialSeen` | int | 1 if tutorial completed |
| `Kings_AudioVolume` | float | Master volume (0.0-1.0) |
| `Kings_BGMToggle` | int | 1 if BGM enabled |
| `Kings_SFXToggle` | int | 1 if SFX enabled |
| `Kings_DailyStreak` | int | Current daily challenge streak |
| `Kings_LastDailyDate` | string | ISO date of last daily completion |

**Storage**: PlayerPrefs (no class file, accessed via wrapper methods)

---

### DailyChallengeData (runtime class)

| Field | Type | Description |
|-------|------|-------------|
| `Date` | DateTime | Challenge date (UTC) |
| `Seed` | int | Deterministic seed from date |
| `LevelData` | LevelData | Generated puzzle for the day |
| `IsCompleted` | bool | Player completed today's challenge |
| `Streak` | int | Current consecutive day streak |

**File**: `Assets/_Project/Scripts/Games/Kings/Logic/DailyChallengeManager.cs`

**State Transitions**:
```
[NotStarted] → player opens Daily Challenge → [Generated]
[Generated] → player completes → [Completed]
[Completed] → next day → [Stale] → regenerate → [Generated]
```

---

### AudioSettings (runtime struct)

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `MasterVolume` | float | 1.0 | Overall volume multiplier |
| `BGMVolume` | float | 0.7 | Background music volume |
| `SFXVolume` | float | 1.0 | Sound effects volume |
| `BGMEnabled` | bool | true | BGM toggle |
| `SFXEnabled` | bool | true | SFX toggle |

**Persistence**: PlayerPrefs keys above

---

### AnalyticsEvent (enum)

| Value | Description |
|-------|-------------|
| `LevelStart` | Player begins a level |
| `LevelComplete` | Player solves puzzle |
| `LevelFail` | Player abandons/quits (future) |
| `AdWatched` | Rewarded ad completed |
| `InterstitialShown` | Interstitial ad displayed |
| `DailyChallengeStart` | Daily challenge begun |
| `DailyChallengeComplete` | Daily challenge solved |
| `StreakUpdated` | Daily streak changed |

**File**: `Assets/_Project/Scripts/Shared/Analytics/AnalyticsEvent.cs`

---

## Entity Relationships

```
LevelData (asset)
    │
    ├── contains ──► RegionDefinition (asset)
    │
    └── loaded by ──► LevelLoader
                          │
                          └── produces ──► GridData (runtime)
                                               │
                                               ├── contains ──► CellData[,]
                                               │                    │
                                               │                    └── references ──► RegionData
                                               │
                                               └── used by ──► KingsGameManager
                                                                    │
                                                                    ├── emits ──► OnGameComplete
                                                                    ├── emits ──► OnConflictDetected
                                                                    └── emits ──► OnUndoStackChanged

DailyChallengeData (runtime)
    │
    ├── generates ──► LevelData (daily)
    │
    └── updates ──► PlayerProgress (streak)

AudioManager (singleton)
    │
    ├── subscribes ──► KingsGameManager events
    │
    └── persists ──► AudioSettings → PlayerPrefs

AnalyticsManager (singleton)
    │
    ├── subscribes ──► KingsGameManager events
    │
    └── logs ──► AnalyticsEvent → Firebase
```

---

## Validation Rules

### GridData Invariants

1. `Cells.Length == Size * Size`
2. Each cell has exactly one `RegionId`
3. All `RegionId` values map to valid `RegionData`
4. Crown count ≤ Size (max one per row)

### LevelData Invariants

1. `Solution.Count == GridSize` (exactly N crowns)
2. Each crown position is within grid bounds
3. Crown positions satisfy all constraints (row, col, region, adjacency)
4. Solution is unique (verified by UniquenessVerifier)

### DailyChallengeData Invariants

1. `Seed` is deterministic from `Date`
2. Same `Date` → same `Seed` across all platforms
3. `Streak` increments only on consecutive days
