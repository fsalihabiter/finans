# ACTIVE — Aktif Görevler (anlık durum)

> Şu an üzerinde çalışılan / sıradaki görevlerin küçük anlık görüntüsü. Oturum
> başında hook bunu otomatik gösterir. Kaynak plan: [`../docs/08-BACKLOG.md`](../docs/08-BACKLOG.md).
> Bir görev bitince buradan çıkar, backlog'da `[x]` işaretle, TASKLOG'a girdi ekle.

**Aktif faz:** ✅ Faz 0 · ✅ Faz 1 · ✅ Faz 2 · ✅ **Faz 3 BİTTİ (LLM yorum katmanı — T3.1→T3.9)** → **Faz 4 — Hisse Temel Analiz**

## Sıradaki (öncelik sırası)
1. **T4.2** — `IStockDataProvider`/`StockDataService` + `GET /api/stocks/{symbol}/metrics` (Finnhub)
2. T4.3 — `LlmStockExplainService` + `GET /.../explain` (tavsiye YOK)
3. T4.4 — Web: sembol arama + `MetricGrid` + açıklama kartları + disclaimer

> ✅ **T4.1 bitti (2026-06-20) — Veri kaynağı kararı: Finnhub (ABD).** Ücretsiz katman 60 çağrı/dk;
> `GET /stock/metric?metric=all` 4 metriğimizi verir (F/K=peTTM, PD/DD=pb, temettü=dividendYield…, kâr
> büyümesi=epsGrowth…); fiyat/ad için `/quote`+`/stock/profile2`. Anahtar env/User Secrets'te (§13).
> Sektör bağlamı MVP'de kaba eşiklerle KODDA. Desen = Faz 2 `IPriceProvider` (typed HttpClient+DI+stub
> test+cache). BIST ertelendi. Kesin alan adları T4.2'de canlı doğrulanacak. Detay: 08-BACKLOG Faz 4 notu.

> ✅ **2026-06-18 — Faz 3 kapanışı + analiz turu temizliği:** T3.5 (çıktı güvenlik filtresi —
> `CommentaryOutputGuard`, bağlam odaklı yasaklı kalıp taraması), T3.6 (cache + son başarılı fallback,
> `CachedLlmCommentaryService`), T3.9 (LLM çağrı/token/served metriği + 3 Prometheus bütçe alarmı)
> tamamlandı. Ayrıca: 2026-06-08 OpenRouter yaması commit edildi (gizli test dispose hatası düzeltildi,
> `tmp_diag/` silindi), derleme uyarıları temizlendi (KnownIPNetworks + 4× CS8604), pnpm-workspace
> `mobile` referansı kapatıldı. **Backend: Application 156/156 · Integration 90/90 · Web build temiz.**
> Detay: TASKLOG 2026-06-18 (5 girdi).

> ✅ **2026-06-08 — OpenRouter free reasoning yaması + dev HTTPS redirect kapatma:**
> Belirti: "Genel Bakış yüklenemedi" + Analiz sayfasında hep fallback. Kök neden 1: dev'de
> `Security:UseHttpsRedirection=true` → Vite proxy 307 zincirinde kesiliyordu (Development'ta false
> yapıldı, prod compose Caddy TLS sonlandırıyor — etkilenmedi). Kök neden 2: free Llama 70B sürekli
> upstream 429 (Venice), az kalabalık modeller (Laguna/Nemotron) ise gizli reasoning tokens harcayıp
> content'i yarım bırakıyor. Yama: OpenRouter request'e `reasoning.exclude=true, enabled=false` +
> `MaxOutputTokens` 1024→2048 + model `poolside/laguna-m.1:free` + parse fail durumunda ham yanıt
> önizleme logu. **+1 regresyon testi. Application 127/127.** Detay: TASKLOG 2026-06-08.

