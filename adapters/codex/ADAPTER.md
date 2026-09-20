# Codex Adapter

> Runtime behavior changes; verify against official docs before each release.

```yaml
runtime: codex
instruction_entry: AGENTS.md (native)
skill_discovery: .agents/skills/ (native)
explicit_invocation: $adm-<name> or runtime skill selector
hooks: enhanced (optional)
subagents: enhanced (optional)
parallel_agents: via separate worktrees only
mcp: optional
shell: available; not required by core
worktree: supported
notes: No adapter files needed beyond this doc; Codex reads the canonical surfaces directly.
```
