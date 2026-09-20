# Antigravity Adapter

> Runtime behavior changes; verify against official docs before each release.

```yaml
runtime: antigravity
instruction_entry: AGENTS.md (workspace)
skill_discovery: .agents/skills/ (workspace skill path)
explicit_invocation: /adm-<name> (skills exposed as slash commands)
hooks: enhanced (optional)
subagents: custom agents (enhanced, optional)
parallel_agents: via separate worktrees only
mcp: optional
shell: available; not required by core
worktree: supported
notes: No adapter files needed beyond this doc.
```
