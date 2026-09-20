# POOL — GD'ye dönüşmemiş iş kalemleri (kanonik backlog kaynağı)

> **Tek kanonik backlog kaynağı budur** (INV-12). `.claude/docs/08-BACKLOG.md`
> 2026-09-20'de buraya işaret eden bir yönlendiriciye indirgendi.
> Açık kalemlerin tamamı (PHASE-006…009 + PHASE-100) burada; kapanmış görevlerin
> tarihsel notları TASKLOG'da ve git geçmişinde kaldı.
> Eski `T*` ID'leri **korunmuştur** — TASKLOG ve doküman referansları kırılmasın.

## PHASE-006 — Eğitim MVP (aktif faz, açık kalemler)

| ID | Title | Phase hint | Priority | Notes |
|---|---|---|---|---|
| T6.16/T6.17g-j | Set 0 dersleri 7-10 tam zenginlikte (Risk ne demek · Vade-hedef-portföy · Fiyat nereden çıkıyor · Bir alım nasıl gerçekleşir) | PHASE-006 | 1 | Sıradaki somut iş: **S0-L7 "Risk ne demek?"**. Künye `16` §S0-L7. LiveContext sorunu **D-017** ile çözüldü (künye kanonik; test istisna listesi taşır) — uygulaması GD-001 içinde. |
| T5E.4b | ConceptTag derin bağlantısı (Analiz/Hisse kartından `/lessons/by-concept`) | PHASE-006 | 2 | T5E.4'ün `[~]` kalan parçası. |
| T6.11c | Set 1 dersleri 3-5 aynı zenginlikte (figür + 9 soru) | PHASE-006 | 2 | M3/M5 sayısal eşiklerinin global açılmasının önkoşulu. |
| T6.3 | Kavram sözlüğü `/egitim/sozluk` (aranabilir InfoTip indeksi + çapraz bağlantı) | PHASE-006 | 2 | Faz 6 DoD'de. |
| T6.22 | Set 2 iskeleti (`grafik-ve-piyasa`, 8 ders) + eski setlerin OrderIndex kayması | PHASE-006 | 2 | Kod etkisi yok, migration gerekmez. |
| T6.23 | Grafik SVG öğeleri (ChartFrame/SeriesPath/Candle/VolumeBars/BookLadder/RangeBrush) | PHASE-006 | 2 | **İçerikten ÖNCE** yapılır. |
| T6.24a-h | Set 2 içerik turu (8 ders, 14-15 aşama, 8-11 figür) | PHASE-006 | 3 | T6.23'e bağlı. |
| T6.25 | Ölçek oynatıcısı (`chart-scale-playground`) | PHASE-006 | 3 | Setin tek etkileşimli aracı; fallback statik figür. |
| T6.26 | Ölçüt (endeks) serisi + kalem eşlemesi + LiveContext anahtarı | PHASE-006 | 3 | Gelene dek ders kurgusal/etiketli örnekle çalışır. |
| T6.27 | Grafik dili guard'ları (M7a/M7b) | PHASE-006 | 3 | İçerikten sonra; T6.20 ile aynı dosya. |
| T6.4 | İlerleme mekaniği: rozetler + haftalık check-in serisi | PHASE-006 | 4 | Ölçüm `14` §8 ile uyumlu. |
| T6.9 | `UserConceptMastery` + aralıklı tekrar | PHASE-006 | 4 | Quiz → ustalık akışı. |
| T6.10 | Eğitim demo bağlam portföyü (salt-okunur, rozetli) | PHASE-006 | 4 | Demo sayı kullanıcının panosuna sızmaz (SC-E3). |
| T6.11 | Set 3 içerikleri — Portföyünü Okumak (4 ders) | PHASE-006 | 5 | |
| T6.12 | Set 4 içerikleri — Davranış (4 ders) | PHASE-006 | 5 | |
| T6.13 | Set 5 içerikleri — Türkiye Gerçekleri (4 ders) | PHASE-006 | 5 | Fon dersi T7.5'e bağlı. |
| T6.14 | LLM ders yorumu katmanı (opsiyonel) | PHASE-006 | 6 | ⚠ **D-018 ile önceliği düştü:** LLM artık varsayılan kapalı, opsiyonel zenginleştirme. Yapılırsa yeni guard kuralı: enstrüman sıralaması (SC-E5). |

## PHASE-007 — Kişiselleşme & Erişim

> DoD: kapalı beta (aile/arkadaş) çalışıyor — kayıt → seviye ölçümü →
> kişiselleşmiş içerik; PWA yüklenebilir; demo mod kayıtsız geziliyor.

