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

**Sıradaki adım → FAZ 1 (Portföy MVP): `T1.1` `PortfolioCalculationService`**
(saf hesap fonksiyonları, altın test verisi 40gr/4.546→181.851/+%43 — birim testli).

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
| T1.4 | Reel getiri (enflasyon oranı girişi) + test | T1.1 | `CLAUDE.md` §6 | [ ] |
| T1.5 | Ort. maliyet türetimi (Transactions'tan) + test | T0.4 | `03` §5 | [ ] |
| T1.6 | Holdings CRUD endpoint'leri + DTO + validasyon | T1.5 | `04` §4 | [ ] |
| T1.7 | `GET /api/portfolio/summary` | T1.1–T1.4 | `04` §4 | [ ] |
| T1.8 | BES özel alanları (devlet katkısı ayrı) — entity→DTO→ekran | T0.4 | `03`, `04`, `05` §7 | [ ] |
| T1.9 | Settings (baz para birimi) endpoint + web seçimi | T0.7 | `04` §4 | [ ] |
| T1.10 | `@finans/shared`: API tipleri (04) + TanStack Query hook'ları + `formatCurrency/formatPercent` (tr-TR) | T0.2, T1.7 | `13` §2, `05` §10 | [ ] |
| T1.11 | **Web:** AppShell (sidebar/topbar) + `HeroCard` + `summary` bağlama | T1.7, T1.10 | `13` §4 | [ ] |
| T1.12 | **Web:** `AllocationDonut` (SVG/conic) + legend | T1.7 | `13` §4 | [ ] |
| T1.13 | **Web:** Holdings tablosu/kartı + varlık detay (modal/route) | T1.6 | `13` §4 | [ ] |
| T1.14 | **Web:** "Varlık Ekle" formu (modal) → `POST /holdings` | T1.6 | `13` §4 | [ ] |
| T1.15 | **Per-user kapsam deseni:** tüm sorgular `UserId`'e kapsanır (EF global query filter / base repo); summary cache anahtarı `UserId` içerir; in-memory cache (varlık kataloğu/summary) | T1.6 | `11` §3, `10` §3 | [ ] |

**Faz 1 DoD:** **Web'de** varlık ekle/sil/listele; toplam/kâr/getiri/dağılım
testlerle doğru; çoklu pb baz pb'ye çevriliyor; BES devlet katkısı ayrı.
→ **Tek başına (web) ürün.**

---

## FAZ 2 — Canlı Fiyat & Bilgilendirme

| ID | Görev | Bağımlılık | Doküman |
|----|-------|-----------|---------|
| T2.1 | Fiyat sağlayıcı seç (altın/döviz ücretsiz katman) + `ILlmClient` benzeri `IPriceProvider` | Faz 1 | `02` |
| T2.2 | `PriceFetchService` + cache (5-15 dk) + `PriceSnapshots`'a yaz | T2.1 | `02` §2.2 |
| T2.3 | Fallback: dış API çökünce son bilinen fiyat + `stale:true` | T2.2 | `04` §5, NFR-5 |
| T2.4 | `GET /api/prices` + summary'i canlı fiyatla besle | T2.2 | `04` §5 |
| T2.5 | `NudgeRuleEngine` (kural tabanlı, örn. nakit oranı eşiği) + `GET /nudges` | Faz 1 | `04` §5 |
| T2.6 | **Web:** yenile + son güncelleme/"yaklaşık" etiketi + Nudge kartı | T2.4 | `13` §4 |
| T2.7 | **Redis cache katmanı:** fiyat/FX/summary cache Redis'e (dağıtık); stampede koruması; cache isabet metriği | T2.2 | `10` §3 |
| T2.8 | **Gözlemlenebilirlik yığını:** Compose'a Seq + Prometheus + Grafana; OTel metrikleri (RED + bağımlılık + cache); ilk dashboard'lar + alarmlar | T0.11 | `12` §2,§4,§6 |
| T2.9 | **Reverse proxy + sınırlama:** Traefik/Caddy (TLS/Let's Encrypt) + rate limiting + güvenlik başlıkları; iç servisler dışarı kapalı | T0.13 | `11` §5, `10` §5 |

**Faz 2 DoD:** Otomatik güncel değer + yenileme; ≥1 not doğru tetikleniyor; dış
API çökünce uygulama çökmüyor; **Redis cache + metrik/dashboard/alarm çalışıyor;
rate limit + TLS proxy ayakta**.

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
