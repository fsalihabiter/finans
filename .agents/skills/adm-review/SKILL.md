---
name: adm-review
description: Risk-based ADM code review. Classifies the diff, selects reviewer perspectives dynamically, runs parallel if the runtime supports subagents or sequential otherwise, and emits schema-conformant findings with a lifecycle. The user only runs adm-review.
---

# adm-review

## Purpose
Produce comparable, evidence-graded review findings for a change set,
independent of runtime capabilities. Runtime differences (parallel vs
sequential) never change the user's workflow or the output schema.

## When to use
Before completing a GD with code changes (`config.review.required_for_code_changes`),
or on demand for any ref range.

## Inputs
`base_ref`, `head_ref` (default: current session's start_sha..HEAD), active GD id.

## Canonical files read
Diff between refs, active GD, relevant `decisions/D-*.md`,
`charter/PROJECT_INTENT.md`, `schemas/review.schema.json`, previous review of
the same GD (to carry forward unresolved findings).

## Canonical files written
`evidence/reviews/REVIEW-<id>.md` (frontmatter conforms to review.schema.json);
`state.yaml → review` block updated.

## Preconditions
Refs resolvable; diff non-empty (else report "nothing to review").

## Procedure
1. **Diff classification** — determine change nature, e.g.:
   - UI-only → correctness, naming, UI regression
   - Auth/security → architecture, correctness, security, authorization, tests
   - DB migration → architecture, data-integrity, migration-safety, rollback, performance
   - Mixed → union of applicable perspectives.
   Reviewer count is dynamic — **no fixed reviewer count is part of the contract**.
2. Run each perspective: parallel subagents if available (performance only),
   otherwise sequential (`config.review.allow_sequential_fallback`).
   Output schema identical either way.
3. Grade every finding: `severity: critical|high|medium|low` +
   `evidence: proven|strong|heuristic`. **Never emit 0-100 confidence scores.**
   Reference violated decisions via `rule_or_decision: D-NNN`.
4. **Finding lifecycle:** every new finding starts `status: open`. Unresolved
   findings from the previous review of this GD are carried forward with their
   ids. Status changes to `resolved | accepted | false-positive` only with the
   human's decision; on resolve, fill the optional `resolution` block
   (resolved_at, evidence_ref, note).
5. Deduplicate; unique RV-NNN ids (INV-11).
6. Write the report (frontmatter English; body language from config).
7. Summarize: counts by severity; explicit list of `open` critical/high items.

## Invariants
INV-11, INV-18 (open criticals block completion); runtime-independence (INV-15).

## Expected output
A REVIEW file + short summary. Open critical findings block GD completion
until resolved or explicitly accepted by the human.

## Failure behavior
- Refs unresolvable → report; never guess a base.
- A perspective fails mid-run → note it (`reviewers` lists attempted ones);
  mark the report partial.

## Human approval points
Every finding status change; accepting risk on critical findings.

## Runtime capability fallback
Parallel subagents optional; the sequential path is the correctness baseline.
