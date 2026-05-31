# 08 — Görev Listesi (Backlog)

> `ROADMAP.md`'yi **uygulanabilir, bağımlılık sıralı** görevlere kırar. Her
> oturumda buradan "Sıradaki" görevi al. İşaretleme: `[ ]` açık, `[~]` devam,
> `[x]` bitti. Görev bitince ilgili dokümanda DoD'yi karşıladığını doğrula.

---

## ⭐ ŞU ANKİ DURUM & SIRADAKİ ADIM

**Durum:** ✅ **FAZ 0 TAMAMLANDI (T0.1-T0.14 [x]).** Monorepo (pnpm) +
`@finans/shared` (tip/api/theme/format) + .NET çözümü (`net10.0`, `.slnx`, 4 katman) +
`GET /api/health` + web (Vite/Router/Query + DESIGN.md token'ları/fontlar) +
**EF Core/Npgsql** (12 tablo, migration, tutarlı seed 422.970/641.403/+%51,6) +
**güvenlik/gözlemlenebilirlik** (Serilog+CorrelationId+redaksiyon, /health(+ready),
hata maskeleme, CORS allow-list, User Secrets) + **Docker** (non-root + compose,
`docker compose up --build` doğrulandı) + **test altyapısı** (Sqlite integration
fixture + Vitest/RTL + Playwright). Testler yeşil: backend `dotnet test` 13/13,
web 2, shared 8, e2e 1.

**Sıradaki adım → FAZ 2 altyapı: `T2.8` Gözlemlenebilirlik (Seq + Prometheus + Grafana; OTel metrik) · `T2.9` reverse proxy + rate limit**
(Faz 0 ✅ · Faz 1 ✅ · **T2.1→T2.6 ✅** fiyat zinciri uçtan uca + Web · **T2.7 ✅** dağıtık cache (`IAppCache`,
Redis-opsiyonel) + single-flight + hit/miss metrik). **Faz 2 işlevsel DoD karşılandı**; kalan T2.8-2.9 dağıtım/gözlem.

---

## FAZ 0 — Hazırlık & İskelet

| ID | Görev | Bağımlılık | Doküman | Durum |
|----|-------|-----------|---------|-------|
| T0.1 | `git init` + `.gitignore` (bin/obj, node_modules, dist, .expo, *.env, secrets) | — | `06` §6 | [x] |
| T0.2 | **Monorepo iskeleti:** pnpm workspaces (`packages/*`, `web`, `mobile`) + `@finans/shared` paketi (types/api/theme/format boş iskelet) | T0.1 | `13` §2, `06` §2 | [x] |
| T0.3 | .NET çözümü + 4 katman projesi + test projesi | T0.1 | `02` §2.1, `06` §2 | [x] |
| T0.4 | EF Core + Npgsql kur; `FinansDbContext` | T0.3 | `02`, `03` | [x] |
| T0.5 | Entity'leri yaz — **portföy** (Assets, Holdings, Transactions, PriceSnapshots, BesDetails, FxRates, InflationRates) + **kimlik/audit iskeleti** (Users, Roles, UserRoles, RefreshTokens, AuditLogs) | T0.4 | `03` §A,§B | [x] |
| T0.6 | İlk migration + `database update` | T0.5 | `03` §13 | [x] |
| T0.6b | **Kapsamlı, tutarlı seeder** (`SeedData.cs`, idempotent): kullanıcı/rol, kur+enflasyon, varlık kataloğu, **tutarlı pozisyonlar (641.403/422.970/+%51,6)**, BesDetails, fiyat geçmişi | T0.6 | `03` §12 | [x] |
| T0.7 | `GET /api/health` → `{status:"ok"}` | T0.3 | `04` §3 | [x] |
| T0.8 | **Web iskeleti (★):** Vite React-TS uygulaması (`web/`) + React Router + TanStack Query + `@finans/shared` bağlı | T0.2 | `13` §3, `06` §2 | [x] |
| T0.9 | Tasarım token'ları `@finans/shared/theme` (DESIGN.md → TS + CSS değişkeni) + web'de uygula + fontlar (Fraunces/Hanken) | T0.8 | `13` §3, `DESIGN.md` | [x] |
| T0.10 | **Web mini deneme:** 2-3 route geçişi + `/api/health`'ten veri çekip gösterme | T0.7, T0.9 | `06` §2 | [x] |
| T0.11 | **Test altyapısı:** `Finans.Integration.Tests` (WebApplicationFactory + Sqlite) + FluentAssertions; web'de **Vitest + RTL** (+ Playwright iskeleti); `dotnet test`/`pnpm test` yeşil | T0.3, T0.8 | `09` §2-3 | [x] |
| T0.12 | **Gözlemlenebilirlik temeli:** Serilog yapılandırılmış log (Console + CorrelationId enricher) + redaksiyon politikası iskeleti; `/health` & `/health/ready` (ASP.NET HealthChecks) | T0.3 | `12` §3,§8 | [x] |
| T0.13 | **Güvenlik temeli:** secret yönetimi (User Secrets/env, repoda sır yok) + `.gitignore` sır kalıpları; global hata maskeleme middleware (sözleşmeli hata, stack trace sızmaz); CORS web origin allow-list | T0.3 | `11` §4,§6, `13` §5 | [x] |
| T0.14 | **Docker temeli:** API için Dockerfile (non-root, minimal imaj) + `docker-compose.yml` (api + postgres); lokal `docker compose up` çalışıyor | T0.4 | `02` §6, `11` §8 | [x] |

**Faz 0 DoD:** **Web** `/api/health`'i gösteriyor; `dotnet ef migrations` ile DB
oluşuyor; tasarım token'ları (`@finans/shared`) web'de kullanılıyor; **test
koşucuları (`dotnet test`, `pnpm test`) kurulu ve yeşil**; **yapılandırılmış log
+ health check + Docker Compose ayakta; repoda sır yok**.

---

## FAZ 1 — Portföy Takip MVP

| ID | Görev | Bağımlılık | Doküman | Durum |
|----|-------|-----------|---------|-------|
| T1.1 | `PortfolioCalculationService` — formüller (saf fonksiyon) | Faz 0 | `02` §2.2, `CLAUDE.md` §6 | [x] |
| T1.2 | **Birim testleri** (altın test verisi: 40gr/4.546→181.851, +%43) | T1.1 | `06` §4 | [x] |
| T1.3 | `CurrencyConversionService` + `FxRates` (Faz 1 elle kur) + test | T1.1 | `02`, `03` | [x] |
| T1.4 | Reel getiri (enflasyon oranı girişi) + test | T1.1 | `CLAUDE.md` §6 | [x] |
| T1.5 | Ort. maliyet türetimi (Transactions'tan) + test | T0.4 | `03` §5 | [x] |
| T1.6 | Holdings CRUD endpoint'leri + DTO + validasyon | T1.5 | `04` §4 | [x] |
| T1.7 | `GET /api/portfolio/summary` | T1.1–T1.4 | `04` §4 | [x] |
| T1.8 | BES özel alanları (devlet katkısı ayrı) — entity→DTO→ekran | T0.4 | `03`, `04`, `05` §7 | [x] (entity→DTO `bes`/SC-04 + detay ekranında "Devlet katkısı" ayrı) |
| T1.9 | Settings (baz para birimi) endpoint + web seçimi | T0.7 | `04` §4 | [x] (endpoint GET/PUT + test; web `CurrencySelector` T1.11'de) |
| T1.10 | `@finans/shared`: API tipleri (04) + TanStack Query hook'ları + `formatCurrency/formatPercent` (tr-TR) | T0.2, T1.7 | `13` §2, `05` §10 | [x] (tipler+istemci shared; hook'lar web — mobilde shared'a taşınır) |
| T1.11 | **Web:** AppShell (sidebar/topbar) + `HeroCard` + `summary` bağlama | T1.7, T1.10 | `13` §4 | [x] (+ baz para birimi seçici) |
| T1.12 | **Web:** `AllocationDonut` (SVG/conic) + legend | T1.7 | `13` §4 | [x] (SVG donut + lejant, varlık-türü renkleri) |
| T1.13 | **Web:** Holdings tablosu/kartı + varlık detay (modal/route) | T1.6 | `13` §4 | [x] (tablo + `/holdings/:id` detay: BES, fiyat güncelle, sil) |
| T1.14 | **Web:** "Varlık Ekle" formu (modal) → `POST /holdings` | T1.6 | `13` §4 | [x] (modal form, tür/pb/birim/işlem; hata zarfı gösterimi) |
| T1.15 | **Per-user kapsam deseni:** tüm sorgular `UserId`'e kapsanır (EF global query filter / base repo); summary cache anahtarı `UserId` içerir; in-memory cache (varlık kataloğu/summary) | T1.6 | `11` §3, `10` §3 | [x] (`ICurrentUser`+`WHERE UserId`/SC-13; FX/enflasyon in-memory cache. Per-user summary server cache ertelendi — React Query istemcide tazeler) |
| T1.16 | **Web:** mevcut pozisyona **alış/satış işlemi ekle** UI → `POST /holdings/{id}/transactions` (backend T1.6'da hazırdı, UI eksikti) | T1.13 | `04` §4 | [x] (`AddTransactionForm` detay sayfasında; Alış/Satış + miktar/fiyat) |
| T1.17 | **BES özel modeli:** BES'e alış/satış engellenir (nominal hesap); `POST /holdings/{id}/bes-contribution` (kendi + devlet %30) → maliyet tabanı büyür; başlangıç tarihi (JoinedAtUtc) gösterimi; web "Aylık katkı ekle" formu | T1.6, T1.13 | `03` §A, `04` §4 | [x] |
| T1.18 | **Pozisyon işlem geçmişi:** `GET /holdings/{id}` yanıtına `transactions` listesi + web `TransactionHistory` tablosu (her pozisyonda) | T1.13 | `04` §4 | [x] |
| T1.19 | **Web görsel yükseltme** (taslak referanslı pano): ikonlu sidebar + KPI şeridi (glow'lu hero) + zengin donut + En İyi/Zayıf + Hızlı Bilgi + Yoğunlaşma + computed nudge + ikonlu/çubuklu pozisyon tablosu + detail-hero/BES split + Ayarlar sayfası | T1.11-14 | `13` §4, taslak | [x] |
| T1.20 | **Web UX/UI yükseltme** (işlevsel/etkileşim): mobil off-canvas drawer + üst bar; skeleton + retry + `EmptyState`(CTA); `Toast` geri bildirimi; stilize `ConfirmDialog` + danger-zone; `AddHoldingDialog` type-chips + odak tuzağı + satır-içi doğrulama; responsive tablo kartları; KPI info-tooltip; a11y (skip-link/focus-visible/reduced-motion) | T1.19 | `13` §4 | [x] |
| T1.21 | **Web UX/UI 2. tur** (canlı geri bildirim): dashboard donut+"Değer Seyri" grid (sağ boşluk) + içerik ortalama; detay formları **modale** (`Modal`) + 2 sütun; tüm taslak menüleri + nav grupları (`ComingSoonPage`: İşlemler/Senaryo/Hisse/Eğitim); **Performans** sayfası (dönem sekmeleri + gerçek getiri çubukları); **mobil menü CSS kaynak-sıra hatası fix**; sticky topbar `top:0` | T1.20 | `13` §4, taslak | [x] |

**Faz 1 DoD:** ✅ **KARŞILANDI** — **Web'de** varlık ekle/sil/listele; toplam/kâr/getiri/dağılım
testlerle doğru; çoklu pb baz pb'ye çevriliyor; BES devlet katkısı ayrı.
→ **Tek başına (web) ürün** (canlı PostgreSQL'e karşı görsel doğrulandı).

---

## FAZ 2 — Canlı Fiyat & Bilgilendirme

| ID | Görev | Bağımlılık | Doküman | Durum |
|----|-------|-----------|---------|-------|
| T2.1 | Fiyat sağlayıcı seç (altın/döviz ücretsiz katman) + `IPriceProvider` | Faz 1 | `02` | [x] (Frankfurter=ECB döviz, Truncgil=TR gram altın; ikisi de **anahtarsız**; `IPriceProvider`+`PriceInstrument`/`PriceQuote`; typed HttpClient+DI; 8 sağlayıcı testi) |
| T2.2 | `PriceFetchService` + cache (5-15 dk) + `PriceSnapshots`'a yaz | T2.1 | `02` §2.2 | [x] (`IPriceFetchService.RefreshAsync`: CanQuote yönlendirme + sağlayıcı izolasyonu; 10 dk in-memory cache; yazım → `PriceSnapshots`+`FxRates`+`Holding.CurrentPrice`; SC-18 3 senaryo) |
| T2.3 | Fallback: dış API çökünce son bilinen fiyat + `stale:true` | T2.2 | `04` §5, NFR-5 | [x] (sağlayıcı çökünce DB'den son-bilinen `IsStale` tırnak; `HasStale`/`FailedSources`; bayat geçmişe yazılmaz; çöken kaynak için kısa retry-TTL; SC-08) |
| T2.4 | `GET /api/prices` + summary'i canlı fiyatla besle | T2.2 | `04` §5 | [x] (`PricesController` → `RefreshAsync`; `PricesResponse`/`PriceDto` (kind/currency/price/asOf/source/**stale**); `Holding.CurrentPrice` yazımı → summary/holdings besleme; e2e test stub sağlayıcıyla, ağsız) |
| T2.5 | `NudgeRuleEngine` (kural tabanlı, örn. nakit oranı eşiği) + `GET /nudges` | Faz 1 | `04` §5 | [x] (saf `NudgeRuleEngine`: yoğunlaşma/tek-varlık/düşük-nakit eşikleri → eğitici not, **tavsiye değil**; `Nudge`/`NudgesResponse`; `INudgeService`→summary; `GET /api/portfolio/nudges` per-user; SC-09 6 unit+2 e2e) |
| T2.6 | **Web:** yenile + son güncelleme/"yaklaşık" etiketi + Nudge kartı | T2.4 | `13` §4 | [x] (shared `PriceDto`/`PricesResponse`/`Nudge`/`NudgesResponse`+`getPrices`/`getNudges`; web `usePrices`/`useNudges`; `LivePrices` çipleri + "Yenile" + stale etiketi; `NudgesCard` (disclaimer); fiyat tazelenince summary/holdings invalidate; PortfolioInsights inline nudge kaldırıldı; 4 yeni test) |
| T2.7 | **Redis cache katmanı:** fiyat/FX/summary cache Redis'e (dağıtık); stampede koruması; cache isabet metriği | T2.2 | `10` §3 | [x] (`IAppCache` `IDistributedCache` üstünde: Redis ya da in-memory (yerel dev Redis'siz çalışır); **single-flight** stampede koruması; **hit/miss `Meter`** (T2.8'e hazır); FX/enflasyon/fiyat decorator'ları taşındı; compose'a redis servisi; SC-19) |
| T2.8 | **Gözlemlenebilirlik yığını:** Compose'a Seq + Prometheus + Grafana; OTel metrikleri (RED + bağımlılık + cache); ilk dashboard'lar + alarmlar | T0.11 | `12` §2,§4,§6 | [ ] |
| T2.9 | **Reverse proxy + sınırlama:** Traefik/Caddy (TLS/Let's Encrypt) + rate limiting + güvenlik başlıkları; iç servisler dışarı kapalı | T0.13 | `11` §5, `10` §5 | [ ] |

**Faz 2 DoD:** Otomatik güncel değer + yenileme; ≥1 not doğru tetikleniyor; dış
API çökünce uygulama çökmüyor; **Redis cache + metrik/dashboard/alarm çalışıyor;
rate limit + TLS proxy ayakta**.

---

### T-BES — Bireysel Emeklilik (BES) Detaylı Analiz (çapraz epik; araştırma tabanlı)

> **Araştırma (2026-05):** Devlet katkısı oranı **%20** (2026-01-01'den; önceki %30 — Resmî Gazete
> 2026-01-07). Üst sınır = **yıllık brüt asgari ücretin %20'si** (2026 ≈ **79.272 ₺**; 2026 aylık brüt
> asgari ücret 33.030 ₺ → yıllık 396.360 ₺ × %20). Hak ediş sistemde kalış süresine kademeli (3/6/10 yıl);
> kesin yüzdeler kaynaklarda farklılık gösterdiğinden uygulamada **kaba durum** (hak edilmedi / kısmi / tam)
> kullanılır. Emeklilik = 10 yıl + 56 yaş. **Oran ve eşikler mevzuata tabidir → lansman öncesi EGM/SPK ile
> DOĞRULANMALI** (CLAUDE.md §2). Tek kaynak: `BesRules`. Kaynaklar: egm.org.tr, allianz.com.tr (SSS).

| ID | Görev | Durum |
|----|-------|-------|
| T-BES.1 | `BesRules` + `BesCalculator` (devlet katkısı %20; hak ediş = sistemde kalış yılından kaba durum) — saf + birim testli (SC-20) | [x] |
| T-BES.2 | Katkıda devlet katkısı **%20** (önceki sabit %30 düzeltildi); hak ediş `JoinedAtUtc`'den okuma anında türetilir | [x] |
| T-BES.3 | `PUT /holdings/{id}/bes` — **başlangıç tarihi güncelle** → hak ediş yeniden türer (SC-21); web detayda "Düzenle" + **devlet katkısı açıklaması** (%20 + üst sınır) + hak ediş kademe notu + disclaimer | [x] |
| T-BES.4 | Devlet katkısı **yıllık üst sınır** uygulaması (takvim-yılı bazlı katkı toplaması — model genişletmesi gerekir) | [ ] |
| T-BES.5 | **Fon dağılımı senaryosu (eğitici projeksiyon):** kullanıcı varsayımları (aylık katkı, süre, varsayılan yıllık getiri / fon karması) → biriken tutar + devlet katkısı + kâr/zarar **illüstrasyonu**. **Tahmin/tavsiye DEĞİL** — açık "varsayımsal senaryo" çerçevesi + kalıcı disclaimer (CLAUDE.md §2: senaryo/farkındalık serbest; gelecek tahmini/yönlendirme yasak). Hesap KODDA (deterministik bileşik getiri), girdiler kullanıcıdan. | [ ] |
| T-BES.6 | **Düzenli katkı:** tarih aralığından **aylık kayıt üretimi** (`POST /holdings/{id}/bes/contributions`; kapsanan aylar, idempotent, gelecek hariç, her ayın oranı) + **katkı işlem geçmişi** (`BesContribution` tablosu) + **"bu ay katkını gir" hatırlatması** (SC-22). Tek katkı da kayıt oluşturur. | [x] |
| T-BES.6b | Katkı planı **kalıcılığı + otomatik devam** (`BesDetails.MonthlyAmount/ContributionDay/PlanActive`; katkı eklerken "bundan sonraki katkılar için kullan" checkbox → plan; görüntülemede **lazy catch-up** ile eksik aylar üretilir). Gerçek arka plan job (uygulama kapalıyken) → ileride. | [x] |
| T-BES.7 | **Maliyet = kendi katkı (cepten)** (own-only; devlet katkısı getiriye dahil) + **katkı düzenle/sil** (`PUT`/`DELETE .../contributions/{cid}`, kümülatif+maliyet yeniden) + geçmiş UX (dikey scroll, yatay scroll yok, otomatik sütun sığdırma, "Kaynak" yerine düzenle/sil ikonları). SC-23. | [x] |

---

## FAZ 3 — LLM Yorum Katmanı

| ID | Görev | Bağımlılık | Doküman |
|----|-------|-----------|---------|
| T3.1 | LLM sağlayıcı seç + `ILlmClient` arayüz/impl | Faz 2 | `07` §2 |
| T3.2 | Sistem promptu + few-shot ("tavsiye değil" korkuluk) | T3.1 | `07` §3 |
| T3.3 | `LlmCommentaryService`: hazır sayı → JSON kart | T3.1, T1.7 | `07` §1,§4 |
| T3.4 | Güvenli parse + fallback + **testleri** | T3.3 | `07` §5, `06` §4 |
| T3.5 | Çıktı güvenlik filtresi (yasaklı yönlendirme kalıbı) | T3.3 | `07` §7 |
| T3.6 | Cache (portföy hash / günde bir) | T3.3 | `07` §6 |
| T3.7 | `GET /api/portfolio/commentary` | T3.3–T3.6 | `04` §6 |
| T3.8 | **Web:** Analiz sayfası kartları + **disclaimer** + loading | T3.7 | `13` §4 |
| T3.9 | **LLM maliyet/çağrı metriği** + bütçe alarmı (Grafana) | T2.8, T3.3 | `12` §4, `10` §7 |

**Faz 3 DoD:** Kartlar gerçek veriyle LLM'den; asla "al/sat/yükselir"; hata
çökertmiyor; cache çalışıyor.

---

## FAZ 4 — Hisse Temel Analiz

| ID | Görev | Bağımlılık | Doküman |
|----|-------|-----------|---------|
| T4.1 | Hisse veri kaynağı kararı (önce ABD; BIST maliyet değerlendirmesi) | Faz 3 | `CLAUDE.md` §3.3, `ROADMAP` Faz4 |
| T4.2 | `StockDataService` + `GET /api/stocks/{symbol}/metrics` | T4.1 | `04` §7 |
| T4.3 | `LlmStockExplainService` + `GET /.../explain` (tavsiye yok) | T4.2, T3.1 | `07` §8 |
| T4.4 | **Web:** sembol arama + `MetricGrid` + açıklama kartları + disclaimer | T4.2,T4.3 | `13` §4 |

**Faz 4 DoD:** Metrik çekiliyor + LLM çerçeve sunarak açıklıyor; veri yoksa
anlamlı hata.

---

## FAZ M — Mobil Kol (React Native / Expo)

> Web parası oturduktan sonra (genelde Faz 1-2 web tamamlanınca paralel
> başlatılabilir). Backend ve `@finans/shared` (tip/token/format/api hook)
> hazır olduğu için mobil **yalnızca sunum katmanını** yazar. Şartname: `05`.

| ID | Görev | Bağımlılık | Doküman |
|----|-------|-----------|---------|
| TM.1 | Expo uygulaması (`mobile/`) + React Navigation + `@finans/shared` bağlı | T0.2 + web Faz 1 | `06` §2, `05` |
| TM.2 | Tema token'larını RN'e uygula (`05` §2) + fontlar (`expo-font`) | TM.1 | `05` §2 |
| TM.3 | Portföy ekranı: `HeroCard` + `AllocationDonut` (react-native-svg) + `HoldingRow` | TM.1 | `05` §3 |
| TM.4 | Varlık detay + ekle (alttan kayan overlay) | TM.3 | `05` §7-8 |
| TM.5 | Analiz / Hisse / Eğitim ekranları (web ile parite) | TM.3 | `05` §4-6 |
| TM.6 | Mobil token saklama (`expo-secure-store`) + Jest/RTL + (Faz 2+) Maestro E2E | TM.1 | `11` §2, `09` §3 |

**Faz M DoD:** Mobil, web ile **işlevsel parite**; aynı API/paket; tasarım dili
(`DESIGN.md`) korunuyor; disclaimer'lar yerinde; testler yeşil.

---

## FAZ 5 — Ötesi (açık uçlu, hukuki onaya bağlı)

- Yeni varlık türleri (fon, gayrimenkul, kripto).
- Geçmişe dönük senaryo simülasyonu (`PriceSnapshots` ile, tahmin değil).
- **Eğitim modülü** (model `03` §C, API `04` §7.5, seed `03` §12.5):
  - T5E.1 — Eğitim entity'leri + migration (Tracks, Lessons, Sections, Prerequisites, ConceptTags, Quizzes, Progress, Attempts).
  - T5E.2 — Eğitim seed'i (5 ders "Temeller" track'i + 1 quiz + örnek ilerleme — taslakla birebir).
  - T5E.3 — Eğitim endpoint'leri (tracks/lessons/progress/quiz) — ilerleme `UserId` kapsamlı.
  - T5E.4 — **Web** Eğitim sayfası: track + ders listesi + ilerleme çubuğu + kilit; ders okuma + mini test; analiz/hisse kartından `ConceptTag` derin bağlantı.
- Bildirimler (eşik tabanlı, bilgi — tavsiye değil).
- Gerçek kimlik/hesap (JWT: access+refresh, Argon2id) + KVKK "verimi sil".
- **Güvenlik tamamlama:** IDOR (SC-13) + AuthZ + rate-limit testleri yeşil;
  audit log tam; güvenlik dashboard'u; secret rotasyonu; bağımlılık/imaj taraması
  (Trivy/gitleaks); `/security-review`. Bkz. `11`, `12`.
- At-rest şifreleme + şifreli yedek + retention politikası (`11` §7).
- **Lansman öncesi: SPK + KVKK hukuki doğrulama (ŞART).**
- Gelir modeli (abonelik).

---

## Çapraz-kesen (her faz)

- [ ] **Her görev = senaryo (09 §5) + test (birim + olaylara yönelik), yeşil olmadan kapanmaz** (`CLAUDE.md` §12).
- [ ] Yeni hesap = yeni birim testi (NFR-1, **zorunlu**).
- [ ] Yeni dış bağımlılık = fallback **+ olay testi** (NFR-5: "API çöktü" senaryosu).
- [ ] Analiz/hisse ekranı = disclaimer (NFR-2).
- [ ] Para = `decimal` + TR format (NFR-1, NFR-7).
- [ ] **Her veri erişimi `UserId` ile kapsanmış** (IDOR yok, NFR-12, `11`§3).
- [ ] **Sır repoda değil; log'da sır/PII yok** (`11`§6, `12`§3).
- [ ] **Dış çağrı cache'li + async; cache anahtarında `UserId`** (`10`§3-4).
- [ ] **Yeni endpoint = log + (Faz 2+) metrik + güvenlik kontrol listesi** (`11`§10, `12`).
- [ ] Faz sonu = "ne öğrendim" notu (`ROADMAP` Genel Notlar).
