---
id: GD-NNN
title: 
phase: PHASE-NNN
status: planned        # planned | in-progress | blocked | review | done
created: 
completed: 
branch: 
worktree: 
decisions_followed: [] # existing D-NNN ids applied
decisions_assumed: []  # implicit decisions made; promote to D-NNN or drop at close
---

# GD-NNN — <title>

## 1. Goal
<!-- What this cycle delivers, tied to the active phase. -->

## 2. Reuse analysis (MANDATORY — before any new code)
Scan `registry/CAPABILITIES.md` first, then apply the ladder:

| Option | Candidate | Fit (~%) | Verdict |
|---|---|---|---|
| Reuse   |  |  |  |
| Extend  |  |  |  |
| Extract |  |  |  |
| New     |  |  |  |

- Threshold: ~70% fit → prefer Reuse/Extend.
- Forced-abstraction signal: if extending requires distorting the existing
  structure's intent, record it here and prefer Extract/New instead.

## 3. Plan / task checklist
<!-- Sub-tasks live HERE as a checklist, not in canonical state (A-004: no WP concept). -->
- [ ] 

## 4. Acceptance criteria (human-approved — INV-06)
- [ ] AC-1:

## 5. Test strategy
<!-- Required validation (must pass to complete — INV-07) vs coverage targets (INV-08). -->
- Required:
- Coverage:

## 6. Artifact map
| Artifact | Change | Evidence (commit/ref) |
|---|---|---|

## 7. Validation evidence
<!-- Build/test outputs, review id, manual verification notes. -->

## 8. Outcome
- coverage debt recorded in pipeline/TEST_DEBT.md: yes/no (link)
- capabilities registry updated: yes/no
- human acceptance: pending | approved by <who> on <date>
