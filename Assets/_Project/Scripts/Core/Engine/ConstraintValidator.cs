using System;
using System.Collections.Generic;
using UnityEngine;
using BrainBattle.Core.Models;

namespace BrainBattle.Core.Engine
{
    // Returned by ValidateMove. ConflictPositions uses the project-wide
    // Vector2Int convention: x = col, y = row.
    public readonly struct ValidationResult
    {
        public bool             IsValid           { get; }
        public List<Vector2Int> ConflictPositions { get; }

        private ValidationResult(bool isValid, List<Vector2Int> conflicts)
        {
            IsValid           = isValid;
            ConflictPositions = conflicts;
        }

        internal static ValidationResult Pass() =>
            new(true, new List<Vector2Int>(0));

        internal static ValidationResult Fail(HashSet<Vector2Int> conflicts) =>
            new(false, new List<Vector2Int>(conflicts));
    }

    // Pure constraint logic for the Kings puzzle. No MonoBehaviour, no state.
    // All positions follow the project convention: Vector2Int(col, row).
    public static class ConstraintValidator
    {
        // 8-directional offsets: x = col delta, y = row delta.
        private static readonly Vector2Int[] Neighbours =
        {
            new(-1, -1), new(0, -1), new(1, -1),
            new(-1,  0),             new(1,  0),
            new(-1,  1), new(0,  1), new(1,  1)
        };

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Validates what would happen if <paramref name="newState"/> were placed at
        /// (row, col) against the current grid state, treating the target cell as empty
        /// regardless of its current value.
        /// Empty and Dot placements always pass — constraints apply only to Crowns.
        /// </summary>
        public static ValidationResult ValidateMove(
            GridData grid, int row, int col, CellState newState)
        {
            if (newState != CellState.Crown)
                return ValidationResult.Pass();

            int size      = grid.Size;
            var conflicts = new HashSet<Vector2Int>();
            var candidate = new Vector2Int(col, row);

            // Row: only one crown allowed per row.
            for (int c = 0; c < size; c++)
            {
                if (c == col) continue;
                if (grid.GetCell(row, c).State == CellState.Crown)
                    conflicts.Add(new Vector2Int(c, row));
            }

            // Column: only one crown allowed per column.
            for (int r = 0; r < size; r++)
            {
                if (r == row) continue;
                if (grid.GetCell(r, col).State == CellState.Crown)
                    conflicts.Add(new Vector2Int(col, r));
            }

            // Region: only one crown allowed per color region.
            var region = FindRegion(grid, grid.GetCell(row, col).RegionId);
            if (region != null)
            {
                foreach (var cell in region.Cells)
                {
                    if (cell == candidate) continue;
                    if (grid.GetCell(cell.y, cell.x).State == CellState.Crown)
                        conflicts.Add(cell);
                }
            }

            // Adjacency: no crown may touch another crown in any of the 8 directions.
            foreach (var offset in Neighbours)
            {
                int nr = row + offset.y;
                int nc = col + offset.x;
                if ((uint)nr >= (uint)size || (uint)nc >= (uint)size) continue;
                if (grid.GetCell(nr, nc).State == CellState.Crown)
                    conflicts.Add(new Vector2Int(nc, nr));
            }

            if (conflicts.Count == 0)
                return ValidationResult.Pass();

            // Include the candidate itself so the UI can highlight it as part of the conflict.
            conflicts.Add(candidate);
            return ValidationResult.Fail(conflicts);
        }

        /// <summary>
        /// Returns true only when the board satisfies every Kings win condition:
        /// exactly one crown per row, per column, and per region, with no adjacency violations.
        /// </summary>
        public static bool CheckWin(GridData grid)
        {
            int size   = grid.Size;
            var crowns = grid.GetCrownPositions();

            // Exactly one crown per row implies total crown count must equal grid size.
            if (crowns.Count != size) return false;

            var rowsSeen    = new HashSet<int>(size);
            var colsSeen    = new HashSet<int>(size);
            var regionsSeen = new HashSet<int>(size);

            foreach (var pos in crowns)
            {
                int r = pos.y;
                int c = pos.x;

                if (!rowsSeen.Add(r))                             return false;
                if (!colsSeen.Add(c))                             return false;
                if (!regionsSeen.Add(grid.GetCell(r, c).RegionId)) return false;
            }

            // Every region must be covered — guards against grids where region count != size.
            if (regionsSeen.Count != grid.Regions.Count) return false;

            // Row, column, and region uniqueness passed — only adjacency remains.
            return !HasAdjacencyConflict(crowns);
        }

        /// <summary>
        /// Returns every crown position that participates in at least one rule violation
        /// (row, column, region duplicate, or adjacency). Empty list means no conflicts.
        /// </summary>
        public static List<Vector2Int> GetAllConflicts(GridData grid)
        {
            var crowns = grid.GetCrownPositions();
            if (crowns.Count < 2) return new List<Vector2Int>(0);

            var conflicts = new HashSet<Vector2Int>();

            for (int i = 0; i < crowns.Count; i++)
            {
                var a       = crowns[i];
                int ar      = a.y;
                int ac      = a.x;
                int regionA = grid.GetCell(ar, ac).RegionId;

                for (int j = i + 1; j < crowns.Count; j++)
                {
                    var b  = crowns[j];
                    int br = b.y;
                    int bc = b.x;

                    bool violated =
                        ar == br ||                                             // same row
                        ac == bc ||                                             // same column
                        regionA == grid.GetCell(br, bc).RegionId ||            // same region
                        (Math.Abs(ar - br) <= 1 && Math.Abs(ac - bc) <= 1);   // adjacency (8-dir)

                    if (!violated) continue;

                    conflicts.Add(a);
                    conflicts.Add(b);
                }
            }

            return new List<Vector2Int>(conflicts);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static RegionData FindRegion(GridData grid, int regionId)
        {
            foreach (var region in grid.Regions)
                if (region.RegionId == regionId) return region;
            return null;
        }

        // Used only by CheckWin after row/col/region uniqueness is already confirmed,
        // so this only needs to test the 8-directional neighbour condition.
        private static bool HasAdjacencyConflict(List<Vector2Int> crowns)
        {
            for (int i = 0; i < crowns.Count; i++)
            {
                var a = crowns[i];
                for (int j = i + 1; j < crowns.Count; j++)
                {
                    var b = crowns[j];
                    if (Math.Abs(a.y - b.y) <= 1 && Math.Abs(a.x - b.x) <= 1)
                        return true;
                }
            }
            return false;
        }
    }
}
