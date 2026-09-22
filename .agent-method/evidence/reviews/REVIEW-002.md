---
review_id: REVIEW-002
runtime: claude-code
timestamp: "2026-09-22"
scope: "GD-002 — BES iki fon havuzu, migration + veri taşıma, okuma yolu, nakit kuralı, detay para birimi, geri linki"
base_ref: 929771a
head_ref: 4bc094e
active_gd: GD-002
reviewers:
  - correctness
  - data-integrity
  - migration-safety
  - rollback
  - api-contract
  - ui-regression
  - architecture
findings:
  - id: RV-005
    severity: high
    evidence: proven
    category: correctness
    file: web/src/routes/HoldingDetailPage.tsx
    line: 284
    summary: "Fiyatı henüz girilmemiş yabancı para birimli kalemde detay 'Toplam maliyet' tutarını YANLIŞ birimle etiketliyor: USD maliyeti ₺ ile gösteriyor (ör. '$43,51' yerine '₺43,51')."
    rationale: "`heroCurrency` yalnız `currentValueNative` doluysa `h.currency` seçiyor; oysa `heroCost = totalCostNative ?? totalCost` ve backend `totalCostNative`'i HER ZAMAN doldurur. Hisse/fon oluşturulunca `CurrentPrice = null` başlar (canlı fiyat yalnız altın/döviz) → `currentValueNative` null → `heroCurrency = baseCurrency` ama `heroCost` USD → USD tutar ₺ etiketiyle. Yeni bir yabancı hisse eklendiği HER seferinde tekrarlanır ve bu, kullanıcının bu GD'de bildirdiği hatanın (yanlış birim etiketi) aynı sınıfıdır. Web testi yalnız fiyatlı durumu kapsıyor."
    status: resolved
    resolution:
      resolved_at: "2026-09-22"
      evidence_ref: "web/src/routes/HoldingDetailPage.tsx · HoldingDetailPage.test.tsx (fiyatsız çapraz kur testi)"
      note: "Kullanıcı kararı (\"hepsini kapat\"). heroCurrency native alanın VARLIĞINA göre seçiliyor. Test eski kodla KIRMIZI, düzeltmeyle yeşil (mutasyonla doğrulandı)."
  - id: RV-006
    severity: medium
    evidence: proven
    category: api-contract
    file: backend/src/Finans.Infrastructure/Services/HoldingService.cs
    line: 268
    rule_or_decision: D-019
    summary: "`PUT /api/holdings/{id}` (currentPrice) BES ve nakit için 200 OK dönüyor ama HİÇBİR ŞEY değişmiyor — okuma yolu bu iki türde fiyatı türettiği için yazılan değer sessizce yok sayılıyor."
    rationale: "GD-002'den sonra BES değeri iki fon alanından, nakit fiyatı `AssetPricing`'den türetiliyor; `UpdateAsync` ise hâlâ `Holdings.CurrentPrice`'a yazıp başarı dönüyor. Web bu iki türde butonu gizlediği için bugün kullanıcıya yansımıyor, ama API sözleşmesi yalan söylüyor (mobil kol ya da eski istemci 'güncellendi' görür, değer değişmez). 400 + açıklayıcı hata (BES → `PUT /bes` iki alan; nakit → sabit) doğru davranış."
    status: resolved
    resolution:
      resolved_at: "2026-09-22"
      evidence_ref: "HoldingService.UpdateAsync · PortfolioHistoryApiTests.Put_current_price_is_rejected_for_bes_and_cash_instead_of_silently_ignored"
      note: "Kullanıcı kararı (\"hepsini kapat\"). BES → 400 derived_for_bes (doğru yol: PUT /bes iki alan); nakit → 400 fixed_price. Değerler bozulmuyor."
  - id: RV-007
    severity: medium
    evidence: strong
    category: architecture
    file: backend/src/Finans.Infrastructure/Services/HoldingService.cs
    line: 615
    rule_or_decision: D-019
    summary: "`ToBesDto` 'fonda fiilen olan katkı' toplamını hâlâ KENDİ switch'iyle hesaplıyor; tek tanım `BesCalculator.DepositedTotals`'a bağlanmadı."
    rationale: "Bu GD'de canlı veride yakalanan hatanın (iki havuzun getirisi %39 ↔ %48) kök nedeni tam olarak bu kavramın iki yerde ayrı yazılmasıydı. Değer yolu ve seri `DepositedTotals`'a bağlandı, detay DTO'su bağlanmadı. Bugün iki kod aynı sonucu veriyor — ama biri değişince sapma sessizce geri gelir. (Bekleyen tutarları da hesapladığı için doğrudan değiştirilemiyor; `DepositedTotals` bekleyenleri de döndürecek şekilde genişletilmeli.)"
    status: resolved
    resolution:
      resolved_at: "2026-09-22"
      evidence_ref: "BesCalculator.ContributionTotals · ToBesDto · BesCalculatorTests.ContributionTotals_classifies_each_status_once"
      note: "Kullanıcı kararı (\"hepsini kapat\"). Tek sınıflandırma (Deposited · StatePending · Future) BesCalculatorda; ToBesDto kendi switchini bıraktı. DepositedTotals alt kümesi — ayrışamaz."
  - id: RV-008
    severity: medium
    evidence: proven
    category: rollback
    file: backend/src/Finans.Infrastructure/Persistence/Migrations/20260920202440_BesSeparateFundValues.cs
    summary: "Migration geri alınırsa (Down) kullanıcının girdiği iki fon değeri kaybolur VE BES değeri sessizce BAYAT bir sayıya döner: `Holdings.CurrentPrice` GD-002'den sonra BES için hiç güncellenmiyor."
    rationale: "`UpdateBesAsync` yalnız `BesDetails.OwnFundValue/StateFundValue` yazıyor; eski tek alan (`Holdings.CurrentPrice`) migration anındaki değerde donuyor. Down iki kolonu düşürünce eski kod o donmuş değeri okur — kullanıcı aylar sonra geri dönüşte eski bir fon değeri görür, uyarı almaz. Seçenekler: Down öncesi iki değerin toplamını CurrentPrice'a geri yazan SQL, ya da migration'ı açıkça 'geri alınamaz' işaretlemek."
    status: resolved
    resolution:
      resolved_at: "2026-09-22"
      evidence_ref: "HoldingService.LegacyBesTotal · migration Down SQL · PortfolioHistoryApiTests.Bes_legacy_current_price_stays_in_sync_and_derived_value_is_never_persisted"
      note: "Kullanıcı kararı (\"hepsini kapat\"). Eski tek alan iki havuzun toplamıyla senkron (oluşturma + güncelleme); Down kolonları düşürmeden önce toplamı geri yazıyor. Gerçek Postgresde transaction içinde doğrulandı, ROLLBACK."
  - id: RV-009
    severity: low
    evidence: heuristic
    category: data-integrity
    file: backend/src/Finans.Infrastructure/Persistence/Migrations/20260920202440_BesSeparateFundValues.cs
    summary: "Veri taşıma SQL'i 'bugün'ü veritabanı sunucusunun saat diliminde (`current_date`, konteynerde UTC) alıyor; uygulama TR saatini (UTC+3) kullanıyor."
    rationale: "Ay sonu devlet katkısı yatma gününde, gece 00:00–03:00 TR arasında migration koşarsa bir katkının 'yatmış/yolda' sınıflandırması uygulamayla ayrışabilir → bölme oranı çok küçük sapar. Tek seferlik taşıma ve kullanıcı gerçek değerleri girecek; etkisi sınırlı. Ayrıca SQL iki kolonu ayrı yuvarlıyor (kalan atanmıyor) → toplam 0,000001 sapabilir (canlı kontrolde sapmadı)."
    status: resolved
    resolution:
      resolved_at: "2026-09-22"
      evidence_ref: "migration BesSeparateFundValues Up SQL"
      note: "Kullanıcı kararı (\"hepsini kapat\"). Bugün TR saatinde (UTC+3, uygulamayla aynı); kendi havuzu 2 ondalığa yuvarlanıyor, kalan devlet havuzuna → toplam kuruşu kuruşuna eşit. Gerçek Postgresde transaction içinde doğrulandı (toplam eşit: t), ROLLBACK. ⚠ Migration zaten uygulanmış DBleri etkilemez; yalnız yeni kurulumlar."
  - id: RV-010
    severity: low
    evidence: heuristic
    category: data-integrity
    file: backend/src/Finans.Infrastructure/Services/HoldingMapping.cs
    summary: "`ApplyReadPosition` İZLENEN (tracked) entity'lerin `CurrentPrice`'ını bellekte değiştiriyor; aynı DbContext'te sonradan bir `SaveChanges` çalışırsa türetilmiş BES değeri (hak ediş uygulanmış) kalıcı olarak `Holdings.CurrentPrice`'a yazılır."
    rationale: "Desen yeni değil (AvgCost/Quantity zaten böyle türetiliyordu), bugün okuma yollarında okuma sonrası kayıt yok. Ama GD-002 ile risk büyüdü: kalıcılaşan değer RV-008'deki geri alma yolunu da bozar. Okuma sorgularında `AsNoTracking` ya da türetilmiş değeri entity yerine DTO'da taşımak bu sınıfı kapatır."
    status: resolved
    resolution:
      resolved_at: "2026-09-22"
      evidence_ref: "AsNoTracking: HoldingService · PortfolioService · PortfolioHistoryService · ScenarioService"
      note: "Kullanıcı kararı (\"hepsini kapat\"). Okuma sorguları izlemesiz. Test: servis aynı DbContextte çağrılıp SaveChanges yapılıyor; AsNoTracking kaldırılınca türetilmiş değer (136.900) DBye SIZIYOR, düzeltmeyle 159.000 kalıyor (mutasyonla doğrulandı)."