| ID | Title | Bağımlılık | Doküman | Notes |
|---|---|---|---|---|
| T7.1 | InfoTip/LLM prompt tonunun `LiteracyLevel`'a bağlanması | T6.6 | `15` §4 | Onboarding'in kendisi T6.6'ya taşındı (2026-07-19); Faz 7'de yalnız ton bağlama kaldı. |
| T7.2 | **Kimlik/çok kullanıcı:** JWT (access+refresh) + Argon2id + kayıt/giriş; KVKK "verimi sil"; IDOR (SC-13) + AuthZ + rate-limit testleri; audit log tam | Faz 1 | `11` §2-3, `03` §B | D-007'nin kapanışı; PHASE-009'un kapısı. |
| T7.3 | **PWA:** manifest + service worker + yüklenebilirlik | Faz 5 | `14` §4-C3, `13` | RN öncesi ara adım. |
| T7.4 | **Bildirim v1:** haftalık portföy özeti + ders hatırlatması | T7.2, T7.3 | `14` §4-C4 | ⚠ Bilgi, tavsiye değil — hukuk merceği (`14` §6). |
| T7.5 | **TEFAS/BEFAS fon verisi:** fon fiyat/kategori + gider oranı kavramı | Faz 2 | `14` §4-B2 | `IPriceProvider` desenine oturur (D-008). |
| T7.6 | **Altın kültürü modülü:** çeyrek/yarım/tam/bilezik/22 ayar + düğün altını | Faz 1 | `14` §4-B3 | |
| T7.7 | **Demo/misafir modu:** kayıtsız örnek portföyle tüm akış (salt-okunur) | T7.2 | `14` §4-D1 | Okul/atölye kullanımı; KVKK yükü sıfır. |
| T7.8 | **Açık kaynak anlatısı:** README konumlandırması + ders içeriği katkı rehberi | T6.1 | `14` §4-D2 | |
| T7.9 | **"Bunu neden görüyorum?":** nudge ve LLM kartlarında açılır kaynak/formül detayı | Faz 3 | `14` §4-A3 | |

## PHASE-008 — Ölçek & Etki (hukuki onaya bağlı)

> DoD: açık uçlu; her görev kendi DoD'sini taşır. **T8.5 (hukuk) yeşil olmadan
> ürünleşme/lansman başlamaz.**

| ID | Title | Bağımlılık | Doküman | Notes |
|---|---|---|---|---|
| T8.1 | **Davranış aynası:** işlem geçmişinden geçmişe dönük, yargısız kalıp farkındalığı | Faz 7 | `14` §4-A5 | ⚠ SPK merceğinde **avukat görüşüyle birlikte** tasarlanır (D-002). |
| T8.2 | **Enflasyon paneli:** nominal vs reel grafiği + "yastık altında/mevduatta dursaydı" | Faz 5 | `14` §4-B1 | Geçmiş veri, TÜİK TÜFE bazlı, kaynak görünür. ⚠ Bugün enflasyon verisi "örnek" etiketli (T6.21). |
| T8.3 | **Senaryo simülatörü tam sürüm:** çoklu dağılım karşılaştırması | T5.4 | `14` §4-C1 | Tahmin değil. |
| T8.4 | **Mobil kolu başlat** (PHASE-100) | Faz 7 | `05` | |
| T8.5 | **Gelir modeli kararı + hukuki doğrulama:** freemium/B2B seçimi + **SPK + KVKK avukat onayı** | Faz 7 | `14` §5-6 | **Lansman kapısı, şart.** Reklam/komisyon asla (D-016). |
| T8.6 | **Güvenlik/dayanıklılık tamamlama:** at-rest şifreleme, şifreli yedek, retention, secret rotasyonu, bağımlılık/imaj taraması, sağlayıcı fallback zinciri | T7.2 | `11`, `12`, `14` §4-C5 | |
| T8.7 | **İşbirlikleri & içerik kanalı:** FODER/üniversite temasları + anonim "portföy okuma" içerikleri | Faz 7 | `14` §4-D3/D4 | Ürün dışı; etki kanıtı `14` §8 metrikleriyle. |
| T8.8 | Yeni varlık türleri: fon (T7.5 üstüne), gayrimenkul, kripto (istenirse) | Faz 5 | `01` | |

## PHASE-009 — Nakit Akışı & Harcama Bilinci

> Kapı: **T7.2 (kimlik) yeşil olmadan canlıya çıkmaz** — harcama verisi
> `X-User-Id` ile korunamayacak kadar hassas. Açık bankacılık/ÖHVPS DEĞİL;
> kullanıcı kendi indirdiği ekstreyi yükler.
> DoD: ekstre yüklenip 5 dakikada kategorize pano görünür; aynı ekstre ikinci
> kez yüklenince rakamlar değişmez; birikim senaryosu geçmişe dönük çalışır;
> IDOR + KVKK silme testleri yeşil.

