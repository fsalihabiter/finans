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

**Sıradaki adım → FAZ 4: Hisse Temel Analiz.** Faz 0 ✅ · Faz 1 ✅ · Faz 2 ✅ ·
**Faz 3 ✅** (LLM yorum katmanı T3.1→T3.9 — sağlayıcı/anonimleştirme/prompt+şema/
güvenli parse/çıktı güvenlik filtresi/cache+fallback/maliyet metriği). **T4.1 ✅
karar: veri kaynağı Finnhub (ABD)** — bkz. Faz 4 tablosu altındaki karar notu.
**Sıradaki somut görev: T4.2** (`IStockDataProvider`/`StockDataService` +
`GET /api/stocks/{symbol}/metrics`).

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
| T-TX.1 | **İşlem geçmişinde düzenle/sil:** `PUT/DELETE /api/holdings/{id}/transactions/{txId}` + servis miktar/AvgCost'u işlemlerden yeniden türetir; son işlem silinemez (`cannot_delete_last`); BES'te düz tx yok; IDOR 404; Web ✎/🗑 + düzenle modalı + ConfirmDialog | T1.18 | `04` §4, `CLAUDE.md` §6 | [x] (2026-06-01) |
| T-BES.10 | **BES fon getirisi own+state'e ayrı yansır:** fon `r = fund/(own+state)−1` her iki katkıya da işler; her birinin ayrı güncel değer + kâr/zarar gösterimi (`BesCalculator.FundReturnFor` saf hesap; `BesDto.FundReturnRatio`/`OwnValue`/`OwnProfit`/`StateValue`/`StateProfit`; web BES split mini-satırları). Hero "yatırım performansım" değişmedi (own perspektifi korunur) | T-BES.9 | `03` §A, `CLAUDE.md` §6 | [x] (2026-06-01) |

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
| T2.8 | **Gözlemlenebilirlik yığını:** Compose'a Seq + Prometheus + Grafana; OTel metrikleri (RED + bağımlılık + cache); ilk dashboard'lar + alarmlar | T0.11 | `12` §2,§4,§6 | [x] (2026-06-04: OTel AspNetCore/HttpClient/Runtime + `Finans.Cache` Meter → `/metrics`; Serilog `Seq` sink opsiyonel (boşken sink eklenmez, dev'i bozmaz); compose'a seq+prometheus+grafana (admin port'ları `127.0.0.1` bind, LAN'a açık değil); Grafana provisioning + "Finans · Genel Bakış" dashboard (RED + cache hit + bağımlılık p95 + .NET GC heap); Prometheus `rules.yml` 3 alarm (5xx>2%, p95>600ms, instance down). Manuel doğrulama: `docker compose up --build` → http://localhost:3001 (Grafana admin/admin) · :9090 (Prometheus) · :8081 (Seq). Application 99/99 yeşil.) |
| T2.9 | **Reverse proxy + sınırlama:** Traefik/Caddy (TLS/Let's Encrypt) + rate limiting + güvenlik başlıkları; iç servisler dışarı kapalı | T0.13 | `11` §5, `10` §5 | [x] (2026-06-02: Caddy `tls internal` localhost; api/postgres/redis iç ağa kapandı; ASP.NET RateLimiter global Sliding 120/dk + "prices" 10/dk + "nudges" 30/dk; 429 ApiError zarfı + Retry-After; /health bypass; güvenlik başlıkları HSTS/XCTO/XFO/Referrer + `-Server`. +2 integration. Lansman'da: gerçek domain + Let's Encrypt) |

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
| T-BES.4 | Devlet katkısı **yıllık üst sınır** uygulaması (takvim-yılı bazlı katkı toplaması) | [x] (2026-06-01: `BesRules.AnnualCaps` tablosu 2024-2026 + fallback · `BesCalculator.ApplyAnnualStateCap` saf helper · 4 servis metodu (Add/Update/Generate/Catch-up) + projeksiyon takvim yılı kümülatife göre keser · web bilgi uyarısı. +8 unit + 3 integration. ⚠ tavan değerleri mevzuata tabidir — EGM/SPK doğrulaması ŞART) |
| T-BES.5 | **Fon dağılımı senaryosu (eğitici projeksiyon):** kullanıcı varsayımları (aylık katkı, süre, varsayılan yıllık getiri / fon karması) → biriken tutar + devlet katkısı + kâr/zarar **illüstrasyonu**. **Tahmin/tavsiye DEĞİL** — açık "varsayımsal senaryo" çerçevesi + kalıcı disclaimer (CLAUDE.md §2: senaryo/farkındalık serbest; gelecek tahmini/yönlendirme yasak). Hesap KODDA (deterministik bileşik getiri), girdiler kullanıcıdan. | [x] (2026-06-01: `BesProjectionCalculator` saf hesap + aylık iterasyon · `POST /bes/projection` · web modalı + chip önerili form + own/state kartları + yıllık seri tablosu + kalıcı disclaimer. Fon karması ileri faza; bu MVP tek `annualReturnRatio`. +10 unit + 4 integration) |
| T-BES.6 | **Düzenli katkı:** tarih aralığından **aylık kayıt üretimi** (`POST /holdings/{id}/bes/contributions`; kapsanan aylar, idempotent, gelecek hariç, her ayın oranı) + **katkı işlem geçmişi** (`BesContribution` tablosu) + **"bu ay katkını gir" hatırlatması** (SC-22). Tek katkı da kayıt oluşturur. | [x] |
| T-BES.6b | Katkı planı **kalıcılığı + otomatik devam** (`BesDetails.MonthlyAmount/ContributionDay/PlanActive`; katkı eklerken "bundan sonraki katkılar için kullan" checkbox → plan; görüntülemede **lazy catch-up** ile eksik aylar üretilir). Gerçek arka plan job (uygulama kapalıyken) → ileride. | [x] |
| T-BES.6b.ileri | **Arka plan job (`BesPlanCatchUpHostedService`)**: aktif tüm BES planlarını periyodik (varsayılan 6h) ilerletir; lazy catch-up'a ek olarak kullanıcı sayfayı haftalarca açmasa bile plan akar. Saf çekirdek `BesPlanCatchUpRunner` (DbContext + holding alır, `ICurrentUser`'a bağlı değil) — hem `HoldingService` (per-user GET) hem hosted service (sistem) çağırır. Konfig `Bes:PlanCatchUp:{Enabled,IntervalHours,InitialDelaySeconds}`; testlerde `Enabled=false`. **+2 integration** (runner: katch-up + idempotent + plan kapalıyken no-op). Application 99/99 · Integration 79/79. | [x] (2026-06-04) |
| T-BES.7 | **Maliyet = kendi katkı (cepten)** (own-only; devlet katkısı getiriye dahil) + **katkı düzenle/sil** (`PUT`/`DELETE .../contributions/{cid}`, kümülatif+maliyet yeniden) + geçmiş UX (dikey scroll, yatay scroll yok, otomatik sütun sığdırma, "Kaynak" yerine düzenle/sil ikonları). SC-23. | [x] |

---

## FAZ 3 — LLM Yorum Katmanı

| ID | Görev | Bağımlılık | Doküman |
|----|-------|-----------|---------|
| T3.1 | LLM sağlayıcı seç + `ILlmClient` arayüz/impl | Faz 2 | `07` §2 | [x] (2026-06-04: **Anthropic Claude** kararı; `ILlmClient` arayüz + `LlmRequest`/`LlmResult` sözleşmesi `Finans.Application.Llm`; `Infrastructure.Llm.AnthropicLlmClient` (typed HttpClient, REST, SDK yok — `tool_use`+input_schema ile JSON şema zorlamaya hazır) + `NoopLlmClient` (API key yokken dev/test safety). `LlmOptions:{Provider,ApiKey,Model,TimeoutSeconds,BaseUrl}`; DI: anahtar varsa Anthropic, yoksa Noop. Sözleşme yorumlarına **KVKK kuralı** (UserId/PII gönderilmez, anonim özet) işlendi. **+3 unit kontrat + 4 stub HTTP test** (header'lar, text parse, tool_use JSON parse, 5xx→Fail). Application 102 · Integration 83.) |
| T3.2 | Sistem promptu + few-shot ("tavsiye değil" korkuluk) | T3.1 | `07` §3 | [x] (2026-06-05: `Finans.Application.Llm.CommentaryPrompts` — statik `SystemPrompt` (eğitmen kimliği + 7 KESİN KURAL + doğru/yanlış few-shot örnekler: yoğunlaşma + reel getiri kartları; yasaklı: yönlendirme/tahmin/yeni rakam üretme) + `CommentaryJsonSchema` (kart şeması: 3-5 kart, body 60-220 char, opsiyonel meter+tags). Cache-friendly statik içerik (T3.6 hazır). **+5 unit regresyon kapısı**: KESİN KURALLAR kaybolmasın, şema parse edilebilir + zorunlu alanlar listede + maliyet/okunabilirlik sınırları korunur. Application 107.) |
| T3.3 | `LlmCommentaryService`: hazır sayı → JSON kart | T3.1, T1.7 | `07` §1,§4 | [x] (2026-06-05: `PortfolioAnonymizer` saf — `PortfolioSummaryDto` → `AnonymizedPortfolioSummary` (PII yok: kullanıcı varlık adları kaybolur; tür-bazlı dilim; oran 3 basamak; total tam sayı; `concentrationTop2` türetilir). `ILlmCommentaryService`+`LlmCommentaryService`: anonim özet → JSON user prompt → `ILlmClient.CompleteAsync(SystemPrompt, user, schema)` → cards parse. `CommentaryResponse`/`CommentaryCard`/`CommentaryMeter` DTO'ları. Hata/parse fail → düz metin fallback kartı (`Source="fallback"`). DI: `services.AddScoped<ILlmCommentaryService>`. **+11 unit** (5 anonymizer + 6 servis: mutlu yol, sistem promptu+şema dayatılır, isim sızmaz, Fail→fallback, geçersiz JSON→fallback, sıfır kart→fallback, eksik alanlı kart düşer). Application 118 · Integration 83.) |
| T3.4 | Güvenli parse + fallback + **testleri** | T3.3 | `07` §5, `06` §4 | [x] (2026-06-05: parse hardening — cards üst sınırı 5, body/title min-max (kısa→düşer, uzun→kırp), meter value [0,1] clamp + boş etiketli meter null, tags non-string filtre + ≤4 + ≤24 char, ek alanlar yutulur. `CommentaryParseConstraints` (07 §4 ile aynı sınırlar). +9 edge unit. T3.6 cache fallback (son başarılı) ileride.) |
| T3.5 | Çıktı güvenlik filtresi (yasaklı yönlendirme kalıbı) | T3.3 | `07` §7 | [x] (2026-06-18: `CommentaryOutputGuard` saf — kuşak-2 koruma. Kart metni (başlık+gövde+etiket) ASCII'ye katlanır (diyakritiksiz LLM çıktısına dayanıklı) → yönlendirme (-malı/-meli ekli al/sat/geç/gir/çık/ekle/tut/yatır, tavsiye fiilleri, "mantıklı olur" çerçevesi, "hemen/şimdi al", "fırsatı kaçırma") + tahmin (yükselecek/düşecek, kazandıracak…) + zaman imi×kesin yön kalıpları yakalanır. **Bağlam odaklı (07 §7):** "satın alma gücü", "enflasyon yükselirse", "değer kaybedebilir" TEMİZ kalır. `LlmCommentaryService` parse'ında takılı kart düşürülür (hepsi düşerse fallback); kaç kartın düştüğü loglanır. **+18 unit** (6 temiz + 9 yasak + 1 başlık/etiket tarama + 2 servis: kart düşürme + tümü-düşünce fallback). Application 127→145.) |
| T3.6 | Cache (portföy hash / günde bir) | T3.3 | `07` §6 | [x] (2026-06-18: `CachedLlmCommentaryService` dekoratörü — FX/enflasyon/fiyat decorator deseni (T2.7). Cache anahtarı `commentary:{UserId}:{anonim özet SHA-256 hash}` → portföy değişince otomatik tazeleme, değişmezse 24s TTL boyunca cache'ten (LLM çağrısı yok, NFR-9). **Son başarılı fallback (07 §5-a):** LLM erişilemez/şema bozuksa son başarılı yorum `Source="cache"` gösterilir (30g saklanır); o da yoksa düz fallback. Yalnız başarılı yorum cache'lenir. **Per-user izolasyon (§13):** anahtar UserId içerir. Single-flight (stampede koruması). DI: iç servis concrete + dış dekoratör. **+5 unit** (cache-hit / portföy değişince yeni çağrı / son-başarılı fallback / düz fallback / JSON round-trip) + commentary integration 2/2 (host DI doğrulandı). Application 145→150.) |
| T3.7 | `GET /api/portfolio/commentary` | T3.3–T3.6 | `04` §6 | [x] (2026-06-05: `PortfolioController.GetCommentary` — per-user özet → `ILlmCommentaryService` → 200+`CommentaryResponse`; `[EnableRateLimiting("commentary")]` dakikada 10 (LLM pahalı). Program.cs'te yeni "commentary" Fixed policy. +2 integration (NoopLlmClient ile 200+fallback; başka kullanıcı kapsam). Application 127 · Integration 85.) |
| T3.8 | **Web:** Analiz sayfası kartları + **disclaimer** + loading | T3.7 | `13` §4 | [x] (2026-06-05: `@finans/shared` tipler (`CommentaryResponse/Card/Meter`) + `getCommentary`; `useCommentary` hook (staleTime 1h, manuel refetch, otomatik refresh yok — NFR-9). `AnalysisPage` yeniden yazıldı: ComingSoon → gerçek sayfa; **Disclaimer her durumda görünür** (loading dahil — CLAUDE.md §2). `CommentaryCardList` komponenti (emoji+title+body+meter+tags); "↻ Yenile" + skeleton + error+retry + source rozeti. CSS: commentary-list/card/meter/skeleton. **Web 54/54 + build temiz.** API anahtarı yokken backend fallback kartı → UI'da "Yorum şu an üretilemedi — sayıların etkilenmedi" mesajı.) |
| T3.9 | **LLM maliyet/çağrı metriği** + bütçe alarmı (Grafana) | T2.8, T3.3 | `12` §4, `10` §7 | [x] (2026-06-18: `ILlmMetrics` portu (Application) + `LlmMetrics` (Infrastructure, Meter `Finans.Llm`) + `NoopLlmMetrics`. Sayaçlar: `finans_llm_calls_total{result}`, `finans_llm_tokens_total{direction}` (maliyet), `finans_llm_guard_blocked_total` (T3.5), `finans_llm_served_total{source=llm/cache/cache_last/fallback}` (cache etkinliği). İç servis çağrı+token+guard sayar; dekoratör sunulan kaynağı sayar. Program.cs OTel'e `AddMeter` eklendi → `/metrics`. Prometheus `rules.yml`: 3 alarm (LlmCallBudgetBurn >60 çağrı/1s, LlmTokenBudgetBurn >200k token/1s, LlmFallbackRateHigh >%50 fallback/15dk — eşikler dev katmanı, ölçekle güncellenir). **+6 unit.** Application 150→156. **Faz 3 DoD tamamlandı.**) |

**Faz 3 DoD:** Kartlar gerçek veriyle LLM'den; asla "al/sat/yükselir"; hata
çökertmiyor; cache çalışıyor.

---

## FAZ 4 — Hisse Temel Analiz

| ID | Görev | Bağımlılık | Doküman |
|----|-------|-----------|---------|
| T4.1 | Hisse veri kaynağı kararı → **Finnhub** (ABD; BIST ertelendi) | Faz 3 | `CLAUDE.md` §3.3, `04` §7 | [x] (2026-06-20) |
| T4.2 | `StockDataService` + `GET /api/stocks/{symbol}/metrics` | T4.1 | `04` §7 |
| T4.3 | `LlmStockExplainService` + `GET /.../explain` (tavsiye yok) | T4.2, T3.1 | `07` §8 |
| T4.4 | **Web:** sembol arama + `MetricGrid` + açıklama kartları + disclaimer | T4.2,T4.3 | `13` §4 |

**Faz 4 DoD:** Metrik çekiliyor + LLM çerçeve sunarak açıklıyor; veri yoksa
anlamlı hata.

> ✅ **T4.1 kararı (2026-06-20) — Veri kaynağı: Finnhub (ABD).** Ücretsiz katman
> **60 çağrı/dk**, anahtar gerekir (env/User Secrets — koda gömülmez, §13). Tek
> `GET /stock/metric?symbol={S}&metric=all` çağrısı 4 metriğimizi de verir:
> F/K = `metric.peTTM` (veya `peNormalizedAnnual`), PD/DD = `metric.pb`, temettü
> verimi = `metric.dividendYieldIndicatedAnnual` (veya `currentDividendYieldTTM`),
> kâr büyümesi = `metric.epsGrowthTTMYoy` (veya `epsGrowth5Y`). Fiyat/ad/borsa
> için `GET /quote` + `GET /stock/profile2`. **Kesin alan adları T4.2'de canlı
> yanıtla doğrulanacak** (boş/null gelen alan → "veri yok", §07 §5 fallback).
> Sektör bağlamı (`above/high/low/positive`) MVP'de **kaba eşiklerle KODDA**
> türetilir (ücretsiz katmanda sektör ortalaması yok); ileride zenginleştirilir.
> Desen Faz 2 `IPriceProvider` ile aynı: `IStockDataProvider` + typed HttpClient +
> DI + stub HTTP testler + cache (07 §6: sembol+snapshot bazlı, NFR-9). **BIST
> ertelendi** (güvenilir veri ücretli/zor — CLAUDE.md §3.3); Faz 4+ değerlendirme.

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
