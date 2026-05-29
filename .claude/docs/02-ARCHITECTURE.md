# 02 — Mimari (Architecture)

> `CLAUDE.md` § 3'teki üst düzey mimariyi somut katman, teknoloji ve klasör
> kararlarına çevirir. **Değişmez ilke:** *Sayısal hesap KODDA (deterministik),
> yorum LLM'de.*

---

## 1. Sistem Görünümü

```
┌──────────────────────┐    HTTPS/JSON    ┌───────────────────────────┐
│   React Native (Expo) │ ───────────────► │      .NET Web API          │
│   mobil ön yüz        │ ◄─────────────── │  (katmanlı, deterministik) │
└──────────────────────┘                  └─────────────┬─────────────┘
                                                         │
                          ┌──────────────────────────────┼───────────────────────┐
                          ▼                               ▼                       ▼
                  ┌───────────────┐            ┌────────────────────┐    ┌────────────────┐
                  │  Veritabanı    │            │  Fiyat API'leri     │    │   LLM API       │
                  │  (PostgreSQL/  │            │  (altın/döviz/hisse)│    │  (yorum/açıklama)│
                  │   SQL Server)  │            │   [Faz 2+]          │    │   [Faz 3+]       │
                  └───────────────┘            └────────────────────┘    └────────────────┘
```

**Sorumluluk sınırı (en kritik karar):**
- **Backend** = tek doğruluk kaynağı. Tüm parasal hesap, kur dönüşümü, dağılım,
  reel getiri burada `decimal` ile deterministik yapılır.
- **LLM** = sadece **hazır sayıyı yorumlar**, yeni rakam üretmez.
- **Mobil** = sadece **sunum + girdi**. Hesap yapmaz; backend'in verdiğini
  gösterir (ve TR formatına çevirir).

---

## 2. Backend Mimarisi (.NET Web API)

### 2.1 Katmanlar (Clean / Onion benzeri)

```
backend/
├── src/
│   ├── Finans.Api/             ← Controller, DI, middleware, Program.cs
│   ├── Finans.Application/     ← İş mantığı, servisler, DTO, arayüzler
│   │   ├── Portfolio/          ← PortfolioCalculationService (FORMÜLLER)
│   │   ├── Pricing/            ← PriceFetchService [Faz 2]
│   │   └── Commentary/         ← LlmCommentaryService  [Faz 3]
│   ├── Finans.Domain/          ← Entity'ler, value object, kurallar (saf C#)
│   └── Finans.Infrastructure/  ← EF Core DbContext, repository, dış API client
└── tests/
    └── Finans.Application.Tests/  ← Hesaplama birim testleri (ZORUNLU)
```

**Bağımlılık yönü:** `Api → Application → Domain`. `Infrastructure`,
`Application`'ın arayüzlerini implemente eder (DI ile bağlanır). `Domain`
hiçbir şeye bağımlı değil (saf, test edilebilir).

**Neden bu yapı?**
- Hesaplama mantığı (`Domain`/`Application`) veritabanı ve dış API'den izole →
  **birim testi kolay** (NFR-1).
- Dış bağımlılıklar (DB, fiyat API, LLM) arayüz arkasında → değiştirilebilir,
  mock'lanabilir, fallback uygulanabilir (NFR-5).

### 2.2 Anahtar Servisler

