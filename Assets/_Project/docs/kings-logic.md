> Folder: `Assets/_Project/Scripts/Games/Kings/Logic/`

# Games/Kings/Logic — Game Loop

Namespace: `BrainBattle.Games.Kings.Logic`

---

## KingsGameManager.cs

Core game loop. MonoBehaviour on the `GameManager` GameObject in SampleScene.

### SerializeFields (wired by KingsSceneBuilder)
```csharp
[SerializeField] private KingsGridRenderer  _gridRenderer;
[SerializeField] private GridData           _currentGrid;       // runtime grid snapshot
[SerializeField] private TutorialController _tutorialController;
```

### Public Events

```csharp
event Action                   OnWin;
event Action<float, int>       OnGameComplete;         // (elapsedSeconds, moveCount)
event Action<List<Vector2Int>> OnConflictDetected;     // Vector2Int(col, row) list
event Action<bool>             OnUndoStackChanged;     // bool = stack is non-empty
event Action<CellState>        OnCellPlaced;           // fired on tap & drag
event Action                   OnUndoPerformed;
event Action                   OnAutoDotsBatch;
```

### Public Properties

```csharp
float ElapsedSeconds   { get; }   // unscaled time, pauses when app is backgrounded
int   MoveCount        { get; }   // total crown placements (not decremented by undo)
int   CurrentGridSize  { get; }   // grid.Size or 0 if no grid loaded
int   HintsUsed        { get; }
```

### Public API

```csharp
void             StartGame(GridData grid);     // call from KingsSceneBootstrap
void             DoUndo();
void             RestartGame();
List<string>     GetHints();                   // up to 3 hint strings, increments HintsUsed
List<string>     GetHints(GridData grid);      // pure version (no side effects)
```

### Input Model

| Gesture | Result |
|---|---|
| Single tap on empty/dot | Toggle Empty ↔ Dot |
| Double-tap (< 300ms) on same cell | Place Crown |
| Tap on Crown | Clear → Empty |
| Drag across cells | Paint all cells with same state as the first drag target |
| Drag never overwrites Crown | Safety guard |

Auto-placed dots:  
When a crown is placed, all cells in the same row, column, region, and 8 adjacent
cells are auto-filled with `Dot`. On undo/crown removal those dots are cleared
(unless also covered by another crown's auto-dots).

### Undo Stack

- Max 50 snapshots (`MaxUndoHistory = 50`).
- Each snapshot is a deep-copy via `JsonUtility` round-trip.
- Undo clears `_autoPlacedDots` (auto-dots are recomputed by the saved snapshot state, not stored separately).
- `OnUndoStackChanged(bool hasUndo)` fires after every push/pop/clear.

### Timer

- Uses `Time.unscaledTime` (not affected by `Time.timeScale`).
- Pauses on `OnApplicationPause(true)` (app backgrounded).
- Stops on `HandleWin()`.
- Resets on `StartGame()` and `RestartGame()`.

### Auto-Save

Every move, undo, and restart calls `AutoSave()`:
```csharp
PlayerPrefs.SetString("Kings_Grid",  JsonUtility.ToJson(_currentGrid));
PlayerPrefs.SetFloat("Kings_Time",   ElapsedSeconds);
PlayerPrefs.SetInt("Kings_Moves",    _moveCount);
```
Win clears these keys. Resume is handled by `KingsSceneBootstrap`.

---

## KingsSceneBootstrap.cs

MonoBehaviour on the `SceneBootstrap` GameObject. Initializes the game scene.

```csharp
[SerializeField] private KingsGameManager _gameManager;
[SerializeField] private LevelLoader      _levelLoader;

public int CurrentLevelNumber { get; private set; }
```

### Startup Flow

```
Awake: read PlayerPrefs("Kings_PendingLevel") → CurrentLevelNumber
Start: yield null (wait 1 frame for Canvas layout pass) → call LoadLevel(CurrentLevelNumber)
```

One frame delay is required because `KingsGridRenderer` needs the `RectTransform` dimensions
finalized by the Canvas layout system before `RenderGrid` can compute cell sizes.

### Resume Saved Game

If `PlayerPrefs("Kings_Grid")` exists for the current level, `BuildGridFromLevel` is
overwritten with the saved JSON state so the player continues where they left off.

```csharp
public void LoadLevel(int levelNumber);    // load by number, start game
```
