---
doctor_run_id: DOCTOR-001
timestamp: "2026-09-18"
runtime: claude-code
mode: full
scope: "."
status: findings
findings:
  - id: DR-001
    severity: high
    evidence: proven
    check: gitignored-canonical-sources
    file: .gitignore
    summary: ".gitignore satır 99-102 `.agents/` ve `AGENTS.md`'yi yok sayıyor. Kanonik skill tanımları ve ADM işletim sözleşmesi repoya girmez; temiz bir klonda 7 Claude shim'i var olmayan hedefe işaret eder (INV-16) ve CLAUDE.md'nin ilk satırı `@AGENTS.md` kırık import olur."
    canonical_source: ".agents/skills/*/SKILL.md ve AGENTS.md (ADM sözleşmesi: bunlar versiyonlanmalı)"
    status: resolved
    resolution:
      resolved_at: "2026-09-20"
      evidence_ref: ".gitignore"
      note: "`.agents/` ve `AGENTS.md` yok sayma satırları kaldırıldı; yerine gerekçe yorumu yazıldı. `.codex/` bilerek yok sayılmaya devam ediyor (yalnız üretilen shim'ler — INV-14). İnsan onayı: kullanıcı düzeltmeyi istedi ve uygulandı."
  - id: DR-002
    severity: high
    evidence: proven
    check: INV-14
    file: .claude/tasks/ACTIVE.md
    summary: "Projenin gerçek iş durumu ADM dışında, runtime adaptör klasöründe yaşıyor: .claude/tasks/ACTIVE.md + TASKLOG.md (aktif görev/oturum geçmişi), .claude/docs/08-BACKLOG.md (backlog), .claude/docs/14-16 (strateji/eğitim kararları). `.claude/` silinirse bu alan verisi kaybolur. CLAUDE.md §11 protokolü de kayıtları .claude/tasks'a yazdırıyor → iki paralel takip sistemi."
    canonical_source: ".agent-method/ (state.yaml, cycles/, decisions/, pipeline/POOL.md, evidence/sessions/) — 2026-09-20'de kanonik kaynak olarak seçildi."
    status: resolved
    resolution:
      resolved_at: "2026-09-20"
      evidence_ref: ".agent-method/ (adm-analyze taşıması) + CLAUDE.md §11/§11.1 + .claude/hooks/adm-session-start.mjs"
      note: "Alan verisi ADM'ye taşındı (PHASES, POOL, D-001..D-016, CAPABILITIES, GD-001); CLAUDE.md §11 adm-open/adm-close akışına çevrildi; SessionStart hook'u artık yalnız .agent-method/ okuyor; .claude/tasks/ tarihsel kayıt olarak donduruldu. 08-BACKLOG.md işaretçiye indirgendi, açık kalemler POOL'a taşındı → tek kanonik backlog kaynağı POOL (INV-12). Artakalan yok."
  - id: DR-003
    severity: high
    evidence: proven
    check: bootstrap-not-run
    file: .agent-method/charter/PROJECT_INTENT.md
    summary: "ADM iskeleti doldurulmamış: PROJECT_INTENT.md tamamen şablon (hedef yok, §3 kapsam-dışı listesi boş), state.yaml project.id=my-project / name='My Project', CAPABILITIES boş, decisions/ ve cycles/ içinde gerçek kayıt yok. INV-02 değerlendirilemiyor; semantik drift kontrolleri için anahtar kelime kaynağı yok."
    status: resolved
    resolution:
      resolved_at: "2026-09-20"
      evidence_ref: ".agent-method/charter/PROJECT_INTENT.md · registry/CAPABILITIES.md · decisions/D-001..D-016 · cycles/GD-001.md · state.yaml"
      note: "adm-analyze taraması onaylanıp uygulandı: 5 hedef (G-1..G-5), somut kapsam-dışı anahtar kelimeleri (semantik drift taraması artık mümkün), 15 yetenek, 16 karar (hepsi status: proposed — accepted'a yükseltme ayrı insan kapısı), GD-001, project.id=nirengi-finans. ⚠ ARTAKALAN: PROJECT_INTENT sonundaki 3 açık soru (G-2 eşiği, K3 onayı, Faz 9'un hedefi) hâlâ karar bekliyor."
  - id: DR-004
    severity: medium
    evidence: proven
    check: INV-01
    file: .agent-method/charter/PHASES.md
    summary: "PHASES.md'de şablon PHASE-001 duruyor: başlık '<title>', status değeri geçersiz ('planned | active | completed'), boş kriterler. Proje fiilen Faz 6'da (CLAUDE.md §9) ama ADM faz tanımı bununla eşleşmiyor."
    canonical_source: ".agent-method/charter/PHASES.md (2026-09-20'den beri kanonik)"
    status: resolved
    resolution:
      resolved_at: "2026-09-20"
      evidence_ref: ".agent-method/charter/PHASES.md"
      note: "Şablon PHASE-001 kaldırıldı; PHASE-000..009 + PHASE-100 yazıldı (Faz 0-5 completed, PHASE-006 active, gerisi planned) ve state.yaml PHASE-006'yı gösteriyor. ⚠ ARTAKALAN: tamamlanmış fazların kabul kriterlerinin geriye dönük yazılıp yazılmayacağı (INV-06) ve PHASE-100 numaralandırması karar bekliyor."
  - id: DR-005
    severity: medium
    evidence: proven
    check: nested-repository
    file: finans/
    summary: "Kök altında izlenmeyen iç içe bir git klonu var (`finans/`, ~15 MB, aynı origin, eski HEAD 950e4a6 vs kök ac61b7e). Yanlışlıkla `git add .` ile gömülü repo olarak eklenme, yinelenen CLAUDE.md/AGENTS talimatları ve arama sonuçlarında eski kodun karışması riski."
    status: open
  - id: DR-006
    severity: low
    evidence: strong
    check: broken-reference
    file: RUNTIME_TEST_PLAN.md
    summary: "ADM template dağıtım dosyaları proje köküne kopyalanmış (V2_REFACTOR_REPORT.md, MIGRATION_V1_TO_V2.md, RUNTIME_TEST_PLAN.md, KULLANIM.md, CONTRIBUTING.md 'Contributing to ADM', adapters/). RUNTIME_TEST_PLAN.md var olmayan `examples/minimal-project/`'e, V2_REFACTOR_REPORT.md repoda olmayan V2_* spec dosyalarına referans veriyor. CONTRIBUTING.md finans projesinin değil ADM'nin katkı rehberi."
    status: open
