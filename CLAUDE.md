# Brain Battle - Unity 6 LTS

## Project
2D puzzle game app for Android & iOS. Game #1: Kings puzzle.

## Tech Stack
- Unity 6.0.75f1 LTS, 2D URP
- C#, uGUI
- Target: Android (Snapdragon 665+), iOS

## Code Standards
- No MonoBehaviour on pure logic classes
- SerializeField for Inspector references, never public fields
- Events via System.Action, not UnityEvent
- Async operations via Coroutine or C# Task (not both mixed)
- All files under Assets/_Project/

## Folder Structure
Assets/_Project/
  Scripts/
    Core/Models/        # CellState, CellData, RegionData, GridData
    Core/Engine/        # ConstraintValidator, WinChecker, HintEngine
    Core/Generators/    # LevelGenerator, RandomMapGen
    Games/Kings/UI/     # KingsGridRenderer
    Games/Kings/Logic/  # KingsGameManager
    Games/Kings/Data/   # Level ScriptableObjects
    Shared/UI/          # UndoButton, RestartButton, TipsButton, VictoryPanel
    Shared/Progression/ # LevelUnlockManager, PlayerDataManager
  Prefabs/
  ScriptableObjects/
  Scenes/
  Resources/

## Conventions
- Namespace: BrainBattle.Core, BrainBattle.Kings, BrainBattle.Shared
- ScriptableObject for level data
- JSON via JsonUtility for save data to PlayerPrefs
- No third-party packages unless explicitly requested

## Out of Scope (for now)
- Multiplayer / PvP (Milestone 5)
- Audio (Milestone 6)
- Game #2+ (Milestone 4+)