> ✅ **T3.1 ek (2026-06-05) — OpenRouter sağlayıcı eklendi:** Geliştirme aşamasında ücretsiz LLM
> seçeneği. `OpenRouterLlmClient` (OpenAI-uyumlu `/v1/chat/completions`); JSON şema verilince
> sistem promptuna şemayı ekler + `response_format=json_object`. `Llm:Provider="OpenRouter"` +
> `Llm:Model` (ör. `meta-llama/llama-3.3-70b-instruct:free`) + `Llm:BaseUrl="https://openrouter.ai/api/"`.
> **+4 stub HTTP test.** Application 127/127.

> ✅ **T3.4 + T3.7 + T3.8 bitti (2026-06-05) — LLM yorum hattı uçtan uca görünür:**
> - T3.4 parse hardening: cards ≤5, body/title min-max (kısa→düş, uzun→kırp), meter [0,1] clamp,
>   tags filtrele/≤4, boş etiketli meter null. +9 edge test.
> - T3.7 endpoint: `GET /api/portfolio/commentary` + `"commentary"` rate limit (10/dk). +2 integration.
> - T3.8 Web: `@finans/shared` tipler + `useCommentary` + `AnalysisPage` (ComingSoon → gerçek sayfa),
>   `CommentaryCardList` komponent, **Disclaimer her durumda** (loading dahil — CLAUDE.md §2),
>   "↻ Yenile" / skeleton / hata-retry / source rozeti.
> - **Application 127/127 · Integration 85/85 · Web 54/54 + build temiz.**
> - API anahtarı yokken backend `NoopLlmClient` → fallback kartı → UI'da "Yorum şu an üretilemedi —
>   sayıların etkilenmedi" bilgilendirmesi (uygulama çökmez — NFR-5).
> - Görmek için: `pnpm dev` → http://localhost:5173/analiz (rota App.tsx menüsünden gelir).

> ✅ **T3.3 bitti (2026-06-05) — LlmCommentaryService + anonimleştirme:**
> `PortfolioAnonymizer` saf — PII'siz özet: kullanıcı varlık adı sızmaz; tür-bazlı dilim grupla; oran
> 3 basamak; total tam sayı; `concentrationTop2` türetilir. `ILlmCommentaryService` orkestrasyon:
> anonim özet → deterministik user prompt JSON → `ILlmClient` (`SystemPrompt`+`JsonSchema` dayatılır)
> → cards parse → `CommentaryResponse{Cards,Source,GeneratedAtUtc}`. Fallback (07 §5 ilk hat): LLM
> Fail / geçersiz JSON / 0 kart → tek "Yorum şu an üretilemedi" kartı (`Source="fallback"`). Per-kart
> zorunlu alan kontrolü (kısmi başarı: kötü kart düşer iyiler kalır). DI: scoped. **+11 unit**
> (5 anonymizer + 6 servis). Application 118/118 · Integration 83/83.

> ✅ **T3.2 bitti (2026-06-05) — Sistem promptu + few-shot + JSON şema:**
> `Finans.Application.Llm.CommentaryPrompts` (statik, cache-friendly). `SystemPrompt`: eğitmen kimliği
> + 7 KESİN KURAL (yeni rakam yok / yönlendirme yok / tahmin yok / Türkçe sade / structured_output
> only / 3-5 kart tema tekrarsız / body 60-220 char) + 2 doğru + 4 yasak few-shot örnek.
> `CommentaryJsonSchema` (07 §4 kart şeması): cards array 3-5, kart `emoji`+`title`+`body` zorunlu,
> `meter`+`tags` opsiyonel — Anthropic `tool_use.input_schema` ile dayatılacak (T3.3). **+5 unit
> regresyon kapısı.** Application 107/107.

