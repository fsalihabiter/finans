---
name: adm-phase
description: ADVANCED governance skill. Open, close or inspect ADM phases; enforces single-active-phase and the GD-count warning threshold from config. Not part of the daily workflow.
---

# adm-phase (advanced)

## Purpose
Manage the project's structural stages consistently with intent and config.
Daily work does not need this skill; it belongs to governance/planning.

## When to use
Starting/finishing a phase, or reviewing phase health.

## Inputs
Action: open | close | status; phase id/title.

## Canonical files read
`config.yaml`, `state.yaml`, `charter/PROJECT_INTENT.md`, `charter/PHASES.md`, `cycles/GD-*.md`.

## Canonical files written
`charter/PHASES.md`, `state.yaml → active.phase_id`.

## Preconditions
Valid charter files exist.

## Procedure
- **open**: verify no other active phase unless `max_active_phases` allows
  (INV-03); require at least one intent goal link (INV-02); write entry
  criteria; set `active.phase_id`. Human confirms.
- **close**: verify all phase GDs are `done` or explicitly moved/deferred by
  the human; verify exit criteria; human approves closure; clear/advance active phase.
- **status**: derive progress from GD files (counts, statuses) — derived
  numbers are never written into canonical files. If the phase's GD count ≥
  `config.workflow.phase_gd_warning_threshold` (single canonical source of the
  threshold, read by the Doctor too), warn the phase may be too large.

## Invariants
INV-01, INV-02, INV-03; threshold single-sourcing.

## Expected output
Updated charter/state and a short phase briefing.

## Failure behavior
Open GDs at close time → refuse closure, list them; no silent deferral.

## Human approval points
Phase open, phase close, any GD deferral.

## Runtime capability fallback
None needed.
