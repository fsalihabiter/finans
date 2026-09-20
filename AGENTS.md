# ADM — Agent Development Method (v2.0-alpha)

This repository uses **ADM**, a vendor-neutral development method. ADM's job:
when the coding agent changes (Claude Code → Codex → Cursor → Antigravity → ...),
the project's intent, decisions, active work, history and validation evidence
do NOT change. This file is a **navigation map and operating contract**, not an
encyclopedia.

## 1. Where things live

| What | Where |
|---|---|
| Canonical framework state | `.agent-method/` |
| Project intent | `.agent-method/charter/PROJECT_INTENT.md` |
| Phases | `.agent-method/charter/PHASES.md` |
| Development cycles (GD) | `.agent-method/cycles/GD-NNN.md` |
| Decisions (one file each) | `.agent-method/decisions/D-NNN-*.md` |
| Capability / reuse registry | `.agent-method/registry/CAPABILITIES.md` |
| Work pipeline (pool, ideas, incidents, test debt) | `.agent-method/pipeline/` |
| Session / review / doctor evidence | `.agent-method/evidence/` |
| Canonical active state | `.agent-method/state.yaml` |
| Schemas | `.agent-method/schemas/` |
| Method documentation | `.agent-method/docs/` |
| Portable workflow skills (canonical) | `.agents/skills/adm-*/SKILL.md` |
| Runtime adapters | `adapters/<runtime>/` |

## 2. How to find the active work

1. Read `.agent-method/state.yaml` → active phase, active GD, current session, blockers.
2. Cross-check: the active GD's own `status` must be `in-progress` (INV-04).
3. Latest session context = file with the newest `closed_at` in
   `.agent-method/evidence/sessions/` — **never** "the top of a log".

## 3. Skills

Daily surface (this is normally all a user needs):

| Skill | Purpose |
|---|---|
| `adm-open` | Restore bearings at session start (read-mostly) |
| `adm-close` | Close a session: evidence, state update, next step |
| `adm-review` | Risk-based code review (when quality gate is needed) |
| `adm-doctor` | Drift / invariant audit (when something feels off) |
| `adm-bootstrap` | One-time project setup |

Advanced (governance/planning — not part of the daily flow):
`adm-analyze` (adopt ADM in an existing codebase), `adm-phase` (phase governance).

Canonical definitions: `.agents/skills/<name>/SKILL.md`.
Invocation syntax (`/adm-open`, `$adm-open`, ...) belongs to the runtime, not to ADM.

## 4. Operating rules (MUST)

1. **One active writable ADM session per repository** (INV-13). A stale open
   `session` block in `state.yaml` is reported, never silently overwritten.
2. **Report inconsistencies, do not silently fix them.** If two files disagree,
   surface the conflict and the canonical source (`.agent-method/docs/INVARIANTS.md`).
3. **Human approval gates are real.** An agent never declares acceptance
   criteria passed or a GD completed. It presents evidence; a human decides.
4. **Reuse before build.** Scan `registry/CAPABILITIES.md` and apply the
   Reuse / Extend / Extract / New ladder before writing new code.
5. **Required validation failing ≠ test debt.** A GD with failing required
   validation cannot complete. Coverage debt goes to `pipeline/TEST_DEBT.md`.
6. If any `generated/` files exist they are projections — never edit them,
   never treat them as truth (INV-12, INV-17). Alpha works without them.
7. **Machine surface is English** (file names, IDs, YAML keys, schemas).
   Human-facing output language comes from `config.yaml → method.language`.
8. Do not depend on OS-specific shell tooling for core workflows; use the
   runtime's native file/search/edit capabilities. Git is a core dependency.

## 5. Vendor neutrality contract

- Deleting any runtime adapter directory (`.claude/`, `.cursor/`, ...) must not
  lose any canonical framework data (INV-14).
- `.claude/skills/` contains **generated shims only**; do not treat `.claude/`
  as framework state. Canonical skills live in `.agents/skills/`.

## 6. Start here

- Working session? Run `adm-open`. Done for the day (or switching runtimes)? `adm-close`.
- New empty project? `adm-bootstrap`. Existing codebase without ADM? `adm-analyze`.
