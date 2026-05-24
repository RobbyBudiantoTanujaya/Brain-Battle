> Folder: `Assets/_Project/Scripts/Games/Kings/Data/`

# Games/Kings/Data — Level Data Layer

Namespace: `BrainBattle.Kings`

---

## LevelData.cs

`ScriptableObject`. One asset per level, stored in `Assets/_Project/ScriptableObjects/Kings/Levels/`.

```csharp
[CreateAssetMenu(fileName = "KingsLevel", menuName = "BrainBattle/Kings/Level")]
public sealed class LevelData : ScriptableObject
{
    // ── Read-only properties ──────────────────────────────────────────────────
    int                LevelNumber { get; }    // 1-based, unique across all difficulties
    string             Difficulty  { get; }    // "Beginner" | "Expert" | "Impossible"
    int                GridSize    { get; }    // N (grid is N×N)
    RegionDefinition[] Regions     { get; }    // one per color region
    Vector2Int[]       Solution    { get; }    // Vector2Int(col, row) per crown

    // ── Editor-only init (called by KingsLevelGenerator) ─────────────────────
    void EditorInit(int levelNumber, string difficulty, int gridSize,
                    RegionDefinition[] regions, Vector2Int[] solution);
}
```

### RegionDefinition (nested class)

```csharp
[Serializable]
public class RegionDefinition
{
    int          RegionId { get; }
    Color        Color    { get; }    // RGBA region fill color
    Vector2Int[] Cells    { get; }    // Vector2Int(col, row) for each cell in region

    void Init(int regionId, Color color, Vector2Int[] cells);
}
```

### Asset Naming Convention

```
Kings_Beginner_01.asset   → LevelNumber=1,  Difficulty="Beginner"
Kings_Beginner_02.asset   → LevelNumber=2,  Difficulty="Beginner"
Kings_Expert_01.asset     → LevelNumber=1,  Difficulty="Expert"
Kings_Impossible_01.asset → LevelNumber=1,  Difficulty="Impossible"
```

LevelNumbers are **per-difficulty** sequential integers starting at 1.  
Asset file names determine sort order in `LevelLoader` and `LevelSelectController`.

---

## LevelLoader.cs

MonoBehaviour on `GameManager` in SampleScene.  
Bridges `LevelData` ScriptableObjects → runtime `GridData`.

```csharp
[SerializeField] private LevelData[] _allLevels;   // wired by KingsSceneBuilder

LevelData GetLevel(int levelNumber);               // lookup by LevelNumber field
GridData  BuildGridFromLevel(LevelData level);     // converts SO → runtime GridData
```

### BuildGridFromLevel Logic

```csharp
var grid = new GridData(level.GridSize);

foreach (var region in level.Regions)
{
    var regionData = new RegionData(region.RegionId, region.Color);
    foreach (var cell in region.Cells)
    {
        regionData.AddCell(cell);                          // cell = Vector2Int(col, row)
        grid.GetCell(cell.y, cell.x).RegionId = region.RegionId;   // assign region ownership
    }
    grid.Regions.Add(regionData);
}
return grid;   // all cells have State=Empty, RegionId assigned
```

### Editor Auto-Load Fallback

In the Unity Editor (not in builds), if `_allLevels` is empty or null, `Awake()`
auto-loads all `LevelData` assets from `Assets/_Project/ScriptableObjects/Kings/Levels/`
via `AssetDatabase.FindAssets`. This supports running from SampleScene without first
running the scene builder.

In **builds**: `_allLevels` must be pre-assigned by `KingsSceneBuilder`. An error is
logged if empty.
