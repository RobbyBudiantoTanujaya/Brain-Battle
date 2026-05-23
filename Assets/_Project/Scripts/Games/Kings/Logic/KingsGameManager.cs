using System;
using System.Collections.Generic;
using UnityEngine;
using BrainBattle.Core.Engine;
using BrainBattle.Core.Models;
using BrainBattle.Games.Kings.UI;
using BrainBattle.Kings;

namespace BrainBattle.Games.Kings.Logic
{
    public sealed class KingsGameManager : MonoBehaviour
    {
        private const int    MaxUndoHistory    = 50;
        private const string SaveKeyGrid      = "Kings_Grid";
        private const string SaveKeyTime      = "Kings_Time";
        private const string SaveKeyMoves     = "Kings_Moves";
        private const float  DoubleTapWindow  = 0.3f;

        [SerializeField] private KingsGridRenderer  _gridRenderer;
        [SerializeField] private GridData           _currentGrid;
        [SerializeField] private TutorialController _tutorialController;

        // ── Events ────────────────────────────────────────────────────────────────

        public event Action                   OnWin;
        public event Action<float, int>       OnGameComplete;
        public event Action<List<Vector2Int>> OnConflictDetected;
        // Fired after every move, undo, start, and restart. bool = undo stack is non-empty.
        public event Action<bool>             OnUndoStackChanged;

        // ── Public properties ─────────────────────────────────────────────────────

        /// <summary>Seconds elapsed since StartGame, paused when app is backgrounded or game ends.</summary>
        public float ElapsedSeconds => _timerActive
            ? _timerOffset + (Time.unscaledTime - _timerStartUnscaled)
            : _timerOffset;

        /// <summary>Total cell-state changes made this session. Not decremented by undo.</summary>
        public int  MoveCount       => _moveCount;
        public int  CurrentGridSize => _currentGrid?.Size ?? 0;
        public int  HintsUsed       { get; private set; }

        // ── Private state ─────────────────────────────────────────────────────────

        private readonly Stack<GridData> _undoStack = new();

        // Key = crown position (x=col, y=row), Value = dot positions auto-placed by that crown.
        private readonly Dictionary<Vector2Int, HashSet<Vector2Int>> _autoPlacedDots = new();

        private GridData _initialGrid;
        private int      _moveCount;
        private bool     _gameActive;

        // Timer: _timerOffset accumulates time from paused periods.
        private float _timerOffset;
        private float _timerStartUnscaled;
        private bool  _timerActive;

        // Double-tap detection: track the last single tap per (row, col).
        private float      _lastTapTime = float.MinValue;
        private Vector2Int _lastTapCell = new(-1, -1);

        // ── MonoBehaviour ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (_gridRenderer != null)
            {
                _gridRenderer.OnCellTapped      += OnCellTapped;
                _gridRenderer.OnCellDragEntered += OnCellDragEntered;
            }
            if (_tutorialController != null)
                _tutorialController.OnTutorialComplete += OnTutorialCompleted;
        }

        private void OnDisable()
        {
            if (_gridRenderer != null)
            {
                _gridRenderer.OnCellTapped      -= OnCellTapped;
                _gridRenderer.OnCellDragEntered -= OnCellDragEntered;
            }
            if (_tutorialController != null)
                _tutorialController.OnTutorialComplete -= OnTutorialCompleted;
        }

