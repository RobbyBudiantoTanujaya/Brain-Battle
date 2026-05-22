---
last_compound_scan: 2026-05-22
compound_scan_mode: full
---
<!--
Sync Impact Report
- Version change: 2.0 -> 2.1
- Modified standards: edit-mode rule verification, reproducible generated content, runtime verification for UI work
- Added sections: explicit amendment workflow, semantic versioning policy, compliance review expectations
- Removed sections: none
- Cross-validation results: ✅ consistent with CONSTITUTION.md, RELIABILITY.md, SECURITY.md, and CODING.md
- Templates requiring updates: ✅ updated `.ttadk/plugins/ttadk/core/resources/templates/plan-template.md`; ✅ updated `.claude/commands/adk/readiness.md`; ✅ updated `.claude/commands/adk/sdd/constitution.md`; ✅ updated `.claude/commands/adk/sdd/{plan,tasks,implement,clarify,specify,analyze,ff,simplify}.md`
- Follow-up TODOs: none
-->

# Brain Battle Quality Standards

## Core Standards

### I. Rules are verified away from the scene

Correctness-critical puzzle behavior must stay testable without entering play mode.
Core board rules are expected to be validated in focused tests so scene churn does not
hide regressions in row, column, region, or adjacency logic.

### II. Generated content must stay reproducible

Quality includes the ability to rebuild the same scene wiring and level content from
code. A change that only works after manual scene edits or hand-authored puzzle data is
considered fragile even if it appears correct once.

### III. UI work must be validated in runtime context

Compile success is not enough for player-facing changes. UI and interaction changes must
be verified in the running game so layout timing, input handling, and gameplay flow are
checked where they actually execute.

## Fixed Rules

- **Edit-mode rule coverage is required for shared puzzle logic**: Core rule validation
  must stay protected by NUnit edit-mode tests rather than only by manual play checks.
  Evidence: file "Assets/_Project/Tests/EditMode/ConstraintValidatorTests.cs"
- **Generated levels must pass uniqueness checks**: Level generation does not produce a
  finished level unless uniqueness verification succeeds.
  Evidence: grep "KingsUniquenessVerifier\.Verify" @ Assets/_Project/Scripts
- **Scene wiring must stay reviewable as code**: The playable Kings scene is expected to
  remain rebuildable from editor code rather than relying on silent scene drift.
  Evidence: file "Assets/_Project/Editor/KingsSceneBuilder.cs"
- **UI changes require runtime verification**: Player-facing UI work is validated in the
  running app, not only by static checks.
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

Quality Standards v2.1 -- aligned with current testing and verification expectations,
2026-05-22
