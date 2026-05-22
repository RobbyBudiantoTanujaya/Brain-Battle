---
last_compound_scan: 2026-05-22
compound_scan_mode: full
---
<!--
Sync Impact Report
- Version change: 2.0 -> 2.1
- Modified conventions: explicit inspector wiring, pure logic boundaries, coordinate consistency, code-generated level content
- Added sections: explicit amendment workflow, semantic versioning policy, compliance review expectations
- Removed sections: none
- Cross-validation results: ✅ consistent with CONSTITUTION.md, QUALITY.md, RELIABILITY.md, and SECURITY.md
- Templates requiring updates: ✅ updated `.ttadk/plugins/ttadk/core/resources/templates/plan-template.md`; ✅ updated `.claude/commands/adk/readiness.md`; ✅ updated `.claude/commands/adk/sdd/constitution.md`; ✅ updated `.claude/commands/adk/sdd/{plan,tasks,implement,clarify,specify,analyze,ff,simplify}.md`
- Follow-up TODOs: none
-->

# Brain Battle Coding Standards

## Core Conventions

### I. Inspector wiring stays explicit

Runtime scene dependencies are expected to arrive through private serialized fields that
the editor builder or Inspector assigns ahead of play mode. Public mutable scene
references are avoided so wiring remains reviewable and predictable.

### II. Pure rules stay outside MonoBehaviour

Puzzle rules, generation steps, and board models live in plain C# types so they can be
tested, copied, and reused without Unity lifecycle coupling. MonoBehaviours act as
adapters for rendering, input, persistence, and startup.

### III. Coordinate conventions must not drift

The project relies on a single `Vector2Int(col, row)` convention across generation,
loading, validation, rendering, and auto-dot placement. Cross-layer code must preserve
that contract or board behavior becomes subtly incorrect.

### IV. Level content is code-generated, not hand-authored

Level grids and region layouts are produced by generator code and editor tooling rather
than by manually maintaining coordinates in source or docs. Changes to level generation
belong in executable code paths that can be rerun.

## Fixed Rules

- **Scene references stay in private serialized fields**: Scene object dependencies are
  stored in private `[SerializeField]` fields rather than public mutable fields.
  Evidence: grep "\[SerializeField\] private" @ Assets/_Project/Scripts
- **Runtime coordination uses System.Action events**: Gameplay UI coordination uses C#
  events and `Action` delegates instead of `UnityEvent`.
  Evidence: grep "public event Action\|public event System.Action" @ Assets/_Project/Scripts
- **Core puzzle logic stays out of MonoBehaviour**: Constraint validation, generators,
  and board models remain plain classes or static helpers, not behaviour components.
  Evidence: file "Assets/_Project/Scripts/Core/Engine/ConstraintValidator.cs"
- **Level generation changes are implemented algorithmically**: Level cell data is not
  maintained by manual computation; generator code and editor-time execution are the
  authoritative path.
  Evidence: file "Assets/_Project/Documentation/KingsLevelGeneration.md"

## Governance

- **Amendment process**: Update the affected constitution document, cross-check the
  other four constitution files, and sync any dependent SDD templates or command docs
  in the same change.
- **Versioning policy**: Use semantic versioning for the constitution set: MAJOR for
  incompatible rule removals or redefinitions, MINOR for new substantive rules or
  materially expanded guidance, PATCH for clarifications.
- **Compliance review**: Before adopting an amendment, verify the change still aligns
  with the current codebase, project instructions, and the rest of the constitution set.

Coding Standards v2.1 -- aligned with current Unity coding and generator conventions,
2026-05-22
