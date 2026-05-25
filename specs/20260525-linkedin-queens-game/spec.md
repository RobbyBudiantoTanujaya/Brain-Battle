# Feature Specification: LinkedIn Queens Game

**Feature**: `20260525-linkedin-queens-game`
**Created**: 2026-05-25
**Status**: Draft
**Input**: https://bytedance.sg.larkoffice.com/docx/SurId2h8covkVHxmgLPl6fBIg56 (local copy: [SurId2h8covkVHxmgLPl6fBIg56.md](../doc_export/SurId2h8covkVHxmgLPl6fBIg56/SurId2h8covkVHxmgLPl6fBIg56.md))

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Core Puzzle Gameplay (Priority: P1)

As a player, I want to play an N×N crown-placement puzzle where I place exactly one crown per row, column, and colored region, with no two crowns touching, so that I can solve logic puzzles.

**Why this priority**: This is the core game mechanic - without it, there is no game. Delivers immediate playable value.

**Technical Implementation**:

**Models:**
| Object | Description |
|--------|-------------|
| `CellState` enum | Empty, Dot, Crown |
| `CellData` class | Position (row, col), state, regionId |
| `RegionData` class | RegionId, color, List<Vector2Int> cells |
| `GridData` class | Size, CellData[,], List<RegionData> |

**Constraint Validation (ConstraintValidator static class):**
- `ValidateMove(grid, row, col, state)` → ValidationResult with conflict positions
- `CheckWin(grid)` → bool (true when exactly N crowns with no conflicts)
- `GetAllConflicts(grid)` → List<Vector2Int> (all violating crown positions)

**State Management (QueensGameManager MonoBehaviour):**
- Moves tracking, undo stack (max 50), timer, win detection
- Events: OnGameComplete, OnConflictDetected, OnUndoStackChanged
- Auto-X: Auto-fill dots when crown placed (row/col/region/adjacent), track per-crown for removal

**Call Chain:** User Tap → GridRenderer → GameManager → ConstraintValidator → Apply State / Highlight Conflicts

**Independent Test**: Can be fully tested by loading any puzzle grid, placing crowns, and verifying win detection triggers when constraints are satisfied.

**Acceptance Scenarios**:

1. **Given** an empty NxN grid with colored regions, **When** player places a crown, **Then** crown appears and Auto-X fills dots in same row/col/region/adjacent cells
2. **Given** a crown already placed at (r,c), **When** player tries to place another crown in same row, **Then** conflict is highlighted
3. **Given** exactly N crowns placed satisfying all constraints, **When** player places final valid crown, **Then** OnGameComplete fires

---

### User Story 2 - Level Progression System (Priority: P1)

As a player, I want to progress through levels organized by difficulty (Beginner/Expert/Impossible) with unlock logic and star ratings, so that I feel progression and accomplishment.

**Why this priority**: Essential for player retention and game feel. Works independently of puzzle mechanics.

**Technical Implementation**:

**Level Data (ScriptableObject):**
| Object | Description |
|--------|-------------|
| `LevelData` | LevelNumber, Difficulty, GridSize, Regions, Solution |
| `LevelLoader` | Load all assets, GetLevel(), BuildGridFromLevel() |

**Level Generation Pipeline:**
- `NQueensSolver`: Backtracking with adjacency constraint, randomized column order
- `RegionBuilder`: Multi-source BFS flood-fill from queen positions
- `UniquenessVerifier`: Tarjan's algorithm for articulation points, border mutation
- `LevelGeneratorService`: Pipeline orchestration (Solve → Build Regions → Verify → Create LevelData)

**Level Select UI:**
- `LevelSelectController`: Tab switching (Beginner/Expert/Impossible), progress bars
- `LevelSelectButton` prefab: State sprites (locked, available, completed)
- Progression: Per-tab unlock, first level always available

**Call Chain:** Generate Levels → NQueensSolver → RegionBuilder → UniquenessVerifier → LevelData SO → LevelLoader → GameManager

**Independent Test**: Can be tested by generating levels and verifying each has exactly one solution, then navigating LevelSelect UI.

**Acceptance Scenarios**:

1. **Given** no previous progress, **When** game launches, **Then** first level of each difficulty tab is available
2. **Given** player completes level N with stars, **When** returning to LevelSelect, **Then** level N+1 unlocks and shows star rating
3. **Given** a level, **When** uniqueness verification runs, **Then** puzzle has exactly one valid solution

