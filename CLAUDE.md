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

## Level Generation
- Full algorithm documented in Assets/_Project/Documentation/KingsLevelGeneration.md
- ALWAYS read this file before any level generation task
- NEVER manually compute levels, always write code that runs at editor-time
- Reference file: Assets/_Project/Editor/KingsLevelGenerator.cs

## Claude Code Rules (MUST FOLLOW)

### Anti-Stuck Rules
- NEVER manually compute, verify, or think through data (levels, grids, coordinates)
- NEVER verify algorithm correctness in thinking - write the code, let Unity run it
- If a task feels like it needs >5 minutes of planning, STOP and ask for clarification
- Always write code that executes at runtime/editor-time, not pre-computed output

### Task Breakdown Rules
- Max one responsibility per prompt
- If asked to create multiple files, do them sequentially, confirm each before next
- If output will be >200 lines, ask to split first

### Output Rules
- Write code immediately, no lengthy preamble
- No explanation after code unless asked
- If stuck or uncertain, output partial code with TODO comments and stop

### Level Generation Specific
- NEVER hardcode level cell data manually
- ALWAYS write algorithmic generator code
- Read Documentation/KingsLevelGeneration.md before any level generation task

## Milestones Tracking
- After completing ANY task, always update Milestones.md
- Mark completed items with [x]
- If a new sub-task is discovered during work, add it as [ ] before marking parent done
- Never mark done if Unity compile errors exist