# Event Contracts: LinkedIn Queens Game

**Feature**: `linkedin-queens-game`
**Date**: 2026-05-25

---

## Gameplay Events (from KingsGameManager)

### OnGameComplete

**Signature**: `Action<float time, int moveCount>`

**Emitted when**: Player places final valid crown, all constraints satisfied

**Payload**:
| Field | Type | Description |
|-------|------|-------------|
| `time` | float | Elapsed time in seconds |
| `moveCount` | int | Number of moves made |

**Subscribers**:
- `VictoryPanel` → show victory UI, save stars
- `AnalyticsManager` → log `level_complete` event
- `AudioManager` → play victory SFX

---

### OnConflictDetected

**Signature**: `Action<List<Vector2Int> conflictingPositions>`

**Emitted when**: Player places crown that violates constraints

**Payload**:
| Field | Type | Description |
|-------|------|-------------|
| `conflictingPositions` | List\<Vector2Int\> | All crown positions involved in conflict |

**Subscribers**:
- `KingsGridRenderer` → highlight conflicting cells
- `AudioManager` → play invalid placement SFX

---

### OnUndoStackChanged

**Signature**: `Action<int undoCount, int maxUndo>`

**Emitted when**: Move is made (undo available) or undo is performed

**Payload**:
| Field | Type | Description |
|-------|------|-------------|
| `undoCount` | int | Current undo stack size |
| `maxUndo` | int | Maximum undo capacity (50) |

**Subscribers**:
- `UndoButton` → update visual state (enabled/disabled)

---

### OnCellStateChanged

**Signature**: `Action<int row, int col, CellState newState>`

**Emitted when**: Cell state changes (Empty → Dot → Crown cycle)

**Payload**:
| Field | Type | Description |
|-------|------|-------------|
| `row` | int | Cell row index |
| `col` | int | Cell column index |
| `newState` | CellState | New cell state |

**Subscribers**:
- `KingsGridRenderer` → update cell visual
- `AudioManager` → play tap/dot SFX

---

## UI Events (from components)

### OnLevelSelected

**Signature**: `Action<int levelNumber>`

**Emitted when**: Player taps level button in LevelSelect

**Payload**:
| Field | Type | Description |
|-------|------|-------------|
| `levelNumber` | int | Selected level number |

**Subscribers**:
- `LevelSelectController` → store pending level, load SampleScene

---

### OnVictoryNextLevel

**Signature**: `Action<int nextLevelNumber>`

**Emitted when**: Player taps "Next Level" in VictoryPanel

**Payload**:
| Field | Type | Description |
|-------|------|-------------|
| `nextLevelNumber` | int | Next level to load |

**Subscribers**:
- `VictoryPanel` → store pending level, load SampleScene
- `AnalyticsManager` → log level progression

---

### OnAdRequested

**Signature**: `Action<AdType type>`

**Emitted when**: Player triggers ad (watch for hint, or interstitial between levels)

**Payload**:
| Field | Type | Description |
|-------|------|-------------|
| `type` | AdType | `Interstitial` or `Rewarded` |

**Subscribers**:
- `AdManager` → show ad, await completion

---

### OnAdCompleted

**Signature**: `Action<AdType type, bool success>`

**Emitted when**: Ad finishes (completed or dismissed)

**Payload**:
| Field | Type | Description |
|-------|------|-------------|
| `type` | AdType | `Interstitial` or `Rewarded` |
| `success` | bool | True if ad watched to completion |

**Subscribers**:
- `TipsButton` → unlock hint if rewarded success
- `AnalyticsManager` → log `ad_watched` event

---

## Daily Challenge Events

### OnDailyChallengeGenerated

**Signature**: `Action<DailyChallengeData data>`

**Emitted when**: Daily challenge puzzle is generated

**Payload**:
| Field | Type | Description |
|-------|------|-------------|
| `data` | DailyChallengeData | Generated challenge data |

**Subscribers**:
- `DailyChallengeUI` → display puzzle info

---

### OnStreakUpdated

**Signature**: `Action<int newStreak, bool isRecord>`

**Emitted when**: Daily streak changes

**Payload**:
| Field | Type | Description |
|-------|------|-------------|
| `newStreak` | int | New streak value |
| `isRecord` | bool | True if new personal best |

**Subscribers**:
- `DailyChallengeUI` → update streak display
- `AnalyticsManager` → log streak event

---

## Audio Events

### OnBGMStateChanged

**Signature**: `Action<bool isPlaying>`

**Emitted when**: BGM starts or stops

**Payload**:
| Field | Type | Description |
|-------|------|-------------|
| `isPlaying` | bool | True if BGM now playing |

**Subscribers**:
- Settings UI → update toggle state

---

### OnVolumeChanged

**Signature**: `Action<float newVolume>`

**Emitted when**: Master volume changes

**Payload**:
| Field | Type | Description |
|-------|------|-------------|
| `newVolume` | float | New volume (0.0-1.0) |

**Subscribers**:
- `AudioManager` → update all source volumes
- PlayerPrefs → persist setting

---

## Event Flow Diagram

```
Player Action
    │
    ▼
KingsGameManager
    │
    ├── OnCellStateChanged ──► KingsGridRenderer (visual update)
    │                      └── AudioManager (SFX)
    │
    ├── OnConflictDetected ──► KingsGridRenderer (highlight)
    │                       └── AudioManager (invalid SFX)
    │
    ├── OnUndoStackChanged ──► UndoButton (state)
    │
    └── OnGameComplete ──► VictoryPanel (show)
                        └── AudioManager (victory SFX)
                        └── AnalyticsManager (log event)
                        └── AdManager (maybe show interstitial)
```
