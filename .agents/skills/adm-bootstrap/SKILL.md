---
name: adm-bootstrap
description: One-time ADM setup in a new project. Creates the .agent-method structure, charter and first phase through a short interview, with human confirmation at each decision point.
---

# adm-bootstrap

## Purpose
Set up a fresh, consistent ADM structure in a new (or empty) repository.

## When to use
Once, at project start. For existing codebases use `adm-analyze` (advanced).

## Inputs
Project id/name, initial intent (interview the user), human language preference.

## Canonical files read
Template package contents (this repository's `.agent-method/` skeleton).

## Canonical files written
- `.agent-method/` skeleton: `config.yaml`, `state.yaml`, `charter/`, `cycles/`,
  `decisions/`, `registry/`, `pipeline/`, `evidence/` dirs, `schemas/`, `docs/`.
- `AGENTS.md`, `CLAUDE.md` (adapter), `.agents/skills/`, `.claude/skills/` shims.
- Charter content (`PROJECT_INTENT.md`, `PHASES.md`) from the interview.

## Preconditions
Git repository exists (or human approves `git init`). No existing `.agent-method/`.

## Procedure
1. Interview the user: purpose, goals, out-of-scope (**concrete keywords** — the
   Doctor uses them mechanically), constraints, success criteria.
2. Write charter files; get explicit human confirmation of the intent text.
3. Create `config.yaml` (set `method.language`) and `state.yaml`
   (project block; empty session; no active GD yet).
4. Propose PHASE-001 and the first 1-3 POOL items; human approves.
   Set `active.phase_id` on approval.
5. Verify structure against `docs/INVARIANTS.md` (INV-14, INV-16, INV-17 —
   with the default empty projection list there must be NO `generated/` directory).
6. Output a short "project initialized" briefing pointing to `adm-open`.

## Invariants
Establishes preconditions for all INV rules.

## Expected output
A ready-to-use ADM project; no application code written.

## Failure behavior
- No Git and human declines init: stop; ADM requires Git (`config.git.required`).
- Existing `.agent-method/`: do not overwrite; report and suggest adm-doctor / adm-analyze.

## Human approval points
Intent text, phase plan, language choice, any overwrite.

## Runtime capability fallback
File creation via the runtime's native tools; no shell scripts required.