> ✅ **T3.1 bitti (2026-06-04) — LLM sağlayıcı + soyutlama:** Karar **Anthropic Claude**
> (`claude-sonnet-4-6` varsayılan; env ile haiku'ya geçilir). `Finans.Application.Llm.ILlmClient`
> provider-neutral arayüz; `Infrastructure.Llm.AnthropicLlmClient` typed HttpClient (REST, SDK YOK)
> — `tool_use` + `input_schema` ile JSON şema zorlamaya hazır. `NoopLlmClient` API key yokken
> dev/test güvenli varsayılan (çağrı `Fail("llm_not_configured")` → üst katman fallback → çökme yok).
> **KVKK kuralı arayüz yorumunda**: UserId/PII gönderilmez; yalnız anonim özet. Hata akışı:
> HTTP/network/timeout/parse → exception fırlatmaz, `Fail(reason)` döner.
> **+3 unit kontrat + 4 stub HTTP test. Application 102/102 · Integration 83/83.**

> ✅ **T-BES.6b ileri bitti (2026-06-04) — Arka plan job:** `BesPlanCatchUpHostedService` (BackgroundService)
> aktif tüm BES planlarını periyodik (varsayılan 6h, başlangıç +60s) ilerletir; saf çekirdek
> `BesPlanCatchUpRunner` (DbContext + holding alır, `ICurrentUser`'a bağlı değil) — hem `HoldingService`
> (per-user GET) hem hosted service (sistem) ortak çağırır. Konfig `Bes:PlanCatchUp:{Enabled,IntervalHours,
> InitialDelaySeconds}`; testlerde `Enabled=false`. Per-holding try/catch + log → bir hata diğer holding'leri
> düşürmez. **+2 integration** (kuruluyor + idempotent + no-op). **Application 99/99 · Integration 79/79.**
> Gelecek: dağıtık/daha sağlam cron için Hangfire/Quartz (12 §9).

> ✅ **T2.8 bitti (2026-06-04) — Gözlemlenebilirlik yığını:** OTel
> (`AspNetCore`+`HttpClient`+`Runtime`+`Finans.Cache` Meter) → `/metrics`; Serilog `Seq` sink opsiyonel
> (boşken sink eklenmez → dev'i bozmaz). Compose'a `seq` (8081), `prometheus` (9090 + `rules.yml` 3 alarm),
> `grafana` (3001, provisioned datasource + "Finans · Genel Bakış" dashboard: RED + cache hit oranı +
> bağımlılık p95 + GC heap). **Tüm admin port'ları `127.0.0.1` bind** (LAN'a açık değil — 11 §5).
> Application 99/99 yeşil. Manuel: `docker compose up --build` → :3001 Grafana (admin/admin), :9090
> Prometheus (Targets → finans-api UP), :8081 Seq.

> ✅ **T2.9 bitti (2026-06-02) — Caddy reverse proxy + TLS + RateLimiter:** `compose/caddy/Caddyfile`
> (localhost `tls internal`, güvenlik başlıkları, /api+/health proxy); compose'da api/postgres/redis
> iç ağa kapandı (sadece Caddy 80/443 dışarı). ASP.NET `AddRateLimiter`: global Sliding 120/dk,
> "prices" Fixed 10/dk, "nudges" Fixed 30/dk; partition kullanıcı/IP; 429 ApiError zarfı + Retry-After;
> /health bypass. **+2 integration test** (kilit bırakılınca koşar). Application 99/99 yeşil.
> ⚠ Web compose'a girmedi (pnpm dev ayrı). Manuel doğrulama: `docker compose up --build`.

> ✅ **`AddBesBirthYear` migration uygulandı (2026-06-02)** — `BesDetails.BirthYear integer NULL` canlı
> Postgres'te (`dotnet ef database update`, additive). `__EFMigrationsHistory` 4/4 migration'la
> güncel. BES "Ayarları düzenle" formunda doğum yılı kalıcı kaydedilebilir; T-BES.5 projeksiyonunda
> "Emeklilik" preset chip'i (10y + 56 yaş) artık tam çalışır.

> ✅ **T-BES.4 bitti (2026-06-01) — Devlet katkısı yıllık üst sınırı:** `BesRules.AnnualCaps` tablosu
> (2024 51.006 · 2025 66.312 · 2026 79.272 ₺) + `BesCalculator.ApplyAnnualStateCap` saf helper. Servisin
> 4 BES katkı metoduna ve `BesProjectionCalculator`'a takvim yılı bazlı kesme eklendi (kümülatif state
> takip, yıl değişiminde sıfırlanır). Web: tahmini katkı tavanı aşıyorsa altın uyarı.
> **+8 unit yeşil · Application 99/99 · integration VS kilidi bırakılınca · web 52/52 + build temiz.**
> ⚠ Tavan değerleri **mevzuata tabidir** — lansman öncesi EGM/SPK ile doğrulanmalı (özellikle 2025).
> **T-BES epiği kapandı.**

> ✅ **T-BES.5 bitti (2026-06-01) — BES eğitici projeksiyon:** `BesProjectionCalculator` saf hesap (aylık
> iterasyon, devlet katkısı oranı ödeme tarihine göre, fon compound). `POST /api/holdings/{id}/bes/projection`.
> Web: BES detayda "📊 Eğitici senaryo" modalı — aylık katkı (plan ön-doldur) + süre + yıllık getiri (chip
> önerili) → fon değeri (hero), own/state ayrı kart, yıllık seri tablo. **Kalıcı disclaimer**: yatırım
> tavsiyesi DEĞİL; vergi/enflasyon dahil değil; gerçek getiri farklı (CLAUDE.md §2). **+10 unit yeşil,
> integration VS kilidi bırakılınca · web 52/52 + build temiz.**

> ✅ **T-BES.10 bitti (2026-06-01) — BES fon getirisi own+state'e ayrı yansır:** `BesCalculator.FundReturnFor`
> saf hesap (taban=own+state; r=fund/taban−1; null/sıfır taban → null oran). BesDto'ya `FundReturnRatio`,
> `OwnValue`/`OwnProfit`/`StateValue`/`StateProfit`. Web: BES split'inde her iki katkı için mini-satır
> (güncel değer + kâr/zarar + oran). Hero değişmedi (önceki "maliyet=own" kararı korunur). **+4 unit yeşil,
> integration VS kilidi bırakılınca · web 52/52 + build temiz.**

> ✅ **T-TX.1 bitti (2026-06-01) — İşlem geçmişinde düzenle/sil:** `PUT/DELETE /api/holdings/{id}/transactions/{txId}`
> + servis tarafında miktar/AvgCost **işlemlerden yeniden türetilir**; son işlem silinemez (`cannot_delete_last`
> → "Pozisyonu sil"e yönlendir); BES'te düz tx reddedilir; **IDOR 404**. Web: TransactionHistory ✎/🗑 ikonlar,
> düzenleme modalı + ConfirmDialog. **+5 integration / 52 web yeşil.**

> ✅ **T-BES.6b/7 bitti** — maliyet=kendi katkı (own-only); katkı düzenle/sil; düzenli plan checkbox +
> lazy otomatik devam; geçmiş UX (dikey scroll/sığdırma/ikon butonlar). Migration'lar (`BesContributions`,
> `BesContributionPlan`) canlı Postgres'e uygulandı (additive). Backend'i **VS'den çalıştır** (ben arka planda bırakmıyorum).

