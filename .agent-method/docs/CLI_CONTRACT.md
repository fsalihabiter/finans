# ADM CLI Contract (future — NOT implemented in v2.0-alpha)

Alpha runs the deterministic steps through skills. This document fixes the
boundary so the future CLI can take them over without redesign.

## Planned commands

```text
adm init
adm open
adm close
adm validate
adm generate
adm doctor --structural
adm doctor --semantic     (delegates reasoning to an agent)
adm runtime detect
adm adapter generate
```

## CLI MUST own (deterministic)
- YAML/JSON schema validation (schemas/*.json)
- state transitions (state.yaml)
- generated projections (exactly config.generated.projections — INV-17)
- Git evidence collection (start_sha..HEAD, staged, unstaged, untracked)
- ID generation (GD-, D-, S-, RV-, DR-) and uniqueness
- cross-reference validation (INV list)
- adapter shim generation and validation (INV-17)
- platform normalization (paths, line endings)

## CLI MUST NOT own (agent reasoning)
- architecture reasoning, semantic review, intent interpretation,
  abstraction choice, product decisions.

## Runtime capability contract (machine-readable, future)

```yaml
runtime: codex
capabilities:
  instructions: { agents_md: true }
  skills: { supported: true, canonical_path: .agents/skills }
  subagents: { supported: true, parallel: true }
  hooks: { supported: true, required: false }
  mcp: { supported: true, required: false }
  shell: { supported: true }
  git: { supported: true }
```
