---
last_compound_scan: 2026-05-22
compound_scan_mode: full
---

# Design Patterns

## Table of Contents

- [Overview](#overview)
- [Creational Patterns](#creational-patterns)
- [Behavioral Patterns](#behavioral-patterns)
- [Pattern Dependencies](#pattern-dependencies)

## Overview

The Kings puzzle codebase concentrates object creation in editor-time generators and scene builders, while runtime gameplay flows through event-driven UI orchestration. The recurring pattern is to keep puzzle rules and data models pure, then bind them to Unity lifecycle code only at scene bootstrap and rendering boundaries.

## Creational Patterns

### Editor-driven scene composition

`KingsSceneBuilder` rebuilds the full playable scene by creating root objects, attaching UI and gameplay components, and wiring serialized references in one pass. This keeps scene setup reproducible and prevents the hand-wired hierarchy drift described in the scene guide.

**Source**: `Assets/_Project/Editor/KingsSceneBuilder.cs` [Code Direct] [Verified]

---

### Deterministic level factory

`LevelGeneratorService.GenerateLevel` acts as a deterministic factory for `LevelData`, chaining queen placement, region construction, uniqueness verification, and `ScriptableObject` materialization behind one entry point. The caller provides `(levelNumber, size, difficulty, seed)` and receives either a fully initialized asset payload or `null` after bounded retries.

**Source**: `Assets/_Project/Scripts/Core/Generators/LevelGeneratorService.cs` [Code Direct] [Verified]

## Behavioral Patterns

### Event-driven gameplay coordination

`KingsGameManager` exposes gameplay state changes as `Action` events (`OnWin`, `OnGameComplete`, `OnConflictDetected`, `OnUndoStackChanged`) and lets UI components subscribe in `OnEnable` instead of polling or reaching into manager internals. This keeps gameplay rules centralized while Undo, Victory, and grid conflict visuals remain independently replaceable.

**Source**: `Assets/_Project/Scripts/Games/Kings/Logic/KingsGameManager.cs` [Code Direct] [Verified]

---

### Shared drag-state mediator

`KingsGridRenderer` uses a nested `CellEventHandler` on each cell, but keeps the drag target state and last-visited cell on the renderer itself. That creates a mediator-style interaction layer where per-cell handlers stay stateless and all gesture interpretation remains consistent across the whole board.

**Source**: `Assets/_Project/Scripts/Games/Kings/UI/KingsGridRenderer.cs` [Code Direct] [Verified]

## Pattern Dependencies

The editor composition pattern feeds the runtime event pattern: `KingsSceneBuilder` serializes the references that `KingsSceneBootstrap` and `KingsGameManager` depend on, and runtime interaction handlers assume that wiring is already correct. The deterministic level factory also depends on the pure-model boundary, because generated `LevelData` must later be reconstructed by `LevelLoader` into mutable `GridData` without any editor-only code leaking into play mode.
