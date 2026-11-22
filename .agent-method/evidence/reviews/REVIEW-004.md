---
id: REVIEW-004
gd: GD-004
base_ref: f713620
head_ref: 0d3b930
created: "2026-11-22"
reviewers:
  - content-correctness        # bağımsız alt-ajan (yazarın kendisi değil)
  - compliance-d002-m7         # bağımsız alt-ajan (yazarın kendisi değil)
  - seed-integrity             # orkestratör
  - test-adequacy              # orkestratör
  - curriculum-conformance     # orkestratör
  - ui-regression              # compliance incelemesinin figür katmanı
partial: false
findings:
  - id: RV-019
    severity: high
    evidence: proven
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3262
    rule_or_decision: D-012
    summary: "S0-L5 ile S0-L9 alış/satış kotasyonunu TERS adlandırıyor. S0-L5: 'alabileceğin fiyat (alış)'. S0-L9: 'Satış kotasyonu … sen alırken bu sayıyı ödersin'. Aynı sayı iki derste iki ayrı adla."
    rationale: "S0-L9'un kullanımı doğru olan (tabelalar kurum perspektifindedir: kurumun 'satış'ı senin ödediğin). Hata S0-L5'te. Ama S0-L9 Deep bloğunda S0-L5'e geri referans veriyor, dolayısıyla öğrenci iki tanımı yan yana görüp doğru olanı yanlış sanacak. Düzeltme S0-L5'e dokunmayı gerektirir (GD-004 kapsamı dışı) VEYA S0-L9'a köprü cümlesi eklemeyi (kapsam içi)."
    status: open
  - id: RV-020
    severity: high
    evidence: strong
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3267
    summary: "'Satış kotasyonu HER ZAMAN alış kotasyonundan yüksektir' mutlak iddiası yanlış; aynı iddia ayrıca doğru/yanlış quiz sorusu olarak soruluyor (doğru cevap 'Doğru' işaretli)."
    rationale: "Gerekçe ('birbirini bulur') yalnız SÜREKLİ İŞLEM sırasında geçerli. Emirlerin eşleşmeden biriktiği tek fiyat (açılış/kapanış) seanslarında defter çaprazlanabilir — denge fiyatı tam olarak böyle bulunur. RV-014 ile aynı kalıp: doğru çekirdek + hak edilmemiş 'her zaman'. Dikkatli bir öğrenci 'Yanlış' deyip haklı olabilir."
    status: open
  - id: RV-021
    severity: medium
    evidence: strong
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3405
    summary: "'Son fiyat, binlerce pazarlığın ortalamasına yakın bir şeydir' yanlış — son işlem fiyatı tanım gereği TEK bir işlemin fiyatıdır, ortalama değildir. Aynı hata quiz Q8 açıklamasında tekrarlanıyor."
    rationale: "Ders bunu 180 satır önce doğru söylüyor ('en son gerçekleşen işlemin fiyatı'). Karşı örnek: gün içinde fiyat %8 yükseldiyse son fiyat o günün ortalamasının çok uzağındadır. Likit piyasada son fiyatın güvenilir olmasının gerçek nedeni ortalama değil, o anda rekabet eden çok sayıda teklifin fiyatı KUŞATMASI."
    status: open
  - id: RV-022
    severity: medium
    evidence: strong
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3362
    summary: "İki mekân örneği anlatmak istediği mekanizmayı desteklemiyor: (a) arbitraj son işlem fiyatından değil KOTASYONDAN yapılır — ders bunu iki blok önce kendisi öğretiyor; (b) verilen sayılarla arbitraj yok: 0,40 ₺ görünür fark, birleşik makasın (0,20+0,90=1,10 ₺) altında."
    rationale: "Orta noktalardan kotasyon türetilirse: 1. yer satış 100,10 / 2. yer alış 99,95 → ucuzdan alıp pahalıya satmak 0,15 ₺ ZARAR. Ayrıca 'fark … küçük KALIR' gelecek kipli bir piyasa beyanı (D-002 hassasiyeti, uyum incelemesi de bağımsız olarak işaretledi)."
    status: open
  - id: RV-023
    severity: medium
    evidence: strong
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3322
    summary: "'Makas yüzünden ekranda ödediğinden YÜKSEK bir sayı görmek' dersin kendi modelinde imkânsız: alan kişi satış kotasyonunu öder, o da iki sayının yükseğidir."
    rationale: "Simetri kurmak için yazılmış ama ulaşılamaz bir durumu tarif ediyor. Simetri satış tarafına taşınmalı: sattıktan sonra ekranda aldığından yüksek bir rakam (satış kotasyonu) görmek 'erken sattım' demek değildir."
    status: open
  - id: RV-024
    severity: medium
    evidence: strong
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3344
    summary: "\"'Herkes almak istiyor' diye bir durum OLAMAZ\" — iddia NİYET durumu hakkında mutlak, gerekçe ise İŞLEM hakkında. Yalnız alıcının olduğu anlar gerçekten olur (fiyat sınırında satıcı kalmaması); imkânsız olan o anda işlemin gerçekleşmesi."
    rationale: "Dersin kendi 2. bloğu bunu zaten kabul ediyor ('buluşmazlarsa fiyat da oluşmaz … ortada sadece iki niyet vardır'). Kalın puntolu başlık cümlesi akılda kalan kısım ve karşı örneği var. Düzeltme: 'bir İŞLEM tek taraflı olamaz'."
    status: open
  - id: RV-025
    severity: medium
    evidence: strong
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3429
    summary: "LiveContext bloğu döviz ve altını mekanizmanın DIŞINA atıyor ('kalan kısımda fiyat başka yollarla oluşur — bazısında bir kur'). Döviz ve altın da alış/satış kotasyonuyla fiyatlanır; tabela bu dersin kotasyonlarının ta kendisidir."
    rationale: "Olgusal yanlış ve kaçırılmış fırsat: Türkiye'de bir öğrencinin bid-ask'i gördüğü EN tanıdık yer döviz tabelasıdır. Üstelik kullanıcının portföyüne dokunan bölümde. Gerçekten mekanizma dışında kalanlar: hesaplanan birim pay değerleri, sabit değerli kalemler, mevduat."
    status: open
  - id: RV-026
    severity: medium
    evidence: proven
    category: ui-regression
    file: web/src/components/LessonFigure.tsx
    line: 2509
    summary: "same-asset-two-venues figüründe makas çubukları 0,20 ₺ ve 0,90 ₺ (4,5 kat) olmasına rağmen BİREBİR aynı genişlikte (68×10) çiziliyor — çubuk hiçbir bilgi taşımıyor. Ayrıca tek fark renk ve o renk taraf tutuyor (fig-seg-lead vurgulu vs fig-bar-muted soluk)."
    rationale: "Dar makaslı VE daha ucuz olan yer vurgulanmış, diğeri geri çekilmiş. Künye yasal notu 'makas dar olan yeri seç' yönlendirmesini açıkça yasaklıyor; metin yasağa uyuyor, figür uymuyor. Düzeltme: genişlikler değere oranlansın (ör. 16px/72px), ikisi de aynı nötr sınıfa alınsın."
    status: open
  - id: RV-027
    severity: medium
    evidence: strong
    category: ui-regression
    file: web/src/components/LessonFigure.tsx
    line: 2461
    rule_or_decision: D-002
    summary: "spread-cost figüründe makas farkı fig-bar-neg (coral/kayıp rengi) ile çiziliyor — oysa dersin BİR SONRAKİ bloğu tam bunu düzeltmek için var: 'Bu bir maliyet farkı, bir kayıp kaydı değil.'"
    rationale: "Figür, bir blok sonra çürütülecek yanılgıyı görsel olarak önceden onaylıyor. RV-016'nın (GD-003) aynı sınıfından tekrar: nötr bilgiye kazanç-kayıp paleti yüklemek. Bilgiyi zaten '2,00 ₺ / %2' metni taşıyor; renk yalnız yargı taşıyor. Düzeltme: fig-seg."
    status: open
  - id: RV-028
    severity: medium
    evidence: heuristic
    category: ui-regression
    file: web/src/components/LessonFigure.tsx
    line: 2566
    rule_or_decision: D-002
    summary: "thin-market figüründe 'çok işlem' paneli mint (fig-dot-steady) + 'makas dar · fiyat çok şey anlatır', 'az işlem' paneli coral (fig-dot-volatile) + 'makas geniş · fiyat sıçrar' — renk ve altyazı birlikte 'dar makas iyi' izlenimi kuruyor."
    rationale: "Künye yasal notunun yasakladığı okumayı sözcük kullanmadan kuruyor. Metin bu izlenimi tek tek söküyor ama figürler ders metninden bağımsız okunur (kaydırılarak geçilir, ekran görüntüsü olarak paylaşılır). Nötrleyici figürde yok."
    status: open
  - id: RV-029
    severity: medium
    evidence: proven
    category: source-integrity
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3446
    rule_or_decision: D-012
    summary: "Source bloğu SPK ve Borsa İstanbul'u doğrulanmış kaynak gibi sunuyor; künye (§S0-L9 Kaynaklar) ikisini de ⟨doğrulanacak⟩ işaretliyor."
    rationale: "T6.21 ile aynı sınıf: kurumsal etiketli ama teyit edilmemiş kaynak beyanı, 'kaynak daima görünür' iddiasını zayıflatır. Ya kaynaklar (belge + tarih) teyit edilip künye işareti kaldırılmalı, ya da blok teyit durumunu dürüstçe yansıtmalı."
    status: open
  - id: RV-030
    severity: low
    evidence: strong
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3263
    summary: "'Sen alırken bu sayıyı ödersin' kayıtsız genelleme: yalnız HEMEN işlem görmek isteyen ve miktarı kotasyon derinliğini aşmayan emir için doğru."
    rationale: "Yeni başlayan için basitleştirme savunulabilir, ama RV-023'teki hatanın doğrudan kaynağı. Tek kelime yeter: 'Hemen almak istersen bu sayıyı ödersin.'"
    status: open
  - id: RV-031
    severity: low
    evidence: strong
    category: ui-regression
    file: web/src/components/LessonFigure.tsx
    line: 2384
    summary: "last-price-is-history figüründe son işlem noktası fig-dot-volatile (coral), geçmiş işlemler fig-dot-steady (mint). Son işlem fiyatı nötr bir olgudur; dersin tezi 'bu bir geçmiş kaydıdır', 'bu oynak/kötü bir sayıdır' değil."
    rationale: "RV-016 sınıfı. Vurgu renk yerine biçimle verilmeli (daha büyük r veya halka); zaten 'son' etiketi var."
    status: open
  - id: RV-032
    severity: low
    evidence: proven
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 4248
    summary: "Quiz Q8'in 'Fiyat her zaman gerçek değerin altında kalır' çeldiricisi açıklamada hiç çürütülmüyor; açıklama yalnız ince piyasayı anlatıyor."
    rationale: "M7a çeldiricide yanlış ifadeye izin veriyor ama yanlışın yanlışlığı söylenmek şartıyla. Çeldirici, dersin 5. bloğunun söktüğü varsayımı ('bir gerçek değer vardır') yeniden kuruyor."
    status: open
  - id: RV-033
    severity: low
    evidence: proven
    category: content-consistency
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3419
    summary: "Metinde sabit ders numarası: 'likidite kavramına bağlanıyoruz (Ders 5)'. RV-015'in çözüm notu (2026-11-22, aynı oturum) bunun tersini karara bağlamıştı: 'Ders numarası bilerek yazılmadı (OrderIndex kayabilir).'"
    rationale: "Aynı dosyada iki farklı konvansiyon. T6.22 Set OrderIndex'lerini kaydıracak. Referansın hedefi doğru (likidite gerçekten S0-L5'te), sorun numaranın sabitlenmesi."
    status: open
  - id: RV-034
    severity: low
    evidence: heuristic
    category: compliance-m7
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3306
    rule_or_decision: D-002
    summary: "'Sık alıp satan biri bu bedeli her turda öder' (quiz açıklamasında da tekrar) — işlem sıklığı üzerinden örtük davranış tercihi ima edebiliyor."
    rationale: "Künye öğrenme çıktısı #3 makası maliyet olarak hesaplatmayı zorunlu kıldığı için cümle büyük ölçüde savunulabilir; normatif sıfat ve kişiye hitap yok. RV-015 precedent'ine en yakın cümle olduğu için insan kararına bırakılıyor. Ucuz sertleştirme: 'Bu bedel işlem başına doğar: iki işlem iki kez… Kaç işlem yapacağın senin kararın.'"
    status: open
  - id: RV-035
    severity: low
    evidence: heuristic
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3246
    summary: "'Fiyatın oluşması için tarafların değer konusunda ANLAŞMAMIŞ olması GEREKİR' fazla güçlü: nakde ihtiyacı olduğu için satan biri karşı tarafla değer konusunda hemfikir olabilir. İşlemi doğuran şey rezervasyon fiyatı farkıdır."
    rationale: "Metnin 'kendisi için' kaydı bunu büyük ölçüde kurtarıyor; 'genellikle' eklemek yükü kaldırır."
    status: open
  - id: RV-036
    severity: low
    evidence: heuristic
    category: content-consistency
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 4203
    summary: "Quiz Q4'ün kipi soruyla uyuşmuyor: soru 'alıp aynı anda satsan ne olur' (tur tamamlanmış), doğru şık '%2 geride başlarsın' (hâlâ elde tutuyormuş gibi)."
    rationale: "Cevap yine tek savunulabilir cevap. 'Geride başlamak' ders metninden alınma, tanıdıklığı pedagojik tercih olabilir — bu yüzden heuristic."
    status: open
  - id: RV-037
    severity: low
    evidence: heuristic
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 4225
    summary: "Quiz Q6 açıklaması 'satış kotasyonu genellikle bundan biraz yüksektir' diyor; son işlem satış kotasyonunda gerçekleşmiş ve o seviyede miktar kalmışsa ikisi EŞİT olur. 'Genellikle' yumuşatıyor ama eşitlik dışlanmamalı."
    status: open
  - id: RV-038
    severity: low
    evidence: heuristic
    category: content-consistency
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3274
    summary: "Makas için iki ton arasında gerilim: 'dar iyi, geniş kötü demek değildir' (3274) ile 'makas ne kadar genişse bu bedel o kadar büyür' (3307). Çelişmiyorlar (maliyet ≠ varlık yargısı) ama okuyucu için gerilimli."
    rationale: "Olgusal hata değil, üslup. Tek cümle netlik katabilir: 'Geniş makas kötü varlık demek değil; alıp satmanın o varlıkta daha pahalı olduğu demek.'"
    status: open
