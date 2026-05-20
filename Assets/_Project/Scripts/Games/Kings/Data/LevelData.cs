using System;
using UnityEngine;

namespace BrainBattle.Kings
{
    [CreateAssetMenu(fileName = "KingsLevel", menuName = "BrainBattle/Kings/Level")]
    public sealed class LevelData : ScriptableObject
    {
        [SerializeField] private int                 _levelNumber;
        [SerializeField] private string              _difficulty;
        [SerializeField] private int                 _gridSize;
        [SerializeField] private RegionDefinition[]  _regions;
        [SerializeField] private Vector2Int[]        _solution;

        public int                LevelNumber => _levelNumber;
        public string             Difficulty  => _difficulty;
        public int                GridSize    => _gridSize;
        public RegionDefinition[] Regions     => _regions;
        public Vector2Int[]       Solution    => _solution;

        [Serializable]
        public class RegionDefinition
        {
            [SerializeField] private int          _regionId;
            [SerializeField] private Color        _color;
            [SerializeField] private Vector2Int[] _cells;

            public int          RegionId => _regionId;
            public Color        Color    => _color;
            public Vector2Int[] Cells    => _cells;

            public void Init(int regionId, Color color, Vector2Int[] cells)
            {
                _regionId = regionId;
                _color    = color;
                _cells    = cells;
            }
        }

        public void EditorInit(int levelNumber, string difficulty, int gridSize,
                               RegionDefinition[] regions, Vector2Int[] solution)
        {
            _levelNumber = levelNumber;
            _difficulty  = difficulty;
            _gridSize    = gridSize;
            _regions     = regions;
            _solution    = solution;
        }
    }
}
