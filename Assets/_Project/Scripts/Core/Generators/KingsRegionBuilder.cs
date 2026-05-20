using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainBattle.Core.Generators
{
    public static class KingsRegionBuilder
    {
        private static readonly (int dr, int dc)[] Dirs4 = { (-1, 0), (1, 0), (0, -1), (0, 1) };

        // Multi-source BFS flood-fill from queen positions.
        // Each queen seeds its own region; cells expand organically via shuffled neighbor order.
        // Returns int[,] regionMap where regionMap[row, col] == regionId (0-indexed by queen).
        public static int[,] Build(int n, Vector2Int[] queens, Random rng)
        {
            var regionMap = new int[n, n];
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    regionMap[r, c] = -1;

            var queue = new Queue<(int row, int col, int region)>();

            for (int i = 0; i < n; i++)
            {
                int r = queens[i].y, c = queens[i].x;
                regionMap[r, c] = i;
                queue.Enqueue((r, c, i));
            }

            var dirs = new (int dr, int dc)[4];

            while (queue.Count > 0)
            {
                var (row, col, region) = queue.Dequeue();

                Array.Copy(Dirs4, dirs, 4);
                Shuffle(dirs, rng);

                foreach (var (dr, dc) in dirs)
                {
                    int nr = row + dr, nc = col + dc;
                    if ((uint)nr >= (uint)n || (uint)nc >= (uint)n) continue;
                    if (regionMap[nr, nc] != -1) continue;
                    regionMap[nr, nc] = region;
                    queue.Enqueue((nr, nc, region));
                }
            }

            return regionMap;
        }

        private static void Shuffle((int, int)[] arr, Random rng)
        {
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }
    }
}
