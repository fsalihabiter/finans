<!-- GENERATED FILE — DO NOT EDIT BY HAND. -->
# BACKLOG — GitHub Issues projeksiyonu

> **Üretilmiş dosya (INV-12 · INV-17).** Elle düzenlenmez; her düzenleme
> bir sonraki üretimde kaybolur.
>
> **Kanonik backlog kaynağı: GitHub Issues** → https://github.com/fsalihabiter/finans/issues (D-022).
> Bu dosya çevrimdışı/ajan okuması için anlık görüntüdür.
>
> Üretim: `.github/scripts/gen-backlog.sh` · 2026-11-22T17:10:38Z · **68 açık issue**
>
> Sütunlar: **Üst** = üst issue (alt-görev ilişkisi) · **Engelleyen** =
> bu iş başlayamadan kapanması gereken issue’lar (`blocked by`).

## Faz-dışı — Spike & Teknik borç  ·  3 açık

| # | Başlık | Tür | Önc. | Alan | Üst | Engelleyen | Etiket |
|---|---|---|---|---|---|---|---|
| #73 | T-LLM.1 — Deterministik yorum çekirdeği (D-018 uygulaması) | Feature | 2 | backend | — | — | — |
| #74 | INC-001 — 4 entegrasyon testi ortam hatasıyla kırmızı (health + observability) | Bug | 1 | infra | — | — | kind:debt |
| #75 | INC-002 — INV-04 ile GD "review" durumu çelişiyor (ADM çerçeve sorunu) | Bug | 5 | adm | — | — | — |

## PHASE-006 — Eğitim MVP + Kavram Sözlüğü  ·  34 açık

| # | Başlık | Tür | Önc. | Alan | Üst | Engelleyen | Etiket |
|---|---|---|---|---|---|---|---|
| #8 | RV-017 — Eski derslerde sabit Türkçe ek hatası (7 satır) | Task | 3 | education | — | — | kind:debt |
| #9 | RV-018 — Ön-koşul kilidi yazma yolunda uygulanmıyor | Task | 2 | backend | — | — | kind:debt |
| #12 | RV-003 — LocalModelTrial: HttpClient dispose + timeout env değişkeni | Task | 4 | backend | — | — | kind:debt |
| #13 | RV-004 — İleri atıf deseni kararı (yayında olmayan derse gönderme) | Task | 4 | education | — | — | — |
| #14 | T6.16v — S0-L4 kaydırıcısı + S0-L5/L6 figürlerinin görsel teyidi | Task | 4 | web | — | — | — |
| #15 | T5E.4b — ConceptTag derin bağlantısı (/lessons/by-concept) | Task | 2 | web | — | — | — |
| #16 | T6.11c — Set 1 dersleri 3-5 içerik turu (figür + 9 soru) | Feature | 2 | education | — | — | — |
| #17 | S1-L3 — "F/K, PD/DD Nedir?" içerik turu | Task | 2 | education | #16 | — | — |
| #18 | S1-L4 — "Risk ve Getiri İlişkisi" içerik turu | Task | 2 | education | #16 | — | — |
| #19 | S1-L5 — "Bileşik Getirinin Gücü" içerik turu | Task | 2 | education | #16 | — | — |
| #20 | T6.3 — Kavram sözlüğü /egitim/sozluk (aranabilir InfoTip indeksi) | Feature | 2 | web, education | — | — | — |
| #21 | T6.22 — Set 2 iskeleti (grafik-ve-piyasa) + OrderIndex kayması | Task | 2 | education | — | — | — |
| #22 | T6.23 — Grafik SVG öğeleri (ChartFrame/SeriesPath/Candle/VolumeBars/BookLadder/RangeBrush) | Feature | 2 | web | — | — | — |
| #23 | T6.24 — Set 2 içerik turu (8 ders) | Feature | 3 | education | — | #22 #21 | — |
| #24 | S2-L1 — "Grafik neyi gösterir?" (eksenleri okumak) | Task | 3 | education | #23 | — | — |
| #25 | S2-L2 — "Aynı veri, üç görüntü" (ölçek ve dönem) ⭐ | Task | 3 | education | #23 | — | — |
| #26 | S2-L3 — "Mum grafiği: bir mumun anatomisi" ⭐ | Task | 3 | education | #23 | — | — |
| #27 | S2-L4 — "Zaman dilimi: aynı hikâye, farklı mum" | Task | 3 | education | #23 | — | — |
| #28 | S2-L5 — "Hacim, makas ve emir defteri" | Task | 3 | education | #23 | — | — |
| #29 | S2-L6 — "Grafik geleceği söylemez: örüntü tuzağı" 🔴 | Task | 3 | education | #23 | — | — |
| #30 | S2-L7 — "Endeks ve ölçüt: piyasa kim?" | Task | 3 | education | #23 | #33 | — |
| #31 | S2-L8 — "Kalemini piyasaya göre okumak" 🔴 | Task | 3 | education | #23 | #33 | — |
| #32 | T6.25 — Ölçek oynatıcısı (chart-scale-playground) | Feature | 3 | web | — | #22 | — |
| #33 | T6.26 — Ölçüt (endeks) serisi + kalem eşlemesi + LiveContext anahtarı | Feature | 3 | backend | — | — | — |
| #34 | T6.27 — Grafik dili guard’ları (M7a/M7b) | Task | 3 | backend | — | #23 | — |
| #35 | T6.4 — İlerleme mekaniği: rozetler + haftalık check-in serisi | Feature | 4 | web | — | — | — |
| #36 | T6.9 — UserConceptMastery + aralıklı tekrar | Feature | 4 | backend | — | — | — |
| #37 | T6.10 — Eğitim demo bağlam portföyü (salt-okunur, rozetli) | Feature | 4 | backend | — | — | — |
| #38 | T6.11 — Set 3 içerikleri: Portföyünü Okumak (4 ders) | Feature | 5 | education | — | — | — |
| #39 | T6.12 — Set 4 içerikleri: Davranış (4 ders) | Feature | 5 | education | — | — | — |
| #40 | T6.13 — Set 5 içerikleri: Türkiye Gerçekleri (4 ders) | Feature | 5 | education | — | #46 | — |
| #41 | T6.14 — LLM ders yorumu katmanı (opsiyonel, D-018 ile düşük öncelik) | Feature | 6 | backend | — | — | — |
| #194 | T6.28 — LiveContext token'ları düz metin içinde: satır düşünce cümle bozuluyor (11 ders) | Bug | 2 | education | — | — | kind:debt |
| #195 | T6.29 — S0-L9 kaynaklarının belge+tarih teyidi (SPK · Borsa İstanbul) | Task | 3 | education | — | — | kind:debt |

