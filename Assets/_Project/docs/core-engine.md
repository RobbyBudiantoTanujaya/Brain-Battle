> Folder: `Assets/_Project/Scripts/Core/Engine/`

# Core/Engine — Pure Constraint Logic

No MonoBehaviour. No state. All methods static.  
Namespace: `BrainBattle.Core.Engine`

---

## ConstraintValidator.cs

Single static class. Validates Kings puzzle rules against a `GridData` snapshot.

### ValidationResult (struct)

```csharp
public readonly struct ValidationResult
{
    public bool             IsValid           { get; }
    public List<Vector2Int> ConflictPositions { get; } // Vector2Int(col, row) convention
}
```

### Rules Enforced

| Rule | Description |
|---|---|
| Row uniqueness | At most one crown per row |
| Column uniqueness | At most one crown per column |
| Region uniqueness | At most one crown per color region |
| Adjacency | No two crowns may touch (8-directional Chebyshev distance = 1) |

`Empty` and `Dot` placements always pass — constraints only apply to `Crown`.

### Public API

```csharp
// Validate placing newState at (row, col) against current grid.
// Treats target cell as empty regardless of its current value.
// Only Crown placements can fail.
static ValidationResult ValidateMove(GridData grid, int row, int col, CellState newState);

// Returns true only when ALL win conditions are satisfied:
// exactly Size crowns, one per row/column/region, no adjacency violations.
static bool CheckWin(GridData grid);

// Returns all crown positions currently involved in any violation.
// Used for full-board conflict highlighting.
static List<Vector2Int> GetAllConflicts(GridData grid);
```

### Usage Pattern

```csharp
// In KingsGameManager after every move:
var result = ConstraintValidator.ValidateMove(_currentGrid, row, col, newState);
if (!result.IsValid)
{
    OnConflictDetected?.Invoke(result.ConflictPositions); // highlight in red
    return;
}
_gridRenderer.ClearConflicts();

if (ConstraintValidator.CheckWin(_currentGrid))
    HandleWin();
```

### Coordinate Convention

All `Vector2Int` values: **x = column, y = row**.  
`ConflictPositions` includes the candidate cell itself when failing,
so the UI can highlight both the new crown and the conflicting one.

### Performance Notes

- `ValidateMove`: O(N) row + O(N) col + O(region size) + O(8) adjacency.
- `CheckWin`: O(crowns²) adjacency only after O(N) uniqueness checks pass.
- `GetAllConflicts`: O(crowns²) — only called for full-board display.
- No allocations on the happy path (pass returns empty list pre-allocated with capacity 0).
