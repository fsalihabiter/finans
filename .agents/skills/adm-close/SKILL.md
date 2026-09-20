---
name: adm-close
description: Close an ADM working session. Collects Git evidence, writes the per-session evidence file, updates canonical state with the next step and clears the session. This is the handoff mechanism between runtimes.
---

# adm-close

## Purpose
End a session leaving a complete, portable trace: **any runtime opening the
project later must resume correctly without this conversation's history.**

## When to use
At the end of every working session, and always before switching runtimes.

## Inputs
- Session context from `state.yaml → session`.
- Short human note on outcome / next step (ask if not provided).

## Canonical files read
`config.yaml`, `state.yaml`, active GD file, `registry/CAPABILITIES.md`, `pipeline/TEST_DEBT.md`.

## Canonical files written
- New `evidence/sessions/S-NNN.md` (frontmatter: id, runtime, started_at,
  closed_at, start_sha, end_sha, active_gd — per ARCHITECTURE.md).
- Active GD file: artifact map, validation evidence, status transition, outcome.
- `pipeline/TEST_DEBT.md`: new coverage debt if identified.
- `registry/CAPABILITIES.md`: if a reusable capability was produced/changed (ask first).
- `state.yaml`: session block cleared; `active.gd_id` kept or advanced per the
  human's next-step decision; blockers updated if genuinely changed.
- IF `config.generated.projections` is non-empty (optional in alpha): regenerate
  exactly those files, nothing more (INV-17). With the default empty list,
  write no generated files at all.

## Preconditions
- `state.yaml → session` has an open session. If not: tell the user there is no
  open session to close; offer a minimal evidence note instead — never fabricate one.

## Procedure
1. Validate state against `schemas/state.schema.json` (as far as the runtime allows).
2. Read the active GD.
3. Collect Git evidence — never from `git diff --name-status` alone:
   - committed: `start_sha..HEAD`; staged; unstaged; untracked — listed separately.
   - **Do not claim changes made outside this session** (already present at
     start_sha, or clearly foreign) as this session's work; list them under
     "observed, not owned".
4. Update the GD artifact map with evidence refs.
5. Acceptance status:
   - Required validation failing → GD stays open (INV-07). This is NOT test debt.
   - Coverage debt → record in TEST_DEBT.md; does not block (INV-08).
   - GD moves to `review`/`done` ONLY with explicit human approval (INV-06).
6. Review requirement (`config.review.required_for_code_changes`): if code
   changed and no review ran, flag it in evidence and summary.
7. Ask about `decisions_assumed`: promote to real D-NNN files or drop (human decides).
8. Write the session evidence file (goal, work done, artifacts, validation
   result, open items, **next step** — body language from config).
9. Clear `state.yaml → session`; update `review` block from the latest review.
10. Consistency check: GD status vs state; report any mismatch — do not silently reconcile.
11. Output a short closing summary ending with an explicit "Next:" line.

## Invariants
Maintains INV-04..INV-08, INV-10, INV-12, INV-13, INV-17.

## Expected output
Committed-ready evidence file, updated canonicals, closing summary with next step.

## Failure behavior
- `start_sha` unreachable (rebase/force-push): produce evidence with an explicit
  "diff base unavailable" note listing only currently attributable changes;
  never silently own everything in the tree.
- Git unavailable/broken: write evidence marked `git_evidence: unavailable`; report.

## Human approval points
GD completion / acceptance (always); decisions_assumed promotion; capability
registry entries; next-step choice.

## Runtime capability fallback
Plain file editing + git. No hooks/subagents needed.
