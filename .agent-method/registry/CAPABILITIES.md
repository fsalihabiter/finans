# CAPABILITIES — reuse registry (canonical)

> GD yeniden kullanım analizi ÖNCE bu dosyayı tarar. İlk sürüm `adm-analyze`
> taramasından geldi (2026-09-20). Konumlar gerçek dosya yollarıdır; `status`
> değerleri **çıkarımdır**. `last_verified_ref`: ac61b7e.

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
    notes: Devlet katkısı ayrı satır, hak ediş, katkı planı + arka plan telafi çalıştırıcısı. TR pazarına özgü derinlik (G-4).
    last_verified_ref: ac61b7e

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
    entry_points: [IEducationService, ILessonContextService, ContextKeys, IDiagnosticService, EducationContent, SeedSet0Async]
    status: healthy
    scope: all
    notes: İçerik kodda seed; idempotent **mutabakat** (metin/katman/figür değişikliği çalışan DB'ye iner). Ders eklerken künye `16-CURRICULUM.md`'den okunur; yapısal sözleşme testleri M1-M9.
    tests: backend/tests/Finans.Integration.Tests/EducationSeedTests.cs (+ EducationApiTests)
    last_verified_ref: ac61b7e

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
    notes: dangerouslySetInnerHTML YOK; şema beyaz-listeli bağlantı (safeHref); figürler elle yazılmış SVG kayıt defteri — seed anahtarlarıyla test mutabakatı (M4). Yeni ders figürü buraya eklenir (D-012).
    last_verified_ref: ac61b7e
```

## Açık sorular
1. `status: degraded` verdiğim iki yetenek (LLM hattı, API kimlik katmanı)
   gerçekten "degraded" mi, yoksa "healthy ama bilinen kısıtlı" mı sayılmalı?
2. `web/e2e` ve `RUNTIME` kapsamı ayrı bir yetenek olarak mı kaydedilsin?
