> Folder: `Assets/_Project/Scripts/Shared/UI/`

# Shared/UI — Shared UI Components

Namespace: `BrainBattle.Shared` / `BrainBattle.Shared.UI`

---

## BrainBattleDesignSystem.cs (`DesignSystem`)

Static class. Single source of truth for all visual constants.  
All rendering code reads from here at runtime — never bake values into scenes.

```csharp
public static class DesignSystem
{
    // ── Colors ─────────────────────────────────────────────────────────────────
    static Color Primary       { get; }   // accent pink  #ff2d78
    static Color Surface       { get; }   // dark panel   #2a2a3e
    static Color Background    { get; }   // dark bg      #0f0f1a
    static Color TextPrimary   { get; }   // white
    static Color TextSecondary { get; }   // grey  #888888

    // ── Grid / cell ────────────────────────────────────────────────────────────
    static float GridPadding           { get; }   // padding inside GridContainer
    static float BorderRegionThickness { get; }   // thick border between regions
    static float BorderCellThickness   { get; }   // thin border between cells in same region

    // ── HUD ────────────────────────────────────────────────────────────────────
    static float HudHeightFraction     { get; }   // HUD strip height as fraction of screen height
}
```

**Usage pattern:**
```csharp
// CORRECT — read at runtime
float bt = DesignSystem.BorderRegionThickness;

// WRONG — stale after scene save
[SerializeField] float _borderWidth = DesignSystem.BorderRegionThickness;
```

---

## VictoryPanel.cs

Fullscreen victory overlay. Subscribes to `KingsGameManager.OnGameComplete`.

### SerializeFields (wired by KingsSceneBuilder)
```csharp
[SerializeField] private GameObject          _panel;           // VictoryContent GO
[SerializeField] private GameObject          _hud;             // hidden during victory
[SerializeField] private TextMeshProUGUI     _timeText;
[SerializeField] private TextMeshProUGUI     _moveCountText;
[SerializeField] private TextMeshProUGUI     _starRatingText;  // uses HUDIcons SDF for ★ glyph
[SerializeField] private Button              _nextLevelButton;
[SerializeField] private Button              _restartButton;
[SerializeField] private Button              _mainMenuButton;
[SerializeField] private KingsGameManager    _gameManager;
[SerializeField] private KingsSceneBootstrap _sceneBootstrap;
```

### Star Rating Formula

| Stars | Condition |
|---|---|
| ★★★ | No hints used AND within par time |
| ★★ | One condition met |
| ★ | Both conditions failed |

Par times per grid size: 4×4=60s, 5×5=90s, 6×6=120s, 8×8=180s, 10×10=300s.

Best star rating saved to `PlayerPrefs("Kings_Level_{N}_Stars")`. Never decremented.

### Animation

Phase 1 (0.25s): fade in + scale 0.85→1.0  
Phase 2 (0.45s): EaseOutBack bounce settle using `c3*(t-1)³ + c1*(t-1)²` formula.  
Uses `Time.unscaledDeltaTime` — plays even if game time is paused.

### Navigation
- **Next Level**: sets `PlayerPrefs("Kings_PendingLevel", currentLevel+1)` → loads `SampleScene`
- **Restart**: calls `_gameManager.RestartGame()`, hides panel
- **Main Menu**: loads `LevelSelect` scene

---

## UndoButton.cs

Manages undo button enabled/disabled state with CanvasGroup fade.

```csharp
[SerializeField] private KingsGameManager _gameManager;
[SerializeField] private Button           _button;
```

- Subscribes to `_gameManager.OnUndoStackChanged`.
- `SetState(hasUndo)`: `_button.interactable = hasUndo`, `_canvasGroup.alpha = hasUndo ? 1f : 0.4f`.
- Starts disabled (alpha=0.4) since undo stack is empty on game start.

---

## RestartButton.cs

Shows confirmation dialog before restarting. Only active if moves have been made.

```csharp
[SerializeField] private KingsGameManager _gameManager;
[SerializeField] private Button           _button;
```

- `ConfirmRestart()` / `CancelRestart()` are wired as persistent `onClick` listeners
  by `KingsSceneBuilder` (appear in Inspector like hand-set references).
- If `_gameManager.MoveCount == 0`, restart happens immediately without dialog.

---

## TipsButton.cs

Hint system UI with cooldown.

```csharp
[SerializeField] private KingsGameManager  _gameManager;
[SerializeField] private TextMeshProUGUI   _tipsText;     // panel showing hint strings
[SerializeField] private Button            _button;
```

- 10-second cooldown between hint requests.
- Calls `_gameManager.GetHints()` → displays up to 3 hint strings in `_tipsText`.
- Cooldown shown as a countdown in the button label.

---

## MenuButton.cs

Navigates to LevelSelect scene.

```csharp
// onClick → SceneManager.LoadScene("LevelSelect")
```

---

## LevelSelectController.cs

Master controller for the Level Select screen.

```csharp
[SerializeField] private LevelData[]       _allLevels;
[SerializeField] private Button[]          _tabButtons;      // length 3: Beginner/Expert/Impossible
[SerializeField] private Image[]           _progressFills;   // progress bar images
[SerializeField] private TextMeshProUGUI[] _progressTexts;   // "X / Y" text
[SerializeField] private Transform         _gridContent;     // scroll content for level buttons
[SerializeField] private GameObject        _levelButtonPrefab;
[SerializeField] private Button            _playButton;
```

- Builds difficulty pools at `Awake()`: splits `_allLevels` by `Difficulty` string into 3 arrays.
- `SwitchTab(int index)`: rebuilds level button grid for selected difficulty.
- Progress bar = (completed levels) / (total levels) in that difficulty.
- Tapping a level button → selects it. Tapping Play → sets `PlayerPrefs("Kings_PendingLevel")` → loads `SampleScene`.
- Reads `PlayerPrefs("Kings_Level_{N}_Stars")` to set button visual state (Locked/Available/Completed).

---

## LevelSelectButton.cs

Individual level cell in the Level Select grid.

```csharp
public void Init(LevelData data, int stars, bool isLocked, Action<int> onSelect);
```

- Shows level number, difficulty name, star rating.
- Visual states: Locked (dark, no interaction), Available (normal), Completed (accent color + stars).
- Calls `onSelect(levelNumber)` when tapped.

---

## MainMenuController.cs

Sets a solid `DesignSystem.Background` color on any menu scene background image.
