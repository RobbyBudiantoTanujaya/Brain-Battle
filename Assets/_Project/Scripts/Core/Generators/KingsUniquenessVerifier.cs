using System;
using System.Collections.Generic;
using SystemRandom = System.Random;
using UnityEngine;

namespace BrainBattle.Core.Generators
{
    public static class KingsUniquenessVerifier
    {
        private static readonly (int dr, int dc)[] Dirs4 = { (-1, 0), (1, 0), (0, -1), (0, 1) };

        // Verifies the regionMap has exactly one valid solution.
        // Mutates regionMap in-place (border cell swaps) until unique or retries exhausted.
        // Returns true if unique solution achieved; false means caller should bump seed.
        public static bool Verify(int n, int[,] regionMap, Vector2Int[] queens, SystemRandom rng)
        {
            int maxRetries = Math.Max(50, n * n * 2);
            int minSwaps   = Math.Max(3, n / 2);
            int maxSwaps   = Math.Max(6, n);

            var queenAt = BuildQueenLookup(n, queens);

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                if (CountSolutions(n, regionMap) == 1) return true;
                int swaps = rng.Next(minSwaps, maxSwaps + 1);
                for (int s = 0; s < swaps; s++)
                    MutateBorder(n, regionMap, queenAt, rng);
            }
            return CountSolutions(n, regionMap) == 1;
        }

        private static bool[,] BuildQueenLookup(int n, Vector2Int[] queens)
        {
            var lookup = new bool[n, n];
            for (int i = 0; i < n; i++)
                lookup[queens[i].y, queens[i].x] = true;
            return lookup;
        }

        // ── Solution counter ─────────────────────────────────────────────────────

        private static int CountSolutions(int n, int[,] regionMap)
        {
            int count = 0;
            Backtrack(n, regionMap, 0, new bool[n], new bool[n], new int[n], ref count);
            return count;
        }

        private static void Backtrack(int n, int[,] regionMap, int row,
                                      bool[] usedCols, bool[] usedRegions,
                                      int[] placedCols, ref int count)
        {
            if (count > 1) return;
            if (row == n) { count++; return; }

            for (int col = 0; col < n; col++)
            {
                if (usedCols[col]) continue;
                int region = regionMap[row, col];
                if (usedRegions[region]) continue;
                if (row > 0 && Math.Abs(placedCols[row - 1] - col) <= 1) continue;

                usedCols[col] = usedRegions[region] = true;
                placedCols[row] = col;
                Backtrack(n, regionMap, row + 1, usedCols, usedRegions, placedCols, ref count);
                usedCols[col] = usedRegions[region] = false;
            }
        }

        // ── Optimized border mutation via articulation points ─────────────────────
        //
        // Previous O(N⁴): scan N² cells × BFS connectivity check per candidate = O(N⁴).
        // New O(N²): one DFS pass to find all articulation points, then linear candidate scan.
        //
        // A cell is an articulation point (AP) if removing it disconnects its region.
        // Non-AP border cells are always safe to reassign to an adjacent region.

        private static void MutateBorder(int n, int[,] regionMap, bool[,] queenAt, SystemRandom rng)
        {
            // Precompute region sizes — skip moves that would leave a 1-cell region.
            var regionSize = new int[n];
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    regionSize[regionMap[r, c]]++;

            // One DFS pass over all cells: O(N²) total across all regions.
            var isAP = ComputeArticulationPoints(n, regionMap);

            var candidates = new List<(int row, int col, int toRegion)>();

            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    if (queenAt[r, c]) continue;               // Never move queen cells
                    if (isAP[r, c]) continue;                  // Removing would disconnect region
                    int fromRegion = regionMap[r, c];
                    if (regionSize[fromRegion] <= 2) continue; // Would leave a 1-cell region

                    foreach (var (dr, dc) in Dirs4)
                    {
                        int nr = r + dr, nc = c + dc;
                        if ((uint)nr >= (uint)n || (uint)nc >= (uint)n) continue;
                        int toRegion = regionMap[nr, nc];
                        if (toRegion != fromRegion)
                            candidates.Add((r, c, toRegion));
                    }
                }
            }

            if (candidates.Count == 0) return;
            var pick = candidates[rng.Next(candidates.Count)];
            regionMap[pick.row, pick.col] = pick.toRegion;
        }

        // ── Tarjan's articulation-point algorithm ─────────────────────────────────
        //
        // Runs one DFS per connected component (= one per region, since regions are connected).
        // Total cost: O(N²) — each cell visited exactly once across all DFS trees.
        //
        // isAP[r,c] = true  →  removing (r,c) from its region disconnects that region.
        // isAP[r,c] = false →  safe to reassign (region stays connected).

        private static bool[,] ComputeArticulationPoints(int n, int[,] regionMap)
        {
            var isAP    = new bool[n, n];
            var disc    = new int[n, n];
            var low     = new int[n, n];
            var visited = new bool[n, n];
            int timer   = 0;

            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    disc[r, c] = -1;

            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    if (!visited[r, c])
                        DfsAP(n, regionMap, r, c, -1, -1, disc, low, visited, isAP, ref timer);

            return isAP;
        }

        private static void DfsAP(int n, int[,] regionMap, int r, int c, int pr, int pc,
                                   int[,] disc, int[,] low, bool[,] visited,
                                   bool[,] isAP, ref int timer)
        {
            visited[r, c] = true;
            disc[r, c] = low[r, c] = timer++;
            int children = 0;
            int regionId = regionMap[r, c];

            foreach (var (dr, dc) in Dirs4)
            {
                int nr = r + dr, nc = c + dc;
                if ((uint)nr >= (uint)n || (uint)nc >= (uint)n) continue;
                if (regionMap[nr, nc] != regionId) continue; // Only traverse same-region edges

                if (!visited[nr, nc])
                {
                    children++;
                    DfsAP(n, regionMap, nr, nc, r, c, disc, low, visited, isAP, ref timer);
                    low[r, c] = Math.Min(low[r, c], low[nr, nc]);

                    // Root of DFS tree: AP if it has 2+ independent subtrees.
                    if (pr == -1 && children > 1) isAP[r, c] = true;
                    // Non-root: AP if no back-edge from its subtree reaches above r,c.
                    if (pr != -1 && low[nr, nc] >= disc[r, c]) isAP[r, c] = true;
                }
                else if (nr != pr || nc != pc) // Back edge (not the tree edge to parent)
                {
                    low[r, c] = Math.Min(low[r, c], disc[nr, nc]);
                }
            }
        }
    }
}