        private void OnApplicationPause(bool paused)
        {
            if (!_gameActive) return;

            if (paused)
            {
                _timerOffset       += Time.unscaledTime - _timerStartUnscaled;
                _timerActive        = false;
            }
            else
            {
                _timerStartUnscaled = Time.unscaledTime;
                _timerActive        = true;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public void StartGame(GridData grid)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            Debug.Log($"[KingsGameManager] StartGame — gridSize={grid.Size}, gridRenderer={(object)_gridRenderer ?? (object)"NULL"}");
            if (_gridRenderer == null)
            {
                Debug.LogError("[KingsGameManager] _gridRenderer is null — check SerializeField wiring on KingsGameManager.");
                return;
            }

            _initialGrid = DeepCopy(grid);
            _currentGrid = DeepCopy(grid);

            _undoStack.Clear();
            _autoPlacedDots.Clear();
            _moveCount    = 0;
            HintsUsed     = 0;
            _gameActive   = true;
            _lastTapTime  = float.MinValue;
            _lastTapCell  = new(-1, -1);

            ResetTimer();

            _gridRenderer.ClearConflicts();
            _gridRenderer.RenderGrid(_currentGrid);
            FireUndoStackChanged();

            OnConflictDetected -= _gridRenderer.HighlightConflicts;
            OnConflictDetected += _gridRenderer.HighlightConflicts;
            _tutorialController?.ShowTutorial();
        }

        public void DoUndo()
        {
            if (_undoStack.Count == 0 || !_gameActive) return;

            _currentGrid = _undoStack.Pop();
            _autoPlacedDots.Clear();
            _lastTapTime = float.MinValue;
            _lastTapCell = new(-1, -1);

            _gridRenderer.ClearConflicts();
            _gridRenderer.RenderGrid(_currentGrid);

            AutoSave();
            FireUndoStackChanged();
        }

        public void RestartGame()
        {
            if (_initialGrid == null) return;

            _currentGrid = DeepCopy(_initialGrid);
            _undoStack.Clear();
            _autoPlacedDots.Clear();
            _moveCount   = 0;
            HintsUsed    = 0;
            _gameActive  = true;
            _lastTapTime = float.MinValue;
            _lastTapCell = new(-1, -1);

            ResetTimer();

            _gridRenderer.ClearConflicts();
            _gridRenderer.RenderGrid(_currentGrid);

            AutoSave();
            FireUndoStackChanged();
        }

        /// <summary>
        /// Returns up to 3 hints. A hint fires when a row, column, or region has exactly
        /// one cell where a crown can currently be placed without violating any constraint.
        /// Positions in returned hints use the project convention: Vector2Int(col, row).
        /// </summary>
        public List<string> GetHints()
        {
            var hints = GetHints(_currentGrid);
            if (hints.Count > 0) HintsUsed++;
            return hints;
        }

        public List<string> GetHints(GridData grid)
        {
            if (grid == null) return new List<string>(0);

            var hints = new List<string>(3);
            int size  = grid.Size;

            for (int r = 0; r < size && hints.Count < 3; r++)
            {
                if (RowHasCrown(grid, r)) continue;
                if (GetRowCandidates(grid, r).Count == 1)
                    hints.Add($"Row {r + 1} has only one valid crown position.");
            }

            for (int c = 0; c < size && hints.Count < 3; c++)
            {
                if (ColHasCrown(grid, c)) continue;
                if (GetColCandidates(grid, c).Count == 1)
                    hints.Add($"Column {c + 1} has only one valid crown position.");
            }

            foreach (var region in grid.Regions)
            {
                if (hints.Count >= 3) break;
                if (RegionHasCrown(grid, region)) continue;

                var candidates = GetRegionCandidates(grid, region);
                if (candidates.Count == 1)
                {
                    var pos = candidates[0]; // x=col, y=row
                    hints.Add($"A color region has only one valid crown position " +
                              $"(row {pos.y + 1}, column {pos.x + 1}).");
                }
            }

            return hints;
        }

        // ── Private: game flow ────────────────────────────────────────────────────

        private void OnCellTapped(int row, int col)
        {
            if (!_gameActive || _currentGrid == null || _gridRenderer == null) return;
            if (IsAutoPlacedDot(row, col)) return;

            CellState currentState = _currentGrid.GetCell(row, col).State;
            CellState newState;

            if (currentState == CellState.Crown)
            {
                // Tap on Crown always clears it.
                newState     = CellState.Empty;
                _lastTapTime = Time.unscaledTime;
                _lastTapCell = new Vector2Int(col, row);
            }
            else
            {
                var   thisCell       = new Vector2Int(col, row);
                float timeSinceLast  = Time.unscaledTime - _lastTapTime;
                bool  isDoubleTap    = timeSinceLast < DoubleTapWindow && thisCell == _lastTapCell;

                if (isDoubleTap)
                {
                    newState     = CellState.Crown;
                    // Reset so a rapid third tap is treated as a new single tap.
                    _lastTapTime = float.MinValue;
                    _lastTapCell = new(-1, -1);
                }
                else
                {
                    newState     = currentState == CellState.Dot ? CellState.Empty : CellState.Dot;
                    _lastTapTime = Time.unscaledTime;
                    _lastTapCell = thisCell;
                }
            }

            PushUndoSnapshot();
            FireUndoStackChanged();

            _currentGrid.SetCellState(row, col, newState);
            _moveCount++;
            _gridRenderer.UpdateCell(row, col, newState);

            if (currentState == CellState.Crown)
                RemoveAutoX(row, col);
            else if (newState == CellState.Crown)
                ApplyAutoX(row, col);

            FinishMove(row, col, newState);
        }

        private void OnCellDragEntered(int row, int col, CellState targetState)
        {
            if (!_gameActive || _currentGrid == null || _gridRenderer == null) return;
            if (IsAutoPlacedDot(row, col)) return;
            CellState current = _currentGrid.GetCell(row, col).State;
            if (current == targetState) return;
            // Never overwrite a Crown via drag; crowns require an explicit double-tap.
            if (current == CellState.Crown) return;

            _currentGrid.SetCellState(row, col, targetState);
            _moveCount++;
            _gridRenderer.UpdateCell(row, col, targetState);

            if (targetState == CellState.Crown)
                ApplyAutoX(row, col);

            FinishMove(row, col, targetState);
        }

        private void FinishMove(int row, int col, CellState newState)
        {
            AutoSave();

            var result = ConstraintValidator.ValidateMove(_currentGrid, row, col, newState);
            if (!result.IsValid)
            {
                OnConflictDetected?.Invoke(result.ConflictPositions);
                return;
            }

            _gridRenderer.ClearConflicts();

            if (ConstraintValidator.CheckWin(_currentGrid))
                HandleWin();
        }

        private void HandleWin()
        {
            // Snapshot final time before stopping the timer.
            _timerOffset = ElapsedSeconds;
            _timerActive = false;
            _gameActive  = false;

            OnWin?.Invoke();
            OnGameComplete?.Invoke(_timerOffset, _moveCount);

            PlayerPrefs.DeleteKey(SaveKeyGrid);
            PlayerPrefs.DeleteKey(SaveKeyTime);
            PlayerPrefs.DeleteKey(SaveKeyMoves);
            PlayerPrefs.Save();
        }

        private void FireUndoStackChanged() => OnUndoStackChanged?.Invoke(_undoStack.Count > 0);
        private void OnTutorialCompleted() { }

        // ── Private: Auto-X ───────────────────────────────────────────────────────

        private void ApplyAutoX(int crownRow, int crownCol)
        {
            // Convention throughout: Vector2Int stores (x=col, y=row).
            // TryAutoPlaceDot(row, col) — row first, matching GetCell(row, col).
            // RegionData.Cells entries are Vector2Int(col, row) as stored by LevelLoader.
            // This was verified against LevelGeneratorService (new Vector2Int(c, r)) and
            // LevelLoader.BuildGridFromLevel (grid.GetCell(cell.y, cell.x)) — no off-by-one.
            var crownPos = new Vector2Int(crownCol, crownRow);
            var autoSet  = new HashSet<Vector2Int>();
            int size     = _currentGrid.Size;
            int regionId = _currentGrid.GetCell(crownRow, crownCol).RegionId;

            // Fill entire row (all columns except crown column).
            for (int c = 0; c < size; c++)
                if (c != crownCol) TryAutoPlaceDot(crownRow, c, autoSet);

            // Fill entire column (all rows except crown row).
            for (int r = 0; r < size; r++)
                if (r != crownRow) TryAutoPlaceDot(r, crownCol, autoSet);

            // Fill all other cells in the same region.
            foreach (var region in _currentGrid.Regions)
            {
                if (region.RegionId != regionId) continue;
                foreach (var pos in region.Cells) // pos.x = col, pos.y = row
                    if (pos.x != crownCol || pos.y != crownRow)
                        TryAutoPlaceDot(pos.y, pos.x, autoSet);
                break;
            }

            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    int nr = crownRow + dr, nc = crownCol + dc;
                    if ((uint)nr < (uint)size && (uint)nc < (uint)size)
                        TryAutoPlaceDot(nr, nc, autoSet);
                }

            _autoPlacedDots[crownPos] = autoSet;
        }

