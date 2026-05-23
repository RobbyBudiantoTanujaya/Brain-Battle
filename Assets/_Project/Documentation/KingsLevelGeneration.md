# Kings Level Generation - Algorithm Documentation

## Game Rules (LinkedIn Queens identical)
- NxN grid, N regions, place exactly one crown per row, column, region
- No two crowns adjacent (including diagonal)
- Always exactly ONE solution, no guessing needed
- Tap cycle: Empty -> Dot -> Crown -> Empty

## Generation Algorithm (DO NOT manually compute, always write code that runs at editor-time)

The pipeline has four phases. Each phase is a separate class under `Scripts/Core/Generators/`.
The outer driver is `LevelGeneratorService.GenerateLevel()`.

---

### Phase 1 — N-Queens Solver (`KingsNQueensSolver.cs`)

**Goal**: place N queens on an NxN board such that no two queens share a row, column, or are Chebyshev-adjacent (distance ≤ 1 in any direction).

**How it works**:
- Row-by-row backtracking. One queen per row is guaranteed by construction.
- For each row, shuffle the column order with `System.Random(seed)` — this is what makes each seed produce a different layout.
- Before placing a queen at `(row, col)`, two checks:
  1. `usedCols[col]` — column not already taken.
  2. `|queens[row-1].x - col| <= 1` — not Chebyshev-adjacent to the queen in the row directly above. Rows two or more apart can never be Chebyshev-adjacent (their distance is already ≥ 2), so only the previous row needs checking.
- Returns `Vector2Int[]` of length N (one per row), or `null` if no solution found for this permutation.

**Complexity**: O(N!) worst case, but the adjacency constraint prunes heavily — in practice sub-millisecond for N ≤ 10.

---

### Phase 2 — Region Builder (`KingsRegionBuilder.cs`)

**Goal**: partition all NxN cells into N connected regions, one region per queen.

**How it works**:
- Multi-source BFS: all N queen cells are enqueued simultaneously at the start.
- On each dequeue, expand to the 4-connected neighbours that are still unassigned.
- Direction order is shuffled per expansion with the same RNG — this organic growth creates irregular, blob-like regions rather than straight stripes.
- Because all queens start at the same BFS depth-zero, each region grows roughly proportional to the available space around its queen. No region can "cut off" another because they all expand in parallel.

**Result**: `int[n,n] regionMap` where every cell holds the index (0…N-1) of its region.

**Complexity**: O(N²) — each cell is visited exactly once.

---

### Phase 3 — Uniqueness Verifier (`KingsUniquenessVerifier.cs`)

**Goal**: ensure the board has exactly one valid solution (required by game rules).

**Why this is hard**: the region map from BFS is random — it frequently produces boards with 2–10 valid solutions. Phase 3 fixes this by iteratively reshaping region borders.

#### 3a — Solution Counter (`CountSolutions`)

Standard backtracking that tries to place one crown per row:
- Tracks `usedCols[n]`, `usedRegions[n]`, `placedCols[n]` (for adjacency check).
- **Early exit at `count > 1`** — we only need to know if solutions ≠ 1, never the exact count. This makes it fast even on 10×10.

#### 3b — Border Mutation (`MutateBorder`)

When `CountSolutions != 1`, randomly reshape the region borders:
1. Scan every cell on a region border (has a 4-connected neighbour in a different region).
2. Skip queen cells — they can never move.
3. For each candidate move `(cell, toRegion)`, run a BFS connectivity check: after removing the cell from its current region, is the current region still connected? Only safe moves are kept.
4. Pick one candidate at random and apply it (`regionMap[r,c] = toRegion`).

#### 3c — Retry loop

```
maxRetries = max(50, n² × 2)   // 50 for 4x4, 200 for 10x10
minSwaps   = max(3, n/2)        // more perturbation on bigger boards
maxSwaps   = max(6, n)

for attempt in 0..maxRetries:
    if CountSolutions == 1: return SUCCESS
    apply random(minSwaps..maxSwaps) border mutations
return CountSolutions == 1
```

