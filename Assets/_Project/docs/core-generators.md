> Folder: `Assets/_Project/Scripts/Core/Generators/`

# Core/Generators — Level Generation Pipeline

No MonoBehaviour. Editor-only pipeline (called from `KingsLevelGenerator.cs`).  
Namespace: `BrainBattle.Core.Generators`

> **RULE:** Never manually compute levels. Always call this pipeline from editor code.  
> Read `Assets/_Project/Documentation/KingsLevelGeneration.md` before modifying anything here.

---

## Pipeline Overview

```
KingsNQueensSolver
       ↓  queen positions (one valid N-Queens solution with adjacency constraint)
KingsRegionBuilder
       ↓  flood-fill regions seeded from queen positions
KingsUniquenessVerifier
       ↓  verify solution is unique (reject if multiple solutions exist)
LevelGeneratorService
       ↓  orchestrate above + enforce region size + return LevelData fields
KingsLevelGenerator (Editor)
       ↓  call service, create ScriptableObject assets
```

---

## KingsNQueensSolver.cs

Backtracking N-Queens solver with **Chebyshev adjacency constraint**
(no two queens within distance 1 in any direction).

```csharp
public static class KingsNQueensSolver
{
    // Returns ONE valid queen placement for an N×N grid.
    // Throws if no solution found within attempt limit.
    public static List<Vector2Int> Solve(int n, System.Random rng);
    // Vector2Int(col, row) convention.
}
```

- Randomized column order per row for variety between calls.
- Returns `Vector2Int(col, row)` list, one entry per row.

---

## KingsRegionBuilder.cs

Multi-source BFS flood-fill that grows N regions from N queen seed positions.

```csharp
public static class KingsRegionBuilder
{
    // Given queen positions, flood-fill the grid into N regions.
    // Returns int[row, col] regionId map (0-indexed).
    public static int[,] BuildRegions(int gridSize, List<Vector2Int> queenPositions, System.Random rng);
}
```

- Each queen starts its own region.
- BFS frontier expands to 4-connected neighbors.
- Tie-breaking is randomized for natural-looking shapes.
- Result: every cell belongs to exactly one region.

---

## KingsUniquenessVerifier.cs

Verifies the generated puzzle has **exactly one solution**.  
Uses Tarjan's articulation-point algorithm (O(N²)) to detect and repair
regions that would allow multiple crown placements.

```csharp
public static class KingsUniquenessVerifier
{
    // Returns true if the grid has a unique solution.
    // Internally: runs a second solver pass; if it finds an alternative solution → not unique.
    public static bool IsUnique(int[,] regionMap, List<Vector2Int> solution, int gridSize);

    // Attempts to mutate the region map until the puzzle is unique.
    // Uses articulation-point detection to safely move border cells between regions
    // without breaking region connectivity.
    public static bool MutateBorderUntilUnique(
        int[,] regionMap, List<Vector2Int> solution, int gridSize,
        int maxAttempts, System.Random rng);
}
```

### Why Articulation Points?
Moving a border cell from region A to region B could split A into two
disconnected parts, making the puzzle unsolvable. Articulation-point detection
(O(V+E) DFS) identifies cells that, if removed, would disconnect their region.
Only non-articulation border cells are candidates for mutation.

---

## LevelGeneratorService.cs

High-level orchestrator. Single entry point for the editor generator.

```csharp
public static class LevelGeneratorService
{
    public struct GeneratedLevel
    {
        public int[,]           RegionMap;      // [row, col] → regionId
        public List<Vector2Int> Solution;       // Vector2Int(col, row) per queen
        public Color[]          RegionColors;   // one Color per regionId
    }

    // Generate one valid, unique Kings level of the given size.
    // Retries internally up to maxRetries times.
    // Throws GenerationFailedException if all attempts fail.
    public static GeneratedLevel Generate(
        int gridSize,
        int maxRetries,
        System.Random rng,
        int minRegionSize = 2,
        int maxRegionSize = -1);   // -1 = unconstrained
}
```

### Region Size Constraints
- `minRegionSize`: rejects puzzles where any region has fewer than N cells (too easy).
- `maxRegionSize`: rejects puzzles where any region is too large (too hard to reason about).
- Default minRegionSize = 2 ensures no 1-cell regions (trivially forced crowns).

### Retry Logic
The generator retries the full pipeline (solve → build → verify → size-check) up to
`maxRetries` times before giving up. Generation is fast (< 100ms for N ≤ 8).
