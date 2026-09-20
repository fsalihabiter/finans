# Sessions & Parallelism (v2 alpha)

## The rule

> **One repository = one active writable ADM session at a time.** (INV-13)

Supported flow (sequential handoff — the product's core value):

```text
Claude opens a session → works → adm-close
Codex opens the repo → adm-open → continues exactly where Claude left off
```

NOT supported in alpha:

```text
Claude writing GD-041 while Codex writes GD-047 in the same repository
```

## What this removes

No locks, no heartbeat, no worktree orchestration, no per-worktree state,
no distributed session ownership. The state model is a single `state.yaml`.

## Worktrees

Git worktrees are not forbidden — use them for your own git workflow if you
like. But ADM alpha does not manage worktrees and gives **no correctness
guarantee** for concurrent writable sessions. If you open a second session
while `state.yaml` still shows an open session, `adm-open` warns about the
stale/unclosed session (INV-13) and asks you to decide.

## Roadmap

Parallel writable work (worktree isolation, session-local state, merge
strategy, distributed evidence ids) is deliberately deferred to **V2.4**,
and only if real usage proves the need. Parallelism is not baked into the core.
