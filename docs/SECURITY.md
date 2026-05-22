---
last_compound_scan: 2026-05-22
compound_scan_mode: full
---
<!--
Sync Impact Report
- Version change: 2.0 -> 2.1
- Modified practices: local-only security boundary, editor/runtime isolation, low-sensitivity persistence rules
- Added sections: explicit amendment workflow, semantic versioning policy, compliance review expectations
- Removed sections: none
- Cross-validation results: ✅ consistent with CONSTITUTION.md, QUALITY.md, RELIABILITY.md, and CODING.md
- Templates requiring updates: ✅ updated `.ttadk/plugins/ttadk/core/resources/templates/plan-template.md`; ✅ updated `.claude/commands/adk/readiness.md`; ✅ updated `.claude/commands/adk/sdd/constitution.md`; ✅ updated `.claude/commands/adk/sdd/{plan,tasks,implement,clarify,specify,analyze,ff,simplify}.md`
- Follow-up TODOs: none
-->

# Brain Battle Security Practices

## Core Practices

### I. The current gameplay surface is local-only

The implemented Kings mode does not expose remote authentication or authorization flows.
The meaningful security boundary in the current codebase is therefore local state
handling, build/runtime separation, and avoiding trust in user-modifiable progress data.

### II. Build-only tooling stays out of runtime paths

Editor automation must remain isolated under editor-only code paths. Shipped gameplay
code should not depend on tooling APIs that do not exist outside the Unity editor.

### III. Persist only low-sensitivity progress data

Local persistence is limited to convenience data such as board state, tutorial flags,
timers, move counts, and star ratings. These values support UX continuity but are not a
trusted authority for credentials, entitlement, or remote trust decisions.

## Fixed Rules

- **Editor APIs stay in editor tooling**: AssetDatabase and MenuItem workflows live
  under the editor folder rather than the runtime script tree.
  Evidence: glob "Assets/_Project/Editor/*.cs"
- **Saved state is local progress only**: Runtime persistence writes grid snapshots,
  timer state, moves, tutorial flags, and stars through `PlayerPrefs`.
  Evidence: grep "PlayerPrefs\.Set\|PlayerPrefs\.DeleteKey" @ Assets/_Project/Scripts
- **Critical missing references fail closed**: Startup paths stop on missing serialized
  dependencies instead of proceeding with partially initialized gameplay state.
  Evidence: grep "LevelLoader or KingsGameManager reference is null\|_gridRenderer is null" @ Assets/_Project/Scripts
- **No remote auth surface is assumed in gameplay scripts**: Current gameplay code is
  built around local assets and scene state rather than credential-bearing network flows.
  Evidence: grep "PlayerPrefs\|BuildGridFromLevel\|LoadLevel" @ Assets/_Project

## Governance

- **Amendment process**: Update the affected constitution document, cross-check the
  other four constitution files, and sync any dependent SDD templates or command docs
  in the same change.
- **Versioning policy**: Use semantic versioning for the constitution set: MAJOR for
  incompatible rule removals or redefinitions, MINOR for new substantive rules or
  materially expanded guidance, PATCH for clarifications.
- **Compliance review**: Before adopting an amendment, verify the change still aligns
  with the current codebase, project instructions, and the rest of the constitution set.

Security Practices v2.1 -- aligned with current local-state and editor/runtime security
boundaries, 2026-05-22
