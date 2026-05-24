using System.Collections.Generic;
using UnityEngine;
using BrainBattle.Core.Models;

namespace BrainBattle.Kings
{
    public sealed class LevelLoader : MonoBehaviour
    {
#if UNITY_EDITOR
        private const string LevelsAssetPath = "Assets/_Project/ScriptableObjects/Kings/Levels";
#endif

        [SerializeField] private LevelData[] _allLevels;

        private void Awake()
        {
            // Purge null slots that appear when level assets are deleted and regenerated.
            // A non-empty array full of nulls must still trigger auto-load.
            if (_allLevels != null)
            {
                var valid = new List<LevelData>(_allLevels.Length);
                foreach (var l in _allLevels)
                    if (l != null) valid.Add(l);
                _allLevels = valid.ToArray();
            }

            if (_allLevels != null && _allLevels.Length > 0) return;

#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:LevelData", new[] { LevelsAssetPath });
            var list  = new List<LevelData>(guids.Length);
            foreach (var guid in guids)
            {
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>(
                                UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) list.Add(asset);
            }
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            _allLevels = list.ToArray();
            Debug.Log($"[LevelLoader] Auto-loaded {_allLevels.Length} levels from {LevelsAssetPath}.");
#else
            Debug.LogError("[LevelLoader] _allLevels is empty. Assign level assets in the Inspector before building.");
#endif
        }

        public LevelData GetLevel(int levelNumber)
        {
            if (_allLevels == null) return null;
            foreach (var level in _allLevels)
                if (level != null && level.LevelNumber == levelNumber)
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
