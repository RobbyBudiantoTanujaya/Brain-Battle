using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainBattle.Core.Models
{
    [Serializable]
    public sealed class RegionData
    {
        [SerializeField] private int            _regionId;
        [SerializeField] private Color          _regionColor;
        [SerializeField] private List<Vector2Int> _cells;

        public int             RegionId    => _regionId;
        public Color           RegionColor { get => _regionColor; set => _regionColor = value; }
        public List<Vector2Int> Cells      => _cells;

        public RegionData(int regionId, Color color)
        {
            _regionId    = regionId;
            _regionColor = color;
            _cells       = new List<Vector2Int>();
        }

        public void AddCell(Vector2Int cell)
        {
            if (!_cells.Contains(cell))
                _cells.Add(cell);
        }

        public bool ContainsCell(Vector2Int cell) => _cells.Contains(cell);
    }
}
