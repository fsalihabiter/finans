# ADM Invariants (canonical list — v2 alpha)

> The SINGLE canonical list of invariants. Doctor findings reference these IDs
> in their `check` field. There is no separate "layer count" — this list IS the
> Doctor's structural capability list.

| ID | Invariant | Check type |
|---|---|---|
| INV-01 | Every GD belongs to an existing Phase. | structural |
| INV-02 | Every Phase maps to at least one Project Intent goal. | structural |
| INV-03 | Only one active Phase unless `config.workflow.max_active_phases` says otherwise. | structural |
| INV-04 | `state.yaml → active.gd_id` points to an existing GD whose status is `in-progress`. | structural |
| INV-05 | Active GD status is consistent with active work (no `done` GD as active). | structural |
| INV-06 | A completed GD carries required acceptance criteria as human-approved. | structural |
| INV-07 | A GD cannot complete while required validation fails (this is NOT test debt). | structural (evidence-based) |
| INV-08 | Coverage debt alone does not block GD completion; it is recorded in TEST_DEBT. | structural |
| INV-09 | Decision IDs are unique and never reused. | structural |
| INV-10 | Session IDs are unique. | structural |
| INV-11 | Finding IDs are unique within a report. | structural |
| INV-12 | Generated files are never canonical sources. | structural |
| INV-13 | One active writable ADM session per repository (single-session assumption). A stale/unclosed `session` block in state.yaml is a finding. | structural (best effort) |
| INV-14 | Deleting a runtime adapter loses no framework domain data. | structural |
| INV-15 | No core skill requires a runtime-specific command for correctness. | semantic/manual |
| INV-16 | Every canonical skill under `.agents/skills/` has a valid Claude shim pointing to it, and every shim's target path exists. | structural |
| INV-17 | Files under `generated/` (if any) exactly match `config.generated.projections`. If the list is empty, no `generated/` directory exists. | structural |
| INV-18 | Critical findings (review or doctor) with `status: open` block GD completion and appear as blockers in the next adm-open. | structural |
