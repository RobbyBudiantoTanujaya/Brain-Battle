using System;
using UnityEngine;

namespace BrainBattle.Core.Models
{
    [Serializable]
    public sealed class CellData
    {
        [SerializeField] private int       _row;
        [SerializeField] private int       _col;
        [SerializeField] private CellState _state;
        [SerializeField] private int       _regionId;

        public int       Row      => _row;
        public int       Col      => _col;
        public CellState State    { get => _state;    set => _state    = value; }
        public int       RegionId { get => _regionId; set => _regionId = value; }

        public CellData(int row, int col, int regionId = -1)
        {
            _row      = row;
            _col      = col;
            _regionId = regionId;
            _state    = CellState.Empty;
        }
    }
}
