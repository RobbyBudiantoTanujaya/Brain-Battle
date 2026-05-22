# Kings Level Generation - Algorithm Documentation

## Game Rules (LinkedIn Queens identical)
- NxN grid, N regions, place exactly one crown per row, column, region
- No two crowns adjacent (including diagonal)
- Always exactly ONE solution, no guessing needed
- Tap cycle: Empty -> Dot -> Crown -> Empty

## Generation Algorithm (DO NOT manually compute, always write code that runs at editor-time)

### Step 1: Place N Queens (Backtracking)
- Standard N-Queens solver with extra constraint: no diagonal adjacency
- Use System.Random(seed) for deterministic output
- Store solution as Vector2Int[]

### Step 2: Build Regions (BFS Flood-Fill)
- Start BFS from each queen position simultaneously
- Each queen "owns" its region, grows organically
- Constraint: each region must be connected
- Constraint: no region completely surrounds another
- Result: int[,] regionMap where value = regionId

### Step 3: Verify Unique Solution (Constraint Propagation)
- Run solver on generated board
- Count total valid solutions
- If solutions != 1: swap 3-5 random border cells between adjacent regions, re-verify
- Max retry: 50 iterations before changing seed

### Step 4: Save as ScriptableObject
- Path: Assets/_Project/ScriptableObjects/Kings/Levels/
- Menu: BrainBattle/Generate Kings Levels
- Use AssetDatabase.CreateAsset()

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
