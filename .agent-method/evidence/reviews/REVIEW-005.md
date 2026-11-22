---
review_id: REVIEW-005
runtime: claude-code
timestamp: "2026-11-22"
scope: "GD-005 — S0-L10 'Bir alım nasıl gerçekleşir?': 14 aşamalı ders gövdesi, 9 soruluk mini test, seed kaydı (ders · ön-koşul · 6 kavram), 10 figür, yapısal test sayımları + M11 testi, künye figür sayısı düzeltmesi"
base_ref: 55682bf
head_ref: 95b15b4
active_gd: GD-005
reviewers:
  - content-correctness        # bağımsız alt-ajan (yazarın kendisi değil)
  - compliance-d002-m7         # bağımsız alt-ajan (yazarın kendisi değil)
  - ui-figure-regression       # compliance incelemesinin figür katmanı
  - seed-integrity             # orkestratör
  - curriculum-conformance     # orkestratör
  - quiz-structure             # orkestratör
  - test-adequacy              # orkestratör
findings:
  # ── HIGH ────────────────────────────────────────────────────────────────
  - id: RV-041
    severity: high
    evidence: proven
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3322
    summary: "Makasın birimi iki derste çelişiyor: S0-L9 'işlem başına doğar: iki işlem iki kez' (3322, 4560), S0-L10 'tur başına doğar: iki tur iki kez' (3694, 4700). Aynı bedel iki ayrı birimle öğretiliyor."
    rationale: "Doğru olan S0-L10'dur: makas bir alım + bir satımın TOPLAMINDA bir kez doğar (aldığın fiyat ile satabileceğin fiyat arasındaki mesafe). 'İki işlem iki kez' maliyeti ikiye katlıyor ve S0-L10'un aritmetiğiyle (10.000 ₺ turda tek bir 30 ₺ makas) doğrudan çelişiyor. KÖK NEDEN: bu hata RV-034 düzeltmesinde girdi — özgün S0-L9 cümlesi 'her turda öder' diyordu, yönlendirme yumuşatılırken birim de değiştirildi. Bir bulgunun düzeltmesi yeni bir bulgu üretti."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz"
      note: "S0-L9 gövdesi ve quiz açıklaması 'tur başına' oldu: bir alım ile onu izleyen satımın toplamında bir kez. İki ders artık aynı birimi öğretiyor. RV-034'ün soktuğu regresyon geri alındı — düzeltme turlarının da inceleme gerektirdiği kayda geçti."
  - id: RV-042
    severity: high
    evidence: proven
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3594
    summary: "'Piyasa emri gerçekleşmeyi garanti eder' mutlak olarak öğretiliyor — tanım tablosunda (3594), anlatımda (3597) ve Easy Q2'nin doğru cevabı olarak (4626-4635). Garanti değildir: karşı tarafta emir yoksa gerçekleşmez."
    rationale: "Karşı kotasyon yokken, sığ defterde ya da işlem durdurulduğunda piyasa emri gerçekleşmez. S0-L9'un 'ince piyasa' bölümü (3440-3458) öğrenciye tam bunu öğretiyor: az sayıda teklif olan bir piyasada karşı taraf her an hazır değildir. Yani serinin ÖNCEKİ dersinin çürüttüğü bir mutlaklık, bu derste tanım satırı + quiz cevabı olarak pekiştiriliyor. Doğru çerçeve: 'karşı taraf varsa hemen gerçekleşir' / 'gerçekleşme önceliği fiyatın önündedir'."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz · compose yığınında tarayıcıyla görüldü"
      note: "Tablo satırları 'Öne aldığı' / 'Karşılığında bıraktığı' oldu; anlatıma 'karşı tarafta emir varsa' koşulu ve S0-L9'un ince piyasa bölümüne köprü eklendi. Easy Q2 istemi, seçenekleri ve açıklaması yeniden yazıldı. Ders ÖZETİ (SeedData) de düzeltildi — ekranda hâlâ 'garanti eder' diye duruyordu."
  # ── MEDIUM · içerik ─────────────────────────────────────────────────────
  - id: RV-043
    severity: medium
    evidence: proven
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3597
    summary: "'O anda duran en iyi karşı kotasyondan işlem görür: hemen gerçekleşir, ama hangi fiyattan gerçekleşeceğini önceden bilemezsin' cümlesi kendi kendini çürütüyor — en iyi kotasyon ekranda duruyorsa fiyat bilinir."
    rationale: "Eksik olan halka emir derinliği: emir tek bir kotasyonda bitmeyebilir, kalan miktar bir sonraki (daha kötü) seviyeden karşılanır; fiyat belirsizliği buradan ve kotasyonun emir ulaşana kadar değişebilmesinden doğar. Ders 'derinlik' ve 'kayma' kavramlarını hiç kurmadığı için belirsizliğin NEDENİ söylenmemiş, yalnız sonucu iddia edilmiş. Başlangıç seviyesinde iki cümlelik bir mekanizma yeter."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz"
      note: "Belirsizliğin NEDENİ iki cümleyle kuruldu: en iyi kotasyonun arkasındaki miktar sınırlıdır, aşan kısım daha uzak seviyeden karşılanır; ayrıca emir borsaya varana kadar kotasyon değişebilir. 'Derinlik' ve 'kayma' terimleri başlangıç seviyesi için adlandırılmadan anlatıldı."
  - id: RV-044
    severity: medium
    evidence: proven
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3572
    summary: "Takası kimin yürüttüğü ders içinde çelişiyor: 4. aşama aracı kurumun 'takası yürüttüğünü' söylüyor (3572), Source bloğu takas sürecini Takasbank'a atfediyor (3787). Künye ise takas süresini Borsa İstanbul'a bağlıyor (16-CURRICULUM.md, S0-L10 Kaynaklar)."
    rationale: "Dersin ilan ettiği ayrım ve 13. aşamadaki tablo tam olarak 'hangi taraf ne yapıyor' sorusunu kurmak üzerine; o ayrımın kendisi üç yerde üç farklı yazılmış. Gerçekte aracı kurum takasın TARAFIdır (takas kuruluşunun üyesi olarak yükümlülüğünü yerine getirir), takası YÜRÜTEN merkezi kuruluştur. Hangisinin kanonik olduğuna karar verilip içerik ve künye birlikte düzeltilmeli."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz"
      note: "Aracı kurum artık 'takasta seni temsil eder'; ayrım cümlesi üç tarafı sayıyor (borsa eşleştirir · aracı kurum iletir ve kaydı tutar · merkezi takas kuruluşu takası yürütür) ve 'aracı kurum takasın bir tarafıdır, yürütücüsü değil' diye kapanıyor. 13. aşama tablosu güncellendi. Künye Kaynaklar satırında takas süresi Borsa İstanbul'dan Takasbank'a taşındı, tarihli düzeltme notuyla."
  - id: RV-045
    severity: medium
    evidence: proven
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3668
    summary: "'Tur maliyeti = alış komisyonu + satış komisyonu + makas' formülü TAM liste gibi sunuluyor (3668) ve sonucu 'Gerçek maliyet %0,7' diye etiketleniyor (3690); quiz de aynı toplamı 'toplam maliyet' olarak soruyor (4644-4653). Vergi/stopaj, borsa-takas payları, asgari komisyon ve döviz dönüşüm maliyeti yok."
    rationale: "Dersin bütün retoriği 'göremediğin bir maliyet var' üzerine kurulu; eksik bir toplamı 'gerçek maliyet' diye etiketlemek tam da bu dersi baltalıyor — öğrenci üç kalemi toplayıp 'hepsini gördüm' sanır. Tüm kalemleri saymak başlangıç seviyesi için gereksiz; 'bu üçü en belirgin olanlar, listenin tamamı değil' diyen tek bir cümle ve 'Gerçek maliyet' → 'Bu üç kalemin toplamı' etiketi yeterli."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz · compose yığınında tarayıcıyla görüldü"
      note: "Formül 'Bir turun EN BELİRGİN maliyeti' oldu ve listenin tamamı olmadığı açıkça yazıldı (vergi · borsa ve takas payları · asgari komisyon). 'Gerçek maliyet %0,7' → 'bu üç kalemin toplamı %0,7'. Medium Q4 istemi 'bu üç kalemin toplamı nedir?' oldu; Medium Q6 ve Hard Q9 açıklamaları ile cost-stack figür metni aynı dile çekildi."
  - id: RV-046
    severity: medium
    evidence: proven
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3743
    summary: "'Seans kapalıyken bir şey olduğunda ne olur? Hiçbir işlem gerçekleşmez' (3743) ve Hard Q8 açıklaması (4691-4695) kapsamsız mutlak. Doğru kapsam 'O BORSADA hiçbir işlem gerçekleşmez'."
    rationale: "S0-L9 öğrenciye bir varlığın birden fazla yerde işlem görebileceğini öğretiyor (3375) ve LiveContext bloğu döviz ile altını aynı kotasyon mekanizmasının örneği olarak gösteriyor (3467-3468) — bunlar tek bir seansın saatleriyle sınırlı değil. Ayrıca S0-L10'un kendi tanıttığı kavram `exchange`: borsa tekil bir dünya değil, bir mekan. Tek kelime ('o borsada') hem doğruyu söylüyor hem S0-L9 ile çelişkiyi kaldırıyor."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz · compose yığınında tarayıcıyla görüldü"
      note: "'O borsada hiçbir işlem gerçekleşmez' oldu; her piyasanın saatlerinin aynı olmadığı, aynı varlığın başka yerde işlem görebileceği ve dövizde günün çok daha büyük bölümünün açık olduğu eklendi. Hard Q8 açıklaması ve after-hours figür altyazısı da düzeltildi — S0-L9'un çok-mekan anlatımıyla çelişki kalktı."
  - id: RV-047
    severity: medium
    evidence: proven
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3601
    summary: "Kısmi gerçekleşme dersin hiçbir yerinde geçmiyor; ders baştan sona 'ya gerçekleşir ya bekler' ikiliği kuruyor (3601, 3617-3624 ve two-orders-one-moment figürü)."
    rationale: "Emrin bir kısmının gerçekleşip kalanının defterde kalması sıradan bir sonuç ve öğrencinin ekranda ilk karşılaşacağı şaşırtıcı durumlardan biri. İkili model yalnız eksik değil, RV-043'teki derinlik boşluğuyla aynı kökten: emir tek bir kotasyonla bitmek zorunda değil. Künye kısmi gerçekleşmeyi istemiyor, dolayısıyla künye uyumsuzluğu DEĞİL; içerik bütünlüğü bulgusu. Tek cümlelik bir not (ya da 7. aşama tuzağına bir satır) yeterli."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz"
      note: "Emir türleri bloğuna üçüncü sonuç eklendi: emrin bir kısmı gerçekleşip kalanı defterde bekleyebilir; 'ya hepsi ya hiçbiri diye bir kural yoktur'. İkili model kırıldı."
  - id: RV-048
    severity: medium
    evidence: proven
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3641
    summary: "'Fiyat senin koyduğun sınıra hiç gelmezse, emir sonsuza kadar bekler' (3641). Emirler geçerlilik süresiyle verilir; süresiz bekleyen emir istisnadır, kural değil."
    rationale: "Gerçekte emir günlük olabilir (seans sonunda iptal), belirli tarihe kadar ya da iptale kadar geçerli olabilir. 'Sonsuza kadar' hem yanlış hem de dersin kendi tuzağını zayıflatıyor: limit emrinin asıl bedeli sonsuza kadar beklemek değil, emrin gerçekleşmemiş olarak KAPANMASIdır — bu daha somut ve daha öğretici. Düzeltme: 'emir geçerlilik süresi boyunca bekler, sonunda gerçekleşmemiş olarak kapanır'."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz"
      note: "'Sonsuza kadar bekler' → 'geçerlilik süresi boyunca bekler ve sonunda gerçekleşmemiş olarak kapanır'; emri verirken sürenin de söylendiği (gün sonu · belirli tarih · iptale kadar) eklendi. Hard Q7 açıklaması da aynı dile çekildi."
  - id: RV-049
    severity: medium
    evidence: proven
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3703
    summary: "'İşlem günü ile takas günü aynı gün olmayabilir. Aradaki SÜREYE valör denir' (3703). Valör bir süre değil, bir TARİHtir (valör/takas günü); süre için kullanılan ad takas süresidir."
    rationale: "`settlement` bu dersin tanıttığı altı kavramdan biri, yani burada kurulan tanım kavram sözlüğüne de inecek — yanlış tanım kalıcı olur. Düzeltme anlatımı bozmuyor: 'Paranın ve kalemin fiilen el değiştirdiği güne valör (takas) günü denir; işlem günü ile arasındaki süre takas süresidir.'"
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz · compose yığınında tarayıcıyla görüldü"
      note: "Valör artık bir GÜN: paranın ve kalemin fiilen el değiştirdiği güne valör (takas) günü denir, ikisi arasındaki süre takas süresidir. Medium Q5 açıklaması, settlement-clock figür altyazısı ve figür içi etiketi de düzeltildi — kavram sözlüğüne doğru tanım inecek."
  - id: RV-050
    severity: medium
    evidence: proven
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3715
    summary: "'Özellikle satıştan sonra paranın çekilebilir hâle gelmesi bir gün alabilir' (3715) — aynı bloğun on satır yukarıdaki örneği iki iş günü veriyor (Pazartesi işlem → Çarşamba takas, 3705-3708)."
    rationale: "Blok kendi örneğiyle çelişiyor ve 'bir gün' sayısal bir kural gibi okunuyor; oysa blok özellikle 'süre piyasaya ve enstrümana göre değişir' diye uyarıyor (3710-3712). Düzeltme: 'bir gün alabilir' → süre belirtmeyen bir biçim ('takas tamamlanana kadar sürebilir'), bloğun kendi uyarısına uygun."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz"
      note: "'Bir gün alabilir' → 'takas tamamlanana kadar sürebilir'. Blok artık kendi iki-günlük örneğiyle ve kendi 'süre değişir' uyarısıyla çelişmiyor."
  - id: RV-051
    severity: medium
    evidence: strong
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3751
    summary: "Açılış boşluğu tek nedene bağlanıyor: 'kapalıyken biriken arz-talep farkının tek seferde fiyata yansıması' (3751 ve Hard Q8, 4691-4695). Temettü ve sermaye artırımı düzeltmeleri de boşluk üretir ve bunlar fikir değişikliği değildir."
    rationale: "Öğrenci temettü gününde grafikte boşluk görüp 'gece bir şey mi oldu?' diye yorumlayacak; dersin verdiği tek açıklama onu yanlış yöne götürür. Anlatılan mekanizma doğru, TEK neden olarak sunulması değil. S2 grafik dersleri boşlukları ayrıca anlatacak (ders bunu söylüyor, 3753-3754), dolayısıyla burada tek cümlelik bir çekince yeterli."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz · compose yığınında tarayıcıyla görüldü"
      note: "'Tek nedeni de bu değildir — temettü gibi kayıt düzeltmeleri de boşluk üretir ve bunlar bir fikir değişikliği değildir' eklendi; 'boşluk iki yöne de olabilir' yazıldı. Hard Q8 açıklaması da genişletildi."
  - id: RV-052
    severity: medium
    evidence: proven
    category: quiz-structure
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 4677
    summary: "Hard Q7 ('Limit emrinin bedeli nedir?') açıklaması doğru cevabı anlatıyor ama üç çeldiricinin (komisyon daha yüksek · makası genişletir · takas süresini uzatır) hiçbirine değinmiyor — neden yanlış oldukları söylenmiyor."
    rationale: "finans-lesson skill'i açıklamanın 'yalnız doğru cevabı değil, neden yanlış olduğunu da' anlatmasını şart koşuyor. Üç çeldirici gerçek yanılgılar (emir türünün komisyonu/makası/takası etkilediği sanısı) ve tam olarak çürütülmeye değer. NOT: uyum incelemesi 'dokuz sorunun tamamında açıklama çeldiricinin neden yanlış olduğunu söylüyor' demişti; kaynak okunarak doğrulandı, bu soru için doğru değil — iki inceleme çeliştiğinde kaynak hakem oldu."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz"
      note: "Q7 açıklaması üç çeldiriciyi de çürütüyor: komisyon kurumun tarifesine, makas piyasadaki kotasyonlara, takas süresi piyasa kurallarına bağlıdır — emir türü bunları değiştirmez. İki incelemenin çeliştiği yerde kaynak hakem oldu; uyum incelemesinin 'dokuz sorunun tamamı temiz' beyanı yanlıştı."
  # ── MEDIUM · figür katmanı ──────────────────────────────────────────────
  - id: RV-053
    severity: medium
    evidence: strong
    category: ui-figure-regression
    file: web/src/components/LessonFigure.tsx
    line: 2329
    summary: "order-journey figüründe 5. durağın ('Takas') yazısı fig-seg-lead dolgusunun üzerinde okunmuyor: opak aksan dolgu üzerine fig-value (gri-mavi, 10px) ≈1,1:1 kontrast. RV-039'un birebir tekrarı."
    rationale: "RV-039 (REVIEW-004, spread-cost figürü) tam bu desendi ve 'çip bilgi taşımıyor, yazıyı çerçeveliyor' gerekçesiyle fig-seg-lead → fig-seg yapılarak çözülmüştü; gerekçe kod yorumuna da düşülmüştü. Desen yeni figürde yeniden üretildi. İroni: okunmayan hücre, metnin 'özellikle sonuncusu' diye öne çıkardığı duraktır (3548-3550). Tarayıcı teyidi gerekir ama sınıf eşleşmesi kesin."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz · compose yığınında tarayıcıyla görüldü — beş kutu aynı tonda, 'Takas' yazısı okunuyor"
      note: "fig-seg-lead kaldırıldı; beş durak da aynı sessiz tonda. 'Takas en önemli durak' vurgusunu anlatım ve altyazı taşıyor. Gerekçe RV-039'a atıfla kod yorumuna düşüldü."
  - id: RV-054
    severity: medium
    evidence: proven
    category: ui-figure-regression
    file: web/src/components/LessonFigure.tsx
    line: 2429
    summary: "two-orders-one-moment figürü iki emir türünü asimetrik tonda çiziyor: piyasa emri çipi fig-seg (2424), limit emri çipi fig-bar-muted (2429) — bir seçeneği soluklaştırma."
    rationale: "Künye emir türlerini TAKAS olarak sunmayı şart koşuyor; dosyanın kendi başlık yorumu da bunu yazıyor ('biri daha iyi gibi vurgulanmaz', 2307-2308) ve market-vs-limit figürü kurala uyuyor (iki panelde de fig-seg) — yani aynı ders içinde tutarsız. 'Bekliyor' bilgisi zaten iki metinle taşınıyor (2431, 2433), dolayısıyla renk yalnız yargı taşıyor. RV-016/026/027/028 ailesinin beşinci üyesi."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz · compose yığınında tarayıcıyla görüldü — iki panel aynı tonda, limit çipi kesikli çerçeveli"
      note: "Limit emri çipi fig-bar-muted → fig-seg; 'henüz gerçekleşmedi' bilgisi RENK yerine kesikli çerçeveyle kodlandı. Ders gövdesindeki 'Fark, neyin garanti edildiğinde' ve 'neyi garanti etmek istediğine göre' cümleleri de 'öne almak' diline çekildi."
  - id: RV-055
    severity: medium
    evidence: strong
    category: ui-figure-regression
    file: web/src/components/LessonFigure.tsx
    line: 2001
    summary: "cost-stack, WeightStack yardımcısını olduğu gibi kullanıyor; sabit sınıf dizisi (fig-seg-lead · fig-seg · fig-bar-muted) en VURGULU tonu en küçük kaleme (alış komisyonu 20 ₺), en SESSİZ tonu metnin 'görünmeyen, asıl gözden kaçan' dediği kaleme (makas 30 ₺) veriyor. Ayrıca bant içi yazı ≈2:1 kontrastta."
    rationale: "Figürün kendi altyazısı (2468) ve metin (3690-3691) gözü makasa çekmek istiyor; figür gözü ters yöne çekiyor — İ6 grafik dürüstlüğü. Tavsiye ihlali DEĞİL (üç kalem alternatif değil, toplamın bileşenleri). Düzeltme WeightStack'in S0-L8 kullanımlarını da etkiler, dolayısıyla isteğe bağlı sınıf parametresi ya da yazıyı bandın dışına taşımak tercih edilmeli; her iki yolda S0-L8 figürleri tarayıcıda yeniden teyit edilmeli (TD-001)."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz · compose yığınında tarayıcıyla görüldü — S0-L10 cost-stack ve S0-L8 ağırlık figürü DOM'da üç fig-seg; S0-L8 ayrıca gözle teyit edildi"
      note: "WeightStack'ten sabit sınıf dizisi (fig-seg-lead · fig-seg · fig-bar-muted) kaldırıldı; üç bant da fig-seg. Hem bant içi yazı okunabilirliği hem 'vurgu metnin tersine' sorunu tek değişiklikle kapandı — vurgu kalmayınca yanlış yere düşemez. Büyüklüğü bant genişliği ve etiket söylüyor."
  # ── LOW ─────────────────────────────────────────────────────────────────
  - id: RV-056
    severity: low
    evidence: proven
    category: quiz-structure
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 4704
    summary: "Hard Q9 istemi 'komisyonu düşük demek maliyeti düşük cümlesi neden EKSİK?' diye soruyor; doğru işaretli üçüncü seçenek 'Bu bedel her turda yeniden doğar' bu soruya cevap vermiyor — tekrarlanma, ölçünün eksikliğiyle değil büyüklüğüyle ilgili."
    rationale: "Diğer iki doğru seçenek istemi doğrudan karşılıyor (makas da maliyet · toplam = komisyon + makas). Üçüncüsü doğru bir bilgi ama başka bir sorunun cevabı; çok seçmeli soruda 'doğru cümle' ile 'istemin cevabı' karıştırılmış. Ya seçenek istemle hizalanmalı ('komisyon oranı turların sayısını hesaba katmaz') ya da istem genişletilmeli."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz"
      note: "Q9'un üçüncü doğru seçeneği istemle hizalandı: 'Bu bedel her turda yeniden doğar' → 'Ürüne göre vergi gibi başka kalemler de girebilir' (istem 'neden EKSİK?' diye soruyor). İkinci seçenek 'Komisyon, maliyetin yalnız bir parçasıdır' oldu; tur başına doğma bilgisi açıklamada korundu."
  - id: RV-057
    severity: low
    evidence: proven
    category: compliance-d002
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3693
    rule_or_decision: D-002
    summary: "'SIRF girip çıkmak bu kadara mal oldu. Ve bu bedel tur başına doğar: iki tur iki kez, beş tur beş kez' (3693-3695) — değerleyici sıfat + tırmanan sayım, örtük 'daha az işlem yap' yönlendirmesi."
    rationale: "Mekanizma doğru ve söylenmesi meşru; sorun çerçeveleme. Projede aynı gerekçeyle iki kez bulgu olmuş sınıf ('bedeli daha sessiz ödenir', 'sık alıp satan her turda öder'). Bu sürüm onlardan iyi — kişiyi teşhis etmiyor — ama retorik eğim duruyor. Dersin kendi quiz açıklaması aynı olguyu tamamen nötr veriyor (4698-4702) ve orada bir dengeleyici de var ('komisyonun kendisi yanlış ölçü değildir, tek başına EKSİK ölçüdür'); gövde o biçime çekilebilir."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz"
      note: "'SIRF girip çıkmak bu kadara mal oldu… iki tur iki kez, beş tur beş kez' → 'bu 70 ₺ yalnızca girip çıkmanın bedeli. Bu bedel tur başına doğar, yani tur sayısıyla birlikte büyür. Kaç tur yapacağın senin kararın.' Değerleyici sıfat ve tırmanan sayım düştü, bilgi kaldı."
  - id: RV-058
    severity: low
    evidence: proven
    category: compliance-d002
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3754
    rule_or_decision: D-002
    summary: "after-hours-world bloğunda limit emrine karşılığı olmayan üçüncü olumsuz dokunuş: 'limit emrin defterde beklerken seans kapanırsa… Beklemenin bedeli burada da görünür' (3754-3756). Aynı blokta piyasa emrinin açılıştaki dezavantajı söylenmiyor."
    rationale: "Ders emir türlerini sözcük düzeyinde iyi dengeliyor, ama TOPLAMDA limit emri üç olumsuz dokunuş alıyor: işlenmiş örnekte gerçekleşmeme (3618-3620), tuzak bloğu (künyede ONAYLI ve dengeli, 3641-3642) ve burada karşılıksız bir ek. 'Beklemenin bedeli' ayrıca değerleyici bir özet. RV-054'teki figür tonuyla birleşince net bir eğim doğuyor."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz"
      note: "Blok simetrik hâle getirildi: 'defterde bekleyen bir emir' (limit demeden) + 'bu ikisi için de geçerli — o anda verilen bir piyasa emri de açılışta beklenmedik bir seviyeden gerçekleşebilir'. 'Beklemenin bedeli burada da görünür' değerleyici özeti kaldırıldı."
  - id: RV-059
    severity: low
    evidence: proven
    category: compliance-d002
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3574
    summary: "'Genellikle işlem tutarının KÜÇÜK bir yüzdesidir' (3574-3575) — gerçek tarife düzeyi hakkında kaynaksız niteleme; Source bloğu bu iddiayı desteklemiyor ve ⟨doğrulanacak⟩ notu duruyor."
    rationale: "Künye komisyonun kurgusal ve etiketli bir oranla anlatılmasını istiyor; bu cümle oran vermiyor ama gerçek dünyadaki düzey hakkında niteleyici bir iddia kuruyor. İkinci sorun iç tutarlılık: iki blok sonra ders 'komisyonu düşük demek maliyeti düşük demek değildir' diye tam tersini öğretiyor (3665-3667), yani 'küçük' sıfatı dersin kendi tezini önceden zayıflatıyor. Düzeltme: 'bir yüzdesi olarak hesaplanır; oranı kuruma, ürüne ve tutara göre değişir'."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz"
      note: "'Genellikle işlem tutarının küçük bir yüzdesidir' → 'İşlem tutarının bir yüzdesi olarak hesaplanır'. Kaynaksız düzey iddiası kalktı; oranın değişkenliği zaten hemen altındaki blok alıntısında duruyor ve dersin kendi 'komisyonu düşük ≠ maliyeti düşük' tezi artık önceden zayıflatılmıyor."
  - id: RV-060
    severity: low
    evidence: strong
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3745
    summary: "Açılış fiyatının tek tek eşleşmeyle değil açılış seansıyla (müzayede) oluştuğu hiç geçmiyor; beş durak modeli baştan sona sürekli eşleşme varsayıyor."
    rationale: "'Açılışta bu birikmiş fikir farkı bir anda karşılaşır' cümlesi mekanizmayı kabaca doğru anlatıyor, ama 'bir anda karşılaşma'nın kendi adı ve kuralı var ve 3. aşamanın 'fiyat önceliği + zaman önceliği' anlatımı açılışta aynı biçimde işlemiyor. Künye bunu istemiyor, dolayısıyla eksiklik değil kapsam kararı; ileride S2-L4 köprüsünde düzeltme gerektirebilir. Bilgi amaçlı kaydedildi."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz"
      note: "Açılış fiyatının 'tek tek eşleşmelerle değil, biriken emirlerin TOPLUCA değerlendirildiği bir açılış adımında' oluştuğu eklendi — mekanizma adlandırılmadan anlatıldı, 3. aşamanın fiyat/zaman önceliği anlatımıyla çelişki kalktı."
  - id: RV-061
    severity: low
    evidence: proven
    category: curriculum-conformance
    file: .claude/docs/16-CURRICULUM.md
    line: 2452
    summary: "M7b beyaz listesi ('metinde geçebilen tek özel ad geniş endeks adlarıdır; tek şirket · fon · aracı kurum adı hâlâ yasak') ile S0-L10 künyesinin Kaynaklar satırı çelişiyor: SPK · Borsa İstanbul · Takasbank künyede açıkça isteniyor, M7b harfiyen uygulanırsa yasağa giriyor."
    rationale: "İçerik künyeye uygun, yani bu bir içerik hatası DEĞİL — kural metni boşluğu. Reklam riski yok: hiçbiri kullanıcının seçebileceği bir aracı kurum değil (ikisi altyapı kurumu, biri düzenleyici). AGENTS.md §4.2 gereği sessizce düzeltilmedi. Öneri: M7b'ye açık muafiyet — 'düzenleyici, borsa ve takas/saklama kurumu adları kaynak gösterimi amacıyla izinlidir; aracı kurum · fon · tek şirket adı yasak kalır' — ve §12-K3'e işlenmesi. Aksi hâlde M7b kodlandığında bu ders ve S0-L9 kırmızıya düşer."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz"
      note: "M7b'ye açık muafiyet yazıldı ve §12'ye K3a kararı olarak işlendi: Source bloklarında düzenleyici · borsa · takas/saklama kuruluşu adları kaynak gösterimi amacıyla izinli; aracı kurum · fon · tek şirket adı yasak kalıyor. Künye ile kural metni arasındaki çelişki giderildi — AGENTS.md §4.2 gereği önce bildirilmiş, ürün sahibi kararıyla kapatıldı."
  - id: RV-062
    severity: low
    evidence: proven
    category: test-adequacy
    file: backend/tests/Finans.Integration.Tests/EducationSeedTests.cs
    line: 355
    summary: "S0-L10'un 🔴 kurallarının makine karşılığı yok: tavsiye taraması altı ifadelik sabit listeden ibaret (355-358); M7a ve M7b hiç kodlanmamış. Aracı kurum adı yasağı, tarife yasağı, kıyas yasağı, 'kurgusal' etiket zorunluluğu ve 13. aşamanın Nirengi cümlesi sıfır otomatik korumaya sahip."
    rationale: "Bugünkü içerik uyumlu (uyum incelemesi üç özel kuralın üçünü de temiz buldu), ama koruma tamamen insan okumasına dayanıyor; ileride bir düzenleme etiketsiz bir oran ya da bir kurum adı sokarsa hiçbir kapı kırmızıya düşmez. İki ucuz iddia bu dersin yasal riskini kapatır: gövde 'Nirengi bir aracı kurum değildir, emir iletmez' içermeli; '%0,2' geçen derste 'kurgusal' kelimesi bulunmalı. TD-001 ailesine ait kapsam borcu."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz"
      note: "Yeni test: Brokerage_naming_and_fictional_rate_labels_are_machine_checked. Dört kapı birden: (1) şirket/aracı kurum adı taşıyıcı kalıpları (Menkul Değerler · Yatırım Menkul · A.Ş.) tüm derslerde yasak; (2) M7a grafik kalıpları Trap DIŞI bloklarda yasak; (3) S0-L10 gövdesi 'Nirengi bir aracı kurum değildir, emir iletmez' içermeli; (4) %0,2 geçen her aşamada 'kurgusal' etiketi bulunmalı."
  - id: RV-063
    severity: low
    evidence: proven
    category: ui-figure-regression
    file: web/src/components/LessonFigure.tsx
    line: 2528
    summary: "after-hours-world figüründe açılış boşluğu yalnız YUKARI yönde çizilmiş (kapanış cy=78, açılış cy=52); metin yön belirtmiyor."
    rationale: "Tek yönlü gösterim öğrenende 'boşluk = fiyat yukarı atlar' izlenimi bırakabilir — tahmin cümlesi değil ama İ6 kapsamında tek yanlı bir resim. Bağlayıcı çizgi nötr (fig-line-flat), orası doğru. Düzeltme: aşağı yönde üçüncü bir soluk nokta, ya da altyazıya 'boşluk iki yöne de olabilir' ibaresi. RV-051 ile aynı kökten (boşluğun tek biçimde sunulması)."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz · compose yığınında tarayıcıyla görüldü — boşluk kapanış noktasından iki yöne çatallanıyor"
      note: "Kapanış noktasından yukarı VE aşağı iki açılış noktası, aynı tonda ve aynı opaklıkta çizildi; özet satırı 'boşluk iki yöne de olabilir' oldu ve figürün erişilebilirlik etiketi de bunu söylüyor. Anlatım yön belirtmiyordu, figür de artık belirtmiyor."
  # ── Düzeltme turunda, CANLI doğrulamada bulundu (statik incelemede değil) ──
  - id: RV-064
    severity: high
    evidence: proven
    category: seed-integrity
    file: backend/src/Finans.Infrastructure/Seed/SeedData.cs
    line: 1109
    summary: "Ders KAYDI (başlık · özet · gövde · süre) yalnız 'yok ise ekle' ile yazılıyordu; metin düzeltmeleri çalışan kurulumlara HİÇ inmiyordu. RV-042/045/049 düzeltildikten sonra bile S0-L10'un özeti ekranda 'piyasa emri … garanti eder' diye duruyordu."
    rationale: "Düzeltme turunun tarayıcı doğrulamasında görüldü: ders listesi hâlâ eski özeti gösteriyordu. Aynı mutabakat LessonSections için T6.7'de gelmiş (dosyadaki MUTABAKAT notu bunu açıkça anlatıyor: 'salt ekle yaklaşımı bu değişiklikleri çalışan kurulumlara indiremiyordu'), dersler o turda atlanmıştı. Sessiz bir sınıf: bir metin hatası düzeltilip testler yeşile döner, ama kullanıcı hatayı görmeye devam eder — yani REVIEW-005'in içerik düzeltmelerinin HİÇBİRİ ders özeti/gövdesi düzeyinde teslim edilmiş olmayacaktı. Hiçbir test bunu yakalamıyordu, çünkü testler her turda temiz veritabanı kuruyor."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "dotnet test: Application 308/308 · Integration 187/191 (4 kırmızı = INC-001, ortam) · web 147/147 · tsc+vite temiz · compose yığınında tarayıcıyla görüldü — S0-L10 özeti yeniden kurulumla canlı veritabanında düzeldi"
      note: "UpsertLesson yardımcısı eklendi ve on Set 0 dersinin 'yok ise ekle' kapısı mutabakata çevrildi: başlık · slug · özet · gövde · süre · seviye seed'den tazeleniyor; IsPublished · TrackId · CreatedAtUtc bilerek dışarıda (işletme kararı / yapısal taşıma / kaydın kendi geçmişi). Yeni test: Reseeding_refreshes_authored_lesson_text_but_not_operational_fields."