| Servis | Faz | Sorumluluk |
|--------|-----|-----------|
| `PortfolioCalculationService` | 1 | `CLAUDE.md` § 6 formülleri. **Saf fonksiyon** — girdi: holdings + fiyat + kur + enflasyon; çıktı: özet. Yan etkisiz, %100 testli. |
| `CurrencyConversionService` | 1 | `tutar × kur(varlık_pb → baz_pb)`. Kurları repository'den/cache'ten alır. |
| `HoldingService` | 1 | CRUD + ort. maliyet türetimi (Transactions'tan, bkz. `03`). |
| `PriceFetchService` | 2 | Dış fiyat API client + cache + fallback (son bilinen fiyat). |
| `NudgeRuleEngine` | 2 | Kural tabanlı eğitici notlar (örn. nakit oranı eşiği). |
| `LlmCommentaryService` | 3 | Hazır sayıları prompt'a koyar, JSON çıktı alır, güvenli parse + cache. |
| `StockDataService` | 4 | Sembolle metrik çek (F/K, PD/DD, ...). |
| `LlmStockExplainService` | 4 | Metrikleri açıklatır (tavsiye yok). |

### 2.3 Teknoloji Kararları

| Konu | Karar | Gerekçe |
|------|-------|---------|
| Framework | **.NET 10 Web API** (LTS) | Geliştiricinin güçlü alanı; LTS. (Kurulu SDK 10.0.300; .NET 8 runtime yok → `net10.0` hedeflendi. Çözüm `.slnx`.) |
| ORM | **Entity Framework Core** | `ROADMAP.md` Faz 0 önerisi; migration kolay. |
| Veritabanı | **PostgreSQL** (öncelik) veya SQL Server | PostgreSQL ücretsiz, prod-dostu, `decimal`/`numeric` güçlü. Karar `03`'te. |
| Para tipi | **`decimal`** (her yerde) | NFR-1. Asla float/double. |
| API stili | **REST + JSON** | Mobil için yeterli, basit. Sözleşme `04`'te. |
| Kimlik doğrulama | Faz 1: yok (tekil kullanıcı). Faz 5: JWT/OAuth | Erken fazda gereksiz karmaşa. |
| Loglama | `ILogger` + yapılandırılmış log | Dış API hatalarını izlemek için. |
| Config/secret | `appsettings` + **User Secrets** (dev) / env (prod) | API anahtarı koda gömülmez (NFR-4). |

---

## 3. Frontend Mimarisi (monorepo, web-öncelikli)

İki ön yüz, **tek API**, paylaşılan paket. Detay: [`13-WEB-FRONTEND.md`](13-WEB-FRONTEND.md).

```
finans/ (monorepo · pnpm workspaces)
├── backend/           ← .NET Web API (tek API)
├── packages/shared/   ← @finans/shared: API tipleri (04), tasarım token'ları
│                          (DESIGN.md), format util'leri (NFR-7). Hesap YOK.
├── web/   ★ BİRİNCİL   ← ReactJS + Vite SPA (13)
└── mobile/  (sonra)   ← React Native / Expo (§3.1, 05)
```

- **Web birincil yüzeydir** (karar). Mobil aynı API + `@finans/shared` üzerine
  sonradan eklenir.
- **Paylaşım sözleşme düzeyinde:** tip/token/format paylaşılır; sunum (DOM vs
  RN) platforma özel. RN-for-web kullanılmaz (bilerek).
- Web ve mobil **aynı TanStack Query hook'larını** (`@finans/shared/api`)
  tüketir → tutarlı veri katmanı.

### 3.1 Mobil Mimari (React Native) — sonraki kol

| Konu | Karar | Gerekçe |
|------|-------|---------|
| Çatı | **Expo (managed)** | RN'de yeni geliştirici için en kolay başlangıç (`ROADMAP.md` Faz 0). |
| Dil | **TypeScript** | Tip güvenliği; backend DTO'larıyla uyumlu tipler. |
| Navigasyon | **React Navigation** (bottom tabs + stack) | Taslaktaki 4 sekme + overlay akışına birebir uyar. |
| Sunucu durumu | **TanStack Query (react-query)** | Cache, yenileme, loading/err durumları hazır (NFR-5/6). |
| İstemci durumu | Hafif: Context veya **Zustand** | Baz para birimi, tema gibi global küçük durum. |
| Grafik (donut) | **react-native-svg** veya `react-native-gifted-charts` | `conic-gradient` RN'de yok (`DESIGN.md` § 6). |
| Tema | `mobile/src/theme/` tek token dosyası | `DESIGN.md` § 6 — `:root` değişkenlerinin RN karşılığı. |
| Form | Controlled + basit validasyon | Varlık ekleme formu (FR-1.1). |
| Para/sayı formatı | `Intl.NumberFormat('tr-TR')` yardımcı | NFR-7. Tek util'den geçir. |

Klasör iskeleti (öneri):

```
mobile/
└── src/
    ├── theme/         ← colors.ts, typography.ts, spacing.ts (DESIGN.md §6)
    ├── api/           ← backend client (fetch wrapper, react-query hooks)
    ├── components/    ← HeroCard, AllocationDonut, HoldingRow, Nudge, Disclaimer...
    ├── screens/       ← PortfolioScreen, AnalysisScreen, StockScreen, EducationScreen
    ├── overlays/      ← AssetDetailSheet, AddAssetSheet
    ├── navigation/    ← tab + stack tanımı
    └── utils/         ← formatCurrency, formatPercent (tr-TR)
```

Bileşen → ekran eşlemesi [`05-MOBILE-SPEC.md`](05-MOBILE-SPEC.md)'te.

---

## 4. Veri Akışı (Faz 1 örneği — portföy özeti)

```
[Mobil] PortfolioScreen mount
   │  GET /api/portfolio/summary?baseCurrency=TRY
   ▼
[Api] PortfolioController
   │  → HoldingService: kullanıcının holdings'i (DB'den)
   │  → CurrencyConversionService: her varlığı TRY'ye çevir
   │  → PortfolioCalculationService: toplam, kâr, getiri%, dağılım, reel getiri
   ▼
[Api] PortfolioSummaryDto (tüm sayılar HAZIR, decimal)
   │  JSON
   ▼
[Mobil] react-query cache → HeroCard / Donut / HoldingRow (sadece formatla & göster)
```

Faz 3'te bu özetin üstüne: `GET /api/portfolio/commentary` → backend aynı hazır
sayıları **LLM'e yorumlatıp** JSON kart döner (yeni sayı üretmeden).

---

## 5. Güvenlik & Uyum (baştan)

> Tam güvenlik mimarisi (tehdit modeli, per-user izolasyon, sırlar, KVKK,
> testler): [`11-SECURITY.md`](11-SECURITY.md). Özet:

- **HTTPS zorunlu** (mobil ↔ backend).
- **Secret yönetimi:** dev'de .NET User Secrets, prod'da ortam değişkeni / vault.
  API anahtarı asla repoda (NFR-4). `.gitignore`'a `appsettings.*.local.json`,
  `*.env`.
- **KVKK (NFR-3):** Veri minimizasyonu (gereksiz kişisel veri toplama),
  şifreli saklama (DB at-rest + hassas alanlar), "verimi sil" yolu Faz 5'te
  hesap gelince. Faz 1-4'te kişisel veri minimum (portföy kalemleri).
- **Girdi doğrulama:** Tüm POST/PUT DTO'ları sunucuda doğrulanır
  (miktar > 0, geçerli para birimi, vb.).
- **Hata sızdırma yok:** İstemciye stack trace değil, sözleşmeli hata (`04` § Hata).

---

## 6. Dağıtım (Deployment) — self-hosted / VPS + Docker

**Karar:** En düşük maliyet için **tek VPS + Docker Compose**, tamamen açık
kaynak yığın. Detaylı topoloji ve ölçeklenme: [`10`](10-PERFORMANCE-SCALABILITY.md) §5.

```
VPS (Docker Compose):
  Reverse Proxy (Traefik/Caddy: TLS + rate limit)
    → API replica(lar) (stateless)
        → PostgreSQL   (veri)
        → Redis        (cache, Faz 2+)
  Gözlemlenebilirlik: Seq + Prometheus + Grafana (12)
```
- Backend: container (Docker), **non-root**, minimal imaj (`11` §8).
- DB: self-host PostgreSQL (container/volume), yedek şifreli.
- Mobil: Expo EAS build → App Store / Play Store (Faz 5, hukuki onay sonrası).
- **Önce dikey ölçek**, sonra proxy arkasında yatay replika (`10` §5).
- Faz 1-4 lokal/dev yeterli; tam VPS dağıtımı Faz 5'te bağlanır, **ama desenler
  (stateless, cache, log, health) baştan** (`10`/`11`/`12`).

---

## 7. Mimari "Yapma" Listesi (Anti-patterns)

- ❌ LLM'e ham sayı verip hesap yaptırma. (Halüsinasyon → yanlış finansal rakam.)
- ❌ Mobilde parasal hesap yapma. (Tek doğruluk kaynağı backend.)
- ❌ `float`/`double` ile para. (`decimal` zorunlu.)
- ❌ Dış API'ye fallback'siz bağımlılık. (Çökerse uygulama çöker.)
- ❌ API anahtarını koda gömme.
- ❌ Erken mikroservis / aşırı soyutlama. Tek modüler monolit yeterli.
