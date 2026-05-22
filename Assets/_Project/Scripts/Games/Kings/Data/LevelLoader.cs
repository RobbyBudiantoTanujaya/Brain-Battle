using System.Collections.Generic;
using UnityEngine;
using BrainBattle.Core.Models;

namespace BrainBattle.Kings
{
    public sealed class LevelLoader : MonoBehaviour
    {
        [SerializeField] private LevelData[] _allLevels;

        private void Awake()
        {
            if (_allLevels != null && _allLevels.Length > 0) return;

#if UNITY_EDITOR
            // _allLevels not populated in Inspector — auto-discover in editor so Play works
            // without requiring a scene rebuild via BrainBattle > Build Kings Scene.
            var guids = UnityEditor.AssetDatabase.FindAssets("t:LevelData");
            var list  = new List<LevelData>(guids.Length);
            foreach (var guid in guids)
            {
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>(
                                UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) list.Add(asset);
            }
            list.Sort((a, b) => a.LevelNumber.CompareTo(b.LevelNumber));
            _allLevels = list.ToArray();
            Debug.Log($"[LevelLoader] Auto-loaded {_allLevels.Length} levels from AssetDatabase.");
#else
            Debug.LogError("[LevelLoader] _allLevels is empty. Assign level assets in the Inspector before building.");
#endif
        }

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