        private void TryAutoPlaceDot(int row, int col, HashSet<Vector2Int> autoSet)
        {
            if (_currentGrid.GetCell(row, col).State != CellState.Empty) return;
            _currentGrid.SetCellState(row, col, CellState.Dot);
            _gridRenderer.UpdateCell(row, col, CellState.Dot);
            autoSet.Add(new Vector2Int(col, row));
        }

        private void RemoveAutoX(int crownRow, int crownCol)
        {
            var crownPos = new Vector2Int(crownCol, crownRow);
            if (!_autoPlacedDots.TryGetValue(crownPos, out var autoSet)) return;

            foreach (var dotPos in autoSet)
            {
                // Keep the dot if another crown also auto-placed it.
                bool coveredByOther = false;
                foreach (var kv in _autoPlacedDots)
                {
                    if (kv.Key == crownPos) continue;
                    if (kv.Value.Contains(dotPos)) { coveredByOther = true; break; }
                }

                if (!coveredByOther && _currentGrid.GetCell(dotPos.y, dotPos.x).State == CellState.Dot)
                {
                    _currentGrid.SetCellState(dotPos.y, dotPos.x, CellState.Empty);
                    _gridRenderer.UpdateCell(dotPos.y, dotPos.x, CellState.Empty);
                }
            }

            _autoPlacedDots.Remove(crownPos);
        }

