---
name: adm-analyze
description: ADVANCED skill. Reconstruct the ADM structure (.agent-method) from an existing codebase. Produces a reviewable proposal first; applies nothing without human approval. Not part of the daily workflow.
---

# adm-analyze (advanced)

## Purpose
Bring an existing codebase under ADM by inferring intent, phases, capabilities
and implicit decisions — through a **review-before-apply gate**.

## When to use
Adopting ADM in a project that already has code. New projects use `adm-bootstrap`.

## Inputs
Repository root; optional focus areas from the user.

## Canonical files read
The codebase (read-only scan), any existing docs, Git history metadata.

## Canonical files written
- Stage 1: ONLY `_scan/PROPOSAL/` (a staging folder outside `.agent-method/`)
  containing the proposed charter, decision drafts (D-NNN, status: proposed),
  capabilities, phases and initial POOL.
- Stage 2 (after human approval): approved content moved into
  `.agent-method/`; `_scan/` removed.

## Preconditions
Git repository; no existing `.agent-method/` (else report and stop).

## Procedure
1. Scan structure, entry points, modules, tests; infer capabilities with
   locations/status → draft CAPABILITIES entries.
2. Infer implicit architectural decisions → D-NNN drafts (status: proposed).
3. Draft PROJECT_INTENT (mark inferred parts clearly as inference, not fact).
4. Write everything to `_scan/PROPOSAL/` with a summary README listing every
   file that WOULD be created.
5. Present the proposal; iterate on human feedback.
6. On explicit approval only: apply to `.agent-method/`, delete `_scan/`.

## Invariants
INV-09 (unique D ids), INV-12 on apply.

## Expected output
First a proposal set, then (after approval) a live ADM structure.

## Failure behavior
- Ambiguous architecture: record open questions in the proposal instead of guessing.
- Apply interrupted midway: report exactly what was applied; do not auto-rollback.

## Human approval points
The entire apply step; every proposed decision's promotion to `accepted`.

## Runtime capability fallback
Uses native search/read tools; subagents may parallelize scanning but are optional.
