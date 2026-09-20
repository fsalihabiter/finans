# Cursor Adapter

> Runtime behavior changes; verify against official docs before each release.

```yaml
runtime: cursor
instruction_entry: AGENTS.md (native)
skill_discovery: .agents/skills/ (native; .cursor/skills/ may also exist — do not make it canonical)
explicit_invocation: /adm-<name>
hooks: enhanced (optional)
subagents: enhanced (optional)
parallel_agents: via separate worktrees only
mcp: optional
shell: available; not required by core
worktree: supported
notes: If a .cursor/skills mirror is ever needed, use the same pointer-shim model as Claude (A-003).
```
