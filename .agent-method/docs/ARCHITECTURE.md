# ADM Architecture (v2.0-alpha)

> Product principle: **Simple outside, disciplined inside.** Users live with
> `adm-open` / `adm-close` (plus `adm-review` / `adm-doctor` when needed);
> everything below is internal discipline, not required knowledge.

## Layering

```text
                REASONING LAYER
    Claude / Codex / Cursor / Antigravity
              Agent Skills (.agents/skills)
                   |
             METHOD CORE
   Intent / Decisions / GD / Evidence (.agent-method)
                   |
        DETERMINISTIC BOUNDARY
   Schemas / State / Git (alpha: skill-driven; future: ADM CLI)
                   |
             STORAGE LAYER
       Git / YAML / Markdown / Code
```

Core principles (MUST): vendor-neutral core (P1), AGENTS.md entry point (P2),
portable skills (P3), runtime syntax ≠ framework API (P4), runtime capabilities
optional (P5), no OS-specific shell in core (P6), no symlinks (P7), hooks
optional (P8), human gates preserved (P9).

## Alpha scope decision (single active session)

v2 alpha's product goal is **sequential cross-runtime handoff**:

```text
Claude → close → Codex → close → Cursor → close → Antigravity → close → Claude
same project, same intent, same decisions, same GDs, same history
```

Parallel writable sessions, worktree orchestration, locks, distributed session
state and generated dashboards are **out of alpha scope** (roadmap V2.4).
Therefore there is ONE canonical state file and one active session assumption
(INV-13). Users may still use git worktrees for their own workflow, but ADM
does not manage them and gives no parallel-correctness guarantee in alpha.

## Canonical ownership

| Data | Canonical source | Notes |
|---|---|---|
| Project intent | `charter/PROJECT_INTENT.md` | |
| Phases | `charter/PHASES.md` | progress is derived, never stored |
| Active state (phase, GD, session) | `state.yaml` | current-only; cleared/updated by skills |
| GD details | `cycles/GD-*.md` | GD `status` fields are the project-wide work truth |
| Backlog | `pipeline/POOL.md` + GD statuses | no generated BACKLOG in alpha |
| Decisions | `decisions/D-*.md` | one decision = one file; flat layout, scope in frontmatter |
| Sessions | `evidence/sessions/S-*.md` | permanent history; latest by `closed_at` |
| Reviews | `evidence/reviews/REVIEW-*.md` | findings carry lifecycle status |
| Doctor runs | `evidence/doctor/DOCTOR-*.md` | findings reference INV ids |
| Capabilities | `registry/CAPABILITIES.md` | scanned by GD reuse analysis |

**Generated projections are optional in alpha.** `config.generated.projections`
defaults to empty; `adm-open` builds the human briefing directly from canonical
sources. Deterministic generation (CURRENT_STATE, BACKLOG, MASTER,
DECISION_INDEX, CHANGELOG) returns with the CLI (roadmap V2.2). If projections
are enabled, they carry a GENERATED header and are never canonical (INV-12, INV-17).

## Session evidence model

One file per session under `evidence/sessions/`:

```yaml
---
id: S-NNN
runtime: claude-code | codex | cursor | antigravity | other
started_at: 
closed_at: 
start_sha: 
end_sha: 
active_gd: 
---
```
Body: session goal, work done, touched artifacts, validation result, open
items, next step. This is the primary memory mechanism of runtime handoff.
Latest session is found by `closed_at` metadata — never by position in a log.

## Invariants

See `INVARIANTS.md` (single canonical list; Doctor references its IDs).

## Roadmap (after alpha is proven)

- **V2.1** Deterministic CLI (validation, git evidence, id generation, adapter generation)
- **V2.2** Generated views (CURRENT_STATE, BACKLOG, MASTER, DECISION_INDEX, CHANGELOG) — deterministic only
- **V2.3** Runtime enhancements (hooks, MCP, native subagents, plugins)
- **V2.4** Parallel work (worktree isolation, session-local state, merge strategy) — only if real need is proven
