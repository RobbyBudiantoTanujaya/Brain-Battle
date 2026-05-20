using System;
using System.Collections.Generic;
using SystemRandom = System.Random;
using UnityEngine;

namespace BrainBattle.Core.Generators
{
    public static class KingsUniquenessVerifier
    {
        private static readonly (int dr, int dc)[] Dirs4 = { (-1, 0), (1, 0), (0, -1), (0, 1) };
        private const int MaxRetries = 50;

        // Verifies the regionMap has exactly one valid solution.
        // Mutates regionMap in-place (border cell swaps) until unique or retries exhausted.
        // Returns true if unique solution achieved; false means caller should bump seed.
        public static bool Verify(int n, int[,] regionMap, Vector2Int[] queens, SystemRandom rng)
        {
            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                if (CountSolutions(n, regionMap) == 1) return true;
                // Spec: swap 3-5 border cells per retry for meaningful perturbation on large boards
                int swaps = rng.Next(3, 6);
                for (int s = 0; s < swaps; s++)
                    MutateBorder(n, regionMap, queens, rng);
            }
            return CountSolutions(n, regionMap) == 1;
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
            if (count > 1) return; // Early exit: uniqueness check only needs to know >1

            if (row == n)
            {
                count++;
                return;
            }

            for (int col = 0; col < n; col++)
            {
                if (usedCols[col]) continue;
                int region = regionMap[row, col];
                if (usedRegions[region]) continue;
                if (row > 0 && Math.Abs(placedCols[row - 1] - col) <= 1) continue;

                usedCols[col]      = true;
                usedRegions[region] = true;
                placedCols[row]    = col;

                Backtrack(n, regionMap, row + 1, usedCols, usedRegions, placedCols, ref count);

                usedCols[col]       = false;
                usedRegions[region] = false;
            }
        }

        // ── Border mutation ───────────────────────────────────────────────────────

        private static void MutateBorder(int n, int[,] regionMap, Vector2Int[] queens, SystemRandom rng)
        {
            var candidates = new List<(int row, int col, int toRegion)>();

            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    int fromRegion = regionMap[r, c];
                    // Never move a queen's own cell
                    if (queens[fromRegion].y == r && queens[fromRegion].x == c) continue;

                    foreach (var (dr, dc) in Dirs4)
                    {
                        int nr = r + dr, nc = c + dc;
                        if ((uint)nr >= (uint)n || (uint)nc >= (uint)n) continue;
                        int toRegion = regionMap[nr, nc];
                        if (toRegion == fromRegion) continue;

                        // Moving (r,c) away from fromRegion must keep fromRegion connected.
                        // The toRegion always stays connected when gaining a cell.
                        if (IsConnectedWithout(n, regionMap, fromRegion, queens[fromRegion],
                                               new Vector2Int(c, r)))
                        {
                            candidates.Add((r, c, toRegion));
                        }
                    }
                }
            }

            if (candidates.Count == 0) return;

            var pick = candidates[rng.Next(candidates.Count)];
            regionMap[pick.row, pick.col] = pick.toRegion;
        }

        // BFS connectivity check: can all cells of regionId be reached from queenPos
        // when 'exclude' is temporarily removed from the region?
        private static bool IsConnectedWithout(int n, int[,] regionMap, int regionId,
                                               Vector2Int queenPos, Vector2Int exclude)
        {
            int total = 0;
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    if (regionMap[r, c] == regionId && !(r == exclude.y && c == exclude.x))
                        total++;

            if (total == 0) return false;

            var visited = new bool[n, n];
            var queue   = new Queue<(int r, int c)>();
            queue.Enqueue((queenPos.y, queenPos.x));
            visited[queenPos.y, queenPos.x] = true;
            int reachable = 1;

            while (queue.Count > 0)
            {
                var (row, col) = queue.Dequeue();
                foreach (var (dr, dc) in Dirs4)
                {
                    int nr = row + dr, nc = col + dc;
                    if ((uint)nr >= (uint)n || (uint)nc >= (uint)n) continue;
                    if (visited[nr, nc]) continue;
                    if (regionMap[nr, nc] != regionId) continue;
                    if (nr == exclude.y && nc == exclude.x) continue;
                    visited[nr, nc] = true;
                    reachable++;
                    queue.Enqueue((nr, nc));
                }
            }

            return reachable == total;
        }
    }
}
