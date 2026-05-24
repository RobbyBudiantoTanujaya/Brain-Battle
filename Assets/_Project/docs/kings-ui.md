> Folder: `Assets/_Project/Scripts/Games/Kings/UI/`

# Games/Kings/UI — In-Game UI Components

Namespace: `BrainBattle.Games.Kings.UI`

---

## KingsGridRenderer.cs

`[RequireComponent(typeof(RectTransform))]` on `GridContainer` in SampleScene.

Responsible for:
- Building all cell GameObjects at runtime from a `GridData`
- Handling pointer events (tap, drag) and firing input events
- Updating individual cells when state changes
- Animating conflict highlights (sine-wave red pulse)

### SerializeFields (wired by KingsSceneBuilder)
```csharp
[SerializeField] private Sprite _dotSprite;
[SerializeField] private Sprite _crownSprite;
[SerializeField] private float  _padding;          // from DesignSystem.GridPadding
[SerializeField] private Color  _conflictColor;    // default red
[SerializeField] private float  _pulseSpeed;       // conflict pulse frequency
```

If sprites are null at runtime, they are loaded from `Resources/Sprites/dot` and
`Resources/Sprites/crown` automatically.

### Public Events

```csharp
event Action<int, int>            OnCellTapped;        // (row, col)
event Action<int, int, CellState> OnCellDragEntered;   // (row, col, targetState)
```

`KingsGameManager` subscribes to these in `OnEnable`.

### Public API

```csharp
void RenderGrid(GridData grid);                    // full rebuild — call from StartGame/Undo
void UpdateCell(int row, int col, CellState state); // incremental — call per move
void HighlightConflicts(List<Vector2Int> positions); // starts sine-wave pulse coroutine
void ClearConflicts();                              // stops coroutine, restores base colors
```

### Cell Layout

```
GridContainer (RectTransform, full screen width)
└── GridPanel (child RectTransform, square, centered)
    └── Cell[row,col] (RectTransform)
        ├── Background (Image, region color)
        │   └── CellEventHandler (IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler)
        └── Icon (Image, dot or crown sprite, centered, 60% of cell size)
```

Cell size = `min(containerW, containerH) / gridSize` (square cells).
Icon size = 60% of cell size for dots, 65% for crowns.

### Drag Behavior

- `PointerDown` → `BeginCellInteraction(row, col)` → fires `OnCellTapped`
- `PointerEnter` while dragging → `ContinueDrag(row, col)` → fires `OnCellDragEntered`
- `PointerUp` → `EndDrag()`
- Drag target state is determined from the first cell of the drag stroke.
- Drag never writes over a `Crown` cell.

### Conflict Animation

`HighlightConflicts` starts a coroutine that pulses conflict cells between their
base color and `_conflictColor` using `Mathf.Sin(Time.unscaledTime / _pulseSpeed)`.
`ClearConflicts` stops it and resets all cells to their base region color.

---

## HUDController.cs

Updates timer and move-counter text every frame.

```csharp
[SerializeField] private TextMeshProUGUI _timerText;
[SerializeField] private TextMeshProUGUI _moveCountText;
[SerializeField] private KingsGameManager _gameManager;
```

- `Update()`: formats `_gameManager.ElapsedSeconds` as `MM:SS` into `_timerText`.
- Subscribes to `_gameManager.OnGameComplete` to stop updating after win.

---

## HUDLayoutController.cs

Adjusts font sizes in the HUD to fit the actual canvas-scaled height.

- Runs once in `Start()` after layout is calculated.
- Reads the HUD strip's actual pixel height via `RectTransformUtility`.
- Sets font sizes proportionally so text never overflows narrow phone screens.

---

## TutorialController.cs

5-step tutorial overlay shown on first play.

```csharp
[SerializeField] private GameObject _overlay;
[SerializeField] private TextMeshProUGUI _stepText;
[SerializeField] private TextMeshProUGUI _promptText;

public event Action OnTutorialComplete;
```

- `ShowTutorial()`: checks `PlayerPrefs("Kings_TutorialSeen")`. If already seen, fires `OnTutorialComplete` immediately.
- Steps advance on tap. After step 5, sets the PlayerPrefs key and fires `OnTutorialComplete`.
- `KingsGameManager` calls `ShowTutorial()` from `StartGame()`.
- Tutorial overlay is on top of the grid but below `VictoryPanel`.
