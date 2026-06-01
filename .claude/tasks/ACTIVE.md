# ACTIVE — Aktif Görevler (anlık durum)

> Şu an üzerinde çalışılan / sıradaki görevlerin küçük anlık görüntüsü. Oturum
> başında hook bunu otomatik gösterir. Kaynak plan: [`../docs/08-BACKLOG.md`](../docs/08-BACKLOG.md).
> Bir görev bitince buradan çıkar, backlog'da `[x]` işaretle, TASKLOG'a girdi ekle.

**Aktif faz:** ✅ Faz 0 BİTTİ · ✅ **Faz 1 — Portföy Takip MVP BİTTİ** → **Faz 2 — Canlı fiyat + nudge**

**Faz 2 işlevsel DoD ✅ KARŞILANDI** (otomatik güncel değer + yenileme; ≥1 not tetiklenir; dış API çökünce
çökme yok). Kalan: dağıtım/gözlem altyapısı.

## Sıradaki (öncelik sırası)
0. ⚠ **`AddBesBirthYear` migration'ını canlı Postgres'e uygula** (VS'den Update-Database; additive nullable). Kod tarafı tüm test paketleri yeşil — yalnız DB uygulanması bekliyor.
1. **T2.8** — Gözlemlenebilirlik yığını (Seq + Prometheus + Grafana; OTel metrik → `Finans.Cache` + RED)
2. **T2.9** — Reverse proxy + rate limit (Traefik/Caddy TLS)
3. T-BES.6b ileri (otomatik zamanlayıcı/plan kalıcılığı — uygulama kapalıyken arka plan job)

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
