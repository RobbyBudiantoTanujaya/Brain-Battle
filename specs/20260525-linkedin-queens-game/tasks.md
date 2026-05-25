# Tasks: LinkedIn Queens Game Enhancements

**Input**: Design documents from `/specs/20260525-linkedin-queens-game/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/events.md ✅

**Tests**: Not explicitly requested — implementation tasks only.

**Organization**: Tasks grouped by user story. US1-US3 (M1-M2) already implemented. Tasks focus on US4-US6 (M3-M6).

**Note**: Core game engine (US1: Core Puzzle, US2: Level Progression, US3: Grid Rendering) is already complete. This task breakdown focuses on polish, audio, daily challenge, and monetization.

## Format: `[ID] [P?] [Story] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US4, US5, US6)
- Include exact file paths in descriptions

## Path Conventions
- Unity project: `Assets/_Project/`
- Scripts: `Assets/_Project/Scripts/`
- Resources: `Assets/_Project/Resources/`
- Editor tools: `Assets/_Project/Editor/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: SDK integration and shared service setup

- [ ] T001 Import Firebase Unity SDK (Analytics + Crashlytics packages) into project
- [ ] T002 Import Google Mobile Ads Unity Plugin into project
- [ ] T003 [P] Add `google-services.json` to `Assets/StreamingAssets/` (Android Firebase config)
- [ ] T004 [P] Add `GoogleService-Info.plist` to `Assets/StreamingAssets/` (iOS Firebase config)
- [ ] T005 Create `Assets/_Project/Resources/Audio/` directory structure (BGM/, SFX/)
- [ ] T006 Create `Assets/_Project/Scripts/Shared/Analytics/` directory
- [ ] T007 Create `Assets/_Project/Scripts/Shared/Monetization/` directory

**Checkpoint**: SDKs and directory structure ready

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core services that all user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T008 Expand `DesignSystem` in `Assets/_Project/Scripts/Shared/UI/BrainBattleDesignSystem.cs` with audio tokens (BGMDefaultVolume, SFXDefaultVolume, BGMFadeDuration, SFXPoolSize)
- [ ] T009 [P] Expand `DesignSystem` with analytics event name tokens (EventLevelStart, EventLevelComplete, EventLevelFail, EventAdWatched)
- [ ] T010 Create `AudioManager.cs` singleton in `Assets/_Project/Scripts/Shared/Audio/` with RuntimeInitializeOnLoadMethod bootstrap
- [ ] T011 Create `AnalyticsManager.cs` singleton in `Assets/_Project/Scripts/Shared/Analytics/` with Firebase initialization

**Checkpoint**: Foundation ready — user story implementation can now begin

---

## Phase 3: User Story 4 - Audio & Polish (Priority: P2) 🎯 MVP for M3

**Goal**: Sound effects for interactions, background music, consistent design system

**Independent Test**: Trigger game interactions → SFX plays; app loads → BGM fades in; UI renders → DesignSystem tokens applied

### Implementation for User Story 4

- [ ] T012 [P] [US4] Add BGM clip (`bgm.mp3`) to `Assets/_Project/Resources/Audio/BGM/` with Streaming load type for Android
- [ ] T013 [P] [US4] Add SFX clips to `Assets/_Project/Resources/Audio/SFX/`: tap_dot.wav, tap_dot_variant.wav, auto_dot.wav, auto_dot_variant.wav, button_tap.wav, invalid_place.wav, invalid_place_variant.wav, victory_sound.wav
- [ ] T014 [US4] Implement SFX pool (8 AudioSources) in `AudioManager.cs`
- [ ] T015 [US4] Implement lazy clip loading (`EnsureClipsLoaded()`) in `AudioManager.cs`
- [ ] T016 [US4] Implement BGM playback with fade-in coroutine in `AudioManager.cs`
- [ ] T017 [US4] Add BGM volume/mute persistence via PlayerPrefs in `AudioManager.cs`
- [ ] T018 [US4] Wire `AudioManager` to `KingsGameManager` events (OnConflictDetected → invalid SFX, OnGameComplete → victory SFX)
- [ ] T019 [US4] Wire `AudioManager` to cell state changes (OnCellStateChanged → tap/dot SFX)
- [ ] T020 [US4] Update `KingsSceneBuilder.cs` to verify no duplicate AudioListener
- [ ] T021 [US4] Test: BGM plays on app launch, continues across scenes, SFX plays on interactions

**Checkpoint**: Audio system complete — game has SFX and BGM

---

## Phase 4: User Story 5 - Daily Challenge (Priority: P2)

**Goal**: Daily puzzle same for all players (seeded by date), streak tracking

**Independent Test**: Generate puzzle on different dates → same date = same puzzle; complete daily → streak increments; skip day → streak resets

### Implementation for User Story 5

- [ ] T022 [P] [US5] Create `DailyChallengeData` runtime class in `Assets/_Project/Scripts/Games/Kings/Logic/DailyChallengeData.cs`
- [ ] T023 [US5] Create `DailyChallengeManager.cs` in `Assets/_Project/Scripts/Games/Kings/Logic/` with seeded RNG from date hash
- [ ] T024 [US5] Implement `GetDailySeed()` method using `DateTime.UtcNow.Date.GetHashCode()`
- [ ] T025 [US5] Implement daily puzzle generation using seeded RNG and `LevelGeneratorService`
- [ ] T026 [US5] Implement streak tracking with PlayerPrefs (Kings_DailyStreak, Kings_LastDailyDate)
- [ ] T027 [US5] Implement streak increment logic (consecutive day = increment, skip = reset)
- [ ] T028 [US5] Add Daily Challenge button to `LevelSelectScene` via `LevelSelectSceneBuilder.cs`
- [ ] T029 [US5] Create `DailyChallengeUI.cs` in `Assets/_Project/Scripts/Games/Kings/UI/` for streak display and puzzle info
- [ ] T030 [US5] Wire `DailyChallengeManager` to `AnalyticsManager` for event logging (daily_start, daily_complete, streak_updated)
- [ ] T031 [US5] Test: Same date on different devices produces same puzzle; streak increments correctly

**Checkpoint**: Daily challenge complete — players have daily reason to return

---

## Phase 5: User Story 6 - Monetization & Analytics (Priority: P3)

**Goal**: AdMob integration (interstitial + rewarded), Firebase Analytics/Crashlytics

**Independent Test**: Complete level → interstitial may show; request hint → rewarded ad → hint unlocked; game actions → Firebase events logged

### Implementation for User Story 6

- [ ] T032 [P] [US6] Create `AdManager.cs` singleton in `Assets/_Project/Scripts/Shared/Monetization/`
- [ ] T033 [P] [US6] Create `AnalyticsEvent.cs` enum in `Assets/_Project/Scripts/Shared/Analytics/`
- [ ] T034 [US6] Initialize AdMob in `AdManager.cs` (early, in LevelSelect scene)
- [ ] T035 [US6] Implement interstitial ad loading and preloading in `AdManager.cs`
- [ ] T036 [US6] Implement rewarded ad loading in `AdManager.cs`
- [ ] T037 [US6] Wire interstitial ad to `VictoryPanel.OnGameComplete()` (show if level % frequency == 0)
- [ ] T038 [US6] Wire rewarded ad to `TipsButton.OnClick()` (show ad → unlock hint on completion)
- [ ] T039 [US6] Initialize Firebase Analytics in `AnalyticsManager.cs`
- [ ] T040 [US6] Implement event logging methods in `AnalyticsManager.cs` (LogLevelStart, LogLevelComplete, LogLevelFail, LogAdWatched)
- [ ] T041 [US6] Wire `AnalyticsManager` to `KingsGameManager` events for automatic logging
- [ ] T042 [US6] Initialize Firebase Crashlytics for crash reporting
- [ ] T043 [US6] Create AdMob config file `Assets/_Project/Resources/admob_config.json` with test ad unit IDs
- [ ] T044 [US6] Test: Interstitial shows between levels; rewarded ad unlocks hint; events appear in Firebase Console

**Checkpoint**: Monetization and analytics complete — game is production-ready

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final integration and validation

- [ ] T045 [P] Update `Milestones.md` to mark M3 complete
- [ ] T046 [P] Update `Milestones.md` to mark M4 complete
- [ ] T047 [P] Update `Milestones.md` to mark M5 complete
- [ ] T048 [P] Update `Milestones.md` to mark M6 complete
- [ ] T049 Review SampleScene wiring/build readiness after scene-related changes
- [ ] T050 Test full game flow: LevelSelect → Play → Win → Victory → Next Level with audio and ads
- [ ] T051 Build Android APK and test on device (audio, ads, analytics)
- [ ] T052 Build iOS and test on device (audio, ads, analytics)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion
  - US4 (Audio) can start after Phase 2
  - US5 (Daily Challenge) can start after Phase 2
  - US6 (Monetization) can start after Phase 2
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 4 (P2)**: Can start after Foundational — no dependencies on other stories
- **User Story 5 (P2)**: Can start after Foundational — no dependencies on other stories
- **User Story 6 (P3)**: Can start after Foundational — no dependencies on other stories

**Note**: All three user stories (US4, US5, US6) can be implemented in parallel if team capacity allows.

### Within Each User Story

- Audio clips before AudioManager implementation
- Manager implementation before wiring to existing code
- Core implementation before testing
- Story complete before moving to next

### Parallel Opportunities

- T003, T004: Firebase config files (different platforms)
- T008, T009: DesignSystem token expansions (same file but different sections)
- T012, T013: Audio clips (different resources)
- T022, T032, T033: New class files (different paths)
- T045, T046, T047, T048: Milestone updates (different sections)

---

## Parallel Example: User Story 4 (Audio)

```bash
# Launch audio clips together:
Task: "Add BGM clip to Assets/_Project/Resources/Audio/BGM/"
Task: "Add SFX clips to Assets/_Project/Resources/Audio/SFX/"

