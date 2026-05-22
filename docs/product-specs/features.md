---
last_compound_scan: 2026-05-22
compound_scan_mode: full
---

# Feature Catalog

## Table of Contents

- [Overview](#overview)
- [Core Puzzle Loop](#core-puzzle-loop)
- [Assistive Features](#assistive-features)
- [Progression and Results](#progression-and-results)
- [Editor Workflows](#editor-workflows)

## Overview

The implemented feature set centers on a single Kings puzzle mode with guided onboarding, touch-friendly board interaction, bounded undo, hints, and editor automation for content generation and scene assembly.

## Core Puzzle Loop

### Kings puzzle validation

Players interact with a mutable board that enforces one crown per row, per column, and per region, while also rejecting adjacent crowns. `ConstraintValidator` exposes both move-time validation and full-board win detection, so the same rules drive live feedback and completion.

**Source**: `Assets/_Project/Scripts/Core/Engine/ConstraintValidator.cs` [Code Direct] [Verified]

---

### Tap and drag board interaction

The board supports single-tap dot toggling, double-tap crown placement, crown clearing, and drag-to-repeat the chosen target state across multiple cells. This gives mobile-friendly marking speed without allowing drag to overwrite crowns implicitly.

**Source**: `Assets/_Project/Scripts/Games/Kings/Logic/KingsGameManager.cs` [Code Direct] [Verified]

## Assistive Features

### First-run tutorial overlay

The game can show a five-step tutorial overlay that explains the crown-placement rules and interaction model, then suppresses itself on later runs using a `PlayerPrefs` flag unless force-shown.

**Source**: `Assets/_Project/Scripts/Games/Kings/UI/TutorialController.cs` [Code Direct] [Verified]

---

### Hints and cooldown

The tips flow surfaces up to three deterministic hints whenever a row, column, or region has exactly one legal crown placement, and it rate-limits repeated requests with a 10-second cooldown. This keeps hint quality tied to real rule deductions rather than random suggestions.

**Source**: `Assets/_Project/Scripts/Shared/UI/TipsButton.cs` [Code Direct] [Verified]

---

### Auto-dot marking

Placing a crown automatically marks forbidden cells in the same row, column, region, and adjacent ring as dots, and removing that crown retracts only the dots that are no longer covered by other crowns. The player gets faster bookkeeping without losing manual control of the board.

**Source**: `Assets/_Project/Scripts/Games/Kings/Logic/KingsGameManager.cs` [Code Direct] [Verified]

## Progression and Results

### Undo and restart controls

Undo restores prior `GridData` snapshots from a bounded stack, while restart resets the session to the initial deep-copied grid and optionally asks for confirmation once the player has already made moves.

**Source**: `Assets/_Project/Scripts/Shared/UI/UndoButton.cs` [Code Direct] [Verified]

---

### Timed victory and star rating

On win, the game records elapsed time, move count, hints used, and grid size to compute a one-to-three-star rating, then persists the best result per level in `PlayerPrefs`. The victory panel also supports restart and next-level progression without reloading the whole scene.

**Source**: `Assets/_Project/Scripts/Shared/UI/VictoryPanel.cs` [Code Direct] [Verified]

## Editor Workflows

### Procedural level generation

Design-time content generation produces five level assets from deterministic seeds and board sizes through a menu action instead of hand-authoring puzzles. This keeps level creation aligned with the documented generator pipeline and the project rule against manually computing levels.

**Source**: `Assets/_Project/Editor/KingsLevelGenerator.cs` [Code Direct] [Verified]

---

### One-click scene assembly

A dedicated editor tool rebuilds the Kings scene hierarchy, UI objects, and serialized references in one operation so a playable scene can be regenerated consistently after structural changes.

**Source**: `Assets/_Project/Editor/KingsSceneBuilder.cs` [Code Direct] [Verified]