---

# REVIEW-005 — GD-005 · S0-L10 "Bir alım nasıl gerçekleşir?"

**Kapsam:** `55682bf..95b15b4` — 6 dosya, +782/−18.
**Sonuç:** 24 bulgu — **0 critical · 3 high · 13 medium · 8 low.**
**Tümü `resolved`** (2026-11-22, ürün sahibi kararı "Hepsini düzelt").

> **RV-064 statik incelemede DEĞİL, düzeltme turunun tarayıcı doğrulamasında
> bulundu** — tıpkı REVIEW-004'ün RV-039/040'ı gibi. Üç içerik düzeltmesi
> (RV-042/045/049) koda inmiş ve testler yeşile dönmüştü, ama ders ÖZETİ ekranda
> hâlâ eski hâliyle duruyordu: seed dersleri yalnız "yok ise ekle" ile yazıyordu.
> Yani o üç düzeltme kullanıcıya hiç ulaşmayacaktı. Bu, "testler yeşil" ile
> "kullanıcı düzelmiş hâli görüyor" arasındaki farkın en net örneği.

İçerik doğruluğu ve D-002/M7 uyumu **bağımsız alt-ajanlara** verildi (yazar
kendi işini incelemez — REVIEW-004'te bu ayrımın değeri görülmüştü: yazarın
"M7 taraması temiz" beyanı figür katmanını tamamen kaçırmıştı). Orkestratör
perspektifleri (seed bütünlüğü · künye uyumu · quiz yapısı · test yeterliliği)
doğrudan kaynak okunarak yürütüldü.

