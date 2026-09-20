---
name: adm-doctor
description: ADM drift and invariant-violation audit. One user-facing command; internally runs structural checks (the canonical invariant list) and semantic checks (intent/decision drift). Reports findings — never auto-fixes.
---

# adm-doctor

## Purpose
Detect inconsistencies between canonical sources, Git reality and the project's
declared intent/decisions.

> The Doctor is an auditor, not a producer. It writes no feature code, fixes
> nothing silently, and never reinterprets canonical state without human approval.

## When to use
When something feels off; when adm-open reports inconsistencies; before phase
close; after large merges; periodically.

## Inputs
- None required. The user just runs `adm-doctor` — mode selection is internal.
- Advanced (optional): restrict to `structural` or `semantic`, or a scope path.

## Canonical files read
Everything under `.agent-method/`, `.agents/skills/`, `.claude/skills/`
(shim check), Git metadata; for semantic checks: the codebase against
`PROJECT_INTENT.md` (out-of-scope keyword list) and `decisions/D-*.md`
(Keywords sections).

## Canonical files written
`evidence/doctor/DOCTOR-<id>.md` only (frontmatter conforms to `doctor.schema.json`).

## Preconditions
None; the Doctor must run even on a broken structure (that is its job).

## Procedure
Internally two layers; the user does not need to know the split.

### Structural checks (deterministic / semi-deterministic)
Walk `docs/INVARIANTS.md` — **that list IS the canonical check list; there is
no separate layer count.** At minimum: broken references, duplicate IDs
(INV-09/10/11), missing phase (INV-01), state/GD mismatch (INV-04/05), orphan
decisions, stale/unclosed session (INV-13), generated-set match if projections
enabled (INV-12/17), invalid schema, same GD in conflicting states, `archive/`
naming, missing required fields, shim validity (INV-16), open critical findings
carried as blockers (INV-18). Every finding's `check` field references the
invariant id or a named check.

### Semantic checks (reasoning)
Intent drift — using the **concrete names** from PROJECT_INTENT §3 and decision
Keywords sections (keyword-based, not judgment-based); wrong abstraction;
conceptual duplication; decision violation; scope inflation; architectural
rollback; forbidden behavior. Grade with severity + evidence (`heuristic`
allowed here, rare in structural).

### Reporting
1. Unique DR-NNN ids; every finding starts with `status: open`.
2. For conflicts, fill `canonical_source` — which file is truth.
3. Carry forward unresolved findings from the previous report (keep their ids,
   update status only with human-confirmed resolution + `resolution` block).
4. Write the report (`status: clean | findings | blocked`); summarize in the
   human language, critical items first.
5. **Propose** fixes as options; apply nothing.

## Invariants
Checks the full INV list; itself maintains INV-12 (writes only evidence).

## Expected output
A DOCTOR report + summary. `status: open` critical findings appear as blockers
in the next `adm-open` (INV-18).

## Failure behavior
- Unreadable/corrupt file: a finding with `evidence: proven`, not a crash.
- Semantic checks without enough context: say so; never fabricate drift.

## Human approval points
Every fix. History-rewrite suggestions are forbidden outright.

## Runtime capability fallback
Pure read + one evidence write; identical in all runtimes.
