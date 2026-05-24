> Folder: `Assets/_Project/Scripts/Core/Models/`

# Core/Models — Pure Data Layer

No MonoBehaviour. No Unity dependencies except `[Serializable]` and `Vector2Int`.  
Namespace: `BrainBattle.Core.Models`

---

## CellState.cs

```csharp
[Serializable]
public enum CellState : byte
{
    Empty = 0,   // untouched cell
    Dot   = 1,   // user-placed exclusion marker (X)
    Crown = 2    // user-placed crown
}
```

Used everywhere as the cell's game state. Stored as `byte` to keep serialized GridData small.

---

## CellData.cs

Represents one cell in the grid.

```csharp
[Serializable]
public sealed class CellData
{
    public int       Row;
    public int       Col;
    public int       RegionId;   // which color region owns this cell
    public CellState State;      // Empty / Dot / Crown
}
```

**Created by** `GridData` constructor — one per cell, row-major order.  
**Modified by** `GridData.SetCellState()` and `LevelLoader.BuildGridFromLevel()` (assigns RegionId).

---

## RegionData.cs

Represents one color region (N cells, one crown slot).

```csharp
[Serializable]
public sealed class RegionData
{
    public int              RegionId { get; }
    public Color            Color    { get; }
    public List<Vector2Int> Cells    { get; }   // each entry: Vector2Int(col, row)

    public RegionData(int regionId, Color color);
    public void AddCell(Vector2Int colRow);      // colRow.x = col, colRow.y = row
}
```

**IMPORTANT:** `Cells` stores `Vector2Int(col, row)` — x = column, y = row.  
Access the corresponding `CellData` as `grid.GetCell(cell.y, cell.x)`.

---

## GridData.cs

Master runtime grid. Implements `ISerializationCallbackReceiver` to flatten the 2D `Cells[,]`
into a `List<CellData>` for Unity/JsonUtility serialization.

```csharp
[Serializable]
public sealed class GridData : ISerializationCallbackReceiver
{
    // ── Properties ─────────────────────────────────────────────────────────────
    public int              Size    { get; }          // grid is Size × Size
    public CellData[,]      Cells   { get; }          // [row, col] — row-major
    public List<RegionData> Regions { get; }

    // ── Constructor ─────────────────────────────────────────────────────────────
    public GridData(int size);                        // creates empty grid, all cells Empty

    // ── Cell access ─────────────────────────────────────────────────────────────
    public CellData         GetCell(int row, int col);
    public void             SetCellState(int row, int col, CellState state);
    public void             CycleState(int row, int col);    // Empty→Dot→Crown→Empty

    // ── Query ───────────────────────────────────────────────────────────────────
    public List<Vector2Int> GetCrownPositions();      // returns Vector2Int(col, row) for each crown
}
```

### Deep-Copy Pattern

`KingsGameManager` deep-copies `GridData` via JSON round-trip for undo snapshots:

```csharp
var copy = new GridData(source.Size);
JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), copy);
```

`ISerializationCallbackReceiver` fires during both `ToJson` and `FromJsonOverwrite`,
rebuilding `Cells[,]` from `_serializedCells` on the copy.

### Bounds Checking

`GetCell` and `SetCellState` throw `ArgumentOutOfRangeException` for invalid (row, col).
Use `(uint)row >= (uint)Size` guard pattern internally for zero-cost bounds check.