---

### User Story 3 - Grid Rendering & Input (Priority: P1)

As a player, I want to interact with a dynamically rendered grid using tap/double-tap/drag gestures, with visual feedback for conflicts, so that I can play the game intuitively.

**Why this priority**: Core UI/UX - enables player interaction with the puzzle. Cannot have a playable game without this.

**Technical Implementation**:

**Components:**
| Component | Description |
|-----------|-------------|
| `QueensGridRenderer` | Dynamic cell creation, border rendering (thin/thick), crown/dot sprites |
| Input Handler | Single tap → cycle, Double tap → crown, Drag → dots |
| `VictoryPanel` | Time, moves, star rating, Next Level/Restart/Menu buttons |
| `HUDController` | Timer display, move counter, undo/restart/tips buttons |

**Events:**
- `OnCellTapped`: (row, col) from GridRenderer
- `OnCellDragEntered`: (row, col) from GridRenderer
- `OnGameComplete`: (time, moves) from GameManager

**Call Chain:** User Input → GridRenderer → GameManager.OnCellTapped → ConstraintValidator → Apply State → GridRenderer.UpdateCell → UI Render

**Independent Test**: Can be tested by spawning a grid and verifying tap/drag interactions update cell states correctly.

**Acceptance Scenarios**:

1. **Given** a cell in Empty state, **When** player single-taps, **Then** cell cycles to Dot
2. **Given** a cell in Dot state, **When** player double-taps within 0.3s, **Then** cell becomes Crown
3. **Given** a conflict detected, **When** system highlights conflicts, **Then** conflicting cells show red pulse animation

---

### User Story 4 - Audio & Polish (Priority: P2)

As a player, I want sound effects for interactions and background music, with a consistent visual design system, so that the game feels polished and professional.

**Why this priority**: Important for user experience and retention, but game is playable without it.

**Technical Implementation**:

**Audio System (AudioManager singleton):**
- RuntimeInitializeOnLoadMethod auto-bootstrap, DontDestroyOnLoad
- 8-source SFX pool (no GC during gameplay)
- BGM with fade-in coroutine, volume/mute persistence via PlayerPrefs

