---
last_compound_scan: 2026-05-22
compound_scan_mode: full
---
<!--
Sync Impact Report
- Version change: 2.0 -> 2.1
- Modified principles: deterministic puzzle content, editor-authored runtime setup, logic-first runtime boundaries, milestone-traceable delivery
- Added sections: explicit amendment workflow, semantic versioning policy, compliance review expectations
- Removed sections: none
- Cross-validation results: ✅ consistent with QUALITY.md, RELIABILITY.md, SECURITY.md, and CODING.md
- Templates requiring updates: ✅ updated `.ttadk/plugins/ttadk/core/resources/templates/plan-template.md`; ✅ updated `.claude/commands/adk/readiness.md`; ✅ updated `.claude/commands/adk/sdd/constitution.md`; ✅ updated `.claude/commands/adk/sdd/{plan,tasks,implement,clarify,specify,analyze,ff,simplify}.md`
- Follow-up TODOs: none
-->

# Brain Battle Constitution

## Core Principles

### I. Deterministic puzzle content

Kings puzzle content must remain reproducible from explicit inputs such as size,
difficulty, and seed. Level data is not treated as hand-authored truth; it is only
accepted once the generator pipeline and uniqueness verification have produced a
reviewable result.

### II. Editor-authored runtime setup

Repeatable scene structure, asset generation, and reference wiring must be owned by
editor tooling or other deterministic setup paths. Manual hierarchy edits are valid for
investigation, but they do not replace the scripted rebuild workflow.

### III. Logic-first runtime boundaries

Puzzle rules, board models, and generation logic must stay separable from Unity
lifecycle code. MonoBehaviours are adapters for input, rendering, persistence, and
startup orchestration; they do not become the home for core game rules.

### IV. Milestone-traceable delivery

Every completed change must leave the project in a traceable state across code,
documentation, and milestone tracking. The repository should show not only what was
implemented, but also whether project status documents still match the current game.

## Fixed Rules

- **Generated levels stay algorithmic**: Kings levels are generated through code and
  saved as `LevelData` assets rather than being hand-computed cell-by-cell.
  Evidence: file "Assets/_Project/Documentation/KingsLevelGeneration.md"
- **Scene assembly stays reproducible**: The playable Kings scene hierarchy and its
  serialized references must remain rebuildable by editor automation.
  Evidence: file "Assets/_Project/Editor/KingsSceneBuilder.cs"
- **Core rule enforcement stays centralized**: Placement validation and win detection
  remain owned by shared gameplay logic instead of being duplicated across UI code.
  Evidence: file "Assets/_Project/Scripts/Core/Engine/ConstraintValidator.cs"
- **Task completion keeps milestone tracking current**: Finishing work requires the
  project milestone tracker to reflect the newly completed state.
  Evidence: file "CLAUDE.md"

## Governance

- **Amendment process**: Update the affected constitution document, cross-check the
  other four constitution files, and sync any dependent SDD templates or command docs
  in the same change.
- **Versioning policy**: Use semantic versioning for the constitution set: MAJOR for
  incompatible rule removals or redefinitions, MINOR for new substantive rules or
  materially expanded guidance, PATCH for clarifications.
- **Compliance review**: Before adopting an amendment, verify the change still aligns
  with the current codebase, project instructions, and the rest of the constitution set.

Constitution v2.1 -- aligned with the five-document project policy set, 2026-05-22
