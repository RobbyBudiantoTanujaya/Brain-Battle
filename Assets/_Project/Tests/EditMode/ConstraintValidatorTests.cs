using System.Collections.Generic;
using BrainBattle.Core.Engine;
using BrainBattle.Core.Models;
using NUnit.Framework;
using UnityEngine;

namespace BrainBattle.Tests.EditMode
{
    [TestFixture]
    public class ConstraintValidatorTests
    {
        // Standard 4x4 quadrant map — four 2×2 regions, one per screen quadrant.
        //
        //   col: 0  1  2  3
        // row 0: 0  0  1  1
        // row 1: 0  0  1  1
        // row 2: 2  2  3  3
        // row 3: 2  2  3  3
        private static readonly int[,] QuadrantMap =
        {
            { 0, 0, 1, 1 },
            { 0, 0, 1, 1 },
            { 2, 2, 3, 3 },
            { 2, 2, 3, 3 }
        };

        // Diagonal map — each region's cells run along a main diagonal, so
        // same-region cells are never directly or diagonally adjacent.
        //
        //   col: 0  1  2  3
        // row 0: 0  1  2  3
        // row 1: 3  0  1  2
        // row 2: 2  3  0  1
        // row 3: 1  2  3  0
        //
        // Region 0 cells (col, row): (0,0) (1,1) (2,2) (3,3)
        private static readonly int[,] DiagonalMap =
        {
            { 0, 1, 2, 3 },
            { 3, 0, 1, 2 },
            { 2, 3, 0, 1 },
            { 1, 2, 3, 0 }
        };

        // ── Helpers ───────────────────────────────────────────────────────────────

        // Builds a GridData from a square region map, assigns RegionId on each cell,
        // and populates RegionData.Cells following the project convention (x=col, y=row).
        private static GridData BuildGrid(int size, int[,] regionMap)
        {
            var grid       = new GridData(size);
            var regionDict = new Dictionary<int, RegionData>();

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    int regionId = regionMap[r, c];

                    if (!regionDict.TryGetValue(regionId, out var region))
                    {
                        region = new RegionData(regionId, Color.white);
                        regionDict[regionId] = region;
                        grid.Regions.Add(region);
                    }

                    grid.GetCell(r, c).RegionId = regionId;
                    region.AddCell(new Vector2Int(c, r)); // x=col, y=row
                }
            }

