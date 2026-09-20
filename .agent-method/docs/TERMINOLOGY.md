# TERMINOLOGY (canonical concept definitions)

> Rule: **Normative definitions live in this file.** Other documents may
> explain concepts contextually, but must not introduce conflicting normative
> definitions. Machine surface (IDs, file names, keys) is always English;
> human-facing bodies follow `config.yaml → method.language`.

| Canonical (EN) | Türkçe | Abbrev / ID | One-line definition |
|---|---|---|---|
| Agent Development Method | Ajan Geliştirme Metodu | ADM | Vendor-neutral method preserving intent, decisions, state and evidence across coding agents. |
| Project Intent | Proje Niyeti | — | Canonical statement of purpose, goals, out-of-scope and constraints. |
| Phase | Faz | PHASE-NNN | A structural stage of the project tied to intent goals. |
| Development Cycle | Geliştirme Döngüsü | GD-NNN | The unit of work: goal, reuse analysis, plan, acceptance, evidence. |
| Decision | Karar | D-NNN | A recorded, scoped choice; one file per decision. |
| Session | Oturum | S-NNN | One agent working session; its permanent trace is an evidence file. |
| Review | Gözden Geçirme | REVIEW-* / RV-NNN | Risk-based code review producing schema-conformant findings with a lifecycle. |
| Doctor | Doktor | DOCTOR-* / DR-NNN | Drift & invariant violation detector; reports, never auto-fixes. |
| Capability Registry | Yetenek Envanteri | CAPABILITIES.md | Reusable capability inventory scanned before building anything new (v1.4: ENVANTER). |
| Pool | Havuz | POOL.md | Goal-driven work items not yet promoted to a GD. |
| Test Debt | Test Borcu | TEST_DEBT.md | Coverage debt from completed work; NOT failing required validation. |
| State | Durum | state.yaml | Single canonical active state (project, active phase/GD, current session). |
| Finding Lifecycle | Bulgu Yaşam Döngüsü | status | open → resolved / accepted / false-positive, with optional resolution evidence. |
| Invariant | Değişmez Kural | INV-NN | A rule the structure must always satisfy; canonical list in INVARIANTS.md. |
| Reuse Ladder | Yeniden Kullanım Merdiveni | — | Reuse / Extend / Extract / New gate with ~70% fit threshold. |
| Human Gate | İnsan Onayı | — | Approval points an agent may never bypass (acceptance, GD completion). |