## İhlal bulunmayan alanlar (açıkça temiz)

- **Seed bütünlüğü:** `OrderIndex 10` tekil, slug tekil, altı yeni kavram
  anahtarı mevcutlarla çakışmıyor, dört idempotent muhafız yerinde.
- **Künye uyumu:** 14 aşamanın katman/tür dizisi künye tablosuyla birebir
  (`Intro · Core ×4 · Ex · Trap · Ctx · ExDeep · Ctx · Trap · Deep · Core · Src`);
  tuzak blokları `Core` değişmezini koruyor; 10 figür anahtarı `FIGURES` kaydıyla
  örtüşüyor.
- **Quiz yapısı:** 9 soru · 3/3/3 zorluk · 34 seçenek · her soruda tek doğru
  (çok seçmelilerde üç doğru), tümünde `Explanation` var.
- **Test sayımları:** `160 → 174` kısmi seed sayımı semantik olarak doğru
  (187 bölüm − 13 LiveContext); LiveContext sayısı değişmedi çünkü bu dersin
  LiveContext bloğu yok (künye kararı).
- **Dersin üç 🔴 kuralı:** aracı kurum adı/tarife/kıyas **yok** (gövde, quiz ve
  10 figür metni tarandı); komisyon oranı dört yerde "kurgusal" etiketli;
  "Nirengi bir aracı kurum değildir, emir iletmez" cümlesi 13. aşamada birebir
  var. **Dersin ağır yasal riski temiz.**
