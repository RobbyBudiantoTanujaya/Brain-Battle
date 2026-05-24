> Folder: `Assets/_Project/`

# BrainBattle — _Project Overview

## Game
Kings puzzle (N-Queens variant) for Android & iOS.  
Unity 6.0.75f1 LTS · 2D URP · C# · uGUI · TextMeshPro

## Rules
- Grid = N × N cells divided into N color regions.
- Place exactly **one crown per row**, **one per column**, and **one per color region**.
- No two crowns may touch — not even diagonally (8-directional adjacency forbidden).
- Tap once → dot (manual exclusion marker). Double-tap → crown. Tap crown → clear.

## Folder Map

```
Assets/_Project/
├── Documentation/          # Algorithm deep-dives (read before touching generators)
├── Editor/                 # Editor-only tools: scene builders, level generator, validator
├── Prefabs/                # LevelSelectButton prefab (built by LevelSelectSceneBuilder)
├── Resources/
│   ├── audio/BGM/          # bgm.mp3 — main background music
│   ├── audio/SFX/          # tap_dot, auto_dot, button_tap, invalid_place, victory_sound
│   ├── Fonts/              # Outfit SDF (body), HUDIcons SDF (star glyphs)
│   └── Sprites/            # dot, crown, UIRoundedRect, main_menu_bg
├── Scenes/
│   ├── LevelSelect.unity   # Scene index 0 — built by LevelSelectSceneBuilder
│   └── SampleScene.unity   # Scene index 1 — built by KingsSceneBuilder (inside Assets/Scenes too)
├── ScriptableObjects/
│   └── Kings/Levels/       # Kings_Beginner_01..N, Kings_Expert_01..N, Kings_Impossible_01..N
└── Scripts/
    ├── Core/
    │   ├── Models/         # Pure data: CellState, CellData, RegionData, GridData
    │   ├── Engine/         # Pure logic: ConstraintValidator
    │   └── Generators/     # Level generation pipeline (no MonoBehaviour)
    ├── Games/Kings/
    │   ├── Data/           # LevelData (ScriptableObject), LevelLoader (MonoBehaviour)
    │   ├── Logic/          # KingsGameManager, KingsSceneBootstrap
    │   └── UI/             # KingsGridRenderer, HUDController, HUDLayoutController, TutorialController
    └── Shared/
        ├── Audio/          # AudioManager (DontDestroyOnLoad singleton)
        └── UI/             # VictoryPanel, UndoButton, RestartButton, TipsButton, MenuButton,
                            # LevelSelectController, LevelSelectButton, BrainBattleDesignSystem
```

## Namespaces

| Folder | Namespace |
|---|---|
| Core/Models | `BrainBattle.Core.Models` |
| Core/Engine | `BrainBattle.Core.Engine` |
| Core/Generators | `BrainBattle.Core.Generators` |
| Games/Kings/Data | `BrainBattle.Kings` |
| Games/Kings/Logic | `BrainBattle.Games.Kings.Logic` |
| Games/Kings/UI | `BrainBattle.Games.Kings.UI` |
| Shared/* | `BrainBattle.Shared` / `BrainBattle.Shared.UI` |
| Editor | `BrainBattle.Editor` |

## Coordinate Convention (CRITICAL)

**`Vector2Int(col, row)`** — x = column index, y = row index — used everywhere:
- `GridData.GetCell(row, col)` — parameters are (row, col)
- `RegionData.Cells[]` stores `Vector2Int(col, row)`
- `ConstraintValidator` ConflictPositions: `Vector2Int(col, row)`
- `KingsGameManager` auto-dot tracking: `Vector2Int(col, row)`

Do NOT mix these up. The convention is consistent but counter-intuitive (x=col not x=row).

## Scene Wiring (MUST READ)

Scenes are **never hand-wired**. Use the builders:
- `BrainBattle → Build Kings Scene` → rebuilds `SampleScene`
- `BrainBattle → Build Level Select Scene` → rebuilds `LevelSelect`
- `BrainBattle → Validate Kings Scene` → checks all SerializeField refs

Run **Build Kings Scene** any time you add/change a SerializeField in a HUD or game script.

## Save System

All saves go to `PlayerPrefs` (JSON via `JsonUtility`):

| Key | Type | Content |
|---|---|---|
| `Kings_Grid` | string | JsonUtility of current GridData |
| `Kings_Time` | float | Elapsed seconds |
| `Kings_Moves` | int | Move count |
| `Kings_PendingLevel` | int | Level to load on next SampleScene entry |
| `Kings_Level_{N}_Stars` | int | Best star rating (1–3) for level N |

## Tech Constraints
- No MonoBehaviour on pure logic classes (`ConstraintValidator`, `GridData`, etc.)
- Events via `System.Action`, never `UnityEvent`
- No third-party packages
- Target device: Android Snapdragon 665+, iOS
