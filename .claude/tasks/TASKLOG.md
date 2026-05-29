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
