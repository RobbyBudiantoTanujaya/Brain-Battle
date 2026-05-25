---
description: "Implementation plan for LinkedIn Queens Game (Kings puzzle enhancement)"
---

# Implementation Plan: LinkedIn Queens Game

**Feature**: `linkedin-queens-game` | **Date**: 2026-05-25 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/20260525-linkedin-queens-game/spec.md`

**Note**: This plan extends the existing Kings puzzle implementation. M1 (Core Game) and M2 (Level Generation) are already complete. This plan focuses on M3-M6 enhancements.

## Summary

Implement full-featured LinkedIn Queens game (Kings puzzle) with polish, audio, daily challenge, and monetization. The core puzzle mechanics (N×N crown placement, constraint validation, level generation) already exist. This implementation adds audio system, design system tokens, daily challenge with seeded RNG, AdMob integration, and Firebase Analytics/Crashlytics.

## Technical Context

**Language/Version**: C# / Unity 6.0.75f1 LTS
**Primary Dependencies**: Unity UI, TextMeshPro (Static atlas), URP 2D, AdMob SDK, Firebase SDK
**Storage**: PlayerPrefs (progress, settings), ScriptableObject (LevelData assets)
**Testing**: Unity Test Framework (EditMode + PlayMode)
**Target Platform**: Android + iOS (mobile 2D puzzle game)
**Project Type**: mobile
**Performance Goals**: 60fps gameplay, <3s load time, <100MB memory target
**Constraints**: Offline-capable core, determinism across platforms, Static TMP fonts for Android
**Scale/Scope**: ~50 scripts, 35+ level assets, 10-15 audio clips

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. Deterministic puzzle content | ✅ Pass | Existing: `LevelGeneratorService` with seeded generation |
| II. Editor-authored runtime setup | ✅ Pass | Existing: `KingsSceneBuilder` owns scene hierarchy |
| III. Logic-first runtime boundaries | ✅ Pass | Existing: `ConstraintValidator` pure logic, no MonoBehaviour |
| IV. Milestone-traceable delivery | ✅ Pass | `Milestones.md` tracks M1-M6; M3-M6 in scope |

**Gate Result**: PASS — All principles satisfied. Existing architecture supports planned enhancements.

## Project Structure

### Documentation (this feature)

```
specs/20260525-linkedin-queens-game/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (event contracts)
└── tasks.md             # Phase 2 output
```

### Source Code (existing + new)

```
Assets/_Project/
├── Scripts/
│   ├── Core/
│   │   ├── Models/           # GridData, CellData, RegionData (EXISTING)
│   │   ├── Engine/           # ConstraintValidator (EXISTING)
│   │   └── Generators/       # LevelGeneratorService (EXISTING)
│   ├── Games/Kings/
│   │   ├── Logic/            # KingsGameManager (EXISTING)
│   │   ├── UI/               # KingsGridRenderer, VictoryPanel (EXISTING)
│   │   └── Data/             # LevelData, LevelLoader (EXISTING)
│   └── Shared/
│       ├── UI/               # DesignSystem (EXISTING, tokens to expand)
│       ├── Audio/            # AudioManager (NEW - M3)
│       └── Analytics/        # Firebase integration (NEW - M6)
├── Editor/
│   └── KingsSceneBuilder.cs  # Update for audio wiring
├── Resources/
│   ├── Audio/                # NEW - SFX + BGM clips
│   └── Sprites/              # Existing sprites
└── ScriptableObjects/
    └── Kings/Levels/         # Existing level assets

Assets/Scenes/
├── SampleScene.unity         # Gameplay (EXISTING, wire audio)
└── LevelSelect.unity         # Menu (EXISTING)
```

**Structure Decision**: Mobile project with existing Kings puzzle foundation. New code integrates with established patterns (event-driven UI, editor-authored setup, logic-first boundaries).

## Complexity Tracking

*No constitution violations — table empty.*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