---

# REVIEW-004 — GD-004 (S0-L9 · Fiyat nereden çıkıyor?)

**Kapsam:** `f713620..0d3b930` — 6 dosya, +854/−27. Kimlik/migration/parasal hesap
dokunuşu yok; değişim **seed içeriği + SVG figürler + yapısal testler.**

## Yöntem notu

İçerik doğruluğu ve D-002 uyumu **bağımsız alt-ajanlara** verildi; dersi yazan
taraf kendi metnini inceleyen taraf olmadı. Seed bütünlüğü, test yeterliliği ve
künye uyumu orkestratörde kaldı. Bu ayrım sonucu belirledi: **yazarın kendi
"M7 taraması temiz" beyanı figür katmanını tamamen kaçırmıştı** (RV-026…RV-028,
RV-031), ve içerik doğruluğunda beş ayrı olgusal hata ancak bağımsız okumayla
çıktı.

## Temiz çıkan kontroller

Uydurma bulgu üretmemek için açıkça kaydediliyor:

- **Aritmetik doğru.** 100,00 − 98,00 = 2,00 → %2. Bölenin seçimi (orta nokta
  değil, ödenen tutar) anlatılan şeye uygun ve S0-L5'teki konvansiyonla tutarlı.
- **"Her satışın karşısında bir alış vardır"** doğru ve doğru nitelenmiş
  (gerçekleşmiş işlem hakkında, emir akışı hakkında değil).
