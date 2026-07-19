# 09 — Test Stratejisi (Testing Strategy)

> **Çekirdek kural:** *Senaryo-önce, test-yanında, yeşil olmadan "tamam" yok.*
> Her geliştirme görevi, kendi testleriyle **birlikte** hazırlanır; testler
> geçmeden görev tamamlanmış sayılmaz. Hesaplama (parasal) testleri **zorunlu**
> (NFR-1). Bu doküman testlerin **ne, nasıl, ne zaman** yazılacağını tanımlar.

İlgili: `06-DEV-PLAYBOOK.md` §4-5 (DoD), `CLAUDE.md` §11-12 (protokol),
`08-BACKLOG.md` (görev başına test eşlemesi), `01-NEEDS-ANALYSIS.md` (FR/NFR).

---

## 1. Felsefe & Test Türleri

İki kategori istiyoruz (kullanıcı talebi):

1. **Unit (birim) testler** — saf fonksiyon/servis davranışı, izole. Özellikle
   **hesaplama** (getiri, dağılım, kur, reel getiri, ort. maliyet).
2. **Olaylara yönelik (senaryo/davranış) testler** — bir **olay** veya
   **kullanıcı akışı** uçtan uca doğru sonuç veriyor mu? Örn:
   - Kullanıcı varlık ekler → portföy özeti doğru güncellenir.
   - Dış fiyat API'si çöker → uygulama çökmez, son bilinen fiyat + "yaklaşık".
   - LLM bozuk JSON döner → fallback devreye girer, kart şeması korunur.

### Test piramidi (bu yığın için)

```
        ▲  Senaryo / E2E akış  (az sayıda, kritik kullanıcı yolları)
       ▲▲  Integration         (endpoint + DB + dış servis mock/fallback)
      ▲▲▲  Unit                (çok sayıda, hızlı; hesap = zorunlu)
```

> Tabana yatır: çok sayıda hızlı birim testi + orta katman integration. E2E
> **az ama kritik** (Faz 2+'da gerçek araçla; Faz 1'de bileşen testi yeterli).

---

## 2. Backend Test Mimarisi (.NET / xUnit)

| Proje | Tür | Kapsam | Araç |
|-------|-----|--------|------|
| `Finans.Application.Tests` | Unit | `PortfolioCalculationService`, `CurrencyConversionService`, ort. maliyet türetimi, reel getiri, nudge kuralları, LLM parse/fallback | xUnit, FluentAssertions |
| `Finans.Integration.Tests` | Integration / Senaryo | HTTP endpoint + gerçek pipeline + DB; dış servis (fiyat/LLM) mock'lu olay testleri | xUnit + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) |

**DB stratejisi (integration):**
- Faz 1: **SQLite in-memory** veya EF in-memory — hızlı, kurulumsuz.
- Faz 2+: **Testcontainers (PostgreSQL)** — prod ile birebir sadakat (kararı
  ihtiyaç doğunca al; erken optimizasyon yok).

**Dış servis (olay) izolasyonu:** Fiyat ve LLM istemcileri `IPriceProvider` /
`ILlmClient` arayüzleri arkasında (bkz. `02` §2.2, `07` §2). Testte bunların
mock'u verilir → "API çöktü", "JSON bozuldu" gibi **olaylar tetiklenebilir**.

### Kurulum (Faz 0 — test altyapısı)
```bash
# unit test projesi (zaten T0.2'de)
dotnet add backend/tests/Finans.Application.Tests package FluentAssertions
# integration/senaryo test projesi
dotnet new xunit -n Finans.Integration.Tests -o backend/tests/Finans.Integration.Tests
dotnet add backend/tests/Finans.Integration.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet add backend/tests/Finans.Integration.Tests package Microsoft.EntityFrameworkCore.Sqlite
# (Faz 2+) dotnet add ... package Testcontainers.PostgreSql
```

### Çalıştırma
```bash
dotnet test                              # tüm testler
dotnet test --filter Category=Calc       # sadece hesaplama (hızlı geri besleme)
```

---

## 3W. Web Test Mimarisi (ReactJS + Vite) — BİRİNCİL

| Tür | Kapsam | Araç |
|-----|--------|------|
| Unit | `@finans/shared` format/util (tr-TR), saf yardımcılar | **Vitest** |
| Bileşen | `HeroCard`/`AllocationDonut`/`HoldingsTable` render; `Disclaimer` Analiz'de **her zaman** var; kâr/zarar renk+işaret | **Vitest + React Testing Library** |
| E2E / akış | Tarayıcıda uçtan uca kullanıcı yolu (varlık ekle → özet güncellenir) | **Playwright** |

```bash
pnpm --filter ./web test          # Vitest (unit + bileşen)
pnpm --filter ./web test:e2e      # Playwright
```
> Web birincil yüzey olduğu için **olaylara yönelik testlerin UI ucu burada**
> (Playwright); iş mantığı/olayın backend ucu integration testlerinde.

## 3. Mobil Test Mimarisi (React Native) — FAZ M

| Tür | Kapsam | Araç | Faz |
|-----|--------|------|-----|
| Unit | `formatCurrency`/`formatPercent` ve saf yardımcılar | **Jest** | 0-1 |
| Bileşen | `HeroCard` özeti gösteriyor mu, `HoldingRow` trend rengi/oku, `Disclaimer` analiz ekranında **her zaman** var mı | **Jest + React Native Testing Library (RTL)** | 1 |
| E2E / akış | Gerçek cihaz/emülatör üzerinde uçtan uca kullanıcı yolu | **(ertelendi)** — Faz 2+'da Maestro önerilir | 2+ |