---

# REVIEW-002 — GD-002 (BES iki havuz + düzeltmeler) · 2026-09-22

**Kapsam:** `929771a..4bc094e` — 23 dosya, +3316/−47 (migration Designer dosyaları dahil).
Değişimin niteliği **karışık ve yüksek riskli**: DB migration **+ veri taşıma**, parasal hesap,
okuma yolunun beş yüzeyi, API sözleşmesi (DTO alanları), web. Bakış açıları buna göre:
correctness · data-integrity · migration-safety · rollback · api-contract · ui-regression ·
architecture. Kimlik/yetkilendirme dokunuşu yok (yeni uç yok; mevcut uçlar zaten `UserId`
kapsamlı — `UpdateBesAsync`/`CreateBesAsync` kontrol edildi).

⚠ **Kendi yazdığım kodu inceledim.** Bu yüzden özellikle "çalışıyor gibi görünen ama test
edilmemiş dal" arandı; RV-005 tam olarak böyle bulundu.

## Özet

| Önem | Sayı |
|---|---|
| critical | 0 |
| high | **1** |
| medium | 3 |
| low | 2 |

**Açık high bulgu var (RV-005).** INV-18 critical'ları engelleyici sayar; high engelleyici
değil, ama bu bulgu kullanıcının bu GD'de bildirdiği hatanın aynı sınıfı olduğu için
**kabul öncesi kapatılmasını öneriyorum.**