If Phase 3 fails (couldn't achieve uniqueness within `maxRetries`), `LevelGeneratorService` increments the seed by 1 and restarts from Phase 1. Up to 10,000 seed bumps are tried.

**Complexity per retry**: O(N² × N²) — scanning all cells for candidates + BFS connectivity check per candidate. Acceptable because retries are few and N ≤ 10.

---

### Phase 4 — Asset Save (`KingsLevelGenerator.cs` + `LevelGeneratorService.cs`)

- `LevelGeneratorService.BuildLevelData()` packs queens + regionMap into a `LevelData` ScriptableObject.
- Region colors are evenly spaced HSV (hue = regionIndex / N), saturation 0.45, value 0.85.
- `KingsLevelGenerator.SaveAsset()` uses `EditorUtility.CopySerialized` to overwrite an existing asset in-place (preserves GUID/references) or `AssetDatabase.CreateAsset()` for new ones.
- After all levels are saved, `SyncOpenSceneLevelLoaders()` updates any open scene's `LevelLoader._allLevels` array to include the new assets, sorted alphabetically by asset name.

---

### Level Naming & Seed Strategy

Asset names follow the pattern `Kings_{Difficulty}_{DifficultyNumber:D2}`.
Each execution of **BrainBattle → Generate Kings Levels** adds 6 new assets per category (Beginner, Expert, Impossible) on top of whatever already exists.

Seeds are time-derived to produce different content each run:
```csharp
int baseSeed = (int)(DateTime.Now.Ticks >> 8);
int seed = baseSeed ^ (categoryOffset + difficultyNumber * 97);
```
- `categoryOffset`: 10000 / 20000 / 30000 per Beginner / Expert / Impossible — prevents two categories with the same difficultyNumber getting the same seed.
- `* 97` (a prime): ensures consecutive levels within a run produce independent-looking seeds even though `baseSeed` is shared.

---

## Known Limitations of the Current Approach

The algorithm is fundamentally **"generate-and-test with random repair"**. The brute-force nature shows up in two places:

1. **Phase 3 (uniqueness)** is a random walk through region space. It stumbles toward uniqueness by randomly nudging borders. For 10x10 it needs up to 200 retries × 10 border swaps = 2000 random mutations per seed attempt.

2. **Outer seed loop** retries the whole pipeline if Phase 3 gives up. In the worst case, 10,000 full pipeline runs are attempted.

---

## Alternative: Constraint-Guided Region Assignment (less brute-force)

Instead of building regions randomly then fixing them, regions can be built with uniqueness in mind from the start.

### Idea A — Solution-first coloring ("blocking assignment")

After placing queens, assign non-queen cells to regions that actively block alternative solutions:
- Cell `(r, c)` where `c ≠ q[r]` (i.e., not the intended crown column for row r):
  - Assign it to the region whose queen is already in column `c` — that region is "spent" in the solution, so placing a crown at `(r,c)` would try to reuse that region.
  - This directly invalidates most alternative placements without any randomness.
- Queen cells keep their own region.

This eliminates most of Phase 3 entirely. A small cleanup pass may still be needed for edge cases, but it converges much faster.

**Tradeoff**: Regions produced this way tend to be "striped" (column-aligned). This looks less organic visually than the current BFS blobs. Aesthetic post-processing (border smoothing) would be needed.

### Idea B — Constraint propagation during BFS

Rather than a separate fix-up phase, integrate the uniqueness check into Phase 2:
- During BFS, before assigning a cell to a region, tentatively try all candidate regions.
- Pick the one that results in the fewest valid solutions (greedy minimization).
- This is like Sudoku's "minimum remaining values" heuristic.

This turns Phase 3 from a random repair into a guided construction — no retries needed in practice. The downside is cost: each BFS step runs `CountSolutions`, making Phase 2 O(N² × N!) instead of O(N²).

For N=10, each `CountSolutions` call is ~microseconds (due to early exit), so it's still fast enough for editor-time generation — but the code is significantly more complex.

### Idea C — Pre-computed catalog (4x4, 5x5 only)

For small boards the total number of valid unique levels is finite and small:
- 4×4: ~30 distinct boards
- 5×5: ~300 distinct boards
- 6×6: ~5,000 distinct boards

These can be exhaustively enumerated once, serialized, and loaded at random — zero search cost at generation time. Not practical for 8×8 or 10×10.

---

**Bottom line**: Idea A is the most practical improvement — moderate code change, visual tradeoff. Idea B gives the best algorithmic behavior but is complex. Idea C is a quick win for Beginner levels only.

## Input Controls

### Tap Behavior
Cells do **not** cycle through all states in order. Instead:

| Gesture | Result |
|---------|--------|
| Single tap on Empty | → Dot |
| Single tap on Dot | → Empty |
| Single tap on Crown | → Empty |
| Double tap (same cell, within 0.3 s) | → Crown (from any non-Crown state) |

Double-tap detection is implemented in `KingsGameManager`: `_lastTapTime` and `_lastTapCell` track the most recent single tap. A second tap on the same cell within `DoubleTapWindow` (0.3 s) is resolved as Crown. The counters reset after a double-tap fires (so a rapid third tap restarts the sequence), and are also cleared on Start, Restart, and Undo.

### Drag Behavior
- **Pointer Down** on a cell: resolves the single-tap gesture for that cell, records the resulting state as the *drag target state*.
- **Pointer Enter** on a subsequent cell (while pointer is held): applies drag target state directly — Crowns on other cells are **never** overwritten by drag (requires an explicit double-tap).
- **Pointer Up**: ends the drag.
- Works for mouse (desktop) and single-finger touch (mobile).
- Implementation: `CellEventHandler` (inner class in `KingsGridRenderer`) implements `IPointerDownHandler`, `IPointerEnterHandler`, `IPointerUpHandler`. Drag state is stored on `KingsGridRenderer` and shared with all cell handlers.
- Undo snapshot is pushed once at drag start (the first tap); subsequent drag cells do not add to the undo stack.

### Auto-X Behavior
- Always active — no toggle.
- When a crown is placed and Auto-X is **on**: all currently *Empty* cells in the same **row**, **column**, **region**, and **8 adjacents** are automatically set to Dot.
- When a crown is **removed**: any dots that were auto-placed by that specific crown (tracked per-crown in `KingsGameManager._autoPlacedDots`) are reverted to Empty, unless they are also covered by another currently-placed crown's Auto-X zone.
- Auto-X tracking is cleared on Restart and Undo (undo restores full grid state from snapshot).

## Difficulty & Size Reference
| Level | Size | Difficulty  | Seed  |
|-------|------|-------------|-------|
