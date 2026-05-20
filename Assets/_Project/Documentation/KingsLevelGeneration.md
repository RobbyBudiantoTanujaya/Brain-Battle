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

## Difficulty & Size Reference
| Level | Size | Difficulty  | Seed  |
|-------|------|-------------|-------|