- **Seed bütünlüğü temiz:** `OrderIndex = 9` track içinde tekil, slug tekil, beş
  yeni kavram anahtarının hiçbiri daha önce yoktu, tüm eklemeler `AnyAsync`
  kapılı, kimlikler deterministik, `Build` çağıranın sırasını koruyor.
- **Test sayıları semantik olarak doğru**, körü körüne büyütülmemiş
  (`147 → 160` = 173 eksi 13 LiveContext bloğu).
- **Künye uyumu birebir:** 14 aşamanın katman/tür eşlemesi, 10 figür anahtarı,
  kavramlar, `stock_weight` token'ı, süre ve seviye künyeyle örtüşüyor.
- **Özel ad yasağı ihlal edilmemiş** — 10 figürün tamamı dahil. Kurum adları
  yalnız Source bloğunda (ama bkz. RV-029: teyit durumu).
- **Quiz yapısı** 9 soru / 3-3-3; çoktan seçmelilerde işaretli doğrular gerçekten
  doğru; çeldiricilerin çoğu açıkça yanlış (istisna RV-032).

## Özet

| Önem | Adet | Bulgular |
|---|---:|---|
| critical | 0 | — |
| **high** | **2** | RV-019 (iki ders ters adlandırma) · RV-020 ("her zaman" + doğru/yanlış sorusu) |
| medium | 8 | RV-021…RV-029 arası |
| low | 9 | RV-030…RV-038 arası |

