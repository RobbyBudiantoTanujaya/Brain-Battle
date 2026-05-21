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
        private const int    MaxUndoHistory = 50;
        private const string SaveKeyGrid    = "Kings_Grid";
        private const string SaveKeyTime    = "Kings_Time";
        private const string SaveKeyMoves   = "Kings_Moves";

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

        /// <summary>Total CycleState calls made this session. Not decremented by undo.</summary>
        public int MoveCount       => _moveCount;
        public int CurrentGridSize => _currentGrid?.Size ?? 0;
        public int HintsUsed       { get; private set; }

        // ── Private state ─────────────────────────────────────────────────────────

        private readonly Stack<GridData> _undoStack = new();

        private GridData _initialGrid;
        private int      _moveCount;
        private bool     _gameActive;

        // Timer: _timerOffset accumulates time from paused periods.
        private float _timerOffset;
        private float _timerStartUnscaled;
        private bool  _timerActive;

        // ── MonoBehaviour ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (_gridRenderer != null)
                _gridRenderer.OnCellTapped += OnCellTapped;
            if (_tutorialController != null)
                _tutorialController.OnTutorialComplete += OnTutorialCompleted;
        }

        private void OnDisable()
        {
            if (_gridRenderer != null)
                _gridRenderer.OnCellTapped -= OnCellTapped;
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

            _initialGrid = DeepCopy(grid);
            _currentGrid = DeepCopy(grid);

            _undoStack.Clear();
            _moveCount  = 0;
            HintsUsed   = 0;
            _gameActive = true;

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
            _moveCount  = 0;
            HintsUsed   = 0;
            _gameActive = true;

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
            Debug.Log($"[KingsGameManager] GameManager received tap: ({row}, {col})");
            if (!_gameActive || _currentGrid == null || _gridRenderer == null) return;

            PushUndoSnapshot();
            FireUndoStackChanged();

            _currentGrid.CycleState(row, col);
            _moveCount++;

            CellState newState = _currentGrid.GetCell(row, col).State;
            _gridRenderer.UpdateCell(row, col, newState);

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
