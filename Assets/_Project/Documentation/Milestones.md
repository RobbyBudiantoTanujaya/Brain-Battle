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
- [x] SpriteGenerator editor tool (BrainBattle/Generate Placeholder Sprites — DotSprite + CrownSprite 32×32 PNGs)
- [x] Auto-X always on (KingsGameManager — auto-dots row/col/region/adjacents on crown place, reverts on crown remove; no toggle, no PlayerPrefs)
- [x] Click+drag support (KingsGridRenderer CellEventHandler — IPointerDownHandler/EnterHandler/UpHandler, single undo snapshot per drag, works mouse+touch)
- [x] Input Controls documented in KingsLevelGeneration.md (tap cycle, drag behavior, Auto-X behavior)
- [x] Fix UndoButton visibility — button always visible; alpha 0.4 / interactable=false when stack empty, alpha 1.0 / interactable=true when stack has items (CanvasGroup)
- [x] Fix grid centering — KingsGridRenderer measures from Canvas rect (no feedback loop), sets _self.sizeDelta = gridTotalSize after building cells
- [x] Fix HUD button layout — all buttons 160×80 fixed size, Undo pinned left-edge, Tips pinned right-edge, always visible with labels
- [x] Fix KingsSceneBootstrap — moved StartGame() from Awake to Start; added null guards for LevelLoader/KingsGameManager
- [x] Fix boot order — KingsSceneBootstrap.Start() now uses BootDeferred coroutine (yield return null) so Canvas CanvasScaler runs before RenderGrid reads rect sizes
- [x] Fix VictoryPanel visible on startup — null guard in Awake logs error instead of NPE-silently-skipping; Start() re-hides as safety; KingsSceneBuilder now saves VictoryContent inactive in scene file
- [x] New tap model — single tap toggles Dot, double tap (0.3 s window, same cell) places Crown, tap on Crown clears it; drag never overwrites Crowns
- [x] Fix UI visibility — KingsSceneBuilder: button Image set to dark-indigo BtnBg, TMP text forced white, HUD gets dark background panel; grid cell fallback uses HSV color by regionId instead of white
- [x] HUD moved to bottom — KingsSceneBuilder: HUD anchor=(0,0)-(1,0) height 80px; GridContainer stretch (0,0)-(1,1) offsetMin.y=80 so grid fills space above HUD
- [x] Region borders fixed — cells inset 1px each side (2px gap, dark GridPanel bg as separator); removed redundant Pass-1 separator lines; region borders bumped to 3px; KingsGridRenderer reads _self.rect for size (works for both stretch and fixed layouts)
- [x] Border double-draw fixed — shared-edge approach: each (r,c) draws its own RIGHT + BOTTOM edge once (region boundary OR outer edge); TOP + LEFT outer edges drawn separately; cells now full cellSize with regionColor*0.92f tint; no stacked/doubled borders possible
- [x] Border rendering matches reference — thin 1px rgba(0,0,0,0.15) between same-region cells; thick 3px rgba(0,0,0,0.85) between different-region cells; 4 outer border lines (3px dark) drawn separately; cell bg uses full region color, no darkening
- [x] Border & centering polish — same-region 1.5px rgba(0,0,0,0.25), diff-region 4px rgba(0,0,0,0.8); usable size now uses side padding×2 vs top-only padding; _gridPanel shifted -padding/2 vertically to center within usable area
- [x] Auto-X verified — row/col/region/adjacent index convention (Vector2Int x=col, y=row) traced through generator→loader→renderer→gamemanager, no off-by-one found; added inline comment
- [x] Compound knowledge docs bootstrap — generated docs/ architecture, product-spec, constitution, quality, reliability, security, and run summary assets from current Unity codebase
- [x] HUDController script (TimerText + MoveCountText live update via KingsGameManager.ElapsedSeconds + MoveCount in Update())
- [ ] Polish & internal build

## Milestone 2: Level Generation + Progression
- [x] Level Select screen (LevelSelectController.cs, LevelSelectButton.cs, LevelSelectSceneBuilder editor tool)
  - [x] 3 difficulty tabs: Beginner (1-2), Expert (3-4), Impossible (5) with active/inactive color
  - [x] Progress bar per difficulty (filled Image, reads Kings_Level_N_Stars PlayerPrefs)
  - [x] Level grid: ScrollView + GridLayoutGroup (3 columns), LevelSelectButton prefab
  - [x] Level states: Locked (dark + X), Available (accent), Completed (accent + checkmark)
  - [x] Unlock rule: previous level must have stars > 0
  - [x] Play button: loads first Available level in active tab
  - [x] Scene navigation: LevelSelect → SampleScene via Kings_PendingLevel PlayerPrefs
  - [x] KingsSceneBootstrap reads Kings_PendingLevel on boot (falls back to level 1)
  - [x] VictoryPanel Next Level + Menu both return to LevelSelect scene
## Milestone 3: Random Map Mode
## Milestone 4: Game #2
## Milestone 5: PvP
## Milestone 6: Audio