| ID | Title | Bağımlılık | Doküman | Notes |
|---|---|---|---|---|
| T9.1 | **Nakit akışı veri modeli:** `CashAccount`, `CashTransaction`, `ExpenseCategory`, `CategoryRule`, `StatementImport` + migration + KVKK kaskadları | T7.2 | `03` | Portföy `Transaction`'ından AYRI domain. |
| T9.2 | **Ekstre içe aktarma:** CSV/Excel + sütun eşleme sihirbazı + **idempotent tekrar-yükleme** (satır hash'i) | T9.1 | `11` §4 | Banka başına parser yok; ham dosya ayrıştırma sonrası saklanmaz. Birim test zorunlu. |
| T9.3 | **Kategorileştirme kural motoru** (deterministik, kodda); kullanıcı düzeltmesi kurala dönüşür | T9.2 | `07` | LLM'e toplam/hesap yaptırılmaz (D-001). |
| T9.4 | **Nakit akışı panosu:** gelir/gider/birikim oranı, kategori dağılımı, aylık trend | T9.3 | `13`, `04` | |
| T9.5 | **LLM kategori önerisi:** yalnız eşleşmeyen satırlar, kullanıcı onaylı | T9.3 | `07` | Onay kurala dönüşür; yapılandırılmış çıktı + fallback (D-009). |
| T9.6 | **Birikim senaryosu (geçmişe dönük):** "bu kategoriden ayda X ayırsaydın…" | T9.4, T5.4 | `14` §4-C1 | Tahmin yok (D-002). |
| T9.7 | **Eğitim entegrasyonu:** bütçe/birikim dersleri + "Senin bütçende" bölümü | T9.4, Faz 6 | `14` §4-A1 | |
| T9.8 | **KVKK sertleştirme:** ham ekstre saklanmaz, hassas alanlar şifreli, "verimi sil" kapsar, log'a işlem açıklaması yazılmaz | T9.1 | `11` §7, `12` §3 | ⚠ Tasarım kısıtı, "sonra" değil. |

## PHASE-100 — Mobil Kol (React Native / Expo)

> Backend ve `@finans/shared` hazır olduğu için mobil **yalnız sunum katmanını**
> yazar. DoD: web ile işlevsel parite, aynı API/paket, tasarım dili korunur,
> disclaimer'lar yerinde, testler yeşil. Şartname: `05`.

| ID | Title | Bağımlılık | Doküman | Notes |
|---|---|---|---|---|
| TM.1 | Expo uygulaması (`mobile/`) + React Navigation + `@finans/shared` | T0.2 + web Faz 1 | `06` §2, `05` | |
| TM.2 | Tema token'larını RN'e uygula + fontlar (`expo-font`) | TM.1 | `05` §2 | |
| TM.3 | Portföy ekranı: `HeroCard` + `AllocationDonut` (react-native-svg) + `HoldingRow` | TM.1 | `05` §3 | |
| TM.4 | Varlık detay + ekle (alttan kayan overlay) | TM.3 | `05` §7-8 | |
| TM.5 | Analiz / Hisse / Eğitim ekranları (web ile parite) | TM.3 | `05` §4-6 | |
| TM.6 | Token saklama (`expo-secure-store`) + Jest/RTL + Maestro E2E | TM.1 | `11` §2, `09` §3 | |

## Spike'lar (karar girdisi üreten kısa işler)

| ID | Title | Phase hint | Priority | Notes |
|---|---|---|---|---|
| ~~SPIKE-001~~ | ~~Yerel açık ağırlıklı model denemesi~~ → **bitti 2026-09-20, sonuç: D-018** | — | — | Ölçüm: `qwen3:0.6b/8b`, `gemma3:4b/12b` · hepsi CPU (RTX 3060 sürücü 512.89 eski → Ollama GPU görmedi, `total_vram=0`) · en iyi başarılı koşu 281 sn (bütçe 150) · şema tutturma ~%50 · 0.6b **yanlış atıf** yaptı (guard yakalamaz). Raporlar: `tmp_diag/llm-trial/`. Düzenek repoda kalır (`LocalModelTrial.cs`) — donanım/model değişirse tekrarlanır. |
| T-LLM.1 | **Deterministik yorum çekirdeği (D-018 uygulaması):** yorum kartları kural + şablon ile üretilir (`NudgeRuleEngine` deseni); LLM opsiyonel ve varsayılan kapalı; LLM açıkken çıktı çekirdeğin ÜSTÜNE gelir (başarısızlıkta kullanıcı tam yorum görür, "üretilemedi" kartı değil) | PHASE-006 sonrası | 2 | Şablon boşlukları hesaplanmış değerlerden dolar → yanlış atıf imkânsız. Çıktı deterministik olduğu için **birim testle kilitlenir** (D-015). Kapsam: `PortfolioController` yorum ucu + `StocksController` açıklama ucu. |

## Çapraz-kesen kurallar (her GD için geçerli, POOL kalemi değil)

Backlog'un "Çapraz-kesen" bölümü ADM'de **karar** olarak yaşamalı:
senaryo+test yeşil kapısı (D-015), `decimal`+TR biçim (D-004), UserId kapsamı
ve IDOR (D-007), sır/PII yok (D-011), cache+async (D-011), disclaimer (D-002).

## Taşıma kaydı (2026-09-20)

`08-BACKLOG.md` bir **işaretçiye indirgendi**; açık kalemlerin tamamı (Faz 6-9 +
Faz M) buraya alındı. Kapanmış görevlerin (Faz 0-5, ~80 satır zengin uygulama
notu) tarihsel ayrıntısı **git geçmişinde** (`git show ac61b7e:.claude/docs/08-BACKLOG.md`)
ve `.claude/tasks/TASKLOG.md`'de durmaya devam ediyor — POOL yalnız **açık işi**
taşır. Tek kanonik backlog kaynağı artık bu dosyadır (INV-12).
