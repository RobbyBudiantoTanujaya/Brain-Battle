using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainBattle.Core.Models
{
    /// <summary>
    /// Runtime grid model. Unity cannot serialize 2D arrays, so cells are flattened
    /// via ISerializationCallbackReceiver and reconstructed into Cells[,] at runtime.
    /// </summary>
    [Serializable]
    public sealed class GridData : ISerializationCallbackReceiver
    {
        [SerializeField] private int              _size;
        [SerializeField] private List<CellData>   _serializedCells = new();
        [SerializeField] private List<RegionData> _regions         = new();

        public int              Size    => _size;
        public CellData[,]      Cells   { get; private set; }
        public List<RegionData> Regions => _regions;

        public GridData(int size)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Grid size must be greater than zero.");

            _size    = size;
            _regions = new List<RegionData>();
            RebuildCells();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public CellData GetCell(int row, int col)
        {
            ValidateBounds(row, col);
            return Cells[row, col];
        }

        public void SetCellState(int row, int col, CellState state)
        {
            ValidateBounds(row, col);
            Cells[row, col].State = state;
        }

        public List<Vector2Int> GetCrownPositions()
        {
            var positions = new List<Vector2Int>(_size);
            for (int r = 0; r < _size; r++)
                for (int c = 0; c < _size; c++)
                    if (Cells[r, c].State == CellState.Crown)
                        positions.Add(new Vector2Int(c, r));
            return positions;
        }

        public void CycleState(int row, int col)
        {
            ValidateBounds(row, col);
            Cells[row, col].State = Cells[row, col].State switch
            {
                CellState.Empty => CellState.Dot,
                CellState.Dot   => CellState.Crown,
                CellState.Crown => CellState.Empty,
                _               => CellState.Empty
            };
        }

        // ── ISerializationCallbackReceiver ───────────────────────────────────────

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // Unity's serializer can reconstruct [Serializable] objects without running
            // constructors or field initializers, so _serializedCells may be null here.
            _serializedCells ??= new List<CellData>();
            _serializedCells.Clear();

            if (Cells == null) return;

            for (int r = 0; r < _size; r++)
                for (int c = 0; c < _size; c++)
                    _serializedCells.Add(Cells[r, c]);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Cells = new CellData[_size, _size];
            if (_serializedCells == null) return;

            foreach (var cell in _serializedCells)
            {
                if (cell == null) continue;
                if ((uint)cell.Row >= (uint)_size || (uint)cell.Col >= (uint)_size) continue;
                Cells[cell.Row, cell.Col] = cell;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void RebuildCells()
        {
            Cells = new CellData[_size, _size];
            for (int r = 0; r < _size; r++)
                for (int c = 0; c < _size; c++)
                    Cells[r, c] = new CellData(r, c);
        }

        private void ValidateBounds(int row, int col)
        {
            if ((uint)row >= (uint)_size)
                throw new ArgumentOutOfRangeException(nameof(row), $"Row {row} is out of range for grid size {_size}.");
            if ((uint)col >= (uint)_size)
                throw new ArgumentOutOfRangeException(nameof(col), $"Col {col} is out of range for grid size {_size}.");
        }
    }
}
