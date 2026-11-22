# 08 — Görev Listesi (Backlog) → **taşındı**

> ⚠ **Bu dosya artık kanonik değildir (2026-09-20).**
> Backlog o gün `.agent-method/pipeline/POOL.md`'ye, **2026-11-22'de de
> GitHub Issues'a** taşındı (D-022):
> **<https://github.com/fsalihabiter/finans/issues>**
>
> Görev durumunu oradan oku ve oraya yaz. İki kanonik kaynak tutulmaz (INV-12).

## Nerede ne var?

| Aradığın | Yeri |
|---|---|
| **Açık işler** (Faz 6-9 + Faz M), bağımlılık ve kırılımıyla | **GitHub Issues** — `gh issue list --state open` |
| Çevrimdışı/ajan okuması için anlık görüntü | `.agent-method/generated/BACKLOG.md` (üretilmiş, elle düzenlenmez) |
| Faz ↔ milestone eşlemesi, etiket sözlüğü, komutlar | `.agent-method/pipeline/POOL.md` (işaretçi) |
| Aktif faz, faz durumları, DoD'ler | `.agent-method/charter/PHASES.md` |
| Aktif döngü (üzerinde çalışılan iş) | `.agent-method/state.yaml` → `active.gd_id`, `.agent-method/cycles/GD-NNN.md` |
| Kalıcı kararlar | `.agent-method/decisions/D-NNN-*.md` (taşıma kararı: `D-022`) |
| Ham fikirler / plan dışı çıkan sorunlar | `.agent-method/pipeline/IDEAS.md` · `INCIDENTS.md` |
| Kapsam (test) borcu | `.agent-method/pipeline/TEST_DEBT.md` |
| Tamamlanmış görevlerin uygulama notları (Faz 0-5) | `.claude/tasks/TASKLOG.md` (dondurulmuş tarihsel kayıt) |
| Bu dosyanın taşınmadan önceki tam hali | `git show ac61b7e:.claude/docs/08-BACKLOG.md` |
| POOL tablosunun taşınmadan önceki tam hali | `git show c584ba1:.agent-method/pipeline/POOL.md` |

Eski `T*` görev ID'leri (T6.16, T7.2, T9.1, TM.3 …) **korunmuştur** — issue
başlıklarında önek olarak duruyorlar, TASKLOG/müfredat referansları geçerli:

```bash
gh issue list --search "T9.1 in:title" --state all
```

Oturum akışı ve kayıt protokolü: [`CLAUDE.md`](../../CLAUDE.md) §11 ·
komutlar: `adm-open` / `adm-close`.
