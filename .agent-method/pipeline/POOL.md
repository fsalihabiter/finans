# POOL — işaretçi (kanonik backlog artık GitHub Issues)

> **Bu dosya 2026-11-22'de bir işaretçiye indirgendi (D-022).**
> Açık iş kalemlerinin **kanonik kaynağı GitHub Issues'tır**:
> <https://github.com/fsalihabiter/finans/issues>
>
> İki kanonik kaynak olamaz (INV-12) — bu yüzden POOL tablosu burada tutulmaz.
> `08-BACKLOG.md` 2026-09-20'de POOL'a devrederken aynı yolu izlemişti.

## Nerede ne var

| Ne | Nerede | Kanonik mi? |
|---|---|---|
| **Açık iş kalemleri (görevler)** | [GitHub Issues](https://github.com/fsalihabiter/finans/issues) | ✅ **kanonik** |
| Fazlar | GitHub **Milestone**'ları ↔ `charter/PHASES.md` | PHASES.md kanonik (niyet), milestone yansıma |
| Çevrimdışı anlık görüntü | `.agent-method/generated/BACKLOG.md` | ❌ üretilmiş projeksiyon (INV-12/17) |
| Döngü gerekçesi + kabul kriterleri | `.agent-method/cycles/GD-NNN.md` | ✅ kanonik |
| Kararlar | `.agent-method/decisions/D-NNN-*.md` | ✅ kanonik |
| Oturum kanıtı | `.agent-method/evidence/sessions/` | ✅ kanonik |
| Ham fikirler | `.agent-method/pipeline/IDEAS.md` | ✅ kanonik |
| Test borcu | `.agent-method/pipeline/TEST_DEBT.md` | ✅ kanonik (INV-08) |
| Incident'lar | `.agent-method/pipeline/INCIDENTS.md` + `Bug` issue'ları | INCIDENTS.md kanonik (kayıt), issue takip |

## Görev okuma / yazma

```bash
gh issue list --state open --label prio:1
```

```bash
gh issue list --milestone "PHASE-006 — Eğitim MVP + Kavram Sözlüğü" --state open
```

```bash
gh issue create --title "T6.28 — <başlık>" --type Task --milestone "PHASE-006 — Eğitim MVP + Kavram Sözlüğü" --label "prio:2,area:web"
```

Etiket sözlüğü: `prio:1…6` (1 = sıradaki) · `area:backend|web|education|mobile|infra|adm|product` ·
`gate:legal` (lansman kapısı) · `adm:gd` (aktif döngüye bağlı) · `kind:spike|debt`.
Tür (`--type`): `Feature` (GD'ye dönüşecek iş) · `Task` (tek adım) · `Bug` (incident).

Bağımlılık ve kırılım:

```bash
gh issue edit 23 --add-blocked-by 22 --add-sub-issue 24,25
```

Projeksiyonu tazelemek için:

```bash
bash .github/scripts/gen-backlog.sh
```

> Projeksiyon ayrıca `.github/workflows/adm-backlog-sync.yml` ile issue olaylarında
> otomatik yenilenir — elle koşmak normalde gerekmez.

## Eski ID'ler

GitHub issue başlıkları eski `T*` / `RV-*` / `INC-*` ID'lerini **önek olarak
taşır** (örn. `T6.3 — Kavram sözlüğü…`), böylece TASKLOG ve doküman referansları
kırılmaz. Eski ID ile arama:

```bash
gh issue list --search "T6.3 in:title" --state all
```

## Taşıma kaydı (2026-11-22)

POOL tablosundaki **50 açık kalem** GitHub Issues'a taşındı ve kırılımla birlikte
**78 issue**'ya açıldı: birleşik satırlar ders bazında alt-görevlere ayrıldı
(Set 1 ders 3-5, Set 2 ders 1-8), REVIEW-001/003'ün açık bulguları ve GD-003'ün
6 kabul kriteri de izlenebilir kalemler hâline geldi. Faz bağımlılıkları 27
`blocked by` ilişkisine çevrildi. Taşımadan önceki POOL tablosunun tam hâli git
geçmişindedir:

```bash
git show c584ba1:.agent-method/pipeline/POOL.md
```

Karar ve gerekçe: `.agent-method/decisions/D-022-github-issues-canonical-backlog.md`.