        private bool IsAutoPlacedDot(int row, int col)
        {
            var pos = new Vector2Int(col, row);
            foreach (var set in _autoPlacedDots.Values)
                if (set.Contains(pos)) return true;
            return false;
        }

        // ── Private: undo ─────────────────────────────────────────────────────────

        private void PushUndoSnapshot()
        {
            if (_undoStack.Count >= MaxUndoHistory)
                TrimUndoStack();

            _undoStack.Push(DeepCopy(_currentGrid));
        }

        private void TrimUndoStack()
        {
            // Stack<T>.ToArray() returns elements in pop order: index 0 = top (newest).
            // Rebuild the stack without the oldest entry (index Length-1).
            var entries = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = entries.Length - 2; i >= 0; i--)
                _undoStack.Push(entries[i]);
        }

        // ── Private: timer ────────────────────────────────────────────────────────

        private void ResetTimer()
        {
            _timerOffset        = 0f;
            _timerStartUnscaled = Time.unscaledTime;
            _timerActive        = true;
        }

        // ── Private: persistence ──────────────────────────────────────────────────

        private void AutoSave()
        {
            PlayerPrefs.SetString(SaveKeyGrid,  JsonUtility.ToJson(_currentGrid));
            PlayerPrefs.SetFloat(SaveKeyTime,   ElapsedSeconds);
            PlayerPrefs.SetInt(SaveKeyMoves,    _moveCount);
            PlayerPrefs.Save();
        }

        // ── Private: deep copy ────────────────────────────────────────────────────

        // Uses JsonUtility round-trip so ISerializationCallbackReceiver fires and
        // Cells[,] is correctly rebuilt from _serializedCells on the copy.
        private static GridData DeepCopy(GridData source)
        {
            var copy = new GridData(source.Size);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), copy);
            return copy;
        }

        // ── Private: hint helpers ─────────────────────────────────────────────────

        private static bool RowHasCrown(GridData grid, int row)
        {
            for (int c = 0; c < grid.Size; c++)
                if (grid.GetCell(row, c).State == CellState.Crown) return true;
            return false;
        }

        private static bool ColHasCrown(GridData grid, int col)
        {
            for (int r = 0; r < grid.Size; r++)
                if (grid.GetCell(r, col).State == CellState.Crown) return true;
            return false;
        }

        private static bool RegionHasCrown(GridData grid, RegionData region)
        {
            foreach (var pos in region.Cells) // pos: x=col, y=row
                if (grid.GetCell(pos.y, pos.x).State == CellState.Crown) return true;
            return false;
        }

        private static List<Vector2Int> GetRowCandidates(GridData grid, int row)
        {
            var candidates = new List<Vector2Int>();
            for (int c = 0; c < grid.Size; c++)
            {
                if (grid.GetCell(row, c).State == CellState.Crown) continue;
                if (ConstraintValidator.ValidateMove(grid, row, c, CellState.Crown).IsValid)
                    candidates.Add(new Vector2Int(c, row));
            }
            return candidates;
        }

        private static List<Vector2Int> GetColCandidates(GridData grid, int col)
        {
            var candidates = new List<Vector2Int>();
            for (int r = 0; r < grid.Size; r++)
            {
                if (grid.GetCell(r, col).State == CellState.Crown) continue;
                if (ConstraintValidator.ValidateMove(grid, r, col, CellState.Crown).IsValid)
                    candidates.Add(new Vector2Int(col, r));
            }
            return candidates;
        }

        private static List<Vector2Int> GetRegionCandidates(GridData grid, RegionData region)
        {
            var candidates = new List<Vector2Int>();
            foreach (var pos in region.Cells) // pos: x=col, y=row
            {
                int r = pos.y, c = pos.x;
                if (grid.GetCell(r, c).State == CellState.Crown) continue;
                if (ConstraintValidator.ValidateMove(grid, r, c, CellState.Crown).IsValid)
                    candidates.Add(pos);
            }
            return candidates;
        }
    }
}
