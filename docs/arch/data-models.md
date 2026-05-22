---
last_compound_scan: 2026-05-22
compound_scan_mode: full
---

# Data Models

## Table of Contents

- [Overview](#overview)
- [Core Runtime Entities](#core-runtime-entities)
- [Level Asset Entities](#level-asset-entities)
- [Model Boundaries](#model-boundaries)

## Overview

The data model splits immutable level definitions from mutable runtime board state. ScriptableObject assets carry authored puzzle content, while runtime classes rebuild that content into mutable grids that support validation, undo snapshots, hints, and persistence.

## Core Runtime Entities

### GridData

`GridData` is the mutable runtime board aggregate. It owns the grid size, a reconstructed `CellData[,]` matrix, and the list of `RegionData` instances, and it centralizes state mutation through methods like `SetCellState`, `CycleState`, and `GetCrownPositions`. Its serialization callback bridge is the key invariant: Unity stores a flattened cell list, then rebuilds the 2D matrix after deserialization so gameplay code can keep array-based access.

**Source**: `Assets/_Project/Scripts/Core/Models/GridData.cs` [Code Direct] [Verified]

---

### CellData

`CellData` is the per-cell record for board position, player state, and region membership. The row and column are immutable after construction, while `State` and `RegionId` are mutable so the same object can serve both authoring reconstruction and live gameplay updates.

**Source**: `Assets/_Project/Scripts/Core/Models/CellData.cs` [Code Direct] [Verified]

---

### RegionData

`RegionData` is the runtime grouping entity that binds a region id, display color, and the set of cells belonging to that region. The rest of the system relies on its `Cells` list using the project-wide `Vector2Int(col, row)` convention, which is why validation, rendering, and auto-dot placement can share the same coordinates without translation.

**Source**: `Assets/_Project/Scripts/Core/Models/RegionData.cs` [Code Direct] [Verified]

## Level Asset Entities

### LevelData

`LevelData` is the authored puzzle asset model. It stores the level number, difficulty label, board size, region definitions, and the solved crown positions, then exposes `EditorInit` as the single editor-time population path used by the generator pipeline.

**Source**: `Assets/_Project/Scripts/Games/Kings/Data/LevelData.cs` [Code Direct] [Verified]

---

### RegionDefinition

`LevelData.RegionDefinition` is the serialized asset-side counterpart to `RegionData`. It captures region id, color, and member cells in asset form so `LevelLoader` can reconstruct runtime region objects without depending on editor-only generator state.

**Source**: `Assets/_Project/Scripts/Games/Kings/Data/LevelData.cs` [Code Direct] [Verified]

## Model Boundaries

### Asset-to-runtime reconstruction

`LevelLoader.BuildGridFromLevel` is the boundary that converts asset data into runtime data. It instantiates a fresh `GridData`, copies region membership into new `RegionData` objects, and writes each region id onto the target `CellData`, which means runtime mutations never touch the underlying `LevelData` asset.

**Source**: `Assets/_Project/Scripts/Games/Kings/Data/LevelLoader.cs` [Code Direct] [Verified]