---

# DOCTOR-001 — İlk ADM denetimi (2026-09-18)

Önceki rapor yok; taşınan bulgu yok.

> **Çözüm turu (2026-09-20, kullanıcı onaylı):** DR-001…DR-004 `resolved`.
> Sırasıyla: `.gitignore` düzeltildi · alan verisi `.agent-method/`'a taşındı
> (adm-analyze) + CLAUDE.md §11 ve SessionStart hook'u ADM'ye çevrildi ·
> intent/kararlar/yetenekler dolduruldu · fazlar yazıldı.
> **Açık kalanlar: DR-005** (iç içe `finans/` klonu) ve **DR-006** (kökteki ADM
> dağıtım dosyaları). Ayrıca her çözümün `note` alanındaki **artakalan** notları
> (08-BACKLOG↔POOL kanonik seçimi, intent açık soruları, geçmiş fazların kabul
> kriterleri) karar bekliyor — bunlar bulgu değil, açık karardır.
> Rapor durumu `findings` olarak kalır.

## Geçen yapısal kontroller
- **INV-16** (yerel): 7 kanonik skill ↔ 7 shim eşleşiyor, hedefler mevcut. *(Klonda bozulur → DR-001.)*
- **INV-17 / INV-12:** `projections: []`, `generated/` yok — tutarlı.
- **INV-13:** state.yaml'da açık/eskimiş oturum yok.
- **INV-04 / INV-05:** aktif GD yok (null) — boş geçer.
- **INV-09 / 10 / 11:** D-/S-/GD- kaydı yok; çakışma yok. Bu rapordaki DR id'leri tekil.
- Şema: state.yaml ve config.yaml `state.schema` / `config.schema` anahtarlarıyla uyumlu görünüyor.

## Semantik kontroller
**Yapılamadı.** PROJECT_INTENT §3 ve kararların Keywords bölümleri boş olduğu için
anahtar kelime tabanlı drift tespiti için girdi yok (DR-003). Drift uydurulmadı.

## Önerilen düzeltmeler (seçenek — hiçbiri uygulanmadı)
- **DR-001:** (a) `.gitignore`'dan `.agents/` ve `AGENTS.md` satırlarını çıkarıp ADM'yi versiyonla; veya (b) ADM'yi yalnızca yerel araç sayacaksan CLAUDE.md'deki `@AGENTS.md` importunu ve `.claude/skills/adm-*` shim'lerini de ignore et (tutarlı olsun).
- **DR-002:** (a) `adm-analyze` ile mevcut `.claude/tasks` + `.claude/docs` içeriğini `.agent-method/`'a taşı (faz → PHASES, backlog → POOL, kararlar → D-NNN, oturum geçmişi → evidence/sessions) ve CLAUDE.md §11'i ADM akışına (adm-open/adm-close) yönlendir; veya (b) ADM'yi kullanmayacaksan `.agent-method/` ve adm-* shim'lerini kaldır — iki paralel sistem tutma.
- **DR-003 / DR-004:** mevcut kod tabanında `adm-analyze` (yeni proje değil, bu yüzden adm-bootstrap değil) çalıştırıp intent, fazlar (Faz 0-8) ve kapsam-dışı listesini (ör. "yatırım tavsiyesi", "al/sat önerisi" — CLAUDE.md §2) doldur.
- **DR-005:** `finans/` klasörünün içinde commit edilmemiş/push edilmemiş iş olup olmadığını kontrol et; yoksa sil veya repo dışına taşı; en azından `.gitignore`'a ekle.
- **DR-006:** template dağıtım dosyalarını kökten kaldır ya da `.agent-method/docs/` altına taşı; CONTRIBUTING.md'yi projeye özgü hale getir.