> **Karar:** Faz 1'de mobil tarafta **Jest + RTL** ile birim + bileşen testi.
> Gerçek E2E aracı (Maestro), ekran akışları oturduğunda (Faz 2+) eklenir.
> O zamana kadar "olaylara yönelik" doğrulamanın ağırlığı **backend integration
> senaryo testlerindedir** (asıl iş mantığı orada).

### Kurulum & çalıştırma
```bash
# Expo + jest-expo genelde hazır gelir; değilse:
npx expo install jest-expo jest @testing-library/react-native
npm test
```

---

## 4. Senaryo Formatı (Given-When-Then)

Her olay/akış testi bir senaryodan türer. Senaryolar **kod yazılmadan önce**
yazılır ve ilgili FR/NFR'ye bağlanır.

```
Senaryo SC-01 (FR-1.3): Varlık eklenince portföy özeti güncellenir
  Given: Boş portföy, baz para birimi TRY
  When : 40 gr altın, ort. 4.546 ₺/gr eklenir; güncel fiyat 6.500 ₺/gr
  Then : totalCost ≈ 181.851 ₺, currentValue = 260.000 ₺,
         netProfit ≈ +78.149 ₺, returnRatio ≈ 0,43
```

Bu senaryo **iki katmanda** test edilebilir: hesaplama kısmı **unit** (sadece
`PortfolioCalculationService`), uçtan uca kısım **integration** (`POST /holdings`
→ `GET /portfolio/summary`).

---

## 5. Senaryo Kataloğu (yaşayan liste)

> Geliştirme ilerledikçe büyür. Her satır: ID, ilgili gereksinim, kapsayan test
> seviyesi, durum. Yeni özellik = önce buraya senaryo ekle.

