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

## 2026-05-30 · Web görsel yükseltme — taslak referanslı zengin pano (T1.19)
- **Görev(ler):** T1.19 (tamam) — kullanıcı: "taslağı referans alıp daha güzel/görsel-yüksek frontend"
- **Ne yapıldı:** `portfoy-web-panosu-taslak.html` tasarım dili Faz 1'in **gerçek-veri** ekranlarına uygulandı
  (gelecek faz ekranları kurgusal koyulmadı). Yeni:
  - **assetMeta** (varlık türü → ikon + renk, ortak). **App.css** baştan tasarım sistemi olarak yazıldı.
  - **Sidebar:** marka mark'ı (◆), ikonlu gezinme + bölüm etiketleri, gradient "Varlık Ekle" (kabuk-seviyesi
    modal), kullanıcı + sağlık. **Topbar:** greeting + serif başlık + badge.
  - **Pano:** `KpiGrid` (glow'lu hero + 4 KPI), zengin `AllocationDonut` (merkez etiket), `PortfolioInsights`
    (En İyi/Zayıf + Hızlı Bilgi + Yoğunlaşma + **computed nudge** — top-2 yoğunlaşma > %50). `HoldingsTable`
    ikon + alt-bilgi + **renkli ağırlık çubuğu**. Tümü gerçek veriden.
  - **Detay:** ikon başlık + `detail-hero` + drow'lar + **BES split** (kendi/devlet katkı açıklamalı) +
    başlangıç tarihi + işlem geçmişi + formlar. **Ayarlar** sayfası (yeni route, para birimi chip'leri + disclaimer).
  - **Modal** (Varlık Ekle) yeni başlık/kapat yapısı. `vite.config` proxy hedefi `VITE_API_TARGET` ile ayarlanabilir.
- **Dokunulan dosyalar:** `web/src/App.css` (rewrite), `web/src/App.tsx`, `web/src/main.tsx`,
  `web/src/lib/assetMeta.ts`, `web/src/components/{KpiGrid,PortfolioInsights,AllocationDonut,HoldingsTable,
  AddHoldingDialog}.tsx`, `web/src/routes/{PortfolioPage,HoldingDetailPage,SettingsPage}.tsx`,
  ilgili testler, `web/vite.config.ts`.
- **Test:** web **19 yeşil** (testler yeni yapıya güncellendi), shared 13, `tsc` + `vite build` temiz.
  **Canlı görsel doğrulama:** kendi Release backend'im (5310, VS Debug kilidine takılmadan) + Vite (5180)
  ile pano/holdings/BES-detay ekran görüntüleriyle onaylandı — tüm tasarım taslakla birebir, gerçek veriyle.
- **Karar/Not:** Gelecek faz ekranları (Senaryo/Hisse/Eğitim/performans grafiği) backend gerektirdiği için
  yapılmadı. Para-birimi maruziyeti kartı atlandı (Fx pozisyonları TRY-fiyatlı → model temiz exposure vermiyor).
- **Durum:** tamamlandı
- **Sıradaki:** Faz 2 — T2.1 fiyat sağlayıcı + `IPriceProvider`.

## 2026-05-30 · BES özel modeli (T1.17) + pozisyon işlem geçmişi (T1.18) + BES veri onarımı
- **Görev(ler):** T1.17, T1.18 (tamam) — kullanıcı bildirimi: "BES'e işlem ekledim tüm değerler bozuldu"
- **Kök neden:** BES nominal hesap (kendi+devlet katkısı + fon getirisi), "alış/satış → ağırlıklı ort.
  maliyet" modeline uymuyor. Genel `AddTransaction` BES'e işlem ekleyince maliyeti işlemlerden yeniden
  türetip 148.554→4.250'ye düşürdü (getiri +%6.473!). Seed BES'i de transaction'sız nominal'di.
- **Ne yapıldı:**
  - **Veri onarımı (SQL):** bozuk BES işlemi silindi, `AvgCost` 148.554'e geri (kendi 120k + devlet 28.554).
  - **T1.17 BES modeli (backend):** `AddTransaction` BES'i reddediyor (400, "aylık katkı kullanın").
    Yeni `POST /holdings/{id}/bes-contribution` (kendi + devlet; verilmezse %30 TR kuralı) → `BesDetails`
    büyür, `AvgCost = own + state`. `BesDto`'ya `JoinedAtUtc` (başlangıç, nullable). Web: BES'te
    "Aylık katkı ekle" formu (alış/satış yerine) + %30 önizleme + başlangıç tarihi gösterimi.
  - **T1.18 işlem geçmişi:** `GET /holdings/{id}` → `transactions` listesi (en yeni üstte); web
    `TransactionHistory` tablosu her pozisyonun detayında. BES'te boş (işlem yok → katkı özeti gösterilir).
- **Dokunulan dosyalar:** `Finans.Application/Portfolio/{PortfolioDtos,IHoldingService}.cs`,
  `Finans.Infrastructure/Services/HoldingService.cs`, `Finans.Api/Controllers/HoldingsController.cs`,
  `packages/shared/src/{types,api}/index.ts`, `web/src/lib/hooks.ts`,
  `web/src/components/{BesContributionForm,TransactionHistory}.tsx` (+test),
  `web/src/routes/HoldingDetailPage.tsx`, `web/src/App.css`,
  `tests/Finans.Integration.Tests/BesAndHistoryApiTests.cs`, `.claude/docs/03-DATA-MODEL.md` §11.
- **Test:** backend **Application 39 + Integration 35 = 74 yeşil** (+4: BES reddi/katkı/geçmiş/başlangıç),
  web **19** (+4 BES form/geçmiş), shared 13, tsc temiz. **Release konfigürasyonunda** build/test edildi —
  kullanıcı app'i VS Debug'da çalıştırdığı için `bin/Debug` kilitli; `bin/Release` ayrı → kilide takılmadan.
- **Karar/Not:** BES ort.-maliyet kuralının istisnası (03 §11'e işlendi). Kullanıcının VS Debug instance'ı
  ESKİ backend → yeni BES uçlarını görmek için **VS debug oturumunu yeniden başlatması** gerekir (frontend HMR).
  BES katkı geçmişi (her katkı kaydı) ileride; şimdilik kümülatif kendi/devlet gösteriliyor.
- **Durum:** tamamlandı
- **Sıradaki:** Faz 2 — T2.1 fiyat sağlayıcı + `IPriceProvider`.

## 2026-05-30 · Mevcut pozisyona alış/satış işlemi ekleme UI'ı (T1.16)
- **Görev(ler):** T1.16 (tamam) — kullanıcı bildirimi: "pozisyonlara ekleme/çıkarma yapamıyorum"
- **Ne yapıldı:** **Gerçek eksik** — backend `POST /holdings/{id}/transactions` + shared `addTransaction`
  + `useAddTransaction` hook'u T1.6'dan beri vardı ama **çağıran UI yoktu** (detayda yalnız fiyat-güncelle/sil).
  - **`AddTransactionForm`:** Alış (ekleme) / Satış (çıkarma) segment + miktar + birim fiyat →
    `useAddTransaction`. Backend ort. maliyet/miktarı işlemlerden yeniden türetir (T1.5); fazla satış→400
    (hata zarfı gösterilir). Başarıda holding + summary + holdings invalidate → sayfa tazelenir.
  - `HoldingDetailPage`'e bağlandı (fiyat-güncelle formunun üstünde). CSS: alış/satış segment (yeşil/kırmızı).
- **Dokunulan dosyalar:** `web/src/components/AddTransactionForm.tsx` (+test),
  `web/src/routes/HoldingDetailPage.tsx`, `web/src/App.css`, `.claude/docs/08-BACKLOG.md`.
- **Test:** web **15 yeşil** (+3: alış POST gövdesi, satış type=Sell, eksikte pasif), `tsc` temiz.
- **Karar/Not:** Görsel doğrulama atlandı — kullanıcı app'i Visual Studio'da debug'da çalıştırıyor
  (Finans.Api PID 14732, parent VsDebugConsole, 5298'i tutuyor) → kendi backend'imi başlatamadım,
  onunkine de dokunmadım. Endpoint zaten o instance'ta mevcut; frontend HMR/rebuild ile görünür.
- **Durum:** tamamlandı
- **Sıradaki:** Faz 2 — T2.1 fiyat sağlayıcı + `IPriceProvider`.

## 2026-05-30 · Faz 1 loose-end taraması — Disclaimer (NFR-2, SC-W2) + bayat işaretler
- **Görev(ler):** Faz 1 kapanış denetimi (kullanıcı "atlanan adım var mı?")
- **Ne yapıldı:**
  - **Denetim:** Faz 0 + Faz 1 tüm T-görevleri `[x]`. Açık SC'lerin çoğu sonraki fazlar
    (SC-08 fiyat/Faz 2, SC-10/11 LLM, SC-12 hisse, SC-14/16 kimlik). Faz 1'e ait 3 loose-end bulundu.
  - **SC-W2 (NFR-2 — #1 kural):** gerçek boşluktu → `Disclaimer` bileşeni ("yatırım tavsiyesi
    değildir") + `AnalysisPage`'de her zaman görünür + RTL testi. (Daha önce Faz 3'e ertelenmişti.)
  - **Bayat işaretler düzeltildi:** SC-W1 (format testi T1.10'da vardı), SC-15 (hata-maskeleme testi
    T0.13'te vardı), T1.9 (`[~]`→`[x]`; web seçici T1.11'de yapılmıştı) → katalog/backlog gerçeğe çekildi.
  - **SC-W3 (Web E2E)** bilinçli ertelendi → Faz 2 (Playwright gerçek akış, iki-sunucu orkestrasyonu).
- **Dokunulan dosyalar:** `web/src/components/Disclaimer.tsx`, `web/src/routes/AnalysisPage.tsx` (+test),
  `web/src/App.css`, `.claude/docs/{08-BACKLOG,09-TESTING-STRATEGY}.md`.
- **Test:** web **12 yeşil** (+1 disclaimer SC-W2), `tsc` temiz.
- **Karar/Not:** Faz 1'de kalan tek bilinçli erteleme **SC-W3 web E2E** (Faz 2'de iki sunucu ayakta
  iken). Geri kalan her şey kapalı.
- **Durum:** tamamlandı
- **Sıradaki:** Faz 2 — T2.1 fiyat sağlayıcı + `IPriceProvider`.

## 2026-05-30 · Seed verisini çeşitlendir (4→7 pozisyon, zarar + USD-fiyatlı hisse)
- **Görev(ler):** ad-hoc (kullanıcı isteği) — seed zenginleştirme
- **Ne yapıldı:**
  - **Seed 4→7 pozisyon:** mevcut (altın/dolar/BES/nakit) + **Euro** (800 € @47,50) + **Apple** (12 @175 $,
    **USD-fiyatlı** → summary'de gerçek USD→TRY ×48 çevrimi) + **Teknoloji Fonu** (1.500 @28,00, güncel
    23,50 → **−%16,1 zarar**, eğitici negatif örnek). Yeni `Asset`/`Holding`/`Transaction`/`PriceSnapshot`.
  - **Yeni baz TRY toplamları:** maliyet 603.770 · değer 839.213 · kâr +235.443 · **+%39,0** · reel **+%0,7**.
  - **Testler güncellendi:** DB-seed okuyan 4 integration testi yeni sayılara çekildi (USD-fiyatlı kalem
    ×48 çevrilerek baz-TRY toplamı). PortfolioApiTests'e AAPL kur-çevrimi + fon negatif-getiri kontrolü
    eklendi. **Application.Tests'e dokunulmadı** (kendi hardcoded "altın formül" setleri, DB'den bağımsız).
  - Yerel PostgreSQL TRUNCATE + `dotnet run -- seed` ile yeniden tohumlandı; **canlı UI görsel doğrulandı**
    (7-dilim donut, Apple $→₺ çevrimi, fon kırmızı zarar).
- **Dokunulan dosyalar:** `Finans.Infrastructure/Seed/SeedData.cs`, `tests/Finans.Integration.Tests/
  {SeedConsistencyTests,SqliteIntegrationTests,PortfolioApiTests,InflationRealReturnTests}.cs`,
  `.claude/docs/03-DATA-MODEL.md` §12.
- **Test:** `dotnet test` **Application 39 + Integration 31 = 70 yeşil**. Görsel doğrulandı.
- **Karar/Not:** Apple USD-fiyatlı tutuldu → summary'de kur çevrimi artık gerçek veriyle de test ediliyor.
  Build sırasında görsel-doğrulama koşumundan kalan orphan `Finans.Api` süreci DLL kilitlemişti → sonlandırıldı
  (ileride TaskStop sonrası child süreç kontrolü).
- **Durum:** tamamlandı
- **Sıradaki:** Faz 2 — T2.1 fiyat sağlayıcı + `IPriceProvider`.

## 2026-05-30 · Canlı görsel doğrulama + FX/enflasyon in-memory cache (T1.15) → FAZ 1 MVP TAMAM
- **Görev(ler):** T1.15 (cache, tamam), T1.8 (BES detay ekranı, tamam) · Faz 1 kapanışı
- **Ne yapıldı:**
  - **Görsel doğrulama:** backend (5298) + Vite (5173) canlı; PostgreSQL'e karşı portföy sayfası,
    HeroCard, donut+lejant, Pozisyonlar tablosu ve "Varlık Ekle" modalı ekran görüntüsüyle doğrulandı
    (tr-TR biçim, renkler, BES devlet katkısı ayrı). Uçtan uca zincir çalışıyor.
  - **Cache (T1.15):** `CachedFxRateProvider` + `CachedInflationRateProvider` decorator'ları
    (`IMemoryCache`, 60 sn TTL, global anahtar — kullanıcı-bağımsız). `AddMemoryCache` + DI'da
    Ef sağlayıcı → cache decorator sarması. §13 "dış çağrı/DB cache'lenir" kapısı.
  - **T1.8:** BES detay ekranında "Kendi katkın / Devlet katkısı (ayrı) / Hak ediş" gösterimi.
- **Dokunulan dosyalar:** `Finans.Infrastructure/Services/{CachedFxRateProvider,CachedInflationRateProvider}.cs`,
  `Finans.Infrastructure/DependencyInjection.cs`, `tests/Finans.Integration.Tests/ProviderCacheTests.cs`.
- **Test:** cache testi (TTL içinde DB'ye güncel kur eklendi → sağlayıcı hâlâ eski/cached değer döndü =
  cache hit kanıtı). `dotnet test` **Application 39 + Integration 31 = 70 yeşil**. Web 11 + shared 13.
  Görsel: canlı PostgreSQL'e karşı tüm ekranlar doğrulandı.
- **Karar/Not:** Per-user summary'nin **server** cache'i ertelendi — tek kullanıcılı dev'de React
  Query istemcide tazeliyor; gerekince UserId anahtarlı server cache + mutation invalidation eklenir.
  Playwright e2e gerçek akış güncellemesi Faz 2'ye (iki sunucu orkestrasyonu).
- **Durum:** tamamlandı → **FAZ 1 — Portföy Takip MVP TAMAM**
- **Sıradaki:** Faz 2 — canlı fiyat API'si + bağlama-duyarlı eğitici notlar (nudge).

## 2026-05-30 · Web: "Varlık Ekle" modal formu → POST /holdings (T1.14) — Faz 1 web seti bitti
- **Görev(ler):** T1.14 (tamam)
- **Ne yapıldı:** `AddHoldingDialog` — özel overlay modal (jsdom-dostu, role=dialog/aria-modal,
  Escape/dış tık kapatma). Form: tür (preset birim), ad, sembol, para birimi, birim, miktar,
  alış birim fiyatı → `useCreateHolding` POST /holdings. İstemci doğrulama (ad/birim/miktar>0/
  fiyat≥0) + backend hata zarfı (çakışma/validasyon) gösterimi. PortfolioPage'e "+ Varlık Ekle"
  butonu. Üretim `vite build` temiz (343 kB / 108 kB gzip).
- **Dokunulan dosyalar:** `web/src/components/AddHoldingDialog.tsx` (+test),
  `web/src/routes/PortfolioPage.tsx`, `web/src/App.css`.
- **Test:** web **11 yeşil** (+3: kapalı render yok, doldur→POST gövdesi doğru→kapan, eksikte pasif),
  `tsc` + `vite build` temiz.
- **Karar/Not:** Faz 1'in tüm web ekranları (T1.11-14) tamam; başarı zinciri kullanıcı→form→
  POST→invalidate→özet/tablo tazelenir. Sıradaki: canlı görsel doğrulama + T1.15 cache.
- **Durum:** tamamlandı
- **Sıradaki:** Canlı görsel doğrulama (web+backend), sonra T1.15 in-memory cache.

## 2026-05-30 · Web: Holdings tablosu + varlık detay (T1.13)
- **Görev(ler):** T1.13 (tamam)
- **Ne yapıldı:** `HoldingsTable` (gerçek tablo; dar ekran yatay kaydırma; ad→detay link, tr-TR
  biçim, pozitif/negatif renk). `HoldingDetailPage` (route `/holdings/:id`): metrikler + BES
  (devlet katkısı ayrı) + **güncel fiyat güncelle** (FR-1.8, `useUpdateHolding`) + **sil**
  (`useDeleteHolding` → portföye dön). `formatNumber` (tr-TR miktar) shared'a eklendi.
  PortfolioPage'e "Pozisyonlar" bölümü.
- **Dokunulan dosyalar:** `web/src/components/HoldingsTable.tsx` (+test),
  `web/src/routes/{HoldingDetailPage,PortfolioPage}.tsx`, `web/src/main.tsx` (route),
  `web/src/App.css`, `packages/shared/src/format/index.ts` (formatNumber).
- **Test:** web **8 yeşil** (+2 tablo: link/biçim, boş), shared 13, `tsc` temiz.
- **Karar/Not:** Detay route olarak yapıldı (modal değil) — derin link + geri tuşu doğal.
  Holdings'in dar-ekran kart varyantı yatay kaydırmaya bırakıldı (MVP).
- **Durum:** tamamlandı
- **Sıradaki:** T1.14 "Varlık Ekle" formu (modal) → POST /holdings.

## 2026-05-30 · Web: AllocationDonut (SVG) + legend (T1.12)
- **Görev(ler):** T1.12 (tamam)
- **Ne yapıldı:** `AllocationDonut` — SVG donut (her dilim ağırlığı kadar yay, varlık-türü
  renkleri DESIGN.md token'ından) + lejant (ad / işaretsiz % / değer). `role="img"` + aria-label
  (erişilebilir özet). PortfolioPage'de hero ile yan yana (geniş ekran grid). `formatPercent`'e
  `signed` parametresi eklendi → ağırlıkta "+" yok (getiride var).
- **Dokunulan dosyalar:** `web/src/components/AllocationDonut.tsx` (+test), `web/src/routes/PortfolioPage.tsx`,
  `web/src/App.css`, `packages/shared/src/format/index.ts` (+test).
- **Test:** shared **13 yeşil** (signed=false), web **6 yeşil** (+3 donut: ad/%, aria, boş), `tsc` temiz.
- **Durum:** tamamlandı
- **Sıradaki:** T1.13 Holdings tablo/kart + varlık detay.

## 2026-05-30 · Web: AppShell (sidebar) + HeroCard + summary bağlama + para birimi seçici (T1.11)
- **Görev(ler):** T1.11 (tamam); T1.9'un web ayağı (para birimi seçici) da burada
- **Ne yapıldı:**
  - **AppShell** sidebar düzenine geçti (geniş ekran sol sidebar; dar ekran üst bar — responsive grid).
  - **HeroCard:** toplam değer (büyük) + net kâr / getiri / reel getiri / maliyet; pozitif/negatif renk.
  - **CurrencySelector:** TRY/USD/EUR segment; seçim `useUpdateSettings` ile tercihi günceller →
    summary/holdings invalidate → seçilen baz pb'ye göre yeniden hesaplanır.
  - **PortfolioPage** veri bağlandı (`usePortfolioSummary`/`useSettings`); loading/error/boş durum.
  - CSS: sidebar, hero, seçici, sayfa başlığı (DESIGN.md token'ları). Test yardımcısı
    `renderWithProviders` (QueryClient sarmalı).
- **Dokunulan dosyalar:** `web/src/App.tsx`, `web/src/App.css`,
  `web/src/components/{HeroCard,CurrencySelector}.tsx`, `web/src/routes/PortfolioPage.tsx`,
  `web/src/routes/PortfolioPage.test.tsx`, `web/src/test/renderWithProviders.tsx`.
- **Test:** web **3 yeşil** (HeroCard tr-TR biçim + seçici aria-pressed; fetch mock), `tsc` temiz.
- **Karar/Not:** Görsel doğrulama web seti (T1.12-14) bitince topluca yapılacak. Donut (T1.12) ve
  holdings tablosu (T1.13) sayfaya eklenecek.
- **Durum:** tamamlandı
- **Sıradaki:** T1.12 `AllocationDonut` (SVG/conic) + legend.

## 2026-05-30 · `@finans/shared` API tipleri + istemci + web React Query hook'ları (T1.10)
- **Görev(ler):** T1.10 (tamam)
- **Ne yapıldı:**
  - **shared/types:** 04 sözleşmesi tipleri — `AssetType`/`TransactionType`/`VestingState`,
    `Holding`/`Bes`/`PortfolioSummary`/`AllocationSlice`, `CreateHoldingInput`/`TransactionInput`/
    `UpdateHoldingInput`, `Settings`, hata zarfı (`ApiErrorEnvelope`).
  - **shared/api:** `createApiClient` genişletildi (summary/holdings CRUD/transactions/settings);
    `request` artık hata zarfını çözüp `ApiError{status,code,message}` fırlatıyor, 204'ü gövdesiz çözüyor.
  - **web/lib/hooks.ts:** TanStack Query hook'ları (usePortfolioSummary/useHoldings/useHolding/
    useSettings + create/addTx/update/delete/updateSettings mutation'ları; query key'ler + invalidation).
  - **vitest:** `include` yalnızca `src` → Playwright `e2e/*.spec.ts` artık vitest'e sızmıyor.
- **Dokunulan dosyalar:** `packages/shared/src/{types,api}/index.ts`, `packages/shared/src/api/api.test.ts`,
  `web/src/lib/hooks.ts`, `web/vite.config.ts`.
- **Test:** shared **12 yeşil** (4 yeni api: baseCurrency URL, hata zarfı parse, 204, gövdesiz hata),
  web **2 yeşil**, shared+web `tsc --noEmit` temiz.
- **Karar/Not:** Hook'lar şimdilik web'de (shared'ı React'a bağımlı kılmamak için); mobil (Faz M)
  gelince shared'a taşınır. Vite proxy backend'i `http://localhost:5298` bekliyor (web `pnpm dev`).
- **Durum:** tamamlandı
- **Sıradaki:** T1.11 Web AppShell + HeroCard + summary bağlama + para birimi seçici.

## 2026-05-30 · Settings endpoint — baz para birimi (T1.9 backend)
- **Görev(ler):** T1.9 (backend kısmı tamam; web seçimi T1.11'de)
- **Ne yapıldı:** `GET/PUT /api/settings` (04 §4) — `ISettingsService`/`SettingsService` (kullanıcıya
  kapsanır), `SettingsDto`/`UpdateSettingsRequest`, `SettingsController`. PUT `User.BaseCurrency`'i günceller.
- **Dokunulan dosyalar:** `Finans.Application/Portfolio/ISettingsService.cs`,
  `Finans.Infrastructure/Services/SettingsService.cs`, `Finans.Infrastructure/DependencyInjection.cs`,
  `Finans.Api/Controllers/SettingsController.cs`, `tests/Finans.Integration.Tests/SettingsApiTests.cs`.
- **Test:** 3 integration (GET seed TRY; PUT→USD kalıcı; ayar kullanıcılar arası sızmıyor).
  `dotnet test` **Application 39 + Integration 30 = 69 yeşil**.
- **Karar/Not:** Web tarafı (para birimi seçici) T1.11 AppShell ile gelecek.
- **Durum:** tamamlandı (backend)
- **Sıradaki:** T1.10 `@finans/shared` (API tipleri + TanStack Query hook + tr-TR format).

## 2026-05-30 · Portföy API: Holdings CRUD + summary (T1.6 + T1.7) — ilk gerçek endpoint'ler
- **Görev(ler):** T1.6 (tamam), T1.7 (tamam) · ayrıca T1.8 kısmi (bes alanı), T1.15 kısmi (per-user)
- **Ne yapıldı:**
  - **Endpoint'ler (04 §4):** `GET/POST/PUT/DELETE /api/holdings`, `POST /holdings/{id}/transactions`,
    `GET /api/portfolio/summary`. DTO'lar Application'da; servisler (`IHoldingService`/`IPortfolioService`)
    arayüzü Application, EF impl Infrastructure (mevcut sağlayıcı desenine uygun).
  - **Per-user izolasyon (11 §3):** `ICurrentUser` (Faz 1: `X-User-Id` başlığı / `Auth:DevUserId`
    dev varsayılanı; Faz 5 JWT'ye hazır). Her sorgu `WHERE UserId`; başkasının id'si → 404 (IDOR yok).
  - **Validasyon + hata:** DataAnnotations → `InvalidModelStateResponseFactory` ApiError zarfı;
    `AppExceptionHandler` (NotFound→404, Validation→400, Conflict→409) GlobalExceptionHandler'dan önce.
  - **Akış:** create varlığı katalogdan bul/oluştur + ilk işlemle pozisyon; ort. maliyet/miktar
    `DerivePosition` ile (T1.5); summary fx (T1.3) + enflasyon (T1.4) ile baz pb'ye çevirip özetler.
  - **JSON:** enum'lar string (`JsonStringEnumConverter`).
- **Dokunulan dosyalar:** `Finans.Application/Common/{ICurrentUser,ApplicationExceptions}.cs`,
  `Finans.Application/Portfolio/{PortfolioDtos,IHoldingService}.cs`,
  `Finans.Infrastructure/Services/{HoldingMapping,HoldingService,PortfolioService}.cs`,
  `Finans.Infrastructure/DependencyInjection.cs`, `Finans.Api/Auth/HttpCurrentUser.cs`,
  `Finans.Api/ErrorHandling/AppExceptionHandler.cs`, `Finans.Api/Controllers/{Holdings,Portfolio}Controller.cs`,
  `Finans.Api/Program.cs`, `appsettings.Development.json` (Auth:DevUserId),
  `tests/Finans.Integration.Tests/PortfolioApiTests.cs`.
- **Test:** SC-01 (summary/holdings) + SC-04 (BES ayrı) + SC-07 (geçersiz→400) + SC-13 (IDOR→404) —
  9 yeni integration testi. `dotnet test` **Application 39 + Integration 27 = 66 yeşil**. Ayrıca
  **canlı PostgreSQL'e karşı smoke**: summary/holdings/create/IDOR/validasyon/delete doğrulandı,
  test verisi temizlendi (DB seed-tutarlı).
- **Karar/Not:** **EF tuzağı** — `Entity.Id = Guid.CreateVersion7()` initializer'ı yüzünden, izlenen
  bir parent'ın koleksiyonuna eklenen yeni child'ı EF "mevcut" sanıp UPDATE'e çevirir (0 row → sahte
  `DbUpdateConcurrencyException`). `AddTransaction`'da yeni işlem **açıkça `EntityState.Added`** ile
  işaretlendi (`db.Add` create'te grafiği zaten Added yapıyor). Allocation şimdilik kalem-başına dilim
  (seed'de tür=kalem). Cache (T1.15) ve web ekranları (T1.8) sonraki.
- **Durum:** tamamlandı
- **Sıradaki:** T1.9 settings (baz pb) endpoint, sonra T1.10 `@finans/shared` + web (T1.11-14).

## 2026-05-30 · Reel getiri verisi bağlama (T1.4) + ort. maliyet türetimi (T1.5)
- **Görev(ler):** T1.4 (tamam), T1.5 (tamam) · Faz 1 · sıralı
- **Ne yapıldı:**
  - **T1.4 — enflasyon bağlama:** `IInflationRateProvider` (Application) + `EfInflationRateProvider`
    (en güncel dönemin yıllık oranı, PeriodEndUtc max). DI scoped. `RealReturn` saf formülü
    zaten vardı (T1.2); artık seed enflasyonu (0,38) yüklenip summary'ye besleniyor →
    reel getiri ≈ %9,89 (nominal %51,6).
  - **T1.5 — ort. maliyet türetimi:** `PortfolioCalculationService.DerivePosition` (saf, statik):
    `AvgCost = Σ(Buy.Qty×UnitPrice + Buy.Fee)/Σ Buy.Qty`, `Quantity = Σ Buy.Qty − Σ Sell.Qty`.
    **Satış ortalamayı bozmaz**, sadece miktarı düşürür (ort. maliyet yöntemi; FIFO/LIFO Faz 5).
    Yeni saf record'lar: `TransactionInput`, `PositionBasis` (EF entity'sine bağımsız).
- **Dokunulan dosyalar:** `Finans.Application/Portfolio/IInflationRateProvider.cs`,
  `.../PortfolioModels.cs` (+TransactionInput/PositionBasis), `.../PortfolioCalculationService.cs`
  (+DerivePosition), `Finans.Infrastructure/Persistence/EfInflationRateProvider.cs`,
  `.../DependencyInjection.cs`, `tests/.../Portfolio/PositionDerivationTests.cs`,
  `tests/Finans.Integration.Tests/{InflationRealReturnTests,PositionDerivationConsistencyTests}.cs`.
- **Test:** SC-05 (enflasyon bağlama integration: provider 0,38 yükler, summary reel %9,89) +
  SC-06 (DerivePosition unit 8 test: tek/çok alış, komisyon, satış ort. bozmaz, alış-satış-alış,
  boş, yalnız satış + seed tx→holding tutarlılık integration). `dotnet test`
  **Application 39 + Integration 18 = 57 yeşil**.
- **Karar/Not:** İki saf çekirdek (RealReturn, DerivePosition) artık veri katmanına bağlandı.
  Enflasyon dönem-duyarlı seçimi (pozisyon ufkuna göre) ileri fazda; cache Faz 2. `DerivePosition`
  T1.6 HoldingService'te işlem değişiminde Holding.Quantity/AvgCost'u yeniden hesaplamak için kullanılacak.
- **Durum:** tamamlandı
- **Sıradaki:** T1.6 Holdings CRUD endpoint + DTO + validasyon (+IDOR/per-user kapsam) · T1.7 `GET /portfolio/summary`.

## 2026-05-30 · `CurrencyConverter` + `IFxRateProvider` (EF) + test (T1.3)
- **Görev(ler):** T1.3 (tamam) · Faz 1
- **Ne yapıldı:**
  - **Saf `CurrencyConverter`** (`Finans.Application/Portfolio/`): değişmez kur anlık
    görüntüsü alan, I/O'suz, deterministik dönüşüm. Aynı pb → birim; doğrudan kur (×);
    ters kur (÷); çapraz kur (pivot pb üzerinden iki adım). `Convert`/`TryConvert`/`RateFor`.
  - **Hassasiyet kararı (NFR-1):** ters yön `1/Rate` saklanıp çarpılmaz — **bölme** ile
    uygulanır. Aksi halde 96.000 ₺ → 1.999,99… $ çıkıyordu; finans uygulamasında kabul
    edilemez. Çapraz kur de adım adım çarp/böl. Sıfır/negatif tırnak güvenlik için atlanır.
  - **`IFxRateProvider`** (Application arayüzü) + **`EfFxRateProvider`** (Infrastructure):
    her pb çifti için EN GÜNCEL tırnağı (AsOfUtc) yükler. EF `GroupBy().First()` SQL'e
    çevrilemediğinden sıralı çekip bellekte gruplanır (FxRates küçük). DI: provider scoped,
    `PortfolioCalculationService` singleton.
- **Dokunulan dosyalar:** `Finans.Application/Portfolio/CurrencyConverter.cs`,
  `Finans.Infrastructure/Persistence/EfFxRateProvider.cs`,
  `Finans.Infrastructure/DependencyInjection.cs`,
  `tests/Finans.Application.Tests/Portfolio/CurrencyConverterTests.cs`,
  `tests/Finans.Integration.Tests/FxRateProviderTests.cs`.
- **Test:** SC-03 unit (11 test: doğrudan/ters/çapraz, tam hassasiyet, bilinmeyen kur→hata/false,
  güvensiz tırnak atlama) + integration (2 test: seed'den en güncel kur 48 seçilir, çapraz
  EUR→USD=104). `dotnet test` **Application 31 + Integration 15 = 46 yeşil**.
- **Karar/Not:** Converter saf; gerçek bağlama (USD varlığın summary'de TRY'ye çevrilmesi)
  T1.7 endpoint'inde. Cache (anahtar pb çifti) T1.15/Faz 2'de.
- **Durum:** tamamlandı
- **Sıradaki:** T1.4 reel getiri (enflasyon verisi bağlama) + T1.5 ort. maliyet türetimi (tx→AvgCost).

## 2026-05-30 · `PortfolioCalculationService` + birim testleri (T1.1 + T1.2) → FAZ 1 başladı
- **Görev(ler):** T1.1 (tamam), T1.2 (tamam) · Faz 1 ilk görevi
- **Ne yapıldı:**
  - **Saf hesaplama servisi** (`Finans.Application/Portfolio/`): CLAUDE.md §6 formülleri
    tek yerde, yan etkisiz, I/O yok. Statik saf yardımcılar: `TotalCost`, `CurrentValue`,
    `Profit`, `ReturnRatio`, `WeightedAverageCost`, `RealReturn` (Fisher). Toplayıcı
    `CalculateSummary` (özet + dağılım, ops. enflasyonla reel getiri) ve `CalculateHoldings`
    (kalem metrikleri + ağırlık). Tüm hesap `decimal`; yuvarlama yok (NFR-1).
  - **Modeller** (`PortfolioModels.cs`): `HoldingInput`/`HoldingResult`/`PortfolioSummary`/
    `AllocationSlice` — EF entity'sine bağımsız saf record'lar (04 §4 şekliyle uyumlu).
  - **Null politikası:** fiyatsız/sıfır-maliyetli kalemde değer/kâr/getiri null; reel
    getiri enflasyon yoksa null (04 §4 sözleşmesi).
  - **20 birim testi:** seed seti BİREBİR (maliyet 422.970, değer 641.403, kâr +218.433,
    +%51,6; altın +%43), dağılım ağırlıkları (04 §4: 0,405/0,436/0,150/0,009, toplam 1),
    reel getiri formülü, kenar durumlar (boş portföy, fiyatsız kalem, sıfır maliyet).
- **Dokunulan dosyalar:** `backend/src/Finans.Application/Portfolio/PortfolioModels.cs`,
  `.../Portfolio/PortfolioCalculationService.cs`,
  `backend/tests/Finans.Application.Tests/Portfolio/PortfolioCalculationServiceTests.cs`.
- **Test:** SC-01 (unit ✓), SC-02 (✓), SC-05 (✓), SC-06 (saf çekirdek ✓) — `dotnet test`
  **Application 20/20 + Integration 13/13 = 33 yeşil**. (SC-01 integration, SC-06 tx türetimi
  sonraki görevlerde.)
- **Karar/Not:** Servis tamamen saf (entity'siz record girdi) → repository/EF bağlama
  T1.6/T1.7'ye bırakıldı. Faz 1 varsayımı: kalemler baz pb cinsinden fiyatlı; para birimi
  dönüşümü (T1.3) girdiler servise verilmeden uygulanacak. `RealReturn`/`WeightedAverageCost`
  saf çekirdekleri burada test edildi; veri bağlama T1.4/T1.5.
- **Durum:** tamamlandı
- **Sıradaki:** T1.3 `CurrencyConversionService` + `FxRates` (elle kur) + test.

## 2026-05-30 · Test altyapısı tamam — Sqlite fixture + Playwright (T0.11) → FAZ 0 BİTTİ
- **Görev(ler):** T0.11 (tamam) · dal `feat/test-infra` · **Faz 0 kapanışı**
- **Ne yapıldı:**
  - **Model sağlayıcı-duyarlı:** Npgsql'e özgü `citext`/`xmin`(xid)/`HasPostgresExtension`
    `FinansDbContext.OnModelCreating`'de `Database.IsNpgsql()` koşuluna alındı; aksi
    sağlayıcıda (Sqlite/InMemory) `IPAddress`→string converter. Npgsql model/migration
    değişmedi (design-time Npgsql). Konfig sınıflarından provider-özgü parçalar çıkarıldı.
  - **Sqlite integration fixture:** `SqliteWebApplicationFactory` (in-memory bağlantı
    açık tutulur; `ConfigureTestServices` ile Npgsql DbContext kaydı ada-göre temizlenip
    Sqlite eklenir) + `SqliteIntegrationTests` (EnsureCreated+seed; /health/ready Healthy,
    seed tutarlılığı relational'da, CK_Holdings_Quantity negatifi reddediyor).
  - **Playwright iskeleti (web):** `@playwright/test` + `playwright.config.ts`
    (channel "chrome" → indirme yok; `webServer` vite'ı otomatik başlatır) +
    `e2e/smoke.spec.ts` (yüklenme + tr-TR format + route geçişi) + `e2e` script'i.
- **Dokunulan dosyalar:** `Finans.Infrastructure/Persistence/FinansDbContext.cs`,
  `Configurations/{Portfolio,Identity}Configurations.cs`, `tests/.../Sqlite*.cs`,
  `tests/.../Finans.Integration.Tests.csproj` (Sqlite paketi), `web/playwright.config.ts`,
  `web/e2e/smoke.spec.ts`, `web/package.json`.
- **Test:** backend `dotnet test` **13/13**, web vitest 2, shared 8, **Playwright e2e 1** —
  hepsi yeşil. (e2e sistem Chrome ile; backend kapalıyken proxy hatası beklenen, test bağımsız.)
- **Karar/Not:** Model artık sağlayıcı-portatif (prod Npgsql, test Sqlite); IsNpgsql
  koşulu tek nokta. Playwright iskelet — gerçek akışlar Faz 1'de. **Faz 0 DoD'un tamamı
  karşılandı.**
- **Durum:** tamamlandı → **FAZ 0 BİTTİ**
- **Sıradaki:** Faz 1 — T1.1 `PortfolioCalculationService` (saf hesap + birim test, NFR-1).

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
