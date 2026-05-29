# TASKLOG — Çalışma Günlüğü

> Append-only, **en yeni en üstte** kronolojik kayıt. Projeyle yapılan her
> anlamlı iş (kod, doküman, karar, düzeltme) buraya bir girdi bırakır. Protokol:
> `CLAUDE.md` §11 ve [`README.md`](README.md). Sohbet/soru-cevap turları kayıt
> gerektirmez — yalnızca **bir şey değiştiğinde** yaz.

**Girdi şablonu** (kopyala, en üste ekle):

```
## YYYY-AA-GG · <kısa başlık>
- **Görev(ler):** <08-BACKLOG ID'leri, örn. T0.2> | (plan dışıysa: ad-hoc)
- **Ne yapıldı:** <1-3 madde, somut>
- **Dokunulan dosyalar:** <yol listesi>
- **Test:** <yazılan/geçen testler + senaryo ID'leri (09 §5), örn. "SC-01 unit+integration ✓ / dotnet test yeşil" | yok (test gerektirmez)>
- **Karar/Not:** <varsa kalıcı karar — ilgili dokümana da işle>
- **Durum:** tamamlandı | devam ediyor | bloke (<sebep>)
- **Sıradaki:** <bir sonraki somut adım>
```

---

## 2026-05-30 · Docker temeli — compose ile migrate+seed'li API (T0.14)
- **Görev(ler):** T0.14 (tamam) · dal `feat/docker`
- **Ne yapıldı:**
  - `backend/Dockerfile`: çok aşamalı (sdk:10.0 build → aspnet:10.0-alpine runtime),
    csproj-önce restore (cache), **non-root** (`USER $APP_UID` → uid 1654), düz HTTP
    8080, busybox wget HEALTHCHECK (`/health`). `backend/.dockerignore`.
  - `docker-compose.yml` (kök): postgres (healthcheck + volume + dev 5433) + api
    (depends_on healthy, env ile connstr/CORS/migrate+seed). Parola env/`.env`
    (dev varsayılan finans_dev; repoda gerçek sır yok).
  - Program.cs: bayrakla **başlangıçta migration (+ops. seed)** (`Database__Apply
    MigrationsOnStartup`/`Database__Seed`; testlerde varsayılan kapalı → DB'siz) +
    **koşullu HttpsRedirection** (`Security__UseHttpsRedirection`, container'da false).
- **Dokunulan dosyalar:** `backend/Dockerfile`, `backend/.dockerignore`,
  `docker-compose.yml`, `Finans.Api/Program.cs`, `appsettings.json`.
- **Test:** `dotnet test` 10/10 yeşil (değişmedi; startup-migration varsayılan kapalı).
  **Canlı `docker compose up --build`:** /health & /health/ready (DB) Healthy,
  /api/health 200; başlangıç migrate+seed → cost=422.970/value=641.403/holdings=4;
  `docker exec api id` → uid=1654(app) (non-root doğrulandı).
- **Karar/Not:** Container düz HTTP servis eder, TLS reverse proxy'de sonlanır
  (T2.9); postgres host portu (5433) dev için, prod'da kaldırılır (iç ağ — 11 §5).
  Startup-migration yalnız dev/compose kolaylığı; prod'da kapalı.
- **Durum:** tamamlandı
- **Sıradaki:** T0.11 kalanı (Sqlite integration fixture + Playwright iskeleti) →
  Faz 0 TAM kapanış.

## 2026-05-30 · Güvenlik + gözlemlenebilirlik temeli (T0.12 + T0.13)
- **Görev(ler):** T0.12, T0.13 (tamam) · dal `feat/security-observability`
- **Ne yapıldı:**
  - **T0.12** Serilog (`Serilog.AspNetCore`, Console sink) + `CorrelationIdMiddleware`
    (X-Correlation-ID üret/echo + LogContext) + `UseSerilogRequestLogging` +
    **redaksiyon iskeleti** (`SensitiveDataDestructuringPolicy`: password/token/
    secret/email içeren nesnelerde `***`, dar etki). Health: `/health` (liveness,
    predicate false) + `/health/ready` (`AddDbContextCheck`, tag "ready").
  - **T0.13** Global hata maskeleme (`GlobalExceptionHandler : IExceptionHandler` +
    `AddExceptionHandler`/`UseExceptionHandler`): istemciye sözleşmeli
    `{error:{code:"INTERNAL_ERROR",message}}`, stack/iç detay yalnız log'da
    (04 §2, 11 §4). CORS allow-list (`Cors:AllowedOrigins`, `*` yok). Secret:
    `dotnet user-secrets init` (UserSecretsId), parola repoda değil (env/secrets).
- **Dokunulan dosyalar:** `Finans.Api/Program.cs` (Serilog+health+CORS+exception+
  correlation boru hattı), `Finans.Api/ErrorHandling/{ApiError,GlobalExceptionHandler}.cs`,
  `Finans.Api/Observability/{CorrelationIdMiddleware,SensitiveDataDestructuringPolicy}.cs`,
  `appsettings.json`+`.Development.json` (Cors), `Finans.Api.csproj` (paketler+UserSecretsId),
  `tests/.../ObservabilitySecurityTests.cs`, `tests/.../AssemblyInfo.cs`.
- **Test:** `dotnet test` **10/10 yeşil** (3 ardışık koşu — flaky değil). Yeni: liveness
  health, correlation üret/echo, hata maskeleme (stack sızmaz), redaksiyon (hassas
  maskelenir/diğerleri korunur). **Canlı doğrulama:** /health & /health/ready (DB)
  Healthy, correlation header, CORS 5174 kabul / evil.com red, Serilog request log.
- **Karar/Not:** Integration testleri **sıralı** koşar (`DisableTestParallelization`)
  — statik `Log.Logger`+`CloseAndFlush` paralel host'larda çakışıyordu. Güvenlik
  başlıkları/rate-limit/TLS reverse proxy'de (T2.9). Redaksiyon alan listesi Faz 1'de genişler.
- **Durum:** tamamlandı (T0.12, T0.13)
- **Sıradaki:** T0.14 (Docker: API Dockerfile non-root + compose api+postgres) +
  T0.11 kalanı → Faz 0 kapanışı.

## 2026-05-30 · Tasarım token'ları + fontlar (DESIGN.md → web)
- **Görev(ler):** T0.9 (tamam) · dal `feat/design-tokens`
- **Ne yapıldı:**
  - `@finans/shared/theme`: DESIGN.md §2-4 token'ları **tek kaynak** TS objesi
    (`tokens`: color/font/radius/space/shadow) + `cssVariables()` üretici
    (camelCase→kebab, grup önekleri: `--font-*`/`--radius-*`/`--space-*`/`--shadow-*`).
  - Web: `@fontsource-variable/fraunces` + `hanken-grotesk` (self-hosted, CDN yok);
    `applyTheme()` token'ları paint öncesi `:root`'a enjekte ediyor; `index.css`
    atmosfer (iki radial-gradient) + Fraunces başlık + Hanken gövde + tabular-nums;
    `App.css` token'lara taşındı (gold/mint/coral, panel, hero gölge).
  - Font ailesi `'Fraunces Variable'` (web) + `'Fraunces'` fallback (mobil expo-font).
- **Dokunulan dosyalar:** `packages/shared/src/theme/index.ts` (+`theme.test.ts`),
  `web/src/lib/applyTheme.ts` (+test), `web/src/main.tsx`, `web/src/index.css`,
  `web/src/App.css`, `web/src/vite-env.d.ts`, `web/package.json`.
- **Test:** `pnpm test` yeşil — shared 8 (format 4 + theme 4), web 2 (render + applyTheme).
  `pnpm --filter @finans/web build` yeşil. **Görsel doğrulama:** Vite dev (5180)
  Chrome screenshot — Fraunces/Hanken, altın aksan, mint pozitif, coral health
  hatası, atmosfer halesi DESIGN.md ile uyumlu.
- **Karar/Not:** Token'lar runtime'da `:root`'a enjekte (FOUC yok, render öncesi);
  fontsource side-effect import'u için `declare module "@fontsource-variable/*"`.
- **Durum:** tamamlandı
- **Sıradaki:** T0.12 (Serilog + /health,/health/ready) / T0.13 (güvenlik+CORS) /
  T0.14 (Docker compose) + T0.11 kalanı → Faz 0 kapanışı.

## 2026-05-29 · Veri katmanı: EF Core + entity'ler + migration + tutarlı seed
- **Görev(ler):** T0.4, T0.5, T0.6, T0.6b (tamam) · dal `feat/data-layer`
- **Ne yapıldı:**
  - **T0.4** EF Core + Npgsql (`Npgsql.EntityFrameworkCore.PostgreSQL`) +
    `FinansDbContext` (Infrastructure). Global convention'lar: decimal→numeric(18,6),
    enum→varchar (HasConversion<string>). citext eklentisi. `AddInfrastructure` DI +
    `DesignTimeDbContextFactory` (env `ConnectionStrings__Postgres`).
  - **T0.5** Domain entity'leri: portföy (Asset, Holding, Transaction, BesDetails,
    PriceSnapshot, FxRate, InflationRate) + kimlik/audit (User, Role,
    UserRoleAssignment, RefreshToken, AuditLog). Base `Entity` (UUIDv7 default).
    Konfigürasyonlar: check constraint'ler (numeric>=0, enum allow-list),
    unique/index'ler, soft-delete query filter, xmin concurrency (xid shadow),
    citext Email, inet IP, FK delete davranışları (User→Holdings cascade,
    Asset→Holdings restrict, AuditLog→User SetNull).
  - **T0.6** `InitialCreate` migration üretildi ve **gerçek Postgres'te (Docker)
    `database update` ile uygulandı** → 12 tablo + 19 check constraint doğrulandı.
  - **T0.6b** `SeedData.cs` — idempotent (deterministik MD5-tabanlı GUID +
    Users.Any guard), `dotnet run -- seed` ile migrate+seed. Sayılar **birebir
    tutarlı**: TotalCost 422.970,00 / Value 641.403,00 / Profit +218.433,00 /
    Return %51,6 (SQL ile doğrulandı). İkinci çalıştırma çoğaltmadı.
- **Dokunulan dosyalar:** `backend/src/Finans.Domain/**` (Common, Enums, Portfolio,
  Identity), `backend/src/Finans.Infrastructure/**` (Persistence/FinansDbContext,
  Configurations, DesignTimeDbContextFactory, DependencyInjection, Seed/SeedData,
  Persistence/Migrations), `Finans.Api/Program.cs` (DI + `-- seed`), `appsettings.json`,
  `tests/Finans.Integration.Tests/SeedConsistencyTests.cs`.
- **Test:** `dotnet test` **4/4 yeşil** — HealthEndpoint (WebApplicationFactory) +
  SeedConsistency (EF InMemory): toplamlar, idempotency, BES devlet katkısı ayrı.
  Testler DB'siz koşar (CI-uyumlu). Canlı doğrulama: migration apply + seed totals
  gerçek Postgres'te SQL ile teyit.
- **Karar/Not (kalıcı):**
  - **xmin concurrency** `UseXminAsConcurrencyToken()` bu Npgsql sürümünde yok →
    `Property<uint>("Version").HasColumnName("xmin").HasColumnType("xid").IsConcurrencyToken()`.
  - **EF paket hizalama:** Npgsql provider Relational 10.0.4 çekiyordu, Design 10.0.8
    → açık `Microsoft.EntityFrameworkCore.Relational 10.0.8` referansıyla birleştirildi
    (MSB3277 giderildi). Test InMemory de 10.0.8.
  - **Seed yeri/şekli:** `dotnet run -- seed` (migrate+seed+çık). Eğitim (C) tabloları
    Faz 5'e ertelendi (§13.3) — T0.5 yalnızca A+B.
  - **Bağlantı dizesi:** parola repoda yok; appsettings parolasız, env/User Secrets
    ile verilir (CLAUDE.md §13). Doğrulama Docker Postgres (5433) ile yapıldı, container temizlendi.
- **Durum:** tamamlandı (T0.4/T0.5/T0.6/T0.6b)
- **Sıradaki:** T0.9 (DESIGN.md token'ları) + T0.12/T0.13/T0.14 (Serilog/güvenlik/Docker)
  → Faz 0 kapanışı.

## 2026-05-29 · Faz 0 iskelet: monorepo + .NET backend + web ayağa kalktı
- **Görev(ler):** T0.1, T0.2, T0.3, T0.7, T0.8, T0.10 (tamam); T0.11 (kısmen)
- **Ne yapıldı:**
  - **T0.1** `.gitignore` (bin/obj, node_modules, dist, .expo, sır kalıpları:
    `*.env`, `appsettings.*.local.json`, secrets).
  - **T0.2** pnpm workspaces (`pnpm-workspace.yaml` + kök `package.json`) +
    `@finans/shared` paketi: `types` (HealthResponse, CurrencyCode), `api`
    (createApiClient + ApiError), `theme` (iskelet), `format` (formatCurrency/
    formatPercent tr-TR, çekirdek). pnpm corepack ile değil, `npm i -g pnpm`
    (11.5.0) ile kuruldu (corepack Program Files'a yazamadı).
  - **T0.3** .NET çözümü `backend/Finans.slnx` + 4 katman (Api/Application/
    Domain/Infrastructure) + 2 test projesi (Application.Tests, Integration.Tests).
    Referans yönü: Api→App+Infra, App→Domain, Infra→App.
  - **T0.7** `GET /api/health` → `{status:"ok"}` (HealthController).
  - **T0.8** Web iskeleti (Vite React-TS `web/`) + React Router (createBrowserRouter)
    + TanStack Query + `@finans/shared` bağlı; AppShell + Portföy/Analiz route'ları.
  - **T0.10** HealthBadge web'de `/api/health`'ten canlı veri çekiyor; **proxy
    zinciri canlı doğrulandı** (vite 5174 → proxy → backend 5298 → `{status:ok}`).
  - **T0.11 (kısmen)** backend `Finans.Integration.Tests` (WebApplicationFactory)
    + FluentAssertions; web Vitest + RTL kuruldu. **Kalan:** Sqlite fixture
    (DB gelince), Playwright iskeleti.
- **Dokunulan dosyalar:** `.gitignore`, `package.json`, `pnpm-workspace.yaml`,
  `packages/shared/**`, `backend/**` (slnx + 6 proje), `web/**` (Vite app + testler).
- **Test:** backend `dotnet test` yeşil (HealthEndpointTests 1/1, WebApplicationFactory);
  `pnpm test` yeşil (shared format 4/4, web PortfolioPage render 1/1);
  `pnpm --filter @finans/web build` yeşil (tsc + vite). Canlı E2E: proxy→health 200 ✓.
- **Karar/Not (kalıcı):**
  - **.NET hedefi `net10.0`** — kurulu SDK 10.0.300; .NET 8 runtime yok. Dokümanlardaki
    "NET 8" yerine net10.0 (LTS). `02 §2.3` güncellendi.
  - **Çözüm formatı `.slnx`** (.NET 10 varsayılanı).
  - **FluentAssertions 7.2.0'a sabitlendi** — 8.x ticari lisansa geçti; 7.x Apache.
  - **Web yığını vite 5.4 + @vitejs/plugin-react 4.3 + vitest 3.2'ye sabitlendi**
    — Vite 8 (scaffold) vitest ile iki-vite çakışması yaratıyordu; tek sürüm vite 5.
  - `@finans/shared` build adımsız tüketiliyor (exports → `src/*.ts`); Bundler
    çözümleme, göreli import'larda uzantı yok; `erasableSyntaxOnly` uyumlu (parametre-
    özelliği yok).
- **Durum:** tamamlandı (T0.1/T0.2/T0.3/T0.7/T0.8/T0.10); T0.11 kısmen.
- **Sıradaki:** T0.4-T0.6 (EF Core + entity'ler + migration + seeder) — backend
  veri katmanı. Paralelde T0.9 (DESIGN.md token'ları) + T0.12/T0.13/T0.14 kapıları.

## 2026-05-29 · Veri modeli derinleştirildi + eğitim modeli + tutarlı seed
- **Görev(ler):** ad-hoc (veri modeli)
- **Ne yapıldı:** `03-DATA-MODEL` baştan yazıldı, kolon-düzeyinde derinlik:
  konvansiyonlar (UUIDv7, numeric(18,6), UTC, concurrency, soft-delete), enum
  allow-list'leri, **kimlik/güvenlik/audit** tabloları (Users genişletildi,
  Roles, RefreshTokens, AuditLogs), Asset'te Unit↔PricingCurrency ayrımı,
  InflationRates. **Eğitim modülü** tam modellendi (Tracks, Lessons, Sections,
  Prerequisites, ConceptTags, Quizzes/Questions/Options, UserLessonProgress,
  UserQuizAttempts). **Kapsamlı seed** taslağa birebir tutarlı (641.403/422.970/
  +218.433/+%51,6 — node ile doğrulandı) + eğitim içeriği seed'i.
- **Dokunulan dosyalar:** `docs/03-DATA-MODEL.md` (yeniden yazıldı),
  `docs/04-API-CONTRACT.md` (§7.5 eğitim uçları), `docs/08-BACKLOG.md`
  (T0.5 kimlik/audit + T0.6b kapsamlı seeder + Faz 5 T5E.1-4 eğitim)
- **Test:** seed sayıları `node` ile yeniden hesaplanıp taslak başlığıyla
  birebir doğrulandı; bu set aynı zamanda integration fixture'ı (`09` SC-01..06).
- **Karar/Not:** BES getiri tabanı = own+state katkı (taslaktaki +%88); devlet
  katkısı UI'da ayrı. Eğitim içeriği DB'de (Markdown gövde). Ders "Locked"
  durumu ön-koşuldan türetilir, saklanmaz. Seeder idempotent, ayrı `SeedData.cs`.
- **Durum:** tamamlandı
- **Sıradaki:** T0.1 — `git init` + `.gitignore`

## 2026-05-29 · Web frontend (ReactJS+Vite) + web-öncelikli yeniden plan
- **Görev(ler):** ad-hoc (mimari/plan yeniden düzenleme)
- **Ne yapıldı:** Web yüzeyi eklendi ve **birincil** yapıldı. Yeni
  `13-WEB-FRONTEND` (monorepo, Vite React, paylaşılan paket, web düzen
  uyarlaması, web'e özel güvenlik/perf/izleme). Tüm plan web-öncelikli olacak
  şekilde yeniden düzenlendi; mobil ayrı "FAZ M" koluna alındı.
- **Dokunulan dosyalar:** `docs/13-` (yeni); `CLAUDE.md` (§3 mimari diyagram +
  monorepo, §7 yapı); `ROADMAP.md` (web-öncelikli not); `docs/01-` (NFR-13),
  `02-` (§3 frontend/monorepo), `05-` (sıra notu), `06-` (monorepo+web kurulum),
  `08-` (Faz 0/1 web'e çevrildi + FAZ M eklendi), `09-` (§3W web test + SC-W1..3),
  `10/11/12-` (web pointer), `docs/README.md` (indeks + sıra notu)
- **Test:** yok (mimari/doküman); web test araçları (Vitest+RTL+Playwright)
  `09` §3W'ye, senaryolar SC-W1..W3'e eklendi.
- **Karar/Not:** Web = **ayrı React+Vite SPA**, monorepo + `@finans/shared`
  (tip/token/format paylaşımı; kod değil sözleşme paylaşımı). **Web öncelikli**;
  mobil sonra (FAZ M). Tek API ikisine hizmet eder; CORS web origin allow-list.
- **Durum:** tamamlandı
- **Sıradaki:** T0.1 — `git init` + `.gitignore`

## 2026-05-29 · Performans, güvenlik & gözlemlenebilirlik mimarisi
- **Görev(ler):** ad-hoc (mimari altyapı)
- **Ne yapıldı:** Çok kullanıcı + hız + maliyet + güvenlik + izleme için üç
  kalıcı doküman: `10-PERFORMANCE-SCALABILITY` (bütçeler, cache katmanları,
  stateless ölçeklenme, maliyet), `11-SECURITY` (STRIDE tehdit modeli, per-user
  izolasyon/IDOR, JWT/Argon2, sırlar, KVKK, güvenlik testleri, kontrol listesi),
  `12-OBSERVABILITY` (Serilog+Seq, OTel+Prometheus+Grafana, health check, audit
  log, alarm). Tümü iş akışına bağlandı.
- **Dokunulan dosyalar:** `docs/10-`, `docs/11-`, `docs/12-` (yeni),
  `CLAUDE.md` (§11 kapı + yeni §13), `docs/01-` (NFR-10/11/12), `docs/02-`
  (§5-6 dağıtım/güvenlik), `docs/06-` (DoD + yapma listesi), `docs/08-`
  (T0.11-13, T1.15, T2.7-9, T3.9, Faz5, çapraz-kesen), `docs/09-` (SC-13..16,
  SC-P1), `docs/README.md` (indeks)
- **Test:** yok (mimari/doküman) — güvenlik/perf testleri `09` §5'e senaryo
  olarak eklendi (SC-13 IDOR zorunlu); kod gelince yazılacak.
- **Karar/Not:** Barındırma = **self-hosted/VPS + Docker** (açık kaynak).
  İzleme = **Serilog+Seq / OTel+Prometheus+Grafana** (maliyetsiz, self-host).
  En kritik kural: **per-user veri izolasyonu** (IDOR/BOLA), kimlik açılmadan
  testi yeşil olmalı.
- **Durum:** tamamlandı
- **Sıradaki:** T0.1 — `git init` + `.gitignore`

## 2026-05-29 · Test disiplini kuruldu (senaryo-önce / yeşil-kapı)
- **Görev(ler):** ad-hoc (süreç altyapısı)
- **Ne yapıldı:** Her geliştirmeyle birlikte birim + olaylara yönelik (senaryo)
  testlerin yazılıp yeşile getirilmesini zorunlu kılan disiplin kuruldu.
  `09-TESTING-STRATEGY.md` (piramit, backend xUnit/integration, mobil Jest+RTL,
  Given-When-Then senaryo formatı, 14 maddelik yaşayan senaryo kataloğu, görev
  başına akış, kapsam) yazıldı. İş akışına entegre edildi.
- **Dokunulan dosyalar:** `.claude/docs/09-TESTING-STRATEGY.md` (yeni),
  `CLAUDE.md` (§11 yeşil-kapı adımı + yeni §12), `06-DEV-PLAYBOOK.md` (§4-5),
  `08-BACKLOG.md` (T0.10 test altyapısı + çapraz-kesen kural),
  `.claude/tasks/TASKLOG.md` (şablona **Test** alanı), `docs/README.md` (indeks)
- **Test:** yok (süreç/doküman; çalışacak kod henüz yok) — kural bundan sonraki
  her kod görevinde geçerli.
- **Karar/Not:** Test disiplini = senaryo-önce, test-yanında, yeşil olmadan
  tamam yok. Mobil E2E (Maestro) Faz 2+'a ertelendi; Faz 1'de mobil Jest+RTL,
  olay testlerinin ağırlığı backend integration'da.
- **Durum:** tamamlandı
- **Sıradaki:** T0.1 — `git init` + `.gitignore`

## 2026-05-29 · Görev takip sistemi kuruldu
- **Görev(ler):** ad-hoc (süreç altyapısı)
- **Ne yapıldı:** `.claude/tasks/` altında otomatik görev takibi kuruldu —
  TASKLOG (bu dosya), ACTIVE.md, README.md (protokol), SessionStart hook.
  Otomasyon iki katmanlı: CLAUDE.md §11 protokolü + `.claude/settings.json`
  oturum-başı hook'u (her oturumda görev durumu otomatik bağlama yüklenir).
- **Dokunulan dosyalar:** `.claude/tasks/README.md`,
  `.claude/tasks/TASKLOG.md`, `.claude/tasks/ACTIVE.md`,
  `.claude/tasks/session-start.mjs`, `.claude/settings.json`, `CLAUDE.md` (§11)
- **Karar/Not:** Worklog backlog'a referans verir; `08-BACKLOG.md` görev
  durumlarının kaynağıdır. Görev içeriği yargı gerektirdiği için güncellemeyi
  Claude yapar; hook yalnızca durumu görünür kılar.
- **Durum:** tamamlandı
- **Sıradaki:** T0.1 — `git init` + `.gitignore`

## 2026-05-29 · Mimari doküman seti + taslak sürücüsü
- **Görev(ler):** ad-hoc (proje tasarımı / dokümantasyon)
- **Ne yapıldı:** Greenfield proje için kıdemli-mimar doküman seti üretildi
  (`.claude/docs/` 01–08 + README). HTML taslağı gerçekten render edilip her
  ekranı incelendi; ondan türetilmiş mobil şartname yazıldı. Taslağı süren
  `run-finans-prototype` skill'i + `driver.mjs` (playwright-core, sistem Chrome)
  kuruldu ve doğrulandı (6 ekran görüntüsü).
- **Dokunulan dosyalar:** `.claude/docs/*.md` (01–08, README),
  `.claude/skills/run-finans-prototype/{SKILL.md,driver.mjs,.gitignore,package.json}`
- **Karar/Not:** Veri modelinde açık kararlar çözüldü (ort. maliyet
  Transactions'tan türetilir; PostgreSQL). Taslaktaki sayılar elle yerleştirilmiş,
  veri kaynağı değil — gerçek uygulamada her rakam .NET'te deterministik.
- **Durum:** tamamlandı
- **Sıradaki:** Görev takip sistemini kur (↑ bir üstteki girdi)