## PHASE-007 — Kişiselleşme & Erişim  ·  9 açık

| # | Başlık | Tür | Önc. | Alan | Üst | Engelleyen | Etiket |
|---|---|---|---|---|---|---|---|
| #42 | T7.1 — InfoTip/LLM prompt tonunun LiteracyLevel’a bağlanması | Feature | — | web | — | — | — |
| #43 | T7.2 — Kimlik/çok kullanıcı: JWT + Argon2id + IDOR/AuthZ/rate-limit testleri | Feature | — | backend, infra | — | — | — |
| #44 | T7.3 — PWA: manifest + service worker + yüklenebilirlik | Feature | — | web, infra | — | — | — |
| #45 | T7.4 — Bildirim v1: haftalık portföy özeti + ders hatırlatması | Feature | — | backend, web | — | #43 #44 | — |
| #46 | T7.5 — TEFAS/BEFAS fon verisi (fiyat/kategori + gider oranı) | Feature | — | backend | — | — | — |
| #47 | T7.6 — Altın kültürü modülü (çeyrek/yarım/tam/bilezik/22 ayar) | Feature | — | backend, web | — | — | — |
| #48 | T7.7 — Demo/misafir modu (kayıtsız, salt-okunur tam akış) | Feature | — | backend, web | — | #43 | — |
| #49 | T7.8 — Açık kaynak anlatısı: README + içerik katkı rehberi | Task | — | product | — | — | — |
| #50 | T7.9 — "Bunu neden görüyorum?" açılır kaynak/formül detayı | Feature | — | web | — | — | — |

## PHASE-008 — Ölçek & Etki  ·  8 açık

| # | Başlık | Tür | Önc. | Alan | Üst | Engelleyen | Etiket |
|---|---|---|---|---|---|---|---|
| #51 | T8.1 — Davranış aynası (geçmişe dönük, yargısız kalıp farkındalığı) | Feature | — | backend | — | #55 | gate:legal |
| #52 | T8.2 — Enflasyon paneli (nominal vs reel, TÜİK TÜFE) | Feature | — | backend, web | — | — | — |
| #53 | T8.3 — Senaryo simülatörü tam sürüm (çoklu dağılım karşılaştırması) | Feature | — | backend, web | — | — | — |
| #54 | T8.4 — Mobil kolu başlat (PHASE-100 kapısı) | Task | — | mobile | — | — | — |
| #55 | T8.5 — Gelir modeli kararı + SPK/KVKK avukat onayı (lansman kapısı) | Task | — | product | — | — | gate:legal |
| #56 | T8.6 — Güvenlik/dayanıklılık tamamlama (şifreleme, yedek, rotasyon, tarama) | Feature | — | infra | — | #43 | — |
| #57 | T8.7 — İşbirlikleri & içerik kanalı (FODER/üniversite + anonim portföy okuma) | Task | — | product | — | — | — |
| #58 | T8.8 — Yeni varlık türleri: fon, gayrimenkul, kripto | Feature | — | backend | — | #46 | — |