# These must be sequential (same file):
T014 → T015 → T016 → T017 → T018 → T019 (AudioManager.cs)
```

## Parallel Example: User Story 5 (Daily Challenge)

```bash
# Launch together (different files):
Task: "Create DailyChallengeData runtime class"
Task: "Create AdManager.cs singleton"
Task: "Create AnalyticsEvent.cs enum"
```

---

## Implementation Strategy

### MVP First (User Story 4 Only = M3)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 4 (Audio)
4. **STOP and VALIDATE**: Test audio independently
5. Deploy with audio polish

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 4 (Audio) → Test independently → Deploy (M3 complete!)
3. Add User Story 5 (Daily Challenge) → Test independently → Deploy (M4 complete!)
4. Add User Story 6 (Monetization) → Test independently → Deploy (M5/M6 complete!)
5. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 4 (Audio)
   - Developer B: User Story 5 (Daily Challenge)
   - Developer C: User Story 6 (Monetization)
3. Stories complete and integrate independently

---

## Task Summary

| Category | Count |
|----------|-------|
| Setup | 7 |
| Foundational | 4 |
| US4 (Audio) | 10 |
| US5 (Daily Challenge) | 10 |
| US6 (Monetization) | 13 |
| Polish | 8 |
| **Total** | **52** |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US1-US3 already implemented (M1-M2 complete)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Review SampleScene wiring/build readiness after any scene-related changes
- Update `Milestones.md` after each milestone completion