| SC | Gereksinim | Senaryo (özet) | Seviye | Durum |
|----|-----------|----------------|--------|-------|
| SC-01 | FR-1.3 | Altın kalemi eklenince değer/kâr/getiri doğru (40gr→260.000, +%43) | Unit + Integration | [x] (unit T1.2 + T1.7 summary/holdings integration) |
| SC-02 | FR-1.3 | Çok varlıklı portföyde dağılım % toplamı 1,0 (±yuvarlama) | Unit | [x] (T1.2) |
| SC-03 | FR-1.4 | USD varlık TRY baz pb'ye güncel kurdan çevrilir | Unit + Integration | [x] (T1.3; ters/çapraz kur dahil, tam hassasiyet) |
| SC-04 | FR-1.5 | BES'te devlet katkısı kendi katkısından **ayrı** dönüyor | Integration | [x] (T1.6; GET /holdings `bes` alanı, integration) |
| SC-05 | FR-1.6 | Reel getiri = (1+nominal)/(1+enflasyon)−1 doğru | Unit | [x] (T1.2 çekirdek + T1.4 enflasyon verisi bağlama, integration) |
| SC-06 | §03 §5 | Birden çok alış → ağırlıklı ort. maliyet doğru türeniyor | Unit | [x] (T1.5: `DerivePosition` unit + seed tx→holding tutarlılık integration) |
| SC-07 | FR-1.1/NFR-4 | Geçersiz girdi (miktar ≤ 0) → 400 + TR hata mesajı | Integration | [x] (T1.6; VALIDATION_ERROR + field quantity, integration) |
| SC-08 | FR-2.5/NFR-5 | **Olay:** fiyat API'si çöker → son bilinen fiyat + `stale:true`, çökme yok | Integration | [x] (T2.3; çöken sağlayıcı → DB'den son-bilinen bayat tırnak, `HasStale`/`FailedSources`, bayat geçmişe yazılmaz, altın taze sürer) |
| SC-09 | FR-2.4 | Kural tabanlı not eşiği aşınca tetiklenir (yoğunlaşma top-2 ≥%60 · tek varlık ≥%40 · nakit <%5); çeşitlenmiş/boş portföyde tetiklenmez; notlar **tavsiye içermez** | Unit + Integration | [x] (T2.5; `NudgeRuleEngine` 6 unit + `GET /portfolio/nudges` 2 e2e (seed→yoğunlaşma+düşük nakit; boş→0)) |
| SC-10 | FR-3.2 | **Olay:** LLM bozuk JSON döner → fallback, 200, şema korunur | Unit + Integration | [ ] |
| SC-11 | FR-3.3/NFR-2 | Commentary çıktısı yasaklı yönlendirme ("al/sat/yükselir") içermez | Unit (filtre) | [ ] |
| SC-12 | FR-4.3 | Bilinmeyen sembol → anlamlı 404 hata | Integration | [ ] |
| SC-17 | FR-2.1 | Fiyat sağlayıcı dış kaynağı doğru ayrıştırır: Frankfurter → her döviz için doğrudan TRY kuru (ters çevirme yok); Truncgil → "GRA" gram altın satış (TRY); yabancı-olmayan/eksik enstrüman atlanır; HTTP/ayrıştırma hatası istisna (fallback T2.3) | Unit (HTTP stub) | [x] (T2.1; Frankfurter+Truncgil sağlayıcı testleri, 8 senaryo) |
| SC-18 | FR-2.2 | `PriceFetchService` canlı tırnağı yazar: `PriceSnapshots`(geçmiş)+`FxRates`(converter)+`Holding.CurrentPrice`(okuma yolu) güncellenir; TTL içinde 2. çağrı cache'ten (dış API tekrar yok); bir sağlayıcı çökse diğeri yazmayı sürdürür | Integration (Sqlite+seed, stub sağlayıcı) | [x] (T2.2; 3 senaryo + T2.4 `GET /api/prices` e2e: canlı fiyat döner + holdings'i besler + stale yüzeyi) |
| SC-19 | NFR-6/10, `10`§3-4 | **Cache:** `IAppCache` ilk çağrıdan sonra cache'ler (dış çağrı tekrar yok); **single-flight** eşzamanlı çağrıda factory'i bir kez koşar (stampede koruması); miss→null/set→değer. Redis yoksa in-memory'e düşer (yerel dev çökmez) | Integration (`DistributedAppCache`) | [x] (T2.7; 3 senaryo) |
| SC-W1 | NFR-7 | `formatCurrency(641403)`="641.403 ₺", `formatPercent(0.516)`="%51,6" (`@finans/shared`) | Unit (Vitest) | [x] (T1.10 format.test.ts) |
| SC-W2 | NFR-2 | **Web** Analiz sayfası render'ında `Disclaimer` her zaman mevcut | Bileşen (RTL) | [x] (Disclaimer bileşeni + AnalysisPage.test) |
| SC-W3 | FR-1.1/1.3 | **Web E2E:** varlık ekle → portföy özeti güncellenir | E2E (Playwright) | [ ] (ertelendi → Faz 2; iki-sunucu orkestrasyonu) |
| SC-W4 | FR-2.5/2.4 | **Web:** `PriceTicker` (kayan canlı fiyat + kaynak etiketi) bayatta "yaklaşık" gösterir; `NudgesCard` notları + "tavsiye değildir" disclaimer'ıyla render eder; not yoksa çizmez | Bileşen (RTL) | [x] (T2.6; PriceTicker 2 + NudgesCard 2) |
| SC-W5 | FR-1.8 | **Web:** elle "Fiyatı güncelle" yalnız fiyatı sabit/canlı OLMAYAN varlıklarda (Hisse/Fon/BES); **Nakit** (sabit ₺1) ve **Altın/Döviz** (canlı) için gizli + bağlam notu | Bileşen (RTL) | [x] (detay sayfası, 3 senaryo) |
| SC-W6 | FR-1.1 | **Web:** Nakit pozisyonunda işlem formu **"Para ekle/çıkar"** (fiyat alanı yok, `unitPrice=1` gönderilir); geçmiş "Para eklendi/çıkarıldı" | Bileşen (RTL) | [x] (AddTransactionForm cash testi) |
| SC-20 | §03 §A | **BES:** devlet katkısı = kendi katkı × **%20** (2026); hak ediş sistemde kalış yılından kaba türetilir (&lt;3 NotVested · 3–10 PartiallyVested · ≥10 Vested) | Unit (`BesCalculator`) | [x] (T-BES.1; 9 senaryo) |
| SC-21 | FR-1.8 | **BES:** `PUT /holdings/{id}/bes` başlangıç tarihini günceller → hak ediş yeniden türer; gelecek tarih → 400; BES olmayan → 400; başkasının id'si → 404 (IDOR) | Integration | [x] (T-BES.3; 3 senaryo) |
| SC-22 | FR-1.1 | **BES:** tarih aralığından aylık katkı üretimi — kapsanan aylar için kayıt (her ayın oranıyla), işlem geçmişine yansır, **idempotent** (tekrar→çiftleme yok); BES olmayan→400 | Unit (`BesContributionPlanner`) + Integration | [x] (T-BES.6; planner 4 + e2e 2) |
| SC-23 | FR-1.8 | **BES:** katkı **düzenle** (tutar/tarih → devlet katkısı + kümülatif + **maliyet=kendi katkı** yeniden) ve **sil** (kümülatif düşülür); "bundan sonraki için kullan" → **PlanActive** | Integration (göreli delta) + Bileşen (RTL: düzenle/sil ikonları) | [x] (T-BES.7; e2e 2 + BesContributionHistory testi) |
| SC-24 | FR-1.1/1.8 | **İleri tarihli katkı/plan serbest** (ileriye dönük planlama): ileri tarihli (örn. 2099) tekil katkı ekle/düzenle ve ileri tarihli aralıkla plan → 200 + istenen kayıt(lar) oluşur (`must_not_be_future` artık atılmaz); web tarih girişi native `<input type=date>` (takvim+↑↓+←→+Tab), `max` yalnız BES başlangıç tarihinde | Integration (ileri-tarih) + Bileşen (DateField native) | [x] (e2e 2: Generate_allows_future_range, Add_and_edit_allow_future_paid_date; DateField 4 ⚠ integration koşumu VS kilidine bağlı) |
| SC-25 | FR-1.5 | **BES katkı durumu tarihten türer** (T-BES.8): ödeme tarihi gelecekte → **Future** (kendi katkı da bekliyor); ödeme geçti ama devlet yatma tarihi (ödeme ayını izleyen ayın sonu) gelmedi → **StatePending** (devlet bekliyor); yatma tarihi de geçti → **Deposited**. **Yalnız yatırılanlar** maliyet/getiri tabanına girer; bekleyenler ayrı, toplama dahil DEĞİL | Unit (`StateDepositDateFor`/`ContributionStatusFor`) + Integration (bugün eklenen katkı → devlet bekliyor; stateDeposited değişmez, statePending=200) | [x] (unit 7; integration `Bes_contribution_increases_own_state_and_cost` güncellendi; web `BesContributionHistory` durum etiketleri) |
| SC-26 | §03 §A | **BES kademeli hak ediş** (T-BES.8): <3y %0 · 3–6y %15 · 6–10y %35 · 10y+ %60 · 10y+56yaş %100; hak kazanılan tutar ≈ oran × yatırılmış devlet katkısı; yaş `BirthYear`'dan | Unit (`VestedRateFor`/`AgeFor`) | [x] (unit 7) |
| SC-27 | FR-1.1 | **BES açılış bakiyesiyle kurulur** (T-BES.8): `POST /holdings/bes` → Holding(Bes)+BesDetails+tek "Opening" katkı kaydı (güncel fon değeri + birikmiş kendi/devlet); ödeme günü/doğum yılı `PUT /bes` ile düzenlenir | Integration (create-bes; ödeme günü) + Bileşen (AddHoldingDialog BES dalı) | [x] (web AddHoldingDialog BES testi; ⚠ create-bes integration koşumu VS kilidine bağlı) |
| SC-28 | 04 §7 | **Hisse metrikleri** (T4.2): sembol → Finnhub 3 uç birleşimi (metric+quote+profile2, token başlıkta → log'a sızmaz); yüzde→oran normalizasyonu; kaba bant etiketleri KODDA (sınır değerleri dahil); geçersiz sembol 400 · bilinmeyen 404 · anahtar yok/kaynak çökük anlamlı 502 (çökme yok, NFR-5); 1 saat ortak cache + tek-uçuş (60 çağrı/dk kota koruması); hata cache'lenmez | Unit (eşik bantları + servis: doğrulama/normalizasyon/cache/404/502) + Integration (Finnhub stub HTTP eşleme + endpoint 200/404/400/502) | [x] (unit 31 + integration 8; 502/400 canlı doğrulandı) |
| SC-29 | 04 §7, 07 §8 | **Hisse metrik açıklaması** (T4.3): metrikler+bant etiketleri → LLM eğitici kartlar (Genel Bakış + metrik başına kart, iki yönlü çerçeve, tavsiye/tahmin/uydurma bilgi YOK); paylaşılan güvenli parse + tavsiye/dil bekçileri; sembol bazlı 24s ortak cache + son-başarılı fallback; LLM yoksa 200+fallback kartı (çökme yok) | Unit (mutlu yol/şema-prompt dayatma/cache/bekçi/fallback/retry + prompt regresyon kapısı) + Integration (endpoint: Noop LLM→200 fallback; bilinmeyen sembol→404) | [x] (unit 6 + integration 2; canlıda Anthropic ile 5 kart doğrulandı) |
| SC-30 | 04 §7 | **Hisse fiyat geçmişi** (T4.5): sembol → Yahoo chart API (anahtarsız, halka arzdan bugüne günlük seri); dönem dilimleme (1w/1m/3m/1y/5y/max) + değişim oranı uçlardan + seyrekleştirmede uçlar korunur — hepsi KODDA; yeni halka arzda pencere boşsa tüm seri; tam seri 24s ortak cache (dönem değişimi kaynaksız); geçersiz dönem 400 · sembol yok 404 · kaynak çökük 502 | Unit (dilimleme/değişim/seyrekleştirme/cache/varsayılan dönem/hatalar 7) + Integration (Yahoo stub parse 4 + endpoint 2) | [x] (canlı: AAPL 1980→bugün 11.5k nokta) |
| SC-31 | 07 §4, 13 | **Yorum gezgini dikey ray + accordion** (T4.5 devamı): geniş ekranda solda dikey başlık rayı (tablist `aria-orientation=vertical`, ↑/↓/Home/End klavye) + sağda tek panel; ≤720px'te accordion (`aria-expanded` başlıklar, tek açık, açığa tıklayınca kapanır); boş kart listesi hiçbir şey çizmez; matchMedia yoksa (jsdom) ray varsayılan | Bileşen (CommentaryTabs: ray 5 + accordion 4) | [x] (canlı: Analiz 12 kart ray + accordion, Hisse 5 kart doğrulandı) |
| SC-32 | NFR-1, 03 §A | **Portföy günlük değer serisi** (T5.1): Transactions+PriceSnapshots → ilk işlem gününden bitişe **her gün** bir nokta (değer + yatırılan maliyet, baz pb); eksik gün = son bilinen fiyat taşınır; işlem günü pozisyon/maliyet değişir (ort. maliyet yöntemi, satış ortalamayı bozmaz, fee dahil); işlem birim fiyatı da fiyat gözlemi (aynı gün snapshot kazanır); hiç fiyat yoksa değer=maliyet; kur gün-bazlı taşınır + seri öncesi kur en eski kayıtla geri-doldurulur; kur hiç yoksa fırlatır (sessiz yanlış sayı yok); bitişten sonraki işlem/fiyat/kur yok sayılır; boş girdi → boş seri | Unit (saf servis; tam hassasiyet decimal) | [x] (unit 13) |
| SC-33 | 04 §4, 11 §3, 10 §3 | **Portföy değer geçmişi API** (T5.2): `GET /api/portfolio/history?period=1m\|3m\|1y\|all` — kullanıcının Transactions+PriceSnapshots+FxRates verisi T5.1 saf servisine indirgenir (BES: katkılar miktar olayı, devlet katkısı yatma tarihinde değere girer, bugünkü fon değeri son gözlem; işlemsiz pozisyon açılış olayı); son gün özet ekranıyla birebir tutarlı (TotalValue/TotalCost); dönem dilimleme + ≤500 nokta seyrekleştirme (uçlar korunur) + değişim oranı; cache anahtarı `UserId` içerir (60s); geçersiz dönem 400; başka kullanıcı boş seri (izolasyon) | Integration (seed ile uçtan uca: özet eşitliği, dönem, 400, izolasyon) | [x] (integration 5; canlı gerçek veriyle teyit) |
| SC-34 | NFR-1, `CLAUDE.md` §6 | **Özet maliyeti okuma anında kaynaktan türetilir** (T5.2'de yakalanan tutarsızlık): saklanan `AvgCost` bayatsa (örn. ileri tarihli BES plan katkısı işlenmişse) özet YANLIŞ maliyet göstermez; ileri tarihli katkı maliyete girmez; **özet = pozisyon listesi = değer serisi** aynı tabanı söyler (ortak `ApplyReadPosition`) | Integration (ileri tarihli BES planı → özet 50.000, seri son gün eşit) | [x] (canlıda 646.635→522.385 düzeldi) |
| SC-35 | 13 §4, NFR-2 | **Değer Seyri web grafiği** (T5.3): `ValueHistoryChart` iki seri çizer (değer: dolgulu ana çizgi; yatırılan: kesikli referans — boşluk kâr/zararı gösterir), ölçek iki serinin ortak min/max'ı, <2 noktada hiçbir şey çizmez (sayfada zarif düşüş metni); pano kartı son 1 yıl compact grafik; Performans sayfası dönem seçici (1A/3A/1Y/Tümü → 1m/3m/1y/all) + değişim rozeti + "veri şu tarihten beri" + **"geçmiş, tahmin değil" notu** (CLAUDE.md §2); **hover**: imleç en yakın noktaya eşlenir (`useChartHover`, fare+dokunmatik) → crosshair + tooltip (tarih + Değer/Yatırılan; hisse grafiğinde Kapanış), imleç çıkınca kaybolur | Bileşen (chart 4 + PriceChart 2 + PerformancePage 2) | [x] (canlı: pano + Performans + dönem geçişi + hover iki grafikte doğrulandı) |
| SC-36 | NFR-1, 14 §4-C1 | **Enflasyon eşiği serisi** (T5.4): günlük maliyet serisinden alım-gücü referans çizgisi — her günkü yatırılan delta kendi gününden itibaren günlük bileşik enflasyonla büyür (`eşik(d) = eşik(d−1)×g + Δmaliyet(d)`, `g=(1+π)^(1/365,25)`); oran 0 → eşik = maliyet; satış deltası nominal düşer; boş seri → boş; enflasyon verisi yoksa çizgi üretilmez (null) | Unit (saf fonksiyon) | [x] (unit 6) |
| SC-37 | 04 §7.2, 11 §3, CLAUDE.md §2 | **Senaryo v1 API + sayfa** (T5.4): `GET /api/portfolio/scenario/{holdingId}` — tek pozisyonun günlük değer vs yatırılan (nakitte dursaydı) serisi + enflasyon eşiği + özet (bugünkü değer, yatırılan, fark ₺/%, eşik); son gün pozisyon detayıyla tutarlı; IDOR: başkasının holding'i → 404; cache `UserId`'li 60s; ≤500 nokta; web ScenarioPage: pozisyon seçici + üç çizgili grafik + kalıcı disclaimer + **"geçmiş, tahmin değil"** — al/sat yönlendirmesi YOK | Integration (200/values/IDOR/404/işlem→anında güncel — 5) + Bileşen (ScenarioPage 2 + narrative unit 4) | [x] (canlı: USD/Altın gerçek veriyle; 3 çizgili tooltip; cache damgasıyla işlem sonrası anında tazelenir + metin okuma) |
| SC-38 | 03 §C/§14 | **Eğitim şeması bütünlüğü** (T5E.1): Lessons/LearningTracks slug UNIQUE; `UserLessonProgress(UserId,LessonId)` UNIQUE + `ProgressPercent` 0-100 CHECK; ders kendine ön-koşul olamaz (CHECK); track silinince dersler+quiz kaskad düşer; enum'lar varchar allow-list | Integration (SQLite model kısıtları) | [x] (integration 4; migration canlı Postgres'e uygulandı — 11 tablo) |
| SC-45 | 13 §4, 04 §7.5 | **Web Eğitim sayfası** (T5E.4): ComingSoon→gerçek — shared tipler+istemci+hook'lar; "Temeller" seti + ilerleme çubuğu (ders/segment, tamamlanan mint) + `x/y ders tamamlandı`; ders satırları (sıra rozeti + başlık + özet + ⏱dk + durum: ✓Tamamlandı/●Devam/🔒Kilitli); **kilitli ders tıklanamaz** (disabled); sayfa-içi ders okuma (`MiniMarkdown` — güvenli lib'siz alt-küme: başlık/kalın/alıntı/liste/paragraf, `dangerouslySetInnerHTML` YOK, XSS testli) + kavram çipleri + "Dersi tamamla" (progress upsert); **mini test** (tek/çok şık, tam-eşleşme; gönderince skor+geçti + doğru şık yeşil/yanlış mercan + açıklama açılır; yeniden çöz) | Bileşen (MiniMarkdown 2 + EducationPage 3: liste/kilit/okuma) | [x] (2026-07-17; web 101/101; **canlı Postgres uçtan uca teyit** — /egitim seed'le birebir: 3/5 tamamlandı, ders okuma+markdown+quiz) |
| SC-44 | 04 §7.5, 11 §3 | **Eğitim endpoint'leri** (T5E.3): `GET /education/tracks` (yayında, ders sayısı) · `GET /tracks/{slug}/lessons` (kullanıcının durumu/ilerlemesi + **kilit ön-koşuldan türetilir**; set yoksa 404) · `GET /lessons/{slug}` (gövde+bölüm+quiz+kavram; **cevap-anahtarı/açıklama SIZMAZ** — yalnız deneme sonucunda) · `PUT /lessons/{id}/progress` (upsert, `UserId` kapsamlı; yüzde 0-100 dışı 400, ders yok 404) · `POST /quizzes/{id}/attempts` (tam-eşleşme değerlendirme, skor+geçti+açıklama+doğru şık; quiz yok 404) · `GET /lessons/by-concept/{key}` (boş olabilir). **Per-user izolasyon:** aynı dersler Investor'a Completed, Admin'e NotStarted; bir kullanıcının yazımı diğerine sızmaz | Integration (SQLite fixture; içerik + izolasyon + kilit + grading + 400/404) | [x] (2026-07-16; integration 12) |
| SC-43 | 03 §12.5 | **Eğitim seed içeriği** (T5E.2): "Temeller" track'i (published, Beginner) + taslakla birebir 5 ders (slug/sıra/dakika; Summary metinleri taslaktan) + sıralı ön-koşul zinciri (her ders bir öncekini ister, Ders 1 kilitsiz) + 6 kavram etiketi (F/K dersi iki etiket) + Ders 1'e bağlı 3 soruluk mini test (her soruda tam 1 doğru şık + eğitici Explanation, PassingScore 60) + örnek ilerleme (User#1: 1-3 Tamamlandı %100 · 4 Devam %0 · 5 kayıt YOK → türetilmiş Kilitli); portföyden **bağımsız idempotent** (LearningTracks var mı? → mevcut DB'ler de alır, çoğaltmaz) | Integration (InMemory içerik/zincir/ilerleme + idempotency; SQLite kısıtları SC-38 seed yolu) | [x] (2026-07-16; integration 6; seed = test fixture) |
| SC-40 | NFR-1, `CLAUDE.md` §6, 03 §11.1 | **Fiyatsız kalem özete maliyetiyle girer** (2026-07-12 B1 düzeltmesi): Given fiyatı hiç girilmemiş kalem (CurrentPrice null, maliyet C) When özet hesaplanır Then kalem toplam değere **C ile** katılır (0 DEĞİL → sahte −%100 zarar yok), NetProfit katkısı 0, dağılımda maliyet değeriyle yer alır, ağırlıklar toplamı 1; kalem satırı null kalır (UI "fiyatsız" gösterebilir); tek fiyatsız kalemli portföyde özet toplamı = Değer Seyri son gün değeri (fee'siz tek alışta birebir; **özet = liste = seri** ilkesi) | Unit (CalculateSummary/CalculateHoldings) + Integration (fiyatsız pozisyon → özet delta = maliyet) | [x] (unit + integration yeşil) |
| SC-41 | NFR-1, 03 §11 | **Kronolojik aşırı satış yazmada reddedilir** (2026-07-12 B2 düzeltmesi): Given 10 Oca'da 10 adet alış When 5 Oca tarihli 5 adet satış eklenir/güncellenir (nihai miktar ≥ 0 olsa bile) Then 400 VALIDATION_ERROR (`quantity`) — her işlem tarihinde kümülatif miktar ≥ 0 kuralı (aynı gün alışlar satıştan önce sayılır, gün granülü seriyle uyumlu); işlem KAYDEDİLMEZ → Değer Seyri hiçbir gün negatif değer/maliyet çizmez; alıştan SONRAKİ tarihli geçerli satış kabul edilir | Unit (`FirstOversoldDate` saf fonksiyon) + Integration (geçmişe tarihli satış → 400; sonrası → 200) | [x] (unit + integration yeşil) |
| SC-39 | FR-1.1, 13 §4 | **Tür-uyarlanır Varlık Ekle formu** (2026-07-12 isteği): her tür yalnız İLGİLİ alanları sorar — Altın (ad+gram+₺/gram; XAU/gram/TRY otomatik), Döviz (USD/EUR seçimi; ad/sembol/birim otomatik), Hisse (sembol terk edilince ad+güncel $ fiyat otomatik; hata → elle gir ipucu), Fon (elle fiyat; canlı kaynak yok notu), Nakit (yalnız ad+tutar; fiyat 1); **canlı fiyat ön-dolar** (altın/döviz/hisse) ama DÜZENLENEBİLİR + "otomatik geldi" ipucu; kullanıcı fiyatı elledikten sonra üzerine yazılmaz | Bileşen (AddHoldingDialog 8) | [x] (canlı: altın 6.225,55 · USD kuru 46,985 · MSFT $385,10 teyit) |
| SC-42 | NFR-5, 10 §3, 13 §4 | **Değer Seyri ilk yükleme FX yarışı** (2026-07-12 teşhisi): kur satırları fiyat tazeleme turunda yazılır; `/history` isteği kur commit edilmeden gelirse backend 500 atmaz — (1) tazelemeyi bir kez kendisi tetikler (single-flight → paralel `/prices` ile birleşir, çift dış çağrı yok) ve yeniden hesaplar; (2) kur yine yoksa sözleşmeli **502 UPSTREAM_ERROR** (`MissingFxRateException` ayrımıyla). Web pano kartı hatayı veri-yokluğu ("en az iki günlük veri") gibi MASKELEMEZ — ayrı `isError` metni; fiyat tazelemesi (`refreshedAtUtc` değişimi) `portfolio-history` sorgusunu da invalidate eder → hatalı kalan sorgu kendini toparlar. **Aynı desen `/scenario`'da** (`ScenarioService` aynı saf servisi kullanır): kur yokken bir kez tazele + yeniden hesapla, yine yoksa 502 | Unit (converter/seri `MissingFxRateException`) + Integration (kur silinmiş seed: stub sağlayıcıyla self-heal 200 · çökük sağlayıcıyla 502 — history 2 + scenario 2) + Bileşen (PortfolioPage: hata metni + history yeniden tetikleme) | [x] |
| SC-E12 | 15 §2, `CLAUDE.md` §2 | **Katmanlı ders içeriği** (T6.1): 5 dersin her biri **L1 Core + L2 Context + L3 Deep** anlatımı + **jenerik örnek** + **tuzak** bloğu taşır (5×5=25); içerik **MiniMarkdown alt kümesinde** kalır (tablo/link/kod YOK, başlık yalnız `##`/`###`); **tavsiye/tahmin ifadesi geçmez** (almalısın·satmalısın·yükselecek…); her derse 3 soruluk test (tek doğru şık + eğitici açıklama); **ayrı kapı geriye dönük yükler** — dersleri zaten olan DB bir sonraki açılışta bölümleri+testleri alır, çoğaltmaz | Integration (seed 4 + API 1) | [x] (2026-07-19; Education 32/32; **canlı Postgres teyitli** — 5 ders × 5 bölüm + 5 quiz/15 soru geriye dönük yüklendi, portföy verisi sağlam) |
| SC-E11 | 15 §2.1, 03 §C | **Katmanlı içerik şeması** (T6.5): `LessonSection.DepthTier/Kind` allow-list CHECK'leri; değer verilmeyen bölüm **Core/Explain**'e düşer (geriye dönük uyum); tüm tier×kind kombinasyonu round-trip; ders silinince bölümler kaskad düşer; API `sections[]` derinlik+tür ile çıkar; **bölümsüz derste `sections` boş + `bodyMarkdown` dolu** (SC-E2'nin veri-sözleşmesi yarısı) | Integration (model 3 + API 2) | [x] (2026-07-19; Education 27/27; **canlı Postgres teyitli** — migration `20260719210255` uygulandı, geçersiz `DepthTier` INSERT'i CHECK ile reddedildi) |
| SC-E1 | 15 §2.2 | **Katmanlı derinlik — başlangıç:** `LiteracyLevel=Başlangıç` kullanıcı ders açar → yalnız **L1 Core** render; "Daha derine in" ile L2 açılır (tavan kapatılmaz) | Bileşen | [ ] |
| SC-E2 | 15 §2.2 | **Katmanlı derinlik — ileri:** `LiteracyLevel=İleri` aynı dersi açar → L1 katlanmış, **L2+L3 açık**; `DepthTier` bölümü olmayan eski derste `BodyMarkdown` fallback'i render edilir (geriye dönük uyum) | Bileşen | [ ] |
| SC-E3 | 15 §3.2 | **Demo bağlam (onboarding 1c):** portföyü boş kullanıcı ders açar → `Demo` durumu: demo portföy sayıları + belirgin "örnek portföy" rozeti; **ders kilitlenmez**, hata yok; **demo sayı kullanıcının pano/özet ekranına SIZMAZ** | Integration + Bileşen | [ ] |
| SC-E4 | 15 §1.1, `CLAUDE.md` §2 | **🔒 SPK sınırı — `RiskAttitude` sızıntısı:** risk tutumu hiçbir API yanıtında, DTO'da, arayüzde veya LLM prompt bağlamında **görünmez**; hiçbir dağılım/portföy yargısı üretmez (kod + çıktı taraması) | Integration (sızıntı taraması) | [ ] |
| SC-E5 | 15 §3.4 | **🔒 SPK sınırı — enstrüman sıralaması:** LLM ders yorumu "X, Y'den daha iyi performans gösterdi" üretirse `CommentaryOutputGuard` kartı **düşürür** (zımni yönlendirme) | Unit (guard) | [ ] |
| SC-E6 | 15 §5 | **Kavram ustalığı:** quiz geçilince ilgili `ConceptTag`'lerin `MasteryScore`'u artar → o kavramın L1 katmanı sonraki derste katlanmış gelir | Integration | [ ] |
| SC-E7 | 15 §4 | **Tanılama atlanabilir:** test atlanır → `LiteracyLevel=null` → Başlangıç gibi davranılır, hiçbir ekranda hata/boşluk yok | Integration + Bileşen | [ ] |
| SC-E8 | 15 §3.1, `CLAUDE.md` §3.1 | **Bağlam determinizmi:** aynı portföy → `LessonContextService` aynı `ContextKey` değerlerini üretir (LLM'siz yol); sayılar koddan gelir, LLM üretmez | Unit | [ ] |
| SC-E9 | 15 §3.2 | **Bayat veri:** fiyat bayatken bağlam bloğu `Stale` damgasıyla ("şu tarihe ait") render edilir, sessizce güncel gibi gösterilmez | Integration | [ ] |
| SC-E10 | NFR-12, `11`§3 | **Güvenlik/IDOR:** A kullanıcısının `UserConceptMastery` / `LiteracyLevel` / `RiskAttitude` kayıtları B'ye sızmaz (per-user kapsam) | Integration | [ ] |
| SC-M1 | NFR-2/7 | (Faz M) Mobil format + Analiz disclaimer pariteleri | Unit/Bileşen (mobil) | [ ] |
| **SC-13** | **NFR-12, `11`§3** | **Güvenlik:** Kullanıcı A, B'nin holding id'siyle istek atar → **404** (IDOR/BOLA yok). **Kimlik açılmadan zorunlu.** | Integration | [x] (T1.6; GET/DELETE başkasının id'si→404, boş liste/sıfır özet, integration) |
| SC-14 | NFR-4, `11`§5 | **Güvenlik:** Eşik üstü istek → **429** (rate limit) | Integration | [ ] |
| SC-15 | NFR-4, `11`§4 | **Güvenlik:** Hata yanıtında stack trace / iç detay sızmıyor | Integration | [x] (T0.13 ObservabilitySecurityTests.Exception_handler_masks_internal_details) |
| SC-16 | NFR-4 | **Güvenlik:** Token'sız/expired istek → 401 (Faz 5+) | Integration | [ ] |
| SC-P1 | NFR-6/10, `10`§2 | **Performans:** N eşzamanlı kullanıcı `summary` → p95 bütçe içinde (cache isabet/ıska) | Yük (k6/NBomber) | [ ] |

> Durum: `[ ]` yazılmadı · `[~]` test var, kod devam · `[x]` yeşil.

---

## 6. Görev Başına İş Akışı (her geliştirme adımı)

Bir backlog görevini (`T-x.y`) alırken:

```
1. SENARYO: İlgili FR/NFR'den senaryo(ları) §5 kataloğuna ekle (Given-When-Then).
2. TEST:    Senaryoyu test koduna dök (unit ve/veya integration).
            - Hesaplama içeren her görevde unit test ZORUNLU.
            - Bir "olay" (dış API/hata/akış) varsa integration/senaryo testi.
3. UYGULA:  Kodu yaz; testleri yeşile getir.
4. ÇALIŞTIR: dotnet test  /  npm test  → hepsi yeşil.
5. KAPAT:   Görev "tamam"; TASKLOG'a **Test** alanıyla işle (CLAUDE.md §11),
            08-BACKLOG'da [x], §5 kataloğunda senaryo [x].
```

> "Senaryo-önce" = adım 1-2 koddan önce başlar. Katı TDD (önce kırmızı test)
> şart değil; ama **görev, testleri yeşil olmadan kapanmaz** (yeşil-kapı).

---

## 7. Kapsam (Coverage) Beklentileri

| Alan | Hedef |
|------|-------|
| Hesaplama servisleri (`PortfolioCalculation`, `CurrencyConversion`, reel getiri, ort. maliyet) | **~%100 satır + kritik sınır durumları** (sıfır miktar, tek varlık, negatif kâr, kur eksik) |
| Endpoint'ler (CRUD + summary + commentary) | En az "mutlu yol" + 1 hata yolu integration |
| Dış servis olayları (fiyat/LLM hata) | Her fallback yolunun en az 1 testi (NFR-5, FR-3.2) |
| Mobil util/kritik bileşen | Format util %100; disclaimer & trend rengi gibi NFR'ye bağlı davranışlar |

> Sayısal kapsam yüzdesini fetiş yapma; **kritik para hesabı ve hata yolları**
> kapalı olsun. Yanlış rakam veya çöken dış servis = kabul edilemez.

---

## 8. Sınır Durumları Kontrol Listesi (hesaplama)

Her hesaplama testinde bunları düşün:
- Sıfır miktar / boş portföy (bölme-sıfır koruması).
- Tek varlık (%100 ağırlık).
- Negatif kâr (zarar) — renk/işaret doğru.
- Eksik kur / eksik fiyat → anlamlı davranış (hata değil çökme değil).
- `decimal` hassasiyeti — yuvarlama yalnızca gösterimde, hesap tam (NFR-1, NFR-7).
- Büyük sayılar (binlik ayraç, tabular hizalama gösterimde).

---

## 9. CI'ya Doğru (ileri not)

Git kurulunca (T0.1) basit bir doğrulama: commit/PR öncesi `dotnet test` +
`npm test` yeşil. İleride GitHub Actions ile otomatikleştirilebilir (Faz 2+).
Erken aşamada **lokal yeşil-kapı** disiplini yeterli.
