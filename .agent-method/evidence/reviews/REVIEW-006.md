---
review_id: REVIEW-006
runtime: claude-code
timestamp: "2026-11-22"
scope: "GD-006 + GD-007 (inceleme borcu) — M12 künye mutabakat kuralı + 10 künyenin toplu düzeltmesi; figür render + okunabilirlik kapısı (FIGURE_KEYS dikişi, 8 opak dolgu düzeltmesi); adm-session-start hook'unun YAML dilimleyici düzeltmesi + oturum uyarısı"
base_ref: a0918a7
head_ref: 17cc3c0
active_gd: null
reviewers:
  - correctness                 # bağımsız alt-ajan — test mantığı, regex, hook
  - gate-efficacy               # bağımsız alt-ajan — kapılar iddia ettikleri çizgiyi tutuyor mu
  - ui-figure-regression        # bağımsız alt-ajan — fig-seg-lead → fig-seg anlam kaybı
  - architecture-docs-contract  # bağımsız alt-ajan — katman yerleşimi, yeniden kullanım, kayıt doğruluğu
  - adm-conformance             # orkestratör — süreç/değişmez uyumu
findings:
  # ── HIGH ────────────────────────────────────────────────────────────────
  - id: RV-065
    severity: high
    evidence: proven
    category: gate-under-scoped
    file: web/src/components/LessonFigure.test.tsx
    line: 62
    rule_or_decision: D-012
    summary: "Okunabilirlik kapısı kuralı 'opak dolgu yazı taşımaz' diye yazıyor ama seçici yalnız `rect.fig-seg-lead`. App.css'te ÜÇ dolgu tam opak: `fig-seg-lead` (--accent), `fig-bar-pos` (#45d5a2), `fig-bar-neg` (#ef7a72). Diğer ikisinde RV-039 geometrisinin BUGÜN yeşil geçen 8 canlı örneği var."
    rationale: "İki bağımsız alt-ajan aynı boşluğu ayrı yollardan buldu. `no-guarantee` (LessonFigure.tsx:661-664) birebir RV-039 şekli: `<rect className=\"fig-bar-pos\">` [152,14 64×26] üzerine ortalanmış `fig-value` (184,31) — opacity niteliği YOK. Kontrast ≈1,31:1; `fig-bar-neg` kolu ≈1,03:1. RV-039'un gerekçesi ≈1,1:1'di. Doğrulanmış sekiz örnek: no-guarantee (×2), pay-yourself-first, money-at-work, two-savers, with-without-buffer, basket-price-up, certain-vs-uncertain. İki sınır örnek daha (debt-cost-ladder, real-vs-nominal) `anchor=\"end\"` olduğu için sayıma katılmadı. GD-007 §3.1 bu sınıfları bilinçli dışarıda bıraktı: '100 figürün 44'ü bu renkleri kullanıyor, 44 kalemlik liste kural değil.' 44 sayısı DOĞRU (bağımsız olarak teyit edildi) ama YANLIŞ popülasyonu ölçüyor: bu renkleri KULLANAN figürleri sayıyor, üzerine YAZI YAZAN figürleri değil. Gerçek düzeltme yüzeyi 8-10 rect/text çifti — GD-007'nin zaten yaptığı 8 düzeltmeyle aynı büyüklükte. Beyaz listeye hiç gerek yoktu. Senaryo: biri `no-guarantee` desenini kopyalayıp yeni bir kazanç/kayıp paneli yazar; inceleme geçer, vitest geçer, kullanıcı tam olarak RV-039/053/055'in açıldığı okunmaz hücreyi görür."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "Kural test seçicisinden CSS özelliğine taşındı: kapı artık ETKİN OPAKLIK okuyor (opak sınıf VE opacity ≥ 0,9), `rect` yanında `circle` görüyor. Kapının bulduğu 10 ihlalin tamamı düzeltildi — anlam taşıyan dolgularda yazı kutunun dışına çıktı (no-guarantee · with-without-buffer · pay-yourself-first · debt-cost-ladder · real-vs-nominal), vurgunun kendisi asimetri olan yerlerde dolgu saydam tona indi (basket-price-up · certain-vs-uncertain · two-savers) ya da vurgu yazı rengine taşındı (money-at-work). Mint/coral AÇIK zemin olduğu için `fig-label`a yükseltmek çözüm DEĞİLDİ (≈1,66:1) — bu yüzden yazı taşındı. Kapının yeni sınıfta kırmızıya düştüğü `no-guarantee` bilerek bozularak kanıtlandı."
  - id: RV-066
    severity: high
    evidence: proven
    category: m12-silent-skip
    file: backend/tests/Finans.Integration.Tests/EducationSeedTests.cs
    line: 398
    rule_or_decision: D-017
    summary: "M12, `16-CURRICULUM.md`'deki 35 `####` künye bloğundan yalnız 18'ini denetliyor; kalan 17'si `if (!baslik.Success) continue;` ile HİÇBİR iddia üretmeden atlanıyor — ikisi (S1-L1, S1-L2) yayında olan dersler."
    rationale: "Doğrudan teyit: dokümanda 35 `^#### ` bloğu var, `**Figür planı (` yalnız 19 yerde geçiyor (biri künye dışı), denetlenen = 18. Atlanan küme S1-L1..L5, S3-L1..L4, S4-L1..L4, S5-L1..L4. S1-L1 ve S1-L2 `*mevcut*` işaretli, gerçek seed'li figürleri olan dersler (`real-vs-nominal`, `purchasing-power`, `concentration`, ...) ve M12'den kaçmalarının TEK nedeni künyede `**Figür planı (5):**` yerine `**Mevcut figürler (5):**` yazması. Atlama kapının tek tolerans mekanizması ve anahtarı katkı verenin yazdığı bir dizgi. S3-L1 künyesini yazan biri başlığı atlar, sayısız yazar ya da S1-L1'in ifadesini kopyalarsa dersi sessizce korumasız kalır ve GD-006'nın tanımladığı hata (başlıktaki sayaç plandan kayar, GD planlaması yanlış sayıyla başlar) o ders için tümüyle geri döner. 'Her `####` ders bloğu bir figür planı taşır' iddiası hiçbir yerde yok — çizgiyi tutacak iddia buydu."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "M12 üç lehçeyi birden tanıyor (`Figür planı` · `Figürler` · `Mevcut figürler`) ve başlıksız künye artık KIRMIZI — sessiz `continue` kalktı. Kapsam 18/35 → 35/35. Genişletme dört birikmiş sapma buldu: S1-L3 sayacı 7→9 düzeltildi, S3-L2'nin eksik 🖼 işareti eklendi, dokuz bağsız figür TD-005 olarak sayılı istisna listesine yazıldı (yenisi kırmızıya düşer, liste daralan kıskaç)."
  - id: RV-067
    severity: high
    evidence: proven
    category: m12-wrong-direction-fix
    file: backend/tests/Finans.Integration.Tests/EducationSeedTests.cs
    line: 410
    rule_or_decision: D-017
    summary: "M12 yalnız 'başlık sayısı == plan anahtarı sayısı' diyor ve iki taraf da düzenlenebilir; kırmızıyı planı KÜÇÜLTEREK yeşile çevirmek, sayacı düzeltmek kadar geçerli — yayında olan bir figür kanonik kaynaktan sessizce silinebiliyor."
    rationale: "Mutasyonla kanıtlandı: dokümanın kopyasında S0-L1'in planından `hold-vs-flip` çıkarıldı, aşama satırı silindi, başlık (8)→(7) yapıldı → M12 tümüyle YEŞİL (18/18 tutarlı). Oysa `hold-vs-flip` hâlâ `EducationContent.cs`'te seed'li ve `LessonFigure.tsx`'te kayıtlı: figür ürüne gidiyor, D-017'nin kanonik ilan ettiği dokümanda ise yok. Senaryo: biri plana 9. figürü ekler ama aşama satırını unutur → M12 aşama kontrolünde kırmızı olur; en ucuz yeşil, plan girdisini silip başlığı azaltmaktır. Sonraki GD gerçeği eksik gösteren bir künyeden planlar — GD-006'nın durdurmak için kurulduğu hata sınıfının ters yönde çalışan hâli. ⚠ Bu TD-002 DEĞİL: TD-002 'planlanmış ama hiç yazılmamış' (plan koddan ileri); bu 'yazılmış, yayınlanmış, sonra sessizce plandan düşmüş' (kod plandan ileri). M4 yardım edemez — tek yönlüdür (her seed anahtarı kayıtta olmalı) ve künyeyi hiç okumaz. Bugün aşama bağı olmayan iki plan anahtarı (`S0-L4:erosion-curve`, `S2-L2:chart-scale-playground`) 🖼 kontrolü hiç fark etmeden silinebilir."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "**M13** yazıldı (`Every_seeded_figure_appears_in_its_lesson_blueprint`): seed'de bir derse bağlı her figür o dersin künyesinde de yazılı olmak zorunda. Kanıt: `hold-vs-flip` planından silinip sayaç azaltıldığında M12 YEŞİL kalıyor ama M13 kırmızıya düşüp figürü adıyla bildiriyor. Ek olarak `*(mevcut)*` iddiası artık kayıt defterinde doğrulanıyor. TD-002 bu yüzden kapanmadı ama DARALDI: açık kalan yön yalnız plan → seed."
  - id: RV-068
    severity: high
    evidence: proven
    category: claim-accuracy
    file: web/src/components/LessonFigure.test.tsx
    line: 25
    summary: "Kayıt defterinde 101 figür var, 100 değil. GD-007'nin başlığı, AC-2, §3.1, §5, §7, TEST_DEBT TD-001 satırı ve CAPABILITIES.md'nin tamamı 100 diyor; `>= 100` tabanı bu yüzden bir birim boşluk taşıyor ve §7'nin kendi kalem dökümü kendi toplamıyla çelişiyor."
    rationale: "Doğrudan sayım: `Object.keys(FIGURES).length = 101`. Kaçan anahtar `concentration` — kayıt defterindeki 100 anahtar tırnaklı, bu biri tırnaksız, dolayısıyla `\"key\":` arayan elle sayım onu atlıyor ama `Object.keys` atlamıyor. Bağımsız teyit: `LessonFigure.test.tsx` 207 test bildiriyor ve dosya `1 + 2×n + 4` test tanımlıyor; `207 = 1 + 2×101 + 4` — 100 olsaydı 205 olurdu. §7'nin TOPLAMI doğru (350 − 203 = 147, REVIEW-005'in notlarıyla da örtüşüyor; ölçülmüş) ama DÖKÜMÜ yanlış: `100 + 100 + 1 = 201 ≠ 203`. Toplam koşturularak, parçalar '100 figür var' varsayımıyla yazılmış ve ikisi hiç karşılaştırılmamış. İşletim sonucu: taban 101'e karşı `>= 100`, yani bir figür silinip sayım testi yeşil kalabilir (M4 bunu kısmen yakalar — seed referansı da silinmedikçe). Ayrıca taban elle tutulan bir sabit: Set 2 ~70 figür eklediğinde taban 100'de kalır ve 'kıskaç' tümüyle kıskaç olmaktan çıkar."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "Gerçek sayı 101 (tırnaksız `concentration`). Taban `>= 100`dan `BEKLENEN_FIGUR_SAYISI = 101` eşitliğine çevrildi — silinme de eklenme de sabitin elle değişmesini gerektiriyor. 100 → 101 düzeltmesi CAPABILITIES.md, TEST_DEBT ve 09-TESTING-STRATEGY'ye de işlendi; GD-007 §7'nin hatalı dökümü §9'da kayda geçti."
  - id: RV-069
    severity: high
    evidence: proven
    category: gate-not-enforced
    file: .github/workflows/adm-backlog-sync.yml
    summary: "Depoda hiçbir CI iş akışı test koşturmuyor — tek workflow backlog senkronu. İki GD'nin de amacı 'gözle denetimden makine kapısına geçmek'ti; kapılar yalnız biri yerelde test koşturduğunda işliyor, yani yerini almak için kurulduğu insan disiplinine bağlı."
    rationale: "`.github/workflows/` içinde tek dosya `adm-backlog-sync.yml`; `.github/` altında `dotnet test`, `npm test`, `pnpm test` ya da `vitest` araması BOŞ dönüyor. Bu durum bu diff'in getirdiği bir gerileme değil, ama bu diff'in merkezî iddiasını doğrudan zayıflatıyor: TD-001'in 'kapandı' kaydı, M12'nin 'makine kapısına bağlandı' ifadesi ve CAPABILITIES.md'nin 'artık TESTLİ' notu hepsi kapının koşmasına dayanıyor. S-009'un kendi süreç bulgusu tam bu sınıftaydı — bir kontrolün var olması işletildiği anlamına gelmiyor. ⚠ Kapsam notu: önceden var olan bir eksik; buraya GD-006/GD-007'nin kabul gerekçesini taşıdığı için yazıldı, bu diff'in soktuğu bir hata olarak değil."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/189 (INC-001'in 4 testi dışlanmış) · EducationSeedTests 27/27"
      note: "Ürün sahibi kararı: 'Yeşil paketler için CI'. `.github/workflows/tests.yml` eklendi — iki iş: web (vitest + tsc, SC-E30 kapısı burada koşar) ve backend (Application 308 birim testi + Integration, SC-E31/M12/M13 burada koşar). INC-001'in dört testi TEK TEK ADLARIYLA dışlandı: sınıf adıyla dışlamak ölçülerek yanlış bulundu, çünkü ObservabilitySecurityTests altı test taşıyor ve yalnız üçü kırmızı — sınıfı dışlamak üç YEŞİL testi de düşürüyordu (189 yerine 186). Dışlama görünür ve gerekçeli: iş akışının başındaki blok INC-001'in kök nedenini ve #74 kapandığında filtrenin silineceğini yazıyor. İş akışı required check olarak işaretlenmedi (birleştirmeyi engellemez); amacı kapıların gerçekten koştuğunu görünür kılmak. Koşulan komutlar gerçek araç zinciriyle yerelde doğrulandı (`dotnet restore backend/Finans.slnx`, filtreli integration koşumu 189/189); pnpm komutları `working-directory` + düz script adıyla yazıldı, `--filter` anlambilimine bağlı değil."
  # ── MEDIUM · kapı hassasiyeti ───────────────────────────────────────────
  - id: RV-070
    severity: medium
    evidence: proven
    category: gate-precision
    file: web/src/components/LessonFigure.test.tsx
    line: 78
    summary: "Örtüşme kontrolü yazının TEK bir çapa noktasını kutuyla karşılaştırıyor; çapası kutunun dışında kalıp harfleri kutuyu kaplayan etiket kaçıyor — ve bu istisna değil, normal durum: 623 `<text>`in 509'u `middle`/`end` çapalı."
    rationale: "Mutasyonla kanıtlandı: `PriceIsNotValue`'nun özgün kusuru geri alınıp [120,30 80×30] kutusuna `textAnchor=\"middle\"` ile x=210'da 26 karakterlik bir etiket konduğunda — 17px'te kabaca x 139–281 arası, yani kutunun çoğunu kaplıyor — 101 figürlük tarama 0 hata veriyor, çünkü çapa x+w=200'ü aşıyor. Kontrol: aynı kutuda çapa içeride olduğunda yakalanıyor. İkinci biçim: RV-039'un özgün kutusunda [24,48 60×20] çapa (54,63) yakalanıyor, (85,63)'e — sağ kenarın bir piksel ötesine — taşındığında yakalanmıyor, oysa ortalanmış metin kutunun ~27 pikselini hâlâ kaplıyor. Kırmızı bir kapıyı düzeltmenin iki yolu var: yazıyı gerçekten dışarı taşımak, ya da x/y'yi bir piksel oynatmak. İkincisi daha hızlı, incelemede meşru bir yerleşim rötuşu gibi duruyor ve çıktıyı ÖZGÜN hâlinden daha kötü yapıyor (yazı dolgunun yarısında). jsdom'da `getBBox` olmadığı için kesin çözüm tahmini genişlik (karakter × fontSize × ~0,55) + çapa yönü, ya da kutuyu birkaç piksel şişirip sınır durumlarını ihlal saymak."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "Çapa noktası yerine yazının TAHMİNİ KAPLADIĞI ALAN karşılaştırılıyor: sınıf başına punto + ilerleme katsayısı, `text-anchor` (start/middle/end) ve taban çizgisi etrafında büyük harf/iniş bandı. Çapa içerdeyse her hâlükârda ihlal (kanıtlanmış ilk kontrol korundu), değilse yazının ≥%15'i dolguya düşmeli. Bir pikselle kaçma yolu kapandı."
  - id: RV-071
    severity: medium
    evidence: proven
    category: yaml-slicer-half-fixed
    file: .claude/hooks/adm-session-start.mjs
    line: 163
    rule_or_decision: INV-18
    summary: "Sondaki açıklama izni `block()`'a eklendi ama `listItems()`'ın satır-içi dizi koluna eklenmedi: `blockers: [INC-001]  # not` sessizce BOŞ engel listesi veriyor — hook'un en önemli uyarısı."
    rationale: "Hook'tan çıkarılan yardımcılarla doğrudan denendi: `blockers: [INC-001, INC-002]` → iki öğe; `blockers: [INC-001]  # INV-18` → `[]` (sessizce boş); `blockers:  # açıklama` + alt satırda `- X` → `[\"X\"]` (bu commit'in düzelttiği kol). `listItems` önce `^${key}:\\s*\\[(.*)\\]\\s*$` deniyor ve bu kalıpta `(#.*)?` izni YOK. Bugün `state.yaml:121` `blockers: []` ve açıklamalar ALT satırlarda olduğu için kayıp yok. Ama `session:            # current open session` satırını üreten alışkanlık `blockers:` satırına uygulandığında `⛔ Engeller:` uyarısı hiçbir teşhis bırakmadan kaybolur — commit mesajının 'aylarca fark edilmedi' dediği hatanın birebir aynısı, aynı dosyada, düzeltilen satırın 70 satır altında."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "listItems()'in satır-içi kolu da sondaki açıklama iznini aldı. Gerçek hook kaynağından çıkarılan fonksiyonla beş durum doğrulandı: düz satır içi · sonda açıklama · boş liste · blok biçimi + açıklama · bitişik açıklama — hepsi doğru."
  - id: RV-072
    severity: medium
    evidence: proven
    category: m12-marked-window
    file: backend/tests/Finans.Integration.Tests/EducationSeedTests.cs
    line: 436
    summary: "'İşaretli mi' kontrolünün 26 karakterlik `sonra` penceresi bir SONRAKİ liste öğesinin açıklamasına taşıyor; hiçbir aşamaya bağlı olmayan figür yanlışlıkla `*(fallback)*` sayılabiliyor — hüküm komşu anahtarın karakter uzunluğuna bağlı."
    rationale: "Mutasyonla kanıtlandı: `erosion-curve` → `erosion-c` kısaltılıp S0-L4'ün planı `` `index-basket` · `erosion-c` *(fallback)* `` sırasına alınıp S0-L8'in aşama tablosundan `🖼 \\`index-basket\\`` silindiğinde — yani M12'nin yakalamak için var olduğu kusur işlendiğinde — test GEÇİYOR. Kontrol: özgün 13 karakterlik ad ve özgün sırayla aynı aşama bağı silindiğinde `UNMARKED S0-L4: index-basket` ile kırmızı. Aritmetik: `sonra` kapanış ters tırnağından sonra 26 karakter; ayırıcı ` · \\`` (4) + `\\` *(` (4) ile, ≤10 karakterlik bir SONRAKİ anahtar `fallback` kelimesini ≤18 offsete, yani pencerenin içine koyuyor. Ters yönde de kırılgan: `once` tam 4 karakter ve `🎛` bir surrogate çifti (2 UTF-16 birimi), yani `\" 🎛 \"` sıfır boşlukla sığıyor — `` `key` 🎛 `` sırası ya da üç boşluk yanlış kırmızı üretir. (`+2`/`-4` aritmetiğinin kendisi doğru: `yer` açılış ters tırnağını gösteriyor.)"
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "26 karakterlik pencere kalktı: plan `·` ile KALEMLERE bölünüyor ve her anahtar kendi kaleminde aranıyor. Komşu kalemin açıklamasına taşma ve komşu anahtar uzunluğuna bağımlılık ortadan kalktı."
  - id: RV-073
    severity: medium
    evidence: proven
    category: m12-silent-skip
    file: backend/tests/Finans.Integration.Tests/EducationSeedTests.cs
    line: 418
    summary: "`if (asamaBas < 0 || asamaBas > baslik.Index) continue;` o künye için M12'nin aşama-planı yarısını teşhissiz düşürüyor, ama künyeyi `denetlenen` sayacında TUTUYOR — yarısı denetlenmemiş künye 'denetlendi' diye raporlanıyor."
    rationale: "Bugün 18 künyenin 0'ı atlanıyor, yani mevcut kapsam tam; sorun bugünkü bir boşluk değil, SESSİZ bir boşluk. Mutasyon: S0-L8'in `**Aşama planı` bölümü figür planı paragrafının ALTINA taşındı ve bir aşama bağı bozuldu (`🖼 \\`weight-share\\`` → planda olmayan bir anahtar) → `stage-check-skipped=[S0-L8]`, test GEÇİYOR, `denetlenen` hâlâ 18. Bölüm sırası farklı yazılmış bir künye kuralın yarısını kaybediyor ve yine denetlenmiş görünüyor. `asamaBas.Should().BeGreaterThanOrEqualTo(0)` ya da aşama-denetlenen künyeyi ayrı sayıp eşiğe bağlamak kapatır."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "Aşama bölümü bulunamadığında künye artık sessizce 'denetlendi' sayılmıyor: aşamasızlar ayrı sayılıyor ve `asamaTasiyan` tam 33'e sabitlendi (35 künyenin 33'ü; S1-L1/S1-L2 farklı biçimde belgeli). Sayı düşerse bir künye kuralın yarısını kaybetmiş demektir ve test kırılır."
  # ── MEDIUM · figür anlamı / okunabilirlik ───────────────────────────────
  - id: RV-074
    severity: medium
    evidence: strong
    category: imperceptible-encoding
    file: web/src/components/LessonFigure.tsx
    line: 2051
    rule_or_decision: D-012
    summary: "`TimeHorizonRuler`: yeni yorum kısa→uzun geçişinin 'opaklık basamağıyla korunduğunu' söylüyor, ama üç bölge aynı sınıfa indiğinde basamak ~1,13:1 — algısal olarak ölü; ayrıca opaklık artık o bölgelerdeki TEK ayırt edici kodlama, ki kayıt defteri bunu yasaklıyor."
    rationale: "Etkin alfalar 0,32 / 0,256 / 0,192 → `#394167` / `#31395c` / `#2a3151`; komşu bölge kontrastı 1,133:1 ve 1,129:1 (uçtan uca 1,279:1). Öncesinde bölge 0 düz accent ile bölge 1'e karşı 3,91:1'di — net. Ders KAYBEDİLMİYOR: her bölge kendi `fig-label` adını kutunun içinde taşıyor (artık 8,83:1, yani ilk bölge OKUNUYOR — düzeltme burada sorunu taşımamış) ve 2px boşluklar bölgeleri 1,75:1 ile ayırıyor. Kaybolan şey yakın→uzak yoğunluk rampası, ve yorum sonraki bakımcıya onun ayakta olduğunu söylüyor. Daha önemlisi `CAPABILITIES.md:154` şunu şart koşuyor: 'Durum bilgisi renkle değil **biçimle** kodlanır' — opaklık-tek-başına kodlama bu kurala uymuyor. Geçiş zaten BİÇİMLE kodlu: genişlikler 70 → 96 → 110 büyüyor, ve dersin kendi metni üç bölgeyi eşit sayıyor ('bu sınırlar bir kural değil, kaba bir cetveldir'). `opacity` niteliğini tümüyle düşürmek hem kuralı hem yorumu temizler."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "`opacity={1 - i * 0.2}` tümüyle kaldırıldı. Kısa→uzun geçişi zaten BİÇİMDE kodlu (genişlikler 70 → 96 → 110); opaklık basamağı 1,13:1 ile algısal olarak ölüydü ve kayıt defterinin 'renkle değil biçimle' kuralını ihlal ediyordu. Yanıltıcı yorum da düzeltildi."
  - id: RV-075
    severity: medium
    evidence: strong
    category: residual-contrast
    file: web/src/components/LessonFigure.tsx
    line: 2646
    rule_or_decision: D-012
    summary: "Çip içi yazıda `fig-value` kullanan beş etiket düzeltmeden sonra da 3,54–3,95:1'de kalıyor — küçük yazı için WCAG AA eşiğinin (4,5:1) altında, yani düzeltme tam da kendisi için yapıldığı dizgilerde hedefinin biraz berisinde duruyor."
    rationale: "`fig-value` = `--muted` (#97A3C9), 10px. `BazaarToExchange`'in iki '90' çipi bir `Panel` içinde olduğu için zemin `fig-card` (#1d243e), çip #404870 → 3,54:1; `LastPriceIsHistory` ('son') ve `BidAskLadder`'ın iki kotasyon etiketi kart üzerinde → 3,95:1. Aynı `--muted` rengi aynı figürlerin başka yerlerinde 6,13–6,89:1'e ulaşıyor, yani çip içi yazı çizimin en zayıf metni olmayı sürdürüyor. Gerileme DEĞİL (öncesi 1,14:1'di) ama ucuz bir tamamlayıcı var: çip içi yazıya `fig-label` vermek (near-white → 7,93–8,83:1), ki `TwoSidesOfATrade`/`PriceIsNotValue`/`PortfolioAnatomy` zaten böyle yapıyor. Kontrast oranları kaynak paletten (App.css + packages/shared/src/theme) hesaplandı; sınıf-renk eşleşmesi doğrudan teyit edildi, oranlar bağımsız olarak yeniden koşturulmadı."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "Çip içi beş etiket `fig-value` → `fig-label` oldu (≈3,5-4,0:1 → ≈7,9-8,8:1): bazaar-to-exchange ×2 · last-price-is-history · bid-ask-ladder ×2. İki kol simetrik kaldı."
  # ── MEDIUM · borç ve kayıt dürüstlüğü ───────────────────────────────────
  - id: RV-076
    severity: medium
    evidence: strong
    category: debt-honesty
    file: .agent-method/registry/CAPABILITIES.md
    line: 154
    rule_or_decision: INV-12
    summary: "`CAPABILITIES.md` opak dolgu kuralını GENEL olarak yazıp 'İlk kural artık TESTLİ (GD-007)' diyor; bu cümle `fig-bar-pos`/`fig-bar-neg` için YANLIŞ ve 8 canlı karşı örneği var — hem de CLAUDE.md §11.5'in 'kod yazmadan önce taranır' dediği dosyada."
    rationale: "Kayıt şöyle diyor: 'yazı taşıyan dolgu OPAK olmaz' (genel kural) + '📌 İlk kural artık TESTLİ'. Test yalnız `fig-seg-lead`'i ve yalnız `rect`i ve yalnız çapa noktasını görüyor (RV-065, RV-070). Sonraki figür yazarı bu notu okuyup 'opak dolgu kuralı testli' varsayımıyla `no-guarantee` tarzı bir opak mint çip yazar, okunmaz hücre yeşil takımla ve rahat vicdanla yayına gider — kaydın amacı tam bunu önlemekti. TD-003 de aynı eksikle yazılmış: yalnız iki 'niyet' sınıfını ve jsdom/kontrast kaydını taşıyor, RV-065/RV-070'in üç makineleştirilebilir kaçışını taşımıyor. TD-003'e üç satır gerekiyor: kural yalnız `fig-seg-lead`i kapsar (diğer opak sınıfları değil), yalnız `rect`i kapsar (`circle`/`path`i değil), yalnız yazının çapa noktasını kapsar (kapladığı alanı değil). GD-007 kapsam dürüstlüğünde alışılmadık biçimde dikkatliydi; bu iki kayıt o çizginin gerisinde kaldı."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "CAPABILITIES.md'nin 'İlk kural artık TESTLİ' cümlesi kapsam sınırlarıyla değiştirildi (yalnız üç opak sınıf + opacity ≥ 0,9 · yalnız rect/circle · tahmini alan · gerçek kontrast ölçülmez) ve 10 canlı ihlalin varlığı kayda geçti. TD-003 aynı üç sınırı taşıyor. 101 düzeltmesi de yapıldı."
  - id: RV-077
    severity: medium
    evidence: proven
    category: debt-closure
    file: web/src/components/LessonFigure.test.tsx
    line: 29
    rule_or_decision: INV-08
    summary: "TD-001 'okunmaz' yarısı üzerinden kapatıldı; 'bozuk' yarısı yalnız ÇÖKME düzeyinde kapsanıyor, çünkü dört render kontrolünün tamamı 101 figürün 100'ünün kullandığı tek bir `Figure` sarmalayıcısından türüyor."
    rationale: "`grep -c \"<Figure\"` → 100 kullanım, 101 kayıt anahtarına karşı (yalnız `inflation-slider` kendi işaretlemesini kuruyor). `Figure` koşulsuz olarak `<svg role=\"img\" aria-label={label}>` ve `<figcaption>{caption}</figcaption>` basıyor, TypeScript de iki prop'u zorunlu kılıyor. Yani `svg[role=\"img\"] != null`, `aria-label.length > 10` ve `figcaption.length > 10` tek bir sarmalayıcı testinin 100 kez tekrarı. En kısa label 52, en kısa caption 59 karakter — eşiklere 40 karakterden fazla mesafe var. Kapı çökme/boş durumunu GERÇEKTEN kapatıyor ve GD-007'nin 7 ek ihlali bulması bunun değerinin kanıtı. Ama okurun 'bozuk' dediği şey — şekillerin üst üste binmesi, viewBox kenarında kırpılan çubuk, çarpışan etiketler, genişlikleri toplanmayan yığın — bir koordinatı tuvalle hiç karşılaştırmayan jsdom kapısının tümüyle dışında. (Tüm 101 figür viewBox taşması için tarandı: 0 bulgu, yani körlük gerçek ama bugün tetiklenmemiş.) TD-001'i bölmek yerine (okunabilirlik → kapandı; görsel bozulma → TD-003 ya da tarayıcı düzeyi bir kalem) tümüyle kapatmak borcu olduğundan küçük gösteriyor; GD-007 §5 jsdom sınırını dürüstçe yazıyor, TD-001'in `closed` satırı o kaydı ileriye taşımıyor."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "Borç bölündü: okunabilirlik TD-001'de kapalı kaldı, GÖRSEL BOZULMA **TD-004** olarak ayrıldı (şekil çarpışması, viewChain kırpılması, çakışan etiket, toplanmayan yığın). TD-004 dört render kontrolünün de tek `Figure` sarmalayıcısından türediğini ve çözümün tarayıcı düzeyi olduğunu yazıyor. TD-001 satırı da geçmiş zamana çevrildi."
  - id: RV-078
    severity: medium
    evidence: proven
    category: adm-process
    file: .agent-method/state.yaml
    line: 128
    rule_or_decision: INV-06
    summary: "GD-006 ve GD-007 kod değişikliğiyle `status: done` oldu ve insan kabulü aldı, ama `config.review.required_for_code_changes: true` olduğu hâlde ikisinde de adm-review koşmadı; `review.status` yine de `clean` duruyor ve hook 'incelendi-temiz' ile 'hiç incelenmedi'yi ayırt edemiyor."
    rationale: "`config.yaml` review.required_for_code_changes: true. GD-006.md ve GD-007.md'de 'review/inceleme' aramasında KENDİ incelemelerine dair tek satır yok (GD-007'dekiler REVIEW-004/005'e, yani önceki turlara atıf). `state.yaml → review.last_review_id` hâlâ REVIEW-005, yani GD-005. S-008 ve S-009 bunu kendi kanıt notlarında dürüstçe kaydetti ('⚠ adm-review çalıştırılmadı'), ama iki GD yine de kapatıldı — sözleşme kaydedilerek değil, karşılanarak tutulur. Bu incelemenin kendisi o borcun ödenmesi: 24 bulgunun 5'i high ve ikisi (RV-065, RV-067) kapıların kendi gerekçelerini zayıflatıyor, yani inceleme atlandığında kaybedilen şey biçimsel bir kayıt değildi. Yapısal kol: hook `reviewStatus !== 'not-run' && !== 'clean'` ile uyarıyor, dolayısıyla iki incelenmemiş GD'den sonra 'clean' sessiz kalıyor. INV-18'in 'açık critical bulgular blocker olarak görünür' sözü de hook'ta yok — hook `blockers` listesini okuyor, açık bulgulardan türetmiyor."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "Borç bu incelemeyle ödendi. GD-006 ve GD-007'ye §9 'İnceleme — GERİYE DÖNÜK' bölümleri eklendi ve atlanan incelemenin maliyeti bulgu bulgu yazıldı. state.yaml → review.status artık `findings`, dolayısıyla hook oturum başında '⚠ Review durumu: findings' basıyor — 'incelendi-temiz' ile 'hiç incelenmedi' ayrımının yapısal kolu da çalışıyor."
  # ── LOW ─────────────────────────────────────────────────────────────────
  - id: RV-079
    severity: low
    evidence: proven
    category: m12-parser-fragility
    file: backend/tests/Finans.Integration.Tests/EducationSeedTests.cs
    line: 407
    summary: "`.Distinct()` eskimiş bir başlıkla birleşince yinelenen-anahtar yazım hatası geçiyor; ayrıca regex plan paragrafındaki her küçük harfli ters-tırnaklı belirteci figür anahtarı sayıyor ve hiçbir anahtar kayıt defterine karşı doğrulanmıyor."
    rationale: "Mutasyon: S0-L1'in planına zaten listelenmiş `no-guarantee` bir kez daha eklendi, başlık (8)'de bırakıldı → ham 9, distinct 8, beyan 8 → test GEÇİYOR, oysa paragraf gözle dokuz kalem listeliyor; yani M12'nin yakalamak için kurulduğu başlık/plan kayması. Ters durum (başlık da 9'a çıkarılsa) yakalanıyor. İki yönlü ikinci kırılganlık: `` `[a-z0-9-]+` `` karakter sınıfı herhangi bir küçük harfli belirteci tutuyor — plan paragrafı bir CSS sınıfı (`fig-seg`) ya da bir slug anarsa sayım şişip ders yanlış kırmızı olur, katkı veren de başlığı artırarak 'düzeltir' ve kanonik kaynağa yanlış sayı gömülür; ters yönde yazım hatalı bir anahtar (`two-side-of-a-trade`) pekâlâ sayılır, yani M12 var olmayan bir figürü adlandıran planın aritmetiğini onaylar. Bugün 167 plan anahtarının tamamı gerçek, stray belirteç yok. Ucuz çözüm: ham sayım üzerinden iddia + `raw.Count == distinct.Count`, ve plan anahtarlarını aynı dosyada hazır duran `WebFigureRegistryKeys()` ile kesiştirmek."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "HAM sayım karşılaştırılıyor (`hamAnahtarlar.Count == beyan`) ve yineleme ayrı bir iddiayla, yinelenen anahtarları adlandırarak yakalanıyor. `*(mevcut)*` iddiası kayıt defterine karşı doğrulanıyor; kalan 'yazım hatalı anahtar' deliği yazılmamış dersler yüzünden kapatılamıyor ve TD-002'de açıkça yazılı."
  - id: RV-080
    severity: low
    evidence: proven
    category: floor-not-ratchet
    file: backend/tests/Finans.Integration.Tests/EducationSeedTests.cs
    line: 449
    summary: "İki 'en az N' koruması da içerik N'i geçtiği anda korumayı bırakıyor: `denetlenen >= 18` ve `FIGURE_KEYS.length >= 100`."
    rationale: "Mutasyon: figür planı taşıyan 19. bir künye eklenip S2-L8'in başlığı makine-okunamaz hâle getirildiğinde ((10) → (on)) `denetlenen = 18` ve test GEÇİYOR — S2-L8'in 10 figürü denetimsiz kalıyor. Aynı biçim `>= 100`a karşı gerçek 101 için geçerli (RV-068). Bugün M12'nin tabanı tam (boşluk 0, teyit edildi: S0-L1'in başlığı `**Mevcut figürler (8):**` yapıldığında denetlenen 17'ye düşüp kırmızı oluyor) — endişe bugünkü gevşeklik değil, gelecekteki boşluk. Bilinçli olarak yükseltilen bir sabite EŞİTLİK, ya da beklenen sayıyı dokümandan/kayıttan TÜRETMEK, ikisini de gerçek kıskaca çevirir."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "İki eşik de eşitliğe çevrildi: `blocks == 35`, `asamaTasiyan == 33`, `FIGURE_KEYS == 101`. Kanıt: 19. künye eklenip S2-L8 başlığı bozulduğunda eski `>= 18` yeşil kalıyordu; eşitlik kırmızıya düşürür."
  - id: RV-081
    severity: low
    evidence: proven
    category: opacity-ignored
    file: web/src/components/LessonFigure.test.tsx
    line: 70
    summary: "Kapı opaklığı yalnız sınıf adından çıkarıyor, `opacity` niteliğini hiç okumuyor — hem kaçırabilir hem yanlış bayrak kaldırabilir."
    rationale: "Kayıt defterinde 11 `fig-bar-pos` ve 3 `fig-bar-neg` rect `opacity < 0,8` taşıyor ve meşru biçimde okunabilir (örn. `BufferTraits`, LessonFigure.tsx:955, `opacity=\"0.22\"`); RV-065'in sayımından yalnız inceleyen kişi bunu elle filtrelediği için dışarıda kaldılar, test böyle bir filtre yapmıyor. Ters yönde: LessonFigure.tsx:2371-2372 `className={i === 0 ? \"fig-seg-lead\" : \"fig-seg\"}` ile `opacity={1 - i * 0.2}`'yi birlikte kullanıyor; bugün lead kolu `i === 0` ile çakıştığı için opaklık 1 ve yanlış pozitif yok (teyit: 0 yarı-saydam lead rect), ama o indeks eşlemesi kayarsa kapı saydam ve okunabilir bir rect'i kırmızıya düşürür. Kuralı sınıf yerine 'etkin opaklık' üzerinden kurmak (sınıf opak VE `opacity >= 0,9`) üçünü birden düzeltir."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "Opaklık artık sınıf adından DEĞİL, `opacity` niteliğinden okunuyor (`etkinOpaklik`, eşik 0,9). 14 meşru yarı-saydam `fig-bar-*` rect doğal olarak kapsam dışında kalıyor; saydam tona inmiş bir lead rect de yanlış kırmızı üretmiyor."
  - id: RV-082
    severity: low
    evidence: proven
    category: registry-bookkeeping
    file: .agent-method/registry/CAPABILITIES.md
    line: 97
    summary: "Bu diff'te notları esaslı biçimde genişletilen iki kayıt girdisi hâlâ `last_verified_ref: ac61b7e` taşıyor — 62 commit ve ~3 ay geride."
    rationale: "`ac61b7e` 2026-08-13 tarihli; `git log ac61b7e..HEAD` 62 commit sayıyor. Düzenlenen iki girdi eğitim seed/müfredat sözleşmesi ile LessonFigure kayıt defteri, yani tam bu diff'in doğruladığı iki yetenek: biri 101 figüre ve M12'ye karşı yeniden doğrulandı, alan güncellenmedi. Alan bir tazelik sinyali olarak okunuyorsa (AGENTS.md kaydı kanonik sayıyor) bu bir defter hatası; yalnızca bir çıkarım olarak duruyorsa alanın anlamı netleştirilmeli."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "Alanın anlamı başlıkta netleştirildi (girdi başına, yalnız o yetenek yeniden doğrulandığında güncellenir) ve bu turda gerçekten yeniden doğrulanan İKİ girdi `HEAD`e alındı. Diğerleri bilinçli olarak ac61b7e'de bırakıldı — hepsini birden ilerletmek doğrulanmamış girdiler için yanlış tazelik iddiası olurdu."
  - id: RV-083
    severity: low
    evidence: proven
    category: doc-code-drift
    file: .claude/docs/16-CURRICULUM.md
    line: 2462
    summary: "M12 kural satırı bağlanmamış figürün 'yalnız 🎛 ya da `*(fallback)*` işaretliyse' kabul edildiğini yazıyor; test ayrıca anahtardan sonraki 28 karakter içinde geçen 'etkileşimli' kelimesini de kabul ediyor."
    rationale: "Test yazılı sözleşmeden biraz daha izin verici (`sonra.Contains(\"etkileşimli\")`). İkisi sessizce ayrışabilir; kuralı okuyup 🎛 koymayan ama 'etkileşimli' yazan bir künye dokümana göre hatalı, teste göre geçerli. Hata değil, ifade uyumsuzluğu — ama M12'nin varlık nedeni tam olarak dokümanla gerçeğin sessizce ayrışmasını durdurmak."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "M12 kural satırı testin gerçek davranışıyla eşitlendi: üç lehçe, ham sayım, yineleme, kalem-içi işaret ('etkileşimli' dahil), `*(mevcut)*` doğrulaması. M13 için ayrı satır eklendi."
  - id: RV-084
    severity: low
    evidence: proven
    category: process-gate
    file: .claude/docs/09-TESTING-STRATEGY.md
    summary: "İki yeni makine kapısı da senaryo kataloğuna girdi eklemeden indi; CLAUDE.md §12.1 senaryoyu KODDAN ÖNCE şart koşuyor. Ayrıca SC-E23 hâlâ `[ ]` işaretli, oysa testleri `LessonFigure.test.tsx` içinde adıyla duruyor ve geçiyor."
    rationale: "`git diff a0918a7..HEAD --name-only` içinde `09-TESTING-STRATEGY.md` yok. `SC-E22` zaten M3/M4/M5/M6'yı taşıyor ve M12'nin doğal yeri; render kapısı da `SC-E16`'nın 'figür anahtarı olan bölümde erişilebilir SVG (role=\"img\") çizilir' satırının yanına ait. Girdi olmadan katalog — projenin 'hangi davranış korunuyor?' sorusuna kendi cevabı — kapsamı olduğundan az gösteriyor ve sonraki inceleyen kapsamı test dosyasından yeniden çıkarmak zorunda kalıyor (bu inceleme de öyle yaptı). SC-E23'ün işaretsizliği bu diff'ten önce de vardı, ama GD-007 tam o test dosyasını düzenledi: en ucuz an buydu."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "İki senaryo yazıldı: **SC-E30** (figür render + okunabilirlik kapısı, kapsam sınırlarıyla) ve **SC-E31** (künye sözleşmesi iki yönlü — M12 + M13). SC-E23 de `[x]` olarak işaretlendi: testleri dosyada adıyla duruyordu ve geçiyordu, yalnız kutu açık kalmıştı."
  - id: RV-085
    severity: low
    evidence: proven
    category: debt-bookkeeping
    file: .agent-method/pipeline/TEST_DEBT.md
    line: 10
    summary: "TD-001 satırının gövde metni boşluğu hâlâ ŞİMDİKİ zamanda anlatıyor ('29 figür render testinde yok', 'yalnız InflationSlider'ı kapsıyor') oysa Status sütunu `closed` diyor."
    rationale: "Satırın iki yarısı birbiriyle çelişiyor: 'Missing coverage' alanı kapanmış bir durumu olgu gibi tarif ediyor. Kapalı borçların gövdesini geçmiş zamana çevirmek ya da kapanış notunu gövdenin başına almak, satırı tek okuyuşta doğru kılar. (Aynı satır 29 figür diyor; gerçek kapsam 101 — RV-068.)"
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "TD-001 satırının gövdesi geçmiş zamana çevrildi ('yoktu', 'kapsıyordu') ve kapanış notu kapsam genişlemesini + TD-004 ayrımını taşıyor; satır tek okuyuşta doğru."
  - id: RV-086
    severity: low
    evidence: heuristic
    category: weakened-distinction
    file: web/src/components/LessonFigure.tsx
    line: 2653
    rule_or_decision: D-012
    summary: "`BazaarToExchange`: '90' anlaşma çipi artık sonucu olduğu altı teklif karesiyle AYNI dolguya sahip (1,09:1), yani girdiler→sonuç okuması tümüyle geometriye kaldı."
    rationale: "Çip (`fig-seg`, opacity 1) ile en parlak teklif karesi (`fig-seg`, opacity 0,85) arasındaki kontrast 1,091:1; en soluğuna karşı 1,334:1. Okuma geometriyle AYAKTA — y=76'da 70×18 çip içinde '90', y=44-54 arası dağılmış 14×10 kareler — ve bu biçim-temelli olduğu için kayıt defterinin kuralına UYGUN; pazar tarafındaki çipin hiç rect kardeşi yok ve sayının kendisi de ayırt edici. Yine de altyazı 'verilen ile istenen buluşunca fiyat doğar' diye söz veriyor ve figür artık doğan şeyi işaretlemiyor. Kesikli ya da konturlu bir çip (kayıt defterinin kendi önerdiği deyim) opak dolguyu geri getirmeden bunu onarır. Düşük önem ve sezgisel kanıt: bu bir gerileme değil, düzeltmenin yan etkisi ve karar tasarım yargısı."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "Yeni `fig-seg-strong` sınıfı eklendi: dolgu `fig-seg` ile aynı saydam tonda kalıyor, ayrım KONTURLA veriliyor — kayıt defterinin 'renkle değil biçimle' kuralına uygun ve opak dolguya dönmüyor. İki '90' anlaşma çipine uygulandı, böylece altyazının söz verdiği 'doğan fiyat' figürde yeniden işaretli."
  - id: RV-087
    severity: low
    evidence: proven
    category: alarm-fatigue
    file: .claude/hooks/adm-session-start.mjs
    line: 92
    summary: "`warn` dizisi artık her koşulda en az bir öğe taşıyor (ya INV-13 uyarısı ya 📌 notu), dolayısıyla '⚠ Uyarılar' bloğu HER oturumda basılıyor ve normal durum uyarı biçiminde bir satır üretiyor."
    rationale: "`if (openSessionId) {...} else {...}` iki kolda da `warn.push` yapıyor, yani alt taraftaki `if (warn.length)` artık her zaman doğru. S-009'un kök neden teşhisi 'hook brifing bastığı için adm-open gereksiz görünüyor' idi; her oturumda görünen bir uyarı satırı aynı alışkanlık mekanizmasını besler — uyarı sürekli olduğunda sinyal olmaktan çıkar. Düzeltmenin amacına ters düşmüyor (not bilgilendirici ve doğru) ama notu uyarı bloğundan ayırmak, ya da yalnız oturumda yazma işareti görüldüğünde göstermek, sinyali korur. Hook'un kendisini koşturarak teyit edildi: açık oturum yokken 📌 kolu doğru basılıyor, simüle edilmiş `id: S-010` ile INV-13 kolu gerçekten canlı (ilk kez)."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "Uyarılar ve notlar ayrıldı: `warn` yalnız eylem gerektiren sapmalar için ('— UYARI —' başlığıyla), 'açık oturum yok' notu ayrı bir satır olarak basılıyor. Normal durum artık uyarı biçiminde görünmüyor."
  - id: RV-088
    severity: low
    evidence: proven
    category: duplication
    file: backend/tests/Finans.Integration.Tests/EducationSeedTests.cs
    line: 836
    summary: "Depoda iki ayrı `RepoRoot()` uygulaması var ve farklı nirengi dosyaları arıyor; biri hata verince susuyor."
    rationale: "`EducationSeedTests.cs:836` `web/src/components/LessonFigure.tsx` arıyor ve bulamazsa açık bir iddia ile patlıyor; `Llm/LocalModelTrial.cs:218` `.agent-method` arıyor ve bulamazsa SESSİZCE çalışma dizinine düşüyor. Bu diff'ten önce de vardı, ama yeni M12 testi katı olanın ikinci tüketicisi oldu — paylaşılan bir yardımcı ayrışmayı kaldırır. Acil değil; CAPABILITIES.md'nin Reuse→Extend→Extract→New merdiveninde 'Extract' kalemi."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "web 350/350 · tsc temiz · Application 308/308 · Integration 189/193 (4 kırmızı = INC-001, ortam) · EducationSeedTests 27/27"
      note: "Tek `RepoPaths` yardımcısına çıkarıldı (Extract). İki nirengi (`.agent-method/` + `LessonFigure.tsx`) birlikte aranıyor ve bulunamazsa açık bir `DirectoryNotFoundException` atılıyor — `LocalModelTrial`in sessizce çalışma dizinine düşen hâli kalktı. Çağrı yerleri sarmalayıcıyla korundu."
---

# REVIEW-006 — GD-006 + GD-007 inceleme borcu (`a0918a7..HEAD`)

## 1. Bu inceleme neden var

`config.yaml → review.required_for_code_changes: true`. GD-006 ve GD-007 kod
değiştirdi, `status: done` oldu ve insan kabulü aldı — **ikisinde de adm-review
koşmadı.** S-008 ve S-009 bunu kendi kanıt notlarında dürüstçe kaydetti; bu
inceleme o borcun ödenmesidir (RV-078).

Diff tabanı insan kararıyla seçildi: açık ADM oturumu olmadığı için `start_sha`
yoktu ve skill taban tahmin etmeyi yasaklıyor. Üç aday sunuldu, `a0918a7..HEAD`
seçildi — iki GD'nin tamamını kapsayan aralık.

## 2. Kapsam

| Dosya | Ne değişti |
|---|---|
| `web/src/components/LessonFigure.tsx` | `FIGURE_KEYS` test dikişi; 8 opak dolgu `fig-seg`'e indirildi (7 figür) |
| `web/src/components/LessonFigure.test.tsx` | Render kapısı + okunabilirlik kapısı (+203 test) |
| `backend/tests/.../EducationSeedTests.cs` | M12 — künye figür sayısı mutabakatı |
| `.claude/docs/16-CURRICULUM.md` | 10 künye sayacı düzeltildi; M11 + M12 kural satırları |
| `.claude/hooks/adm-session-start.mjs` | YAML dilimleyici düzeltmesi + oturum yok uyarısı |
| ADM defterleri | GD-006, GD-007, S-008, S-009, TEST_DEBT (TD-002/TD-003), CAPABILITIES, state.yaml |

**Sınıflandırma:** karma — UI/SVG gerilemesi + test altyapısı + doküman-olarak-sözleşme
+ geliştirme aracı. Kimlik/yetkilendirme, DB migration ve parasal hesap kapsam dışı,
dolayısıyla güvenlik/authorization/migration-safety perspektifleri seçilmedi.

## 3. Doğrulama kanıtı (orkestratör, doğrudan koşturdu)

- `web`: **350/350 yeşil**, 34 dosya · `npx tsc --noEmit` temiz
- `EducationSeedTests`: **26/26 yeşil** (M12 dahil)
- Çalışma ağacı temiz (`git status --porcelain` boş), HEAD `17cc3c0`
- 10 künye düzeltmesinin tamamı elle sayıldı → gerçek anahtar sayılarıyla **tutuyor**,
  S2-L5'in ters yönü (9→8) dahil. GD-006'nın "11 değil 10, S2-L2 doğruymuş"
  öz-düzeltmesi de doğrulandı.

## 4. Ana bulgu: iki kapı da gerçek, ama kayıtlarından DAHA DAR

GD-007'nin gerekçesi **sağlam** — RV-039/053/055'in özgün kodu geri alındığında
kapı üçünde de kırmızıya düşüyor (mutasyonla kanıtlandı, üstüne iki ek tüketici
daha yakalandı). Kapılar tiyatro değil. Sorun şu: ikisi de kendi kayıtlarının
söylediğinden dar ve aradan **aynı kusur** geçiyor.

1. **Okunabilirlik kuralı üç opak dolgu sınıfından birine yazılmış** (RV-065).
   `fig-bar-pos` ve `fig-bar-neg` de tam opak ve RV-039 geometrisinin **8 canlı
   örneği bugün yeşil geçiyor** — `no-guarantee` birebir aynı şekil, ≈1,31:1 ve
   ≈1,03:1 kontrastla. GD-007 §3.1 bunları "44 figür bu renkleri kullanıyor"
   diye dışarıda bıraktı; 44 doğru ama yanlış popülasyon — üzerine **yazı yazan**
   yüzey 8-10 çift, yani GD-007'nin zaten yaptığı işin boyutunda. Beyaz listeye
   gerek yoktu.
2. **M12, 35 künye bloğunun 17'si için hiçbir şey iddia etmiyor** (RV-066) ve
   bunların ikisi yayında olan ders; kaçış yolu yalnızca künyede farklı bir
   başlık ifadesi kullanmak. Dahası kırmızı bir M12, **planı küçültüp** yeşile
   çevrilebiliyor: yayında olan `hold-vs-flip` figürünün D-017'nin kanonik ilan
   ettiği dokümandan sessizce silinebildiği mutasyonla kanıtlandı (RV-067) —
   TD-002'nin tersi yön, hiçbir borçta yazılı değil.
3. **Kayıt defteri 101 figür tutuyor, bütün kayıtlar 100 diyor** (RV-068);
   kaçan anahtar tırnaksız yazılmış `concentration`. `>= 100` tabanı bir birim
   boşluk taşıyor ve GD-007 §7'nin dökümü (100+100+1=201) kendi doğru toplamıyla
   (203) çelişiyor.
4. **Hiçbir CI testi koşturmuyor** (RV-069) — tek workflow backlog senkronu.
   "Gözle denetimden makine kapısına geçtik" iddiası, kapının koşmasını yerelde
   bir insanın hatırlamasına bağlı bırakıyor.

Bu dördü birlikte okunduğunda `CAPABILITIES.md`'nin "İlk kural artık TESTLİ"
notu — CLAUDE.md §11.5'in *kod yazmadan önce taranır* dediği dosyada — sonraki
figür yazarını yanlış yönlendiriyor (RV-076).

## 5. Sayımlar

| Önem | Adet | Durum |
|---|---|---|
| critical | 0 | — |
| high | 5 | hepsi `resolved` |
| medium | 9 | hepsi `resolved` |
| low | 10 | hepsi `resolved` |
| **toplam** | **24** | **24 resolved · 0 open** |

**Açık critical yok → INV-18 gereği hiçbir GD tamamlanması bloke değil.**

Ürün sahibi kararı **"Hepsini düzelt"** (2026-11-22) — bu projede REVIEW-004 ve
REVIEW-005'te de aynı ifade *tüm* bulguları kapsamıştı. 24 bulgunun tamamı
düzeltildi ve durumları `resolved` yapıldı.

**RV-069 ayrı bir karar gerektirdi** ve ajan tek başına kapatmadı: düzeltmesi
**dışa dönük** bir değişiklik (depoda koşan bir iş akışı). Ürün sahibine sunulan
dört seçenekten **"yeşil paketler için CI"** seçildi ve `tests.yml` buna göre
yazıldı; INC-001'in dört testi, gerekçesi iş akışının başında yazılı olacak
şekilde **görünür** biçimde dışlandı.

## 6. Tek bir şey değişecekse → YAPILDI

Okunabilirlik yüklemi `rect.fig-seg-lead`den **etkin opaklığa** çevrildi: üç
opak sınıf (`fig-seg-lead` · `fig-bar-pos` · `fig-bar-neg`) **ve** `opacity ≥
0,9`, `rect` yanında `circle`, çapa noktası yerine yazının **tahmini kapladığı
alan**. Kapı kurulur kurulmaz **10 ihlal** bildirdi (öngörülen 8 + `anchor="end"`
olduğu için ilk taramada sınırda kalan 2) ve hepsi düzeltildi. Sayı 100 → **101**.

İki bulgu için düzeltme, incelemenin önerdiğinden **daha ileri** gitti:
- **RV-065:** mint/coral *açık* zemin olduğu için yazıyı `fig-label`a yükseltmek
  çözüm değildi (≈1,66:1). Dolgunun anlam taşıdığı yerlerde yazı kutunun DIŞINA
  taşındı; vurgunun kendisi asimetri olan yerlerde dolgu saydam tona indi.
- **RV-067:** incelemenin "kapatılamaz" dediği yön yanlıştı. TD-002'nin
  **seed → künye** yarısı kapatılabilirdi ve kapatıldı (**M13**): yazılmamış
  dersler bu yönü kırmaz, çünkü seed'deki her figür zaten yazılmış bir derse ait.

## 7. Durum geçişleri

Bulguların tamamı `status: open` açıldı; 23'ü ürün sahibinin **"Hepsini düzelt"**
kararı üzerine düzeltilip `resolved` yapıldı ve her birine `resolution` bloğu
(tarih · kanıt · not) yazıldı. Kabul kriterlerini ajan onaylamaz (AGENTS.md
§4.3); burada onaylanan şey bulgunun *düzeltildiği*, kanıtı §9'da.

Kapsam dürüstlüğü: **RV-069** ve **RV-088** bu diff'in soktuğu hatalar değil,
önceden var olan durumlar — GD-006/GD-007'nin kabul gerekçesini taşıdıkları için
buraya yazıldılar ve öyle işaretlendiler.

## 8. Düzeltmelerin bulduğu YENİ işler

Kapıları genişletmek, kapsamları dışında kalmış **beş birikmiş sapma** ortaya
çıkardı — yani bulguların kendileri kadar önemli bir sonuç:

| Ne | Nerede | Durum |
|---|---|---|
| `fig-bar-pos`/`fig-bar-neg` üzerinde 10 okunmaz yazı | 9 figür | düzeltildi |
| **S1-L3** künyesi 7 ilan ↔ 9 anahtar | `16-CURRICULUM.md` | düzeltildi (7→9) |
| **S3-L2**'de `price-doesnt-know-you` aşamada tarif edilmiş, 🖼 işareti yok | `16-CURRICULUM.md` | düzeltildi |
| Dokuz künyede planın son figürü aşamaya bağsız | Set 3/4/5 | **TD-005** (editöryal karar) |
| Figürlerde görsel bozulma denetlenmiyor | jsdom sınırı | **TD-004** (tarayıcı düzeyi) |

S1-L3, GD-006'nın düzelttiği on künyeyle **aynı hatanın** başka bir lehçedeki
hâliydi: M12 o lehçeyi tanımadığı için tarama onu hiç görmemişti.

## 9. Düzeltme sonrası doğrulama (orkestratör doğrudan koşturdu)

- `web`: **350/350 yeşil** (34 dosya) · `npx tsc --noEmit` **temiz**
- `Finans.Application.Tests`: **308/308 yeşil**
- `Finans.Integration.Tests`: **189/193** — 4 kırmızı, tam olarak **INC-001**'in
  bilinen ortam hatası (`HealthEndpointTests` 1 + `ObservabilitySecurityTests` 3,
  `WebApplicationFactory` ayağa kalkmıyor; issue #74). Bu diff'ten önce de aynı
  dördü kırmızıydı — değişikliklerle ilgisiz.
- `EducationSeedTests`: **27/27 yeşil** (M12 sertleştirildi + M13 eklendi)
- Çalışma ağacı: yalnız amaçlanan dosyalar; mutasyon denemeleri geri alındı.

**Kapılar kırmızıya düşüyor mu? — üç ayrı mutasyonla kanıtlandı:**
1. `no-guarantee`'nin etiketi opak `fig-bar-pos` üzerine geri alındı → render
   kapısı kırmızı, ihlali koordinatıyla bildirdi (eski kapının GÖREMEDİĞİ sınıf).
2. S0-L1'in planından `hold-vs-flip` silinip sayaç azaltıldı → **M12 yeşil
   kaldı** ama **M13 kırmızıya düştü** ve figürü adıyla bildirdi. RV-067'nin
   tam senaryosu.
3. `listItems()` beş girdi biçimiyle, hook'un **gerçek kaynağından çıkarılan**
   fonksiyonla sınandı (elle kopyalamak farkı gizleyebilirdi).

## 10. İncelendi, temiz

- **Kapı RV-039/053/055'i gerçekten yakalıyor** — iddia edilmedi, kanıtlandı.
  Üç özgün kod geri alınıp gerçek test koşturuldu: 5 kırmızı, üç hedef figür
  kendi geometrisini raporladı. Ayrıca reviews'ın hiç anmadığı iki `WeightStack`
  tüketicisi daha yakalandı. **Gerekçe fazla iddia etmiyor.**
- **RV-054 ve RV-063'ün makineleştirilemez ilan edilmesi** doğru ve üç ayrı
  yerde (test dosyası başlığı, GD §3.1, TD-003) açıkça yazılı.
- **`FIGURE_KEYS` gerçekten eskiyemez** — `Object.keys(FIGURES)` türetilmiş;
  yeni figür otomatik kapsanıyor. Eskiyen şey yanındaki `>= 100` tabanı.
- **CRLF endişesi yok** — `16-CURRICULUM.md`: 2561 LF, 0 CR; `.gitattributes`
  `*.md text` ile LF'i yapılandırmayla sabitliyor. CRLF'e çevrilmiş kopyada test
  **yüksek sesle** kırılıyor, sessizce geçmiyor.
- **M12 aritmetiği ve ayrıştırıcısının geri kalanı** — `yer == -1` imkânsız,
  `+2`/`-4` dilimleri güvenli, 18 ders id'si doğru çözülüyor, kod bloklarında
  phantom `####` yok, künye dışı 4 "Figür planı" anması `\(\d+\)` şartına
  takılmıyor, 253 🖼'nin 248'i anahtarlı ve kalan 5'i düzyazı.
- **`num()` yardımcısının kaçış yolları bugün boş** — 0 eksik x/y, 0 `tspan`,
  0 sayısal olmayan geometri (%/px/em yok), `fig-seg-lead` atasında 0 `transform`.
- **Hook `block()` düzeltmesi doğru ve tam** — açıklamalı/açıklamasız/boşluksuz/
  sekmeli bütün biçimler doğru dilimleniyor; `session:` state.yaml'da sondaki
  açıklamayı taşıyan tek anahtar, başka bir şey bozulmadı. INV-13 kolu ilk kez
  gerçekten canlı.
- **Figür düzeltmelerinin 5'i sağlam** — `PortfolioAnatomy`, `TwoSidesOfATrade`,
  `PriceIsNotValue`, `LastPriceIsHistory`, `BidAskLadder`. `BidAskLadder` eskiden
  kayıt defterinin ikinci ton kuralını da ihlal ediyordu (alış/satış eşit kalem,
  biri vurguluydu); şimdi simetrik.
- **Düzeltme eksiksiz** — değişen 8 rect'in hepsi yazı taşıyordu; kalan
  `fig-seg-lead` öğelerinin hiçbirinde yazı yok (`circle` dahil).
- **`MatchingEngine` hâlâ `i === 0 ? lead : seg` + opacity desenini kullanıyor** —
  doğru, çünkü üzerinde yazı yok. Yazarın deyimi topluca temizlemek yerine
  yalnız okunabilirlik hatasının gerektirdiği yeri değiştirdiğinin kanıtı.
- **Katman yerleşimi** — M12'nin integration projesinde durması emsalli: M4 zaten
  aynı dosyada `LessonFigure.tsx`'i aynı biçimde okuyor, ve §9.1'in bütün M
  kuralları `EducationSeedTests`'te yaşıyor diye belgeli. `RepoRoot()` çalışma
  dizininden bağımsız, bulamazsa açık iddia ile patlıyor. Taşınması önerilmiyor.
- **`FIGURE_KEYS` üretim kodunda kullanılmıyor** (grep: yalnız test dosyası).
- **Yeniden kullanım ayrışması yok** — `16-CURRICULUM.md`'yi başka hiçbir test
  okumuyor, iki regex lehçesi yan yana durmuyor.
- **M-kural tablosundaki adlandırılmış testlerin hepsi var** — M10, M11, M12
  (M1-M9 test adı vermiyor, ayrışacak bir şey yok). M11'in testi
  `EducationSeedTests.cs:736`.
- **viewBox taşması yok** — 101 figür tarandı, 0 bulgu.
- **Önceki incelemenin açık kalemi** — REVIEW-004'ün frontmatter'ı hâlâ
  `review.schema.json`'a uymuyor (`id`/`gd`/`created`/`partial` yazıyor; şema
  `review_id`/`runtime`/`timestamp`/`scope`/`active_gd` istiyor). Bu incelemenin
  kapsamı dışında ve bir RV numarası almadı; state.yaml'da bildirilmiş olarak
  duruyor, insan kararı bekliyor (AGENTS.md §4.2).