## PHASE-009 — Nakit Akışı & Harcama Bilinci  ·  8 açık

| # | Başlık | Tür | Önc. | Alan | Üst | Engelleyen | Etiket |
|---|---|---|---|---|---|---|---|
| #59 | T9.1 — Nakit akışı veri modeli + migration + KVKK kaskadları | Feature | — | backend | — | #43 | — |
| #60 | T9.2 — Ekstre içe aktarma (CSV/Excel + sütun eşleme + idempotent yükleme) | Feature | — | backend | — | #59 | — |
| #61 | T9.3 — Kategorileştirme kural motoru (deterministik, kullanıcı düzeltmesi kurala dönüşür) | Feature | — | backend | — | #60 | — |
| #62 | T9.4 — Nakit akışı panosu (gelir/gider/birikim oranı, kategori dağılımı, trend) | Feature | — | web | — | #61 | — |
| #63 | T9.5 — LLM kategori önerisi (yalnız eşleşmeyen satırlar, kullanıcı onaylı) | Feature | — | backend | — | #61 | — |
| #64 | T9.6 — Birikim senaryosu (geçmişe dönük) | Feature | — | backend, web | — | #62 | — |
| #65 | T9.7 — Eğitim entegrasyonu: bütçe dersleri + "Senin bütçende" | Feature | — | education | — | #62 | — |
| #66 | T9.8 — KVKK sertleştirme (ham ekstre saklanmaz, şifreleme, silme kapsamı) | Feature | — | backend, infra | — | #59 | — |

## PHASE-100 — Mobil Kol  ·  6 açık

| # | Başlık | Tür | Önc. | Alan | Üst | Engelleyen | Etiket |
|---|---|---|---|---|---|---|---|
| #67 | TM.1 — Expo uygulaması + React Navigation + @finans/shared | Feature | — | mobile | — | #54 | — |
| #68 | TM.2 — Tema token’ları + fontlar (expo-font) | Task | — | mobile | — | #67 | — |
| #69 | TM.3 — Portföy ekranı (HeroCard + AllocationDonut + HoldingRow) | Feature | — | mobile | — | #67 | — |
| #70 | TM.4 — Varlık detay + varlık ekle (alttan kayan overlay) | Feature | — | mobile | — | #69 | — |
| #71 | TM.5 — Analiz / Hisse / Eğitim ekranları (web paritesi) | Feature | — | mobile | — | #69 | — |
| #72 | TM.6 — Token saklama (expo-secure-store) + Jest/RTL + Maestro E2E | Feature | — | mobile, infra | — | #67 | — |

---

## Tamamlananlar — 124 kapalı issue

> Faz 0-5 ve Faz 6'nın bitmiş kalemleri 2026-11-22'de **geriye dönük**
> kapalı issue olarak girildi (D-022). Uygulama notları issue gövdelerinde;
> kaynak `git show ac61b7e:.claude/docs/08-BACKLOG.md` ve `.claude/tasks/TASKLOG.md`.

| Faz | Kapalı |
|---|---:|
| Faz-dışı — Spike & Teknik borç | 4 |
| PHASE-000 — Hazırlık & İskelet | 15 |
| PHASE-001 — Portföy Takip MVP | 33 |
| PHASE-002 — Canlı Fiyat & Bilgilendirme | 10 |
| PHASE-003 — LLM Yorum Katmanı | 12 |
| PHASE-004 — Hisse Temel Analiz | 5 |
| PHASE-005 — Değer Seyri + Senaryo v1 | 6 |
| PHASE-006 — Eğitim MVP + Kavram Sözlüğü | 39 |

Listelemek için:

```bash
gh issue list --repo fsalihabiter/finans --state closed --limit 200
```

---

## Kapsam dışı (burada görünmez)

- **Kapanmış işlerin tam listesi** → `gh issue list --state closed` (yukarıda yalnız faz bazlı sayım var); tarihsel notlar `.claude/tasks/TASKLOG.md` ve git geçmişindedir.
- **Kararlar** → `.agent-method/decisions/D-NNN-*.md` (kanonik, taşınmadı).
- **Döngü gerekçesi / kabul kriterleri** → `.agent-method/cycles/GD-NNN.md` (kanonik).
- **Fikirler** → `.agent-method/pipeline/IDEAS.md` (issue'ya yükseltme insan kararıdır).
- **Test borcu** → `.agent-method/pipeline/TEST_DEBT.md` (INV-08).
