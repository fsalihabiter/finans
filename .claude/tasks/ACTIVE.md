# ACTIVE — Aktif Görevler (anlık durum)

> Şu an üzerinde çalışılan / sıradaki görevlerin küçük anlık görüntüsü. Oturum
> başında hook bunu otomatik gösterir. Kaynak plan: [`../docs/08-BACKLOG.md`](../docs/08-BACKLOG.md).
> Bir görev bitince buradan çıkar, backlog'da `[x]` işaretle, TASKLOG'a girdi ekle.

**Aktif faz:** ✅ Faz 0 BİTTİ · ✅ **Faz 1 — Portföy Takip MVP BİTTİ** → **Faz 2 — Canlı fiyat + nudge**

## Sıradaki (öncelik sırası) — Faz 2
1. **T2.4** — `GET /api/prices` (RefreshAsync'i tetikler; `stale`/`asOf`/`source` yüzeye) + summary'i
   canlı fiyatla besle. **← Web görünürlüğü burada başlar.** · **T2.5** `NudgeRuleEngine` + `GET /nudges`
2. **T2.6** — Web: yenile butonu + "son güncelleme/yaklaşık" etiketi + Nudge kartı

> ✅ **T2.1+T2.2+T2.3 bitti** — sağlayıcılar (Frankfurter+Truncgil, anahtarsız) + `PriceFetchService`
> (CanQuote yönlendirme + 10 dk cache + snapshot/fxrate/CurrentPrice yazımı) + fallback (çöken kaynakta
> son-bilinen `stale`, çökme yok). Bkz. TASKLOG 2026-05-31.
> Altyapı (Faz 2 boyunca): T2.7 Redis cache · T2.8 Seq/Prometheus/Grafana · T2.9 reverse proxy+rate limit.

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
- **Yeşil kapı:** backend **85** (Application 39 + Integration 46) · web **33** · shared **13** · eslint 0 hata

## Faz 2 tamamlananlar (özet)
- **T2.1:** Fiyat sağlayıcı seçimi + `IPriceProvider`. Döviz=Frankfurter (ECB, anahtarsız, doğrudan
  TRY kuru), Altın=Truncgil (TR gram, yerel primli). `PriceInstrument`/`PriceQuote` sözleşmesi;
  typed HttpClient + DI (`IEnumerable<IPriceProvider>`); 8 sağlayıcı testi (SC-17, HTTP stub).
- **T2.2:** `PriceFetchService` (`IPriceFetchService.RefreshAsync`) — CanQuote yönlendirme + sağlayıcı
  izolasyonu (`FailedSources`); 10 dk in-memory cache (`prices:refresh`); yazım → `PriceSnapshots`
  (geçmiş) + `FxRates` (converter) + `Holding.CurrentPrice` (okuma yolu, global). SC-18 (3 senaryo).
- **T2.3:** Fallback — sağlayıcı çökünce DB'den son-bilinen `PriceQuote.IsStale` tırnak (`HasStale`);
  bayat geçmişe yazılmaz; çöken kaynakta kısa retry-TTL (1 dk). Çökme yok (NFR-5). SC-08.

## Devam eden / Bloke
- (yok)

---
*Güncelleme kuralı: CLAUDE.md §11. Bu dosya kısa kalmalı — detay TASKLOG.md'de,
tam plan 08-BACKLOG.md'de.*
