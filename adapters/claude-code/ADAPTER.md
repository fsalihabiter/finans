# Claude Code Adapter

> Runtime behavior changes; verify against official docs before each release.

```yaml
runtime: claude-code
instruction_entry: CLAUDE.md (imports @AGENTS.md)
skill_discovery: .claude/skills/ (generated pointer-shims -> .agents/skills/)
explicit_invocation: /adm-<name>
hooks: enhanced (optional)
subagents: enhanced (optional; adm-review may parallelize)
parallel_agents: via separate worktrees only
mcp: optional
shell: available; core skills must not require OS-specific shell
worktree: supported
notes: |
  - .claude/ holds NO framework state (INV-14).
  - Shim model per addendum A-003; Test B must verify the canonical skill is
    actually loaded through the shim. Fallback: test-proven full mirror via a
    dedicated adm-sync-adapters procedure (record the decision in the report).
  - No symlinks (P7).
```
