# CAPABILITIES — reuse registry (canonical)

> GD yeniden kullanım analizi ÖNCE bu dosyayı tarar. İlk sürüm `adm-analyze`
> taramasından geldi (2026-09-20). Konumlar gerçek dosya yollarıdır; `status`
> değerleri **çıkarımdır**. Dosya genelinde taban `last_verified_ref`: ac61b7e.
>
> ⚠ `last_verified_ref` **girdi başınadır** ve yalnız o yetenek gerçekten yeniden
> doğrulandığında güncellenir (REVIEW-006/RV-082). Hepsini birden ileri almak
> doğrulanmamış girdiler için **yanlış bir tazelik iddiası** olur; bu yüzden
> ac61b7e'de duran satırlar bilinçli olarak öyle bırakılır.

```yaml
capabilities:
  - capability: Portfolio calculation (deterministic money math)
    location: backend/src/Finans.Application/Portfolio/
    entry_points: [PortfolioCalculationService, CurrencyConverter, PortfolioValueHistoryService, ScenarioCalculationService]
    status: healthy
    scope: all
    notes: CLAUDE.md §6 formülleri; decimal; saf/yan etkisiz; birim testli (Application 291/291).
    tests: backend/tests/Finans.Application.Tests/Portfolio/
    last_verified_ref: ac61b7e

  - capability: BES (private pension) domain
    location: backend/src/Finans.Application/Portfolio/Bes*.cs + Finans.Domain/Portfolio/Bes*.cs
    entry_points: [BesCalculator, BesContributionPlanner, BesProjectionCalculator, BesRules, BesPlanCatchUpHostedService]
    status: healthy
    scope: all
    notes: >
      Devlet katkısı ayrı satır, hak ediş, katkı planı + arka plan telafi çalıştırıcısı.
      TR pazarına özgü derinlik (G-4). GD-002 (D-019) ile **iki fon havuzu**: kendi
      katkının fon değeri ve devlet katkısının fon değeri ayrı girilir; portföy değeri
      = kendi fon + hak ediş × devlet fon (hak edilmemiş kısım toplama girmez).
      Tek tanım noktaları — `DepositedTotals` / `ContributionTotals` (katkı
      sınıflandırması), `FundReturnFor` (iki havuz getirisi),
      `VestedPortfolioValueFor` (değer), `SplitTotalFundValue` (eski tek değerin
      bölünmesi). Bu kural beş yüzeyde aynı: liste · özet · detay · değer serisi · senaryo.
    last_verified_ref: 1bbf7d7

  - capability: Sabit birim fiyatlı varlıklar
    location: backend/src/Finans.Application/Portfolio/AssetPricing.cs
    entry_points: [AssetPricing.FixedUnitPriceFor]
    status: healthy
    scope: all
    notes: >
      Tanım gereği birim fiyatı sabit olan varlık türleri (şimdilik yalnız Cash = 1).
      Okuma yolunda ve oluşturmada uygulanır; INC-003'ün kök nedeni buydu — nakit
      pozisyonu fiyatsız kalıyor, toplam görünen satırların toplamına eşit olmuyordu.
      Yeni sabit fiyatlı tür eklenecekse tek yer burasıdır.
    tests: backend/tests/Finans.Integration.Tests/PortfolioHistoryApiTests.cs
    last_verified_ref: 1bbf7d7

  - capability: Holdings & transactions CRUD
    location: backend/src/Finans.Infrastructure/Services/HoldingService.cs (+ HoldingMapping, PortfolioService)
    status: healthy
    scope: all
    notes: Ortalama maliyet Transactions'tan türetilir; UserId kapsamlı.
    last_verified_ref: ac61b7e

  - capability: Pricing providers (FX + gold)
    location: backend/src/Finans.Infrastructure/Pricing/
    entry_points: [IPriceProvider, FrankfurterPriceProvider, TruncgilGoldPriceProvider, PriceFetchService]
    status: healthy
    scope: all
    notes: Anahtarsız kaynaklar; cache + son bilinen fiyata fallback. Yeni sağlayıcı = bu arayüze bağlanır (D-008).
    last_verified_ref: ac61b7e

  - capability: LLM commentary (guarded)
    location: backend/src/Finans.Application/Llm/ + Finans.Infrastructure/Llm/
    entry_points: [ILlmClient, AnthropicLlmClient, OpenRouterLlmClient, NoopLlmClient, LlmCommentaryService, CachedLlmCommentaryService, CommentaryOutputGuard, CommentaryLanguageGuard, AnonymizedPortfolioSummary]
    status: degraded
    scope: all
    notes: Ücretsiz katmanlarda 429/kesinti riski (14 §2, TASKLOG 2026-07-10). Sağlayıcı yapılandırmayla seçilir; yapılandırılmamışsa Noop. YENİ LLM ÖZELLİĞİ BU HATTI YENİDEN KULLANMALI (D-009).
    last_verified_ref: ac61b7e

  - capability: Stock fundamentals & history
    location: backend/src/Finans.Application/Stocks/ + Finans.Infrastructure/Stocks/
    entry_points: [IStockDataProvider, FinnhubStockDataProvider, YahooStockHistoryProvider, StockDataService, StockHistoryService, LlmStockExplainService]
    status: healthy
    scope: us-equities
    notes: BIST kapsam dışı (maliyet). Sağlayıcı anahtarı yoksa NotConfiguredStockDataProvider.
    last_verified_ref: ac61b7e

  - capability: Education module (content + delivery)
    location: backend/src/Finans.Domain/Education/ + Finans.Application/Education/ + Finans.Infrastructure/Seed/EducationContent.cs + Services/EducationService.cs
    entry_points: [IEducationService, ILessonContextService, ContextKeys, IDiagnosticService, EducationContent, SeedSet0Async, UpsertLesson]
    status: healthy
    scope: all
    notes: >-
      İçerik kodda seed; idempotent **mutabakat** — yazılı içerik değişikliği çalışan DB'ye iner.
      Mutabakat İKİ katmanda ayrı ayrı kurulu: **bölümler** (metin · katman · figür, T6.7) ve
      **ders kaydı** (başlık · slug · özet · gövde · süre · seviye, `UpsertLesson`, RV-064).
      ⚠ Ders kaydında `IsPublished` · `TrackId` · `CreatedAtUtc` bilerek DIŞARIDA — işletme
      kararı, yapısal taşıma ve kaydın kendi geçmişi seed tarafından geri alınmaz.
      Yeni bir içerik alanı eklenirse HEM karşılaştırmaya HEM kopyaya eklenmeli; eksik
      bırakılan alan sessizce eskimiş kalır (bu `Heading`'de bir kez, ders özetinde bir kez oldu).
      Ders eklerken künye `16-CURRICULUM.md`'den okunur; yapısal sözleşme testleri **M1-M12**
      + tavsiye/kurum adı makine kapısı (RV-062).
      📌 **Künye artık makine tarafından da okunuyor** (M12, GD-006): başlıktaki figür
      sayısı ↔ figür planı ↔ aşama planı mutabakatı testle korunuyor. Yani
      `16-CURRICULUM.md` yalnız insan dokümanı değil, **testi olan bir sözleşme**;
      künye biçimi değişirse (`**Figür planı (N):**`, 🖼, 🎛, `*(fallback)*`)
      `Curriculum_figure_counts_match_their_own_figure_plans` regex'i de güncellenmeli.
      ⚠ Künye ↔ seed mutabakatının **plan → seed** yönü hâlâ açık (TD-002):
      planlanıp yazılmamış figür ne M12'ye ne M4'e takılır. Ters yön (seed → künye)
      **M13** ile kapandı (REVIEW-006/RV-067): kullanıcıya görünen her figür kanonik
      künyede de yazılı olmak zorunda, yani kırmızı bir M12'yi *planı küçülterek*
      yeşile çevirmek artık mümkün değil.
      📌 M12 REVIEW-006'da **üç lehçeyi** birden tanıyacak şekilde genişletildi
      (`Figür planı` · `Figürler` · `Mevcut figürler`); ilk hâli 35 künyenin yalnız
      18'ini görüyordu. Genişletme dört birikmiş sapma buldu (S1-L3 sayacı, S3-L2
      eksik 🖼) ve dokuz bağsız figürü **TD-005** olarak kayda geçirdi.
    tests: backend/tests/Finans.Integration.Tests/EducationSeedTests.cs (+ EducationApiTests)
    last_verified_ref: HEAD   # REVIEW-006 (2026-11-22) ile yeniden doğrulandı

  - capability: Nudge rule engine (educational notes)
    location: backend/src/Finans.Application/Portfolio/NudgeRuleEngine.cs + Infrastructure/Services/NudgeService.cs
    status: healthy
    scope: all
    notes: Kural tabanlı, deterministik; LLM'siz eğitici not üretimi.
    last_verified_ref: ac61b7e

  - capability: Caching & external-call resilience
    location: backend/src/Finans.Infrastructure/Caching/ + Services/Cached*.cs
    entry_points: [IAppCache, DistributedAppCache, CacheMetrics, PortfolioCacheStamp, CachedFxRateProvider, CachedInflationRateProvider]
    status: healthy
    scope: all
    notes: Cache anahtarı UserId içerir (10 §3-4).
    last_verified_ref: ac61b7e

  - capability: API cross-cutting (auth stub, errors, observability, rate limit)
    location: backend/src/Finans.Api/
    entry_points: [HttpCurrentUser, ICurrentUser, GlobalExceptionHandler, AppExceptionHandler, CorrelationIdMiddleware, SensitiveDataDestructuringPolicy, Program.cs]
    status: degraded
    scope: all
    notes: Kimlik hâlâ `X-User-Id` başlığı (JWT Faz 7). Serilog(+Seq) · OpenTelemetry/Prometheus · /health + /health/ready · uç bazlı rate limit.
    last_verified_ref: ac61b7e

  - capability: Persistence (EF Core / PostgreSQL)
    location: backend/src/Finans.Infrastructure/Persistence/
    entry_points: [FinansDbContext, Configurations/, Migrations/, DesignTimeDbContextFactory]
    status: healthy
    scope: all
    notes: 9 migration; testlerde SQLite fixture (SqliteWebApplicationFactory).
    last_verified_ref: ac61b7e

  - capability: Shared contract package (@finans/shared)
    location: packages/shared/src/
    entry_points: [types/, api/, theme/, format/]
    status: healthy
    scope: web + (gelecek) mobil
    notes: Hesap YOK — yalnız tip, istemci/hook, tasarım token'ı, TR biçimleyici (NFR-7).
    last_verified_ref: ac61b7e

  - capability: Web UI (React + Vite)
    location: web/src/
    entry_points: [routes/, components/, lib/appShell.tsx, lib/hooks.ts]
    status: healthy
    scope: web
    notes: 141 birim/bileşen testi + Playwright smoke. Grafikler kütüphanesiz SVG (Sparkline, PriceChart, ValueHistoryChart, ScenarioChart, AllocationDonut).
    last_verified_ref: ac61b7e

  - capability: Lesson rendering (safe markdown + figures)
    location: web/src/components/MiniMarkdown.tsx + LessonFigure.tsx
    status: healthy
    scope: web/education
    notes: dangerouslySetInnerHTML YOK; şema beyaz-listeli bağlantı (safeHref); figürler elle yazılmış SVG kayıt defteri — seed anahtarlarıyla test mutabakatı (M4). Yeni ders figürü buraya eklenir (D-012). Çok panelli figürler için ortak yardımcı: `WeightStack` (ağırlık yığını; GD-003/S0-L8, S0-L10'da `cost-stack` olarak yeniden kullanıldı) — yeni figür yazmadan önce bakılır. ⚠ **Ton kuralı:** yazı taşıyan dolgu OPAK olmaz (RV-039, RV-053, RV-055, RV-065) ve bir seçenek/kalem diğerinden daha vurgulu çizilmez (RV-016/026/027/028, RV-054). Durum bilgisi renkle değil **biçimle** kodlanır (kesikli çerçeve, kontur — `fig-seg-strong`). 📌 **İlk kural testli, ama SINIRLI** (GD-007 + REVIEW-006): `LessonFigure.test.tsx` kayıttaki **101** figürün her birini çizip hem render ediyor mu (svg · aria-label · şekil · altyazı) hem de opak dolgunun üzerine yazı düşmüş mü diye bakar; liste `FIGURE_KEYS` ile kayıt defterinden **türetilir**, elle tutulmaz, yani yeni figür otomatik kapsanır. **Kapının sınırlarını bilerek yaz:** yalnız *tam opak* üç dolgu sınıfı (`fig-seg-lead` · `fig-bar-pos` · `fig-bar-neg`) ve yalnız `opacity ≥ 0,9`; yalnız `rect` ve `circle` (`path`/`polygon` DEĞİL); yazının **tahmini** kapladığı alan (jsdom `getBBox` vermez) ve **gerçek kontrast oranı ölçülmez**. ⚠ Bu satır REVIEW-006'dan önce kuralı *genel* olarak "TESTLİ" ilan ediyordu; o sırada `fig-bar-pos`/`fig-bar-neg` üzerinde **10 canlı ihlal** vardı ve kapı onları göremiyordu (RV-065/RV-076). Bu dosya CLAUDE.md §11.5 gereği **kod yazmadan önce** okunur, bu yüzden kapsam burada abartılmaz. Vurgu/niyet kuralı hâlâ insan incelemesinde — **TD-003**; görsel bozulma — **TD-004**.
    last_verified_ref: HEAD   # REVIEW-006 (2026-11-22) ile yeniden doğrulandı

  - capability: Backlog projection (GitHub Issues -> generated/BACKLOG.md)
    location: .github/scripts/gen-backlog.sh + .github/workflows/adm-backlog-sync.yml
    entry_points: [gen-backlog.sh, adm-backlog-sync.yml, adm-session-start.mjs::openWork]
    status: healthy
    scope: adm/tooling
    notes: >
      D-022 — kanonik backlog GitHub Issues; bu üretici commit'li çevrimdışı
      projeksiyonu yazar (INV-12/INV-17). gh'ın gömülü jq'sunu kullanır, harici
      jq gerekmez. Issue olaylarında + günlük cron'da CI tazeler; adm-close de
      projections listesi gereği yeniden üretir. Yeni bir ADM görünümü (ör.
      CURRENT_STATE) gerekirse bu script'in kalıbı genişletilir, yenisi yazılmaz.
      Oturum başı brifingi aynı dosyadan prio:1 satırlarını okur — ağ çağrısı yok.
    last_verified_ref: HEAD

  - capability: Test CI (gates actually run)
    location: .github/workflows/tests.yml
    entry_points: [web (vitest + tsc), backend (Application unit + Integration)]
    status: healthy
    scope: all
    notes: >
      REVIEW-006/RV-069 — bu iş akışından ÖNCE depoda hiçbir CI testi
      koşturmuyordu (tek iş akışı backlog senkronuydu). Yani GD-006/GD-007'nin
      kurduğu "makine kapıları" yerini almak için kuruldukları insan disiplinine
      bağlı kalıyordu. Kapılar artık burada koşar: **SC-E30** (figür render +
      okunabilirlik) web işinde, **SC-E31** (künye sözleşmesi M12 + M13) backend
      işinde. Parasal hesap birim testleri (NFR-1) de backend işinde.
      ⚠ INC-001'in DÖRT testi tek tek ADLARIYLA dışlanmış (issue #74). Sınıf
      adıyla dışlamak ölçülerek yanlış bulundu: `ObservabilitySecurityTests`
      altı test taşıyor, yalnız üçü kırmızı — sınıfı dışlamak üç YEŞİL testi de
      düşürüyordu (189 yerine 186). #74 kapandığında `--filter` satırı silinir.
      Yeni bir paket/test projesi eklenirse buraya da bir adım eklenir; yoksa
      testi olan ama hiç koşmayan bir paket oluşur — bu yeteneğin varlık sebebi o.
      Required check olarak işaretlenmedi: birleştirmeyi engellemez, görünür kılar.
    tests: (kendisi bir kapı — doğrulaması koşum günlüğü)
    last_verified_ref: HEAD
```

## Açık sorular
1. `status: degraded` verdiğim iki yetenek (LLM hattı, API kimlik katmanı)
   gerçekten "degraded" mi, yoksa "healthy ama bilinen kısıtlı" mı sayılmalı?
2. `web/e2e` ve `RUNTIME` kapsamı ayrı bir yetenek olarak mı kaydedilsin?
