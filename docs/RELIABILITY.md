---
last_compound_scan: 2026-05-22
compound_scan_mode: full
---
<!--
Sync Impact Report
- Version change: 2.0 -> 2.1
- Modified practices: layout-safe startup, cloned mutable state, bounded retry and history paths
- Added sections: explicit amendment workflow, semantic versioning policy, compliance review expectations
- Removed sections: none
- Cross-validation results: ✅ consistent with CONSTITUTION.md, QUALITY.md, SECURITY.md, and CODING.md
- Templates requiring updates: ✅ updated `.ttadk/plugins/ttadk/core/resources/templates/plan-template.md`; ✅ updated `.claude/commands/adk/readiness.md`; ✅ updated `.claude/commands/adk/sdd/constitution.md`; ✅ updated `.claude/commands/adk/sdd/{plan,tasks,implement,clarify,specify,analyze,ff,simplify}.md`
- Follow-up TODOs: none
-->

# Brain Battle Reliability Practices

## Core Practices

### I. Startup waits for valid UI layout

Runtime board rendering depends on canvas layout being settled before the first level is
loaded. Startup logic must protect against zero-sized UI bounds and similar timing issues
instead of assuming layout is already valid.

### II. Mutable gameplay state is copied, not shared

Restart, undo, and save flows depend on cloned board state rather than shared mutable
references. Runtime sessions operate on deep-copied grids so authored data and initial
snapshots are never mutated accidentally.

### III. Recovery paths stay bounded and observable

Potentially expensive loops such as level generation retries and undo growth must have
explicit limits and visible failure signals. Reliability depends on predictable bounds,
not silent repetition.

## Fixed Rules

- **Bootstrap waits one frame before first load**: Scene startup defers `LoadLevel(1)`
  until canvas layout has run.
  Evidence: file "Assets/_Project/Scripts/Games/Kings/Logic/KingsSceneBootstrap.cs"
- **Grid copies use JsonUtility round-trips**: Restart and undo depend on JSON-based
  cloning so serialized board state is rebuilt consistently.
  Evidence: grep "JsonUtility\.FromJsonOverwrite" @ Assets/_Project/Scripts
- **Undo history is bounded**: Gameplay history remains capped to a fixed maximum size.
  Evidence: grep "MaxUndoHistory" @ Assets/_Project/Scripts
- **Level generation retries are bounded and logged**: Seed bumps and uniqueness retries
  stop at explicit limits and surface failures through logs.
  Evidence: grep "MaxSeedBumps\|MaxRetries\|Debug\.LogError" @ Assets/_Project/Scripts/Core/Generators

## Governance

- **Amendment process**: Update the affected constitution document, cross-check the
  other four constitution files, and sync any dependent SDD templates or command docs
  in the same change.
- **Versioning policy**: Use semantic versioning for the constitution set: MAJOR for
  incompatible rule removals or redefinitions, MINOR for new substantive rules or
  materially expanded guidance, PATCH for clarifications.
- **Compliance review**: Before adopting an amendment, verify the change still aligns
  with the current codebase, project instructions, and the rest of the constitution set.

Reliability Practices v2.1 -- aligned with current startup, cloning, and retry guards,
2026-05-22
