---
name: adm-open
description: Restore project bearings at session start. Reads canonical state, verifies consistency, opens the session record and produces a short briefing. Use at the beginning of every working session in any runtime.
---

# adm-open

## Purpose
Rebuild full working context for a stateless agent session — in any runtime,
without any previous conversation history — and verify the structure is
consistent before work starts.

## When to use
At the start of every session; especially right after a runtime handoff.

## Inputs
- Optional: GD id the user intends to work on.

## Canonical files read
1. `AGENTS.md`
2. `.agent-method/config.yaml`
3. `.agent-method/state.yaml`
4. Active GD file (from `state.active.gd_id`; project-wide check = GDs with `status: in-progress`)
5. Active Phase in `charter/PHASES.md`
6. Decisions referenced by the active GD (`decisions_followed`)
7. Latest closed session in `evidence/sessions/` (by `closed_at` metadata — never by file position)
8. Latest doctor report in `evidence/doctor/` and latest review in `evidence/reviews/` (unresolved `status: open` critical findings)
9. Git status: current branch, uncommitted changes, HEAD sha

## Canonical files written
- `state.yaml → session` block only: if no session is open, start one
  (new unique `S-NNN` id, runtime name, `started_at`, `start_sha` = current HEAD).
- Nothing else. This skill never "fixes" anything.

## Preconditions
- Repository is a Git repository (see Failure behavior otherwise).

## Procedure
1. Read the files above.
2. Consistency checks:
   - Active phase exists in `PHASES.md` and respects `max_active_phases` (INV-01, INV-03).
   - `active.gd_id` exists and its GD status is `in-progress` (INV-04, INV-05).
   - Active GD's `phase` matches the active phase.
   - **Session check (INV-13):** if `state.yaml → session` already shows an open
     session, this repository assumes ONE active session. Report it (id, runtime,
     started_at) and ask the user: previous session not closed properly (offer to
     close it as evidence first) or genuinely concurrent use (unsupported in alpha).
     Never silently overwrite the session block.
   - Latest doctor/review reports: unresolved `status: open` critical findings → blockers (INV-18).
   - If `config.generated.projections` is non-empty: files under `generated/`
     exactly match that list and carry the GENERATED header (INV-17).
3. Produce a short briefing (language: `config.method.language`):
   project + active phase; active GD + its next unchecked plan item; blockers;
   previous session's runtime and "next step" note; review/doctor status; git state.
4. If the user named a different GD than state: surface the mismatch, ask — do not silently switch.

## Invariants
Checks INV-01, INV-03, INV-04, INV-05, INV-13, INV-17, INV-18.

## Expected output
A concise human-language briefing, e.g.:

```text
Project: Consumer Portal
Phase: Authentication
Active GD: GD-014 — Refresh Token (in progress)
Previous runtime: claude-code (S-013)
Blockers: none
Next: complete refresh-token revocation tests.
```

## Failure behavior
- Not a Git repo / unborn repo / detached HEAD: report clearly; do not
  initialize or checkout anything without a human decision.
- Any inconsistency: **report, do not fix** (drift resistance). Suggest `adm-doctor`.

## Human approval points
Switching the active GD; resolving a stale-session conflict; any repair.

## Runtime capability fallback
Pure reading + one state field update — works identically in every runtime.
No subagents, hooks or MCP required.
