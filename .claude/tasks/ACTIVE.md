# ACTIVE — Aktif Görevler (anlık durum)

> Şu an üzerinde çalışılan / sıradaki görevlerin küçük anlık görüntüsü. Oturum
> başında hook bunu otomatik gösterir. Kaynak plan: [`../docs/08-BACKLOG.md`](../docs/08-BACKLOG.md).
> Bir görev bitince buradan çıkar, backlog'da `[x]` işaretle, TASKLOG'a girdi ekle.

**Aktif faz:** ✅ Faz 0 BİTTİ · ✅ **Faz 1 — Portföy Takip MVP BİTTİ** → **Faz 2 — Canlı fiyat + nudge**

**Faz 2 işlevsel DoD ✅ KARŞILANDI** (otomatik güncel değer + yenileme; ≥1 not tetiklenir; dış API çökünce
çökme yok). Kalan: dağıtım/gözlem altyapısı.

## Sıradaki (öncelik sırası) — Faz 2 altyapı
1. **T2.8** — Gözlemlenebilirlik yığını (Compose'a Seq + Prometheus + Grafana; OTel metrik exporter →
   `Finans.Cache` hit/miss + RED + bağımlılık; ilk dashboard/alarm)
2. **T2.9** — Reverse proxy + rate limit (Traefik/Caddy TLS; güvenlik başlıkları)

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
- **Yeşil kapı:** backend **98** (Application 45 + Integration 53) · web **37** · shared **13** · eslint 0 hata · tsc/vite temiz

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