**Design System (DesignSystem static):**
- Color tokens: Primary (#ff2d78), Background (#1a1a1e), Surface (#2a2a3e)
- Size tokens: HUDHeight (80), TimerBarHeight (48), ButtonHeight (64)
- Grid tokens: GridPadding (16), BorderRegionThickness (5), BorderCellThickness (3)

**Independent Test**: Can be tested by triggering game interactions and verifying SFX plays, BGM fades in, and design tokens apply correctly.

**Acceptance Scenarios**:

1. **Given** game loads, **When** AudioManager initializes, **Then** BGM fades in over 1 second
2. **Given** player taps a cell, **When** interaction occurs, **Then** appropriate SFX plays
3. **Given** a UI component, **When** rendered, **Then** colors/sizes from DesignSystem are applied

---

### User Story 5 - Daily Challenge (Priority: P2)

As a player, I want a daily puzzle that is the same for all players (seeded by date), with streak tracking, so that I have a reason to return each day.

**Why this priority**: Increases retention and social competition, but game is complete without it.

**Technical Implementation**:

**Seeded RNG:** Deterministic puzzle generation by date (same puzzle for all players globally)

**Streak Tracking:**
- PlayerPrefs storage for current streak
- Reset on skip day

**Independent Test**: Can be tested by generating puzzles on different dates and verifying same puzzle is produced for same date across sessions.

**Acceptance Scenarios**:

1. **Given** today's date, **When** daily challenge is generated, **Then** same puzzle is produced for all players
2. **Given** player completes daily challenge, **When** checking streak, **Then** streak increments by 1
3. **Given** player skips a day, **When** returning next day, **Then** streak resets to 0

---

### User Story 6 - Monetization & Analytics (Priority: P3)

As a product owner, I want AdMob integration (interstitial + rewarded) and Firebase Analytics/Crashlytics, so that the game can be monetized and behavior tracked.

**Why this priority**: Required for production launch but not for core gameplay validation.

**Technical Implementation**:

**AdMob:**
- Interstitial ads: Between levels
- Rewarded ads: Unlock hints

**Firebase Analytics Events:**
- level_start, level_complete, level_fail, ad_watched

**Firebase Crashlytics:**
- Crash reporting for stability monitoring

**Independent Test**: Can be tested by triggering ad placements and verifying Firebase events are logged.

**Acceptance Scenarios**:

1. **Given** player completes a level, **When** transitioning to next, **Then** interstitial ad may show
2. **Given** player requests hint, **When** rewarded ad completes, **Then** hint is unlocked
3. **Given** any game action, **When** event occurs, **Then** Firebase Analytics logs appropriate event

---

### Edge Cases

- What happens when level generation fails to find a unique solution? Retry with different seed up to max retries, then fallback to smaller grid size.
- How does system handle rapid tap/drag inputs? Debounce and queue inputs, process one at a time to avoid state inconsistency.
- What happens when BGM source is already playing on scene transition? Guard with `isPlaying && clip == _bgm` to avoid duplicate BGM.
- How does Android handle TextMeshPro atlas? Must use Static mode + pre-baked atlas + `m_ClearDynamicDataOnBuild=0` to prevent text disappearing.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST validate crown placement against row, column, region, and adjacency constraints
- **FR-002**: System MUST auto-fill dots (Auto-X) when a crown is placed in same row/col/region/adjacent cells
- **FR-003**: Users MUST be able to undo moves (max 50 undo steps)
- **FR-004**: System MUST detect win condition when exactly N crowns are placed with no constraint violations
- **FR-005**: System MUST generate levels with exactly one unique solution (uniqueness verification)
- **FR-006**: System MUST support grid sizes from 4×4 to 12×12+
- **FR-007**: Users MUST be able to interact via tap (cycle state), double-tap (place crown), and drag (place dots)
- **FR-008**: System MUST highlight conflicting cells when a move violates constraints
- **FR-009**: System MUST persist level progress and star ratings via PlayerPrefs
- **FR-010**: System MUST provide 3 difficulty tiers (Beginner, Expert, Impossible) with independent unlock logic
- **FR-011**: System MUST play SFX for game interactions and BGM with fade-in
- **FR-012**: System MUST apply design tokens (colors, sizes) from DesignSystem at runtime
- **FR-013**: System MUST generate daily challenge puzzle deterministically by date
- **FR-014**: System MUST track daily challenge streak and reset on skip
- **FR-015**: System MUST show interstitial ads between levels and rewarded ads for hints
- **FR-016**: System MUST log analytics events (level_start, level_complete, level_fail, ad_watched)

### Key Entities

- **CellData**: Represents a single cell with position (row, col), state (Empty/Dot/Crown), and regionId
- **GridData**: Represents the complete puzzle grid with size, 2D cell array, and region definitions
- **RegionData**: Represents a colored region with regionId, color, and list of cell positions
- **LevelData**: ScriptableObject storing level definition (number, difficulty, grid size, regions, solution)
- **PlayerProgress**: PlayerPrefs data including unlocked levels, star ratings, tutorial seen flag, audio settings

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Players can complete a puzzle level in under 5 minutes on average for Beginner difficulty
- **SC-002**: Level generation produces unique-solution puzzles with 99% success rate within retry limits
- **SC-003**: 90% of players successfully complete the tutorial on first attempt
- **SC-004**: App loads and displays LevelSelect screen within 3 seconds on mobile devices
- **SC-005**: No frame drops below 60fps during gameplay on target mobile devices
- **SC-006**: Daily challenge streak retention: 40% of players return next day
- **SC-007**: Crash-free rate above 99.5% (via Crashlytics monitoring)

## Risk Points

1. **Level Generation Performance**: 10×10+ grids may require tuning retry/swap parameters in UniquenessVerifier
2. **Android TMP**: TextMeshPro must use Static atlas + pre-baked, `m_ClearDynamicDataOnBuild=0` required
3. **Ad SDK Integration**: AdMob + Firebase require platform-specific setup and test device configuration
4. **Daily Challenge Determinism**: Seeded RNG must produce identical results across Android/iOS
5. **Memory on Mobile**: Large sprite sheets and font atlases may cause memory pressure on low-end devices

## Estimated Scope

| Category | Count |
|----------|-------|
| Scripts | ~50 |
| Scenes | 2 (LevelSelect, Game) |
| Level Assets | 35+ ScriptableObjects |
| Prefabs | 5-10 |
| Sprites | 10-15 |
| Audio Clips | 10-15 |
| Estimated Dev Time | 6-8 weeks |
