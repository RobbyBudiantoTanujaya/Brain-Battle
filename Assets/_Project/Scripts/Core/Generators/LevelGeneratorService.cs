using System.Collections.Generic;
using SystemRandom = System.Random;
using UnityEngine;
using BrainBattle.Kings;

namespace BrainBattle.Core.Generators
{
    public static class LevelGeneratorService
    {
        private const int MaxSeedBumps = 10000;

        // Deterministic: same (levelNumber, size, difficulty, seed) always produces the same LevelData.
        // Returns null if generation fails after MaxSeedBumps attempts (should never happen for size >= 4).
        public static LevelData GenerateLevel(int levelNumber, int size, string difficulty, int seed)
        {
            for (int bump = 0; bump < MaxSeedBumps; bump++)
            {
                int currentSeed = seed + bump;
                var rng = new SystemRandom(currentSeed);

                var queens = KingsNQueensSolver.Solve(size, rng);
                if (queens == null) continue;

                var regionMap = KingsRegionBuilder.Build(size, queens, rng);

                if (!KingsUniquenessVerifier.Verify(size, regionMap, queens, rng)) continue;

                return BuildLevelData(levelNumber, size, difficulty, queens, regionMap);
            }

            Debug.LogError($"[LevelGeneratorService] Failed to generate unique level " +
                           $"(size={size}, seed={seed}) after {MaxSeedBumps} attempts.");
            return null;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static LevelData BuildLevelData(int levelNumber, int size, string difficulty,
                                                Vector2Int[] queens, int[,] regionMap)
        {
            var regionDefs = BuildRegionDefinitions(size, queens, regionMap);

            var levelData = ScriptableObject.CreateInstance<LevelData>();
            levelData.EditorInit(levelNumber, difficulty, size, regionDefs, queens);
            return levelData;
        }

        private static LevelData.RegionDefinition[] BuildRegionDefinitions(int size,
                                                                            Vector2Int[] queens,
                                                                            int[,] regionMap)
        {
            var cellsPerRegion = new List<Vector2Int>[size];
            for (int i = 0; i < size; i++) cellsPerRegion[i] = new List<Vector2Int>();

            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    cellsPerRegion[regionMap[r, c]].Add(new Vector2Int(c, r));

            var defs = new LevelData.RegionDefinition[size];
            for (int i = 0; i < size; i++)
            {
                var def = new LevelData.RegionDefinition();
                def.Init(i, Color.HSVToRGB(i / (float)size, 0.45f, 0.85f), cellsPerRegion[i].ToArray());
                defs[i] = def;
            }
            return defs;
        }
    }
}