**Açık critical yok** → INV-18 gereği GD kapanışını bloke eden bulgu yok.
Ancak **iki high bulgu da yayına çıkmadan düzeltilmeli**: biri öğrenciye iki
dersin çelişen tanımını gösteriyor, diğeri doğru/yanlış sorusunda dikkatli
öğrenciyi haksız yere yanlışlıyor.

## Kapsam dışı kalan, bildirilen

- `Blk` tanımının üstündeki XML doküman bloğu, artık var olmayan bir metoda ait
  `<param name="live">` / `<param name="figure">` etiketleri taşıyor ve `Build`'in
  sırayı koruduğu hâlde "sıra: Core→Context→Deep→…" diyor. **GD-004'ün getirdiği
  değil**, yardımcılar bölgesinde zaten vardı.
- **RV-019'un kökü S0-L5'te.** Düzeltmenin tam hâli GD-004 kapsamı dışına
  taşıyor; S0-L9'a köprü cümlesi eklemek kapsam içi bir azaltma.

## Devralınan borç

**TD-001 büyüdü:** S0-L9'un 10 figürü de render testsiz eklendi; borç artık
**19 figürü** kapsıyor (S0-L8'in 9'u + S0-L9'un 10'u). Bu incelemenin figür
bulgularının (RV-026…RV-028, RV-031) hiçbiri otomatik testle yakalanamazdı.