            return grid;
        }

        // Shorthand that matches the project convention (x=col, y=row).
        private static Vector2Int Pos(int col, int row) => new(col, row);

        // ── ValidateMove — row conflict ───────────────────────────────────────────

        [Test]
        [Description("Placing a second crown in the same row must fail and report the existing crown.")]
        public void ValidateMove_RowConflict_ReturnsInvalidAndReportsExistingCrown()
        {
            var grid = BuildGrid(4, QuadrantMap);
            grid.SetCellState(0, 0, CellState.Crown);           // existing crown at (row=0, col=0)

            var result = ConstraintValidator.ValidateMove(grid, row: 0, col: 2, CellState.Crown);

            Assert.IsFalse(result.IsValid);
            Assert.Contains(Pos(col: 0, row: 0), result.ConflictPositions,
                "Existing crown in the same row must appear in ConflictPositions.");
        }

        // ── ValidateMove — column conflict ────────────────────────────────────────

        [Test]
        [Description("Placing a second crown in the same column must fail and report the existing crown.")]
        public void ValidateMove_ColumnConflict_ReturnsInvalidAndReportsExistingCrown()
        {
            var grid = BuildGrid(4, QuadrantMap);
            grid.SetCellState(0, 0, CellState.Crown);           // existing crown at (row=0, col=0)

            var result = ConstraintValidator.ValidateMove(grid, row: 2, col: 0, CellState.Crown);

            Assert.IsFalse(result.IsValid);
            Assert.Contains(Pos(col: 0, row: 0), result.ConflictPositions,
                "Existing crown in the same column must appear in ConflictPositions.");
        }

        // ── ValidateMove — region conflict ────────────────────────────────────────

        [Test]
        [Description("Placing a second crown in the same region must fail even when row, column, "
                   + "and adjacency constraints are satisfied.")]
        public void ValidateMove_RegionConflict_ReturnsInvalid()
        {
            // DiagonalMap region 0 contains (col=0,row=0) and (col=2,row=2).
            // These cells are in different rows, different columns, and 2 steps apart
            // (not adjacent), so the only violation is the shared region.
            var grid = BuildGrid(4, DiagonalMap);
            grid.SetCellState(0, 0, CellState.Crown);           // region 0, (row=0, col=0)

            var result = ConstraintValidator.ValidateMove(grid, row: 2, col: 2, CellState.Crown);

            Assert.IsFalse(result.IsValid);
            Assert.Contains(Pos(col: 0, row: 0), result.ConflictPositions,
                "The crown that caused the region conflict must appear in ConflictPositions.");
        }

        // ── ValidateMove — diagonal adjacency conflict ─────────────────────────────

        [Test]
        [Description("Placing a crown diagonally adjacent to an existing crown must fail.")]
        public void ValidateMove_DiagonalAdjacency_ReturnsInvalid()
        {
            // This map places (row=0,col=0) in region 0 and (row=1,col=1) in region 2,
            // so the only violation between them is 8-directional adjacency.
            //
            //   col: 0  1  2  3
            // row 0: 0  1  2  3
            // row 1: 1  2  3  0   ← (1,1) = region 2  ≠  region 0
            // row 2: 2  3  0  1
            // row 3: 3  0  1  2
            var regionMap = new int[,]
            {
                { 0, 1, 2, 3 },
                { 1, 2, 3, 0 },
                { 2, 3, 0, 1 },
                { 3, 0, 1, 2 }
            };
            var grid = BuildGrid(4, regionMap);
            grid.SetCellState(0, 0, CellState.Crown);           // (row=0, col=0)

            var result = ConstraintValidator.ValidateMove(grid, row: 1, col: 1, CellState.Crown);

            Assert.IsFalse(result.IsValid);
            Assert.Contains(Pos(col: 0, row: 0), result.ConflictPositions,
                "The diagonally adjacent crown must appear in ConflictPositions.");
        }

        // ── ValidateMove — valid placement ────────────────────────────────────────

        [Test]
        [Description("A placement that violates no constraint must return valid with an empty conflict list.")]
        public void ValidateMove_ValidPlacement_ReturnsValidWithEmptyConflicts()
        {
            // Crown at (row=0, col=0) is region 0.
            // Candidate (row=2, col=2) is region 3 — different row, different column,
            // 2√2 steps away (not adjacent), and in a different region.
            var grid = BuildGrid(4, QuadrantMap);
            grid.SetCellState(0, 0, CellState.Crown);

            var result = ConstraintValidator.ValidateMove(grid, row: 2, col: 2, CellState.Crown);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.ConflictPositions.Count,
                "A valid placement must produce no conflict positions.");
        }

        // ── CheckWin — solved board ────────────────────────────────────────────────

        [Test]
        [Description("A correctly solved 4×4 board must be recognised as a win.")]
        public void CheckWin_ValidCompletedBoard_ReturnsTrue()
        {
            // QuadrantMap solution (verified: unique row, col, region; no pair adjacent):
            //
            //   col: 0  1  2  3
            // row 0: .  ♛  .  .   region 0
            // row 1: .  .  .  ♛   region 1
            // row 2: ♛  .  .  .   region 2
            // row 3: .  .  ♛  .   region 3
            //
            // Cols used: 1, 3, 0, 2  (all distinct)
            // Nearest pair distance: (0,1)↔(1,3) → |Δrow|=1, |Δcol|=2  → not adjacent ✓
            var grid = BuildGrid(4, QuadrantMap);
            grid.SetCellState(0, 1, CellState.Crown);
            grid.SetCellState(1, 3, CellState.Crown);
            grid.SetCellState(2, 0, CellState.Crown);
            grid.SetCellState(3, 2, CellState.Crown);

            Assert.IsTrue(ConstraintValidator.CheckWin(grid));
        }

        // ── CheckWin — incomplete board ────────────────────────────────────────────

        [Test]
        [Description("A board with fewer crowns than regions must not be recognised as a win.")]
        public void CheckWin_IncompleteBoard_ReturnsFalse()
        {
            var grid = BuildGrid(4, QuadrantMap);
            grid.SetCellState(0, 1, CellState.Crown);   // only 2 of 4 regions covered
            grid.SetCellState(2, 0, CellState.Crown);

            Assert.IsFalse(ConstraintValidator.CheckWin(grid));
        }

        // ── GetAllConflicts ────────────────────────────────────────────────────────

        [Test]
        [Description("Two crowns in the same row must both appear in the global conflict list.")]
        public void GetAllConflicts_TwoRowConflicts_ReturnsBothPositions()
        {
            // (row=0,col=0) → region 0 ;  (row=0,col=2) → region 1
            // Only violation: shared row.
            var grid = BuildGrid(4, QuadrantMap);
            grid.SetCellState(0, 0, CellState.Crown);   // Pos(col=0, row=0)
            grid.SetCellState(0, 2, CellState.Crown);   // Pos(col=2, row=0)

            var conflicts = ConstraintValidator.GetAllConflicts(grid);

            CollectionAssert.Contains(conflicts, Pos(col: 0, row: 0),
                "The first crown in the conflicting row must be reported.");
            CollectionAssert.Contains(conflicts, Pos(col: 2, row: 0),
                "The second crown in the conflicting row must be reported.");
        }
    }
}
