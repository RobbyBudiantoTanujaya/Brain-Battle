using System;
using SystemRandom = System.Random;
using UnityEngine;

namespace BrainBattle.Core.Generators
{
    public static class KingsNQueensSolver
    {
        // Places N queens on an NxN board. Adjacency rule (LinkedIn Queens):
        // two queens are illegal only if Chebyshev distance == 1.
        // Returns queen positions as Vector2Int(col, row), or null if no solution found.
        public static Vector2Int[] Solve(int n, SystemRandom rng)
        {
            var queens   = new Vector2Int[n];
            var usedCols = new bool[n];
            return Backtrack(n, 0, usedCols, queens, rng) ? queens : null;
        }

        private static bool Backtrack(int n, int row, bool[] usedCols, Vector2Int[] queens, SystemRandom rng)
        {
            if (row == n) return true;

            var cols = new int[n];
            for (int i = 0; i < n; i++) cols[i] = i;
            Shuffle(cols, rng);

            foreach (int col in cols)
            {
                if (usedCols[col]) continue;
                // Only the immediately preceding row can be Chebyshev-adjacent (dr==1).
                // Rows further back have dr>=2, so Chebyshev distance is always >=2.
                if (row > 0 && Math.Abs(queens[row - 1].x - col) <= 1) continue;

                queens[row]   = new Vector2Int(col, row);
                usedCols[col] = true;

                if (Backtrack(n, row + 1, usedCols, queens, rng)) return true;

                usedCols[col] = false;
            }
            return false;
        }

        private static void Shuffle(int[] arr, SystemRandom rng)
        {
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }
    }
}