> ✅ **T-BES.9 bitti (2026-05-31) — BES yeniden tasarımı kapandı:** katkı durumu tarihten türer
> (Deposited/StatePending/Future; devlet katkısı ödeme ayını izleyen ayın sonunda yatar, ~+1 ay; bekleyenler
> toplama girmez); geçmiş listesinde **yeşil/sarı/gri** renk + lejant. Kademeli hak ediş %0/15/35/60/100
> (10y+56yaş için `BirthYear`) + **hak kazanılan tutar**. **`POST /api/holdings/bes`** açılış bakiyesiyle BES
> kurma (Opening katkı kaydı) + BES'e özel ekleme formu. `PUT /bes` ödeme günü/doğum yılı düzenleme.
> Geçmiş paneli **sol içerik yüksekliğine uyar** (sağ kart absolute → satır=sol; iç dikey scroll).
> Tüm toplamlar katkı satırlarından okuma anında türetilir. `AddBesBirthYear` migration üretildi (canlı
> Postgres'e uygulama bekliyor). **Backend: 72 unit + 62 integration · Web: 52 · eslint/build temiz.**

> ✅ **UX turu (2026-05-31, geri bildirim):** (1) "İşlem ekle" modalı scroll/kırpma yerine alana sığan ızgara
> (`.tx-row` auto-fit + buton tam genişlik, modal 540px). (2) Tarih girişi **native `<input type=date>`** —
> takvim/autocomplete + ↑↓ artır-azalt + ←→ segment + **Tab** (özel maskeli `DateField` ve `dateMask.ts` kaldırıldı).
> (3) **İleri tarihli katkı/plan serbest** (backend `must_not_be_future` ×3 kaldırıldı; `max` yalnız BES joined-date'te).
> (4) **Ort. maliyet anti-stale:** okuma yolu (`BuildHoldingDtosAsync` → yeni `ApplyReadPosition`) pozisyonu artık
> her GET'te KAYNAKTAN türetir (cache `Holding.AvgCost`'a güvenmez) — sürüklenmiş BES own+state değeri (350.573)
> gösterimde otomatik own-only'ye (277.060) düzelir; salt okunur. (5) Geçmiş listesi sol sütun yüksekliğine uyar
> (`.detail-grid` stretch + `.detail-col>.card` flex + `.history-scroll` flex:1). web 49 yeşil + build temiz;
> backend App.Tests 59 + Infra derlendi (0 hata). ⚠ **Integration testleri VS Api kilidi bırakılınca** koşulacak.

> ✅ **T-BES.1-3 bitti** — araştırma + `BesRules`/`BesCalculator` (devlet katkısı **%20** 2026, hak ediş
> `JoinedAtUtc`'den türetilir) + `PUT /holdings/{id}/bes` başlangıç tarihi + web devlet-katkısı açıklaması.
> Oran/eşik **lansman öncesi EGM/SPK doğrulaması ŞART** (08 T-BES, 03 §A).

> ✅ **T2.1→T2.6 bitti** — fiyat zinciri uçtan uca: sağlayıcılar (Frankfurter+Truncgil, anahtarsız) →
> `PriceFetchService` (yönlendirme + 10 dk cache + snapshot/fxrate/CurrentPrice yazımı) → fallback (`stale`) →
> `GET /api/prices` + `NudgeRuleEngine`/`GET /nudges` → **Web** (canlı fiyat çipleri + "Yenile" + stale
> etiketi + Nudge kartı). **CANLI DOĞRULANDI** (5298 PostgreSQL + 5174, gerçek dış API): gram altın
> 6.687,67 ₺, USD 45,886, EUR 53,43; 3 nudge; holdings/toplam canlı. Bkz. TASKLOG 2026-05-31.

## Faz 1 tamamlananlar (özet)
- **Saf hesap (T1.1-1.5):** `PortfolioCalculationService` (§6), `CurrencyConverter` (ters/çapraz,
  tam hassasiyet), reel getiri + enflasyon bağlama, `DerivePosition` (ort. maliyet türetimi)
- **API (T1.6-1.9):** Holdings CRUD + `portfolio/summary` + `settings`; `ICurrentUser` per-user
  izolasyon (IDOR→404), DTO+validasyon (ApiError zarfı), fx/enflasyon entegre
- **Web (T1.10-1.14):** `@finans/shared` tipler/istemci + React Query hook'ları; AppShell (sidebar) +
  HeroCard + AllocationDonut + Holdings tablo + varlık detay (BES/fiyat/sil) + "Varlık Ekle" modal
- **T1.15:** FX/enflasyon in-memory cache · **canlı PostgreSQL'e karşı görsel doğrulandı**
- **T1.19:** taslak referanslı zengin pano (tasarım dili). **T1.20:** UX/UI yükseltme — mobil drawer + üst
  bar, skeleton/retry/empty-state, toast geri bildirimi, stilize confirm + danger-zone, type-chips +
  odak tuzağı, responsive tablo kartları, KPI info-tooltip, a11y (skip-link/focus-visible/reduced-motion).
- **T1.21:** UX/UI 2. tur (geri bildirim) — donut+Değer Seyri grid (sağ boşluk), detay formları modale
  (yoğunluk), tüm taslak menüleri (İşlemler/Performans/Senaryo/Hisse/Eğitim) + nav grupları,
  Performans sayfası (dönem sekmeleri + gerçek getiri çubukları), **mobil menü CSS-sıra hatası fix**,
  sticky topbar top:0. Canlı doğrulandı (5173+5298+PostgreSQL).
- **Yeşil kapı:** backend **119** (Application 59 + Integration 60) · web **49** (DateField native testleri) · shared **15** · eslint 0 hata · tsc/vite temiz
  · ⚠ ileri-tarih için +2 integration testi (Generate_allows_future_range, Add_and_edit_allow_future_paid_date) — VS Api kilidi bırakılınca koşulacak
- **Tarih biçimi (NFR-7):** gösterimler `formatDate` → **gg.aa.yyyy** (noktalı, UTC). Girişler **özel `DateField`**
  (maskeli gg.aa.yyyy, her OS/locale'de aynı, ISO emit) — native `<input type=date>` yerine. **İşlem ekle** artık
  **işlem tarihi** alır (geçmiş tarihli işlem; performans/maliyet için, backend `Date ?? now`).

## Faz 2 tamamlananlar (özet)
- **T2.1:** Fiyat sağlayıcı seçimi + `IPriceProvider`. Döviz=Frankfurter (ECB, anahtarsız, doğrudan
  TRY kuru), Altın=Truncgil (TR gram, yerel primli). `PriceInstrument`/`PriceQuote` sözleşmesi;
  typed HttpClient + DI (`IEnumerable<IPriceProvider>`); 8 sağlayıcı testi (SC-17, HTTP stub).
- **T2.2:** `PriceFetchService` (`IPriceFetchService.RefreshAsync`) — CanQuote yönlendirme + sağlayıcı
  izolasyonu (`FailedSources`); 10 dk in-memory cache (`prices:refresh`); yazım → `PriceSnapshots`
  (geçmiş) + `FxRates` (converter) + `Holding.CurrentPrice` (okuma yolu, global). SC-18 (3 senaryo).
- **T2.3:** Fallback — sağlayıcı çökünce DB'den son-bilinen `PriceQuote.IsStale` tırnak (`HasStale`);
  bayat geçmişe yazılmaz; çöken kaynakta kısa retry-TTL (1 dk). Çökme yok (NFR-5). SC-08.
- **T2.4:** `GET /api/prices` (`PricesController` → `RefreshAsync`) — `PricesResponse`/`PriceDto`
  (kind/currency/price/asOf/source/**stale** + tur meta'sı). `Holding.CurrentPrice` yazımı → summary/holdings
  saf okuma canlı yansır (summary network-refresh'e bağlanmadı: deterministik + ağsız test). 2 e2e (stub).
- **T2.5:** `NudgeRuleEngine` (saf) — yoğunlaşma/tek-varlık/düşük-nakit eşikleri → eğitici not
  (**tavsiye değil**, CLAUDE.md §2). `Nudge`/`NudgesResponse`; `INudgeService`→summary; `GET /api/portfolio/nudges`
  per-user. SC-09 (6 unit + 2 e2e).
- **T2.6:** Web — shared tipler/istemci (`getPrices`/`getNudges`) + `usePrices`/`useNudges`; `LivePrices`
  çipleri + "↻ Yenile" + stale "yaklaşık" etiketi; `NudgesCard` (disclaimer); fiyat tazelenince
  summary/holdings invalidate (canlı yansır); PortfolioInsights inline nudge kaldırıldı. web 33→37. SC-W4.
- **T2.7:** Dağıtık cache — `IAppCache` (`IDistributedCache` üstünde JSON) Redis-opsiyonel (yoksa in-memory,
  yerel dev Redis'siz); **single-flight** stampede koruması; hit/miss `Meter` (T2.8'e hazır). FX/enflasyon/
  fiyat decorator'ları taşındı; compose'a redis. SC-19. backend 95→98 (App 45 + Integration 53).

## Devam eden / Bloke
- (yok)

---
*Güncelleme kuralı: CLAUDE.md §11. Bu dosya kısa kalmalı — detay TASKLOG.md'de,
tam plan 08-BACKLOG.md'de.*
