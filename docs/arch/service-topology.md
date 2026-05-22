---
last_compound_scan: 2026-05-22
compound_scan_mode: full
---

# Service Topology

## Table of Contents

- [Overview](#overview)
- [Runtime Flow](#runtime-flow)
- [Editor-Time Flow](#editor-time-flow)
- [State Boundaries](#state-boundaries)

## Overview

This project is a single-player Unity game rather than a networked service mesh, so the meaningful topology is the dependency chain between editor tooling, asset loading, gameplay logic, and UI presentation. The key contract is that editor-time builders author or wire everything ahead of play mode, and runtime components only exchange strongly typed model objects and event payloads.

## Runtime Flow

### Scene bootstrap contract

`KingsSceneBootstrap.LoadLevel(int levelNumber)` requests a `LevelData` asset from `LevelLoader`, converts it to `GridData`, and passes the result into `KingsGameManager.StartGame`. The request/response shape is therefore `int levelNumber -> LevelData -> GridData`, and the main error contract is a missing serialized dependency or missing level asset, both of which are logged and abort startup.

**Source**: `Assets/_Project/Scripts/Games/Kings/Logic/KingsSceneBootstrap.cs` [Code Direct] [Verified]

---

### Interaction contract between renderer and game manager

`KingsGridRenderer` emits `(row, col)` and `(row, col, CellState)` events, while `KingsGameManager` responds by mutating the grid and pushing visual updates back through `RenderGrid`, `UpdateCell`, and conflict highlighting. The contract is intentionally narrow: the renderer never decides validity, and the manager never owns pointer event plumbing.

**Source**: `Assets/_Project/Scripts/Games/Kings/UI/KingsGridRenderer.cs` [Code Direct] [Verified]

---

### Completion and HUD contract

`KingsGameManager` publishes completion, undo-stack, and conflict signals that downstream UI components consume to update button state, show results, or animate conflicts. The response and error surface stays local to the scene: there are no transport retries or remote codes, only guarded null checks and early returns when a dependency is missing.

**Source**: `Assets/_Project/Scripts/Games/Kings/Logic/KingsGameManager.cs` [Code Direct] [Verified]

## Editor-Time Flow

### Level asset generation pipeline

`KingsLevelGenerator` invokes `LevelGeneratorService.GenerateLevel`, then persists the returned `LevelData` objects through `AssetDatabase` into `Assets/_Project/ScriptableObjects/Kings/Levels`. The effective interface is `menu action -> generated LevelData -> saved asset path`, with failures surfaced as editor logs when unique generation exhausts retry bounds.

**Source**: `Assets/_Project/Editor/KingsLevelGenerator.cs` [Code Direct] [Verified]

---

### Scene rebuild pipeline

`KingsSceneBuilder` creates the canvas, managers, panels, renderer, and serialized field assignments in one editor transaction, then saves the current scene. That makes the scene builder the owning integration point for most cross-component dependencies in the project.

**Source**: `Assets/_Project/Editor/KingsSceneBuilder.cs` [Code Direct] [Verified]

## State Boundaries

Runtime state crosses boundaries through three storage forms only: `LevelData` assets for authored puzzle input, `GridData` instances for mutable session state, and `PlayerPrefs` keys for lightweight persistence such as current grid snapshots, tutorial completion, elapsed time, moves, and star ratings. This narrow boundary keeps editor tooling, puzzle logic, and UI presentation coupled by data contracts rather than by direct scene lookups.
