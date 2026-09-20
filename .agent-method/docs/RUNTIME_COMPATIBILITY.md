# Runtime Compatibility Matrix

> "Supported" ≠ "Tested". Status values: Designed | Smoke Tested | Verified | Degraded | Unsupported.
> Feature claims MUST be re-verified against official vendor docs before each release.

| Runtime | Instructions | Skills | Explicit invocation | Subagents | Hooks | Worktree | Status |
|---|---|---|---|---|---|---|---|
| Claude Code | `CLAUDE.md → @AGENTS.md` adapter | `.claude/skills/` shims → `.agents/skills/` | runtime-specific (`/adm-*`) | enhanced | enhanced | supported | Designed |
| Codex | native `AGENTS.md` | native `.agents/skills/` | runtime-specific | enhanced | enhanced | supported | Designed |
| Cursor | native `AGENTS.md` | native `.agents/skills/` (also `.cursor/skills/`) | runtime-specific | enhanced | enhanced | supported | Designed |
| Antigravity | workspace `AGENTS.md` | `.agents/skills/` | slash command | enhanced | enhanced | supported | Designed |

Core correctness never depends on subagents, hooks, MCP or parallelism (P5, P8).
Update the Status column only with evidence from RUNTIME_TEST_PLAN.md runs.
