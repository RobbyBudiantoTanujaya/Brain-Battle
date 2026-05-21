# Brain Battle - Milestones

## Milestone 1: Core Game (In Progress)
- [x] GridData, CellData, CellState, RegionData models
- [x] ConstraintValidator
- [x] Unit tests ConstraintValidator
- [x] KingsGridRenderer
- [x] KingsGameManager
- [x] LevelData + LevelLoader (ScriptableObject schema)
- [x] Runtime LevelGeneratorService (NQueensSolver, RegionBuilder, UniquenessVerifier)
- [x] KingsLevelGenerator Editor tool (BrainBattle/Generate Kings Levels menu)
- [x] Undo, Restart, Tips UI
- [x] Tutorial overlay
- [x] Victory screen (VictoryPanel.cs — time, moves, star rating, next level, restart)
- [x] Scene bootstrap / game orchestrator (KingsSceneBootstrap.cs)
- [x] Fix: KingsGridRenderer.OnCellTapped now uses `event` keyword
- [x] Fix: KingsGameManager wired to TutorialController (ShowTutorial + OnTutorialComplete)
- [x] Fix: OnConflictDetected subscribed to KingsGridRenderer.HighlightConflicts
- [x] Fix: LevelData and LevelLoader are now sealed
- [x] Scene setup guide (SceneSetupGuide.md — hierarchy, SerializeField wiring, mismatches)
- [x] KingsSceneBuilder editor tool (BrainBattle/Build Kings Scene — auto-builds and wires full hierarchy)
- [x] Visual fixes: grid centering enforced in Awake, 1px cell borders + 2px region borders, region/bounds debug logs
- [x] Fix: Cell tap not firing — EnsureEventSystem now uses InputSystemUIInputModule via reflection (activeInputHandler:2 compatibility); debug logs added to KingsGridRenderer + KingsGameManager
- [ ] HudController script (TimerText + MoveCountText live update — see SceneSetupGuide §7 MISMATCH 3)
- [ ] Polish & internal build

## Milestone 2: Level Generation + Progression
## Milestone 3: Random Map Mode
## Milestone 4: Game #2
## Milestone 5: PvP
## Milestone 6: Audio