- **Gelecek tahmini yok:** M7a kalıpları (sinyal · formasyon · hedef fiyat ·
  destek-direnç) hiç geçmiyor; yönlendirme fiili taraması sıfır sonuç verdi.
- **Aritmetik:** 20+20+30=70 ₺ = %0,7 · komisyon tek başına %0,4 · 40/70 ≈ %57
  ("yarısından biraz fazlası") · makas 0,30/100,15 ≈ %0,3 — hepsi doğru.
- **Renk semantiği ana kuralı:** 10 figürün hiçbirinde `fig-bar-pos` /
  `fig-bar-neg` kullanılmamış (RV-016 uyumlu).

## Desen: aynı hatalar tekrar ediyor

Üç medium bulgu (RV-053 · RV-054 · RV-055) figür katmanında ve hepsi daha önce
kapatılmış desenlerin tekrarı — RV-039 (okunmayan yazı) ve RV-016/026/027/028
(asimetrik vurgu) aileleri. Üçünün de yakalanması için figürlerin çalışan yığında
gözle görülmesi gerekiyordu; hiçbir otomatik test bunları yakalayamaz.
**TD-001 (figür render testi borcu) tam bu boşluk** ve borç bu GD ile 19 → 29
figüre büyüyor.

RV-041 ayrı bir ders: hata **bir önceki incelemenin düzeltmesinde** girdi.
RV-034 yönlendirmeyi yumuşatırken makasın birimini "tur"dan "işlem"e kaydırdı ve
S0-L9 ile S0-L10 çelişik hâle geldi. Düzeltme turlarının kendisi de inceleme
gerektiriyor.

## Kapsam dışı tespit (bildirim, AGENTS.md §4.2)

**REVIEW-004'ün frontmatter'ı `review.schema.json`'a uymuyor.** REVIEW-001/002/003
şema alanlarını kullanıyor (`review_id` · `runtime` · `timestamp` · `scope` ·
`active_gd`); REVIEW-004 bunların yerine `id` · `gd` · `created` · `partial`
yazmış. Şema `additionalProperties: false` olduğu için dört alan da geçersiz ve
beş zorunlu alan eksik. REVIEW-005 şemaya uygun yazıldı; REVIEW-004 **sessizce
düzeltilmedi** — kanonik kaynak `.agent-method/schemas/review.schema.json`,
kararı insan verir.