## Bulguların okunuşu

**RV-005 (high)** — Yeni bir yabancı hisse eklendiğinde (henüz fiyatı yok) detay sayfası
dolar maliyeti ₺ ile gösteriyor. Kullanıcının ilk bildirdiği hatanın **aynısı**, bu sefer
fiyatsız dalda. Test yalnız fiyatlı durumu kapsıyordu. Düzeltme tek satır (`heroCurrency`
native alan varlığına göre seçilmeli) + fiyatsız durum testi.

**RV-006 + RV-008 + RV-010 (tek tema)** — GD-002 BES ve nakit için `Holdings.CurrentPrice`'ı
**türetilmiş** bir değere çevirdi, ama o alan hâlâ yazılabilir ve kalıcı. Sonuç: yazma
sessizce yok sayılıyor (RV-006), geri alma bayat değer gösteriyor (RV-008), türetilmiş değer
yanlışlıkla kalıcılaşabilir (RV-010). Kök çözüm: bu iki tür için `CurrentPrice`'ı açıkça
"kaynak değil" kılmak (yazmayı reddet + okumada `AsNoTracking`).

**RV-007 (medium)** — Bu GD'de canlıda yakalanan %39/%48 hatasının kök nedeni aynı
kavramın iki yerde yazılmasıydı; bir yer hâlâ ayrı yazılı.

## Doğrulanan noktalar (bulgu değil)

- **Parasal hesap saf ve testli:** `BesCalculator` (iki havuz, hak edişe göre değer, bölme,
  `DepositedTotals`) ve `AssetPricing` birim testli; `decimal`; yuvarlama gösterimde.
- **Tek kural, beş yüzey:** liste = özet = değer serisi son noktası entegrasyon testiyle
  kilitli (%35 hak ediş senaryosu) ve canlı veride doğrulandı (759.165,52).
- **Veri taşıma canlıda doğrulandı:** toplam korundu (323.390,45), oran = katkı oranı.
- **Per-user izolasyon:** yeni alanlar mevcut `UserId` kapsamlı uçlardan geçiyor; yeni uç yok.
- **Sır/PII yok;** log'a yeni alan yazılmıyor.
- **Geri uyumluluk:** yalnız toplam fon değeri gönderen istemci (eski `CreateBesRequest`)
  bölünerek karşılanıyor — girilen değer kaybolmuyor (regresyon testli).
- **Testler:** Application 307/307 · web 146/146 · `tsc -b` temiz · Integration 179/183
  (4 = INC-001, ortam).

## Sıradaki adım

Altı bulgunun durumu **insan kararı** bekliyor. Önerim: **RV-005'i kabul öncesi kapat**
(küçük, kullanıcıya görünür, bildirilen hatanın aynısı). RV-006/008/010'u tek bir
"türetilmiş CurrentPrice" düzeltmesi olarak ele al. RV-007 küçük bir refactor. RV-009 kabul
edilebilir.
