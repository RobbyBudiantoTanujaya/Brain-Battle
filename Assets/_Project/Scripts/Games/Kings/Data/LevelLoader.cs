using UnityEngine;
using BrainBattle.Core.Models;

namespace BrainBattle.Kings
{
    public sealed class LevelLoader : MonoBehaviour
    {
        [SerializeField] private LevelData[] _allLevels;

        public LevelData GetLevel(int levelNumber)
        {
            foreach (var level in _allLevels)
                if (level.LevelNumber == levelNumber)
                    return level;
            return null;
        }

        public GridData BuildGridFromLevel(LevelData level)
        {
            var grid = new GridData(level.GridSize);

            foreach (var region in level.Regions)
            {
                var regionData = new RegionData(region.RegionId, region.Color);
                foreach (var cell in region.Cells)
                {
                    regionData.AddCell(cell);
                    grid.GetCell(cell.y, cell.x).RegionId = region.RegionId;
                }
                grid.Regions.Add(regionData);
            }

            return grid;
        }
    }
}
