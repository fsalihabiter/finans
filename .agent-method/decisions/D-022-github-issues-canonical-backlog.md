---
id: D-022
title: Açık iş kalemlerinin kanonik kaynağı GitHub Issues; POOL.md projeksiyona indirgenir
status: accepted
scope: structure
created: 2026-11-22
supersedes: null
superseded_by: null
---

# D-022 — Kanonik backlog GitHub Issues'a taşındı

## Context
Backlog `.agent-method/pipeline/POOL.md` içinde markdown tablosu olarak
tutuluyordu: 50 açık kalem, altı faz başlığı altında, `ID | Title | Bağımlılık |
Doküman | Notes` sütunlarıyla. Bu yapı kanonikliği doğru tutuyordu (INV-12) ama
**takip yüzeyi olarak yetersizdi**:

- Durum alanı yoktu. Bir kalemin açık / başlanmış / engellenmiş olduğu ancak
  "Notes" sütununun serbest metninden okunabiliyordu (`T6.16` satırı buna örnek:
  dört ayrı durum bilgisi tek hücreye sıkışmıştı).
- Bağımlılıklar metindi ("Faz 7", "T9.3'e bağlı") — makine okuyamıyor, bir iş
  açılabilir hâle geldiğinde kimse haber almıyor.
- Geçmiş yoktu: bir kalemin ne zaman, hangi commit'le kapandığı yalnız
  TASKLOG/git arkeolojisiyle bulunuyordu.
- Kırılım yoktu: "Set 2 içerik turu (8 ders)" tek satırdı; 8 dersin kaçının
  bittiği görünmüyordu.
- Pano/filtre yoktu. Kullanıcının isteği açıkça buydu: *"planlanan her şeyin
  tasklar halinde takibini yapabilmek"*.

Kod zaten GitHub'da (`fsalihabiter/finans`). GitHub Issues 2025'ten beri
**alt-görev** (parent → sub-issue), **issue type** (Feature/Task/Bug) ve
**bağımlılık** (`blocked by` / `blocking`) ilişkilerini destekliyor; `gh` CLI
2.94+ bunların hepsini terminalden yazıyor, yani ajanlar da POOL tablosunu
düzenler gibi görev yazabiliyor. Alternatif olarak Azure Boards (TFS'in bugünkü
hâli) değerlendirildi.

## Decision
**Açık iş kalemlerinin kanonik kaynağı GitHub Issues'tır.**
`.agent-method/pipeline/POOL.md` bir **işaretçiye** indirgendi; tablo orada
tutulmaz (INV-12 — iki kanonik kaynak olamaz). `08-BACKLOG.md`'nin 2026-09-20'de
POOL'a devrederken izlediği yolun aynısı.

Eşleme:

| ADM kavramı | GitHub karşılığı | Kanoniklik |
|---|---|---|
| Açık iş kalemi | Issue (`Feature` / `Task` / `Bug`) | ✅ GitHub |
| Faz | Milestone | `charter/PHASES.md` kanonik (niyet); milestone yansıma |
| Kalem kırılımı | Sub-issue (parent ilişkisi) | ✅ GitHub |
| Bağımlılık | `blocked by` / `blocking` | ✅ GitHub |
| Öncelik / alan / kapı | `prio:*`, `area:*`, `gate:legal`, `adm:gd` etiketleri | ✅ GitHub |
| GD gerekçesi, reuse analizi, kabul kriterleri | `cycles/GD-NNN.md` | ✅ repo (taşınmadı) |
| Kararlar | `decisions/D-NNN-*.md` | ✅ repo (taşınmadı) |
| Oturum kanıtı, review, doctor | `evidence/` | ✅ repo (taşınmadı) |
| Fikirler, test borcu, incident kaydı | `pipeline/IDEAS.md`, `TEST_DEBT.md`, `INCIDENTS.md` | ✅ repo (taşınmadı) |

**Çevrimdışı okunabilirlik zorunlu:** `.agent-method/generated/BACKLOG.md`
açık issue'ların commit'li anlık görüntüsünü taşır. `config.generated.projections`
artık `[BACKLOG]`; dosya GENERATED başlığı taşır ve **asla kanonik değildir**
(INV-12, INV-17). Üretici `.github/scripts/gen-backlog.sh`; issue olaylarında ve
günlük cron'da `.github/workflows/adm-backlog-sync.yml` otomatik tazeler.

Reddedilen alternatifler:
- **Azure Boards (TFS):** hiyerarşi ve sprint planlaması daha olgun, ama ikinci
  bir platform + ikinci kimlik demek; `gh` CLI'nin ajan tarafındaki karşılığı yok
  (yalnız REST + PAT), kod ile görev ayrı yerlerde kalır ve tek kişilik ekipte
  sprint kapasitesi/alan ağacı boşa gider.
- **POOL.md'yi kanonik bırakıp Issues'ı ayna yapmak:** iki yazılabilir yüzey →
  INV-12 ihlali, kaçınılmaz ayrışma. Issue'da yazılan yorum ve kapanış kaybolur.
- **Hiç taşımamak, POOL'a `status` sütunu eklemek:** sıfır risk ama istenen şeyi
  (pano, geçmiş, kırılım, bağımlılık uyarısı) vermiyor.

## Consequences
- **ADM'nin "kanonik durum `.agent-method/` altındadır" sözleşmesi backlog için
  gevşedi.** Bilinçli bir ödünç: azaltıcı önlem, projeksiyonun repoya commit'li
  olması — depo tek başına her zaman tam bir açık iş kopyası taşır, GitHub
  erişilemese veya repo başka bir yere taşınsa bile liste kaybolmaz.
- **INV-14 (vendor neutrality) etkilenmez:** GitHub bir runtime adapter değil;
  `.claude/` silindiğinde hiçbir şey kaybolmaz.
- Repo **public** → yol haritası, açık güvenlik kalemleri (T8.6, T9.8) ve hukuki
  kapı (T8.5) artık aranabilir biçimde herkese görünür. Kabul edildi (POOL.md
  zaten public idi); gizlilik gerekirse repo private'a alınır.
- `adm-open` / `adm-close` / `adm-doctor` artık açık işi `gh issue list` ile
  okur; ağ yoksa `generated/BACKLOG.md`'ye düşer (son üretim zaman damgası
  dosyanın başında yazılıdır).
- Eski `T*` / `RV-*` / `INC-*` ID'leri issue **başlığında önek** olarak korundu
  → TASKLOG ve doküman referansları kırılmadı; `gh issue list --search "T6.3
  in:title"` ile bulunur.
- Taşıma 50 POOL kalemini **78 issue**'ya açtı (ders bazlı kırılım + REVIEW
  bulguları + GD-003'ün 6 kabul kriteri) ve 27 `blocked by` ilişkisi kurdu.
- GD'ler issue'ya **dönüşmedi**: bir GD açıldığında ilgili issue'ya `adm:gd`
  etiketi gelir ve issue gövdesi `cycles/GD-NNN.md`'ye işaret eder. Kabul
  kriterleri alt-görev olarak izlenir ama **kutuyu insan işaretler** (INV-06).

## Keywords (for Doctor drift detection)
- POOL.md
- GitHub Issues
- generated/BACKLOG.md
- gen-backlog.sh
- adm-backlog-sync
- projections
- INV-12
- INV-17
