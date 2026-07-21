# 16 — Müfredat (İçerik Sözleşmesi)

> **Tarih:** 2026-07-22 · **Durum:** ✅ Onaylandı (kararlar `15` §9.1 + bu doküman §11)
> **Kapsam:** Eğitim modülünün **25 dersinin** ders bazında müfredatı — öğrenme
> çıktıları, kavram haritası, aşama planları, değerlendirme ve **kaynak politikası.**
> **Kardeş doküman:** [`15-EDUCATION-PLAN.md`](15-EDUCATION-PLAN.md) *tasarımı*
> anlatır (şema, kişiselleştirme, canlı bağlam). Bu doküman *içeriği* anlatır.
> **Yasal çerçeve:** `CLAUDE.md` §2 (yatırım tavsiyesi DEĞİL) — her künyede ⚠ notu.

---

## 1. Amaç ve kullanım

Bu doküman **ders yazmadan önce okunur.** Bir dersin içeriği yazılırken
"ne anlatayım" sorusu burada zaten cevaplanmıştır; yazarın işi künyeyi
[`EducationContent.cs`](../../backend/src/Finans.Infrastructure/Seed/EducationContent.cs)
bloklarına çevirmektir.

**Neden gerekti:** T6.11a/b'de ilk iki ders doğaçlama zenginleştirildi. 25 derslik,
*derinlemesine ve ciddi bilgi* iddiası taşıyan bir eğitimde bu sürdürülebilir
değil — kavramlar dersler arasında ya tekrar eder ya boşta kalır, öğrenme
çıktıları yazılı olmaz, olgusal iddiaların kaynağı bulunmaz.

| Soru | Cevap nerede |
|---|---|
| Ders hangi kavramı **tanıtır**, hangisini pekiştirir? | §3 kavram haritası |
| Ders bittiğinde okuyucu **ne yapabilir olacak**? | §5 künye → *öğrenme çıktıları* |
| Ders **kaç aşama**, hangi sırada, hangi figürle? | §5 künye → *aşama planı* |
| Bir sayı/tanım **nereden geliyor**? | §6 kaynak politikası + künye → *kaynaklar* |
| Test **neyi ölçüyor**? | §7 değerlendirme tasarımı + künye |
| Bunların hangisi **otomatik denetleniyor**? | §9 yapısal sözleşme |

**İş bölümü:** `15` = şema/derinlik/kişiselleştirme/canlı bağlam · `16` = müfredat
ve içerik kuralları · `08-BACKLOG.md` = görev sırası.

---

## 2. Pedagojik çerçeve

### 2.1 Öğrenme çıktısı yazım kuralı

Çıktı **ölçülebilir bir fiille** yazılır. "Anlar", "bilir", "farkındadır"
**kullanılmaz** — bunlar sınanamaz.

| ❌ Yazma | ✅ Yaz |
|---|---|
| Enflasyonu anlar | Nominal ve reel getiriyi **ayırt eder** |
| Riski bilir | Yüksek getiri vaadinin **hangi soruyu doğurduğunu söyler** |
| Çeşitlendirmenin önemini kavrar | İki portföyün yoğunlaşmasını **karşılaştırır** |

Kabul edilen fiiller, Bloom kademesine göre:

| Kademe | Fiiller | Hangi katmanda |
|---|---|---|
| **Hatırla / Anla** | tanımlar · ayırt eder · örnekler · kendi cümlesiyle açıklar | **L1 Core** |
| **Uygula** | hesaplar · uygular · yerine koyar · okur | **L2 Context** |
| **Analiz et** | karşılaştırır · yanıltıcı olduğu durumu gösterir · sınırını belirler | **L3 Deep** |

Her ders **3-5 çıktı** taşır; en az biri L1 kademesinde olmalıdır (yoksa ders
başlangıç seviyesine kapalıdır).

### 2.2 Katman eşlemesi

`15` §2.2'deki derinlik katmanları doğrudan Bloom kademesine bağlanır:

```
L1 Core     → Anla        → "kavram nedir, neden önemli" (jargonsuz)
L2 Context  → Uygula      → "nasıl hesaplanır, nerede kullanılır"
L3 Deep     → Analiz et   → "ne zaman yanıltır, sınırı nedir, TR'ye özgü incelik"
```

### 2.3 Spiral tasarım — aynı kavram üç kez

Bir kavram tek derste bitmez; **derinleşerek üç kez** geçer:

| Tur | Nerede | Nasıl |
|---|---|---|
| 1 · **Sezgi** | Set 0 | Formülsüz, somut sahne. "Bekleyen para erir." |
| 2 · **Formül** | Set 1 | Hesap ve tanım. `reel = (1+n)/(1+e) − 1` |
| 3 · **Kendi verisi** | Set 2 | `LiveContext` — kullanıcının gerçek rakamı |

Bu yüzden §3'te her kavramın **tek bir "tanıtıldığı ders"i** ve birden çok
"pekiştirildiği ders"i vardır. Tanıtım dersi kavramın *sahibidir*; sözlük
(T6.3) tanımı oradan alır.

### 2.4 Aşama kalıbı

T6.11a/b'de oturan kalıp **tüm derslerde zorunludur**:

```
kavram (Explain) → işlenmiş örnek (Example) → tuzak (Trap) → sonraki kavram …
```

- Hiçbir kavram **örneksiz** bırakılmaz. Soyut anlatım tek başına aşama olamaz.
- **İşlenmiş örnek** = adımları görünen örnek. "Şöyle olur" değil, "şu sayıdan
  şu sayıya nasıl gidildi".
- **Tuzak** = yaygın yanılgı; testin çeldiricileri buradan türer (§7).

### 2.5 Ders iskeleti (sabit)

Her ders şu üç aşamayı **sabit konumda** taşır:

| Konum | Blok | İçerik |
|---|---|---|
| **İlk aşama** | `Core/Explain` | **"Bu derste ne öğreneceksin?"** — §2.1 çıktıları madde madde. Aşamalı okuyucunun açılış adımı; yol haritasında ilk başlık. |
| **Sondan bir önce** | `Core/LiveContext` | "Senin portföyünde" (`15` §3) — dersin kavramı kullanıcının rakamıyla |
| **Son aşama** | `Core/Source` | **"Bu bilgiler nereden geliyor?"** (§6) |

Aradaki aşama sayısı serbesttir (hedef ~13). Mini test bunların **dışında**,
ayrı ve son adımdır (T6.10).

---

## 3. Kavram haritası

**64 kavram / 25 ders.** Her kavramın **tam bir** tanıtım dersi vardır. Bu tablo
aynı zamanda **kavram sözlüğünün (T6.3) omurgasıdır** ve `ConceptTag` seed'inin
kaynağıdır (bugün yalnız 6 etiket var).

> Okuma: **Tanıtım** = kavramın sahibi ders (tanım burada verilir).
> **Pekiştirme** = kavramın tekrar kullanıldığı dersler (§2.3 spiral).

### Set 0 — İlk Adımlar (26 kavram)

| Anahtar | Etiket | Tanıtım | Pekiştirme |
|---|---|---|---|
| `investment` | Yatırım | S0-L1 | S0-L5, S0-L6 |
| `saving-vs-investing` | Biriktirmek ve yatırmak | S0-L1 | S0-L2, S0-L3 |
| `speculation` | Spekülasyon | S0-L1 | S0-L7, S3-L2 |
| `risk-premium` | Risk primi | S0-L1 *(Deep)* | S0-L7, S1-L4 |
| `income-expense` | Gelir ve gider | S0-L2 | S0-L3 |
| `savings-rate` | Birikim oranı | S0-L2 | S0-L3, S1-L5 |
| `pay-yourself-first` | Önce kendine ayır | S0-L2 | — |
| `emergency-fund` | Acil durum fonu | S0-L3 | S0-L8, S2-L1 |
| `debt-cost` | Borcun maliyeti | S0-L3 | — |
| `opportunity-cost` | Fırsat maliyeti | S0-L3 | S0-L6, S3-L3 |
| `inflation` | Enflasyon | S0-L4 | S1-L1, S4-L3 |
| `purchasing-power` | Alım gücü | S0-L4 | S1-L1, S4-L3 |
| `price-index` | Fiyat endeksi (TÜFE) | S0-L4 | S1-L1, S2-L4 |
| `asset-class` | Varlık sınıfı | S0-L5 | S1-L2, S2-L3 |
| `ownership-vs-lending` | Ortaklık ve alacaklılık | S0-L5 | S0-L6, S1-L3, S4-L4 |
| `liquidity` | Likidite | S0-L5 | S0-L8, S3-L4, S4-L2 |
| `return-source` | Getirinin kaynağı | S0-L6 | S1-L3 |
| `capital-gain` | Değer artışı | S0-L6 | S2-L2 |
| `cash-flow-return` | Nakit akışı getirisi | S0-L6 | S1-L3, S1-L5 |
| `risk` | Risk | S0-L7 | S0-L8, S1-L2, S1-L4, S3-L1 |
| `volatility` | Oynaklık | S0-L7 | S1-L4, S2-L4 |
| `guaranteed-return-fallacy` | "Garantili yüksek getiri" çelişkisi | S0-L7 | S3-L2, S4-L3 |
| `time-horizon` | Vade (zaman ufku) | S0-L8 | S1-L5, S2-L4, S4-L1 |
| `portfolio` | Portföy | S0-L8 | S1-L2, S2-L1 |
| `position` | Kalem (pozisyon) | S0-L8 | S2-L1, S2-L2 |
| `weight` | Ağırlık | S0-L8 | S1-L2, S2-L1, S2-L3 |

### Set 1 — Yatırım Kavramları (12 kavram)

| Anahtar | Etiket | Tanıtım | Pekiştirme |
|---|---|---|---|
| `nominal-return` | Nominal getiri | S1-L1 | S2-L4, S4-L3 |
| `real-return` ✳ | Reel getiri | S1-L1 | S1-L5, S2-L4, S4-L3 |
| `diversification` ✳ | Çeşitlendirme | S1-L2 | S2-L1 |
| `correlation` | Korelasyon | S1-L2 | S2-L3 |
| `concentration-drift` | Ağırlık kayması | S1-L2 | S2-L1 |
| `pe-ratio` ✳ | F/K oranı | S1-L3 | S4-L4 |
| `pb-ratio` ✳ | PD/DD oranı | S1-L3 | — |
| `dividend-yield` | Temettü verimi | S1-L3 | S1-L5 |
| `peer-comparison` | Emsal karşılaştırma | S1-L3 | S4-L4 |
| `risk-return` ✳ | Risk ve getiri ilişkisi | S1-L4 | S3-L1 |
| `compound` ✳ | Bileşik getiri | S1-L5 | S2-L4, S4-L1, S4-L4 |
| `reinvestment` | Yeniden yatırım | S1-L5 | S4-L4 |

✳ = bugün seed'de **var olan** 6 `ConceptTag`.

### Set 2 — Portföyünü Okumak (9 kavram)

| Anahtar | Etiket | Tanıtım | Pekiştirme |
|---|---|---|---|
| `concentration` | Yoğunlaşma | S2-L1 | S3-L2 |
| `top-n-weight` | En büyük N ağırlığı | S2-L1 | — |
| `cost-basis` | Ortalama maliyet | S2-L2 | S3-L3 |
| `cost-averaging` | Kademeli alım | S2-L2 | S3-L4 |
| `realized-unrealized` | Gerçekleşmiş / gerçekleşmemiş kâr | S2-L2 | S3-L1 |
| `base-currency` | Baz para birimi | S2-L3 | S4-L2 |
| `fx-effect` | Kur etkisi | S2-L3 | S4-L2 |
| `period-selection` | Dönem seçimi | S2-L4 | S3-L3 |
| `annualization` | Yıllıklandırma | S2-L4 | — |

### Set 3 — Davranış (8 kavram)

| Anahtar | Etiket | Tanıtım | Pekiştirme |
|---|---|---|---|
| `loss-aversion` | Kayıptan kaçınma | S3-L1 | S3-L3 |
| `disposition-effect` | Kazananı erken satma eğilimi | S3-L1 | S3-L4 |
| `fomo` | FOMO (kaçırma korkusu) | S3-L2 | S3-L4 |
| `herding` | Sürü davranışı | S3-L2 | — |
| `anchoring` | Çıpalama | S3-L3 | — |
| `sunk-cost` | Batık maliyet | S3-L3 | — |
| `overtrading` | Aşırı işlem | S3-L4 | — |
| `transaction-cost` | İşlem maliyeti | S3-L4 | S4-L4 |

### Set 4 — Türkiye Gerçekleri (9 kavram)

| Anahtar | Etiket | Tanıtım | Pekiştirme |
|---|---|---|---|
| `bes` | Bireysel Emeklilik Sistemi | S4-L1 | — |
| `state-contribution` | Devlet katkısı | S4-L1 | — |
| `vesting` | Hak kazanma (vesting) | S4-L1 | — |
| `gold-forms` | Altın türleri (gram/çeyrek/ayar) | S4-L2 | — |
| `making-charge` | İşçilik payı / milyem | S4-L2 | — |
| `deposit-insurance` | Mevduat sigortası | S4-L3 | — |
| `saving-in-inflation` | Enflasyon ortamında birikim | S4-L3 | — |
| `fund-expense-ratio` | Fon gider oranı | S4-L4 | — |
| `tefas` | TEFAS | S4-L4 | — |

---

## 4. Ders künyesi şablonu

§5'teki her künye şu alanları taşır. **Boş bırakılan alan yoktur** — yoksa "—"
yazılır.

| Alan | Kural |
|---|---|
| **Kod / slug** | `S<set>-L<sıra>` · slug Türkçe-kebab (`yatirim-nedir`) |
| **Seviye · süre** | `LessonLevel` + tahmini dakika |
| **Ön bilgi** | **Yalnız daha önceki derslerde tanıtılmış kavramlar** (§9-K2) |
| **Tanıtılan / pekiştirilen** | §3 haritasıyla birebir tutarlı |
| **Öğrenme çıktıları** | 3-5 madde, §2.1 fiil kuralı |
| **Aşama planı** | Sıra · `Katman/Tür` · konu · 🖼 figür anahtarı |
| **İşlenmiş örnekler** | Adımları görünen örnek(ler) |
| **Tuzaklar** | Yaygın yanılgılar → testin çeldiricileri |
| **Figür planı** | Anahtar → tür (tek sahne · **çok panelli** · etkileşimli) |
| **LiveContext** | `ContextKey` listesi ya da "—" |
| **Değerlendirme** | 9 soru: 3 Easy / 3 Medium / 3 Hard, her biri bir çıktıyı ölçer |
| **Kaynaklar** | `Source` bloğunun içeriği (§6) |
| **⚠ Yasal not** | `CLAUDE.md` §2 sınırına yakınlık ve alınan önlem |

> **Aşama sayısı** serbesttir (hedef 13-15). **Zorunlu olan** §2.5 iskeletidir:
> açılış bloğu ve `Source` bloğu her derste vardır; `LiveContext` yalnız dersin
> karşılığı bir metrik varsa konur.

---

## 5. Ders künyeleri

### Set 0 — İlk Adımlar (8 ders · sıfır bilgi)

Track: `ilk-adimlar` · `OrderIndex = 0` · hepsi `Beginner`.
Bu set **Set 1'in ön koşulu değildir** (`15` §6.3) — yönlendirme "Buradan başla"
rozetiyle yapılır.

---

#### S0-L1 · Yatırım nedir, ne değildir?

| | |
|---|---|
| **Slug** | `yatirim-nedir` |
| **Seviye · süre** | Başlangıç · ~6 dk |
| **Ön bilgi** | **— (yok)** ← sıfır bilgi kapısı, boş kalmalı |
| **Tanıtılan** | `investment` · `saving-vs-investing` · `speculation` · `risk-premium` *(Deep)* |
| **Pekiştirilen** | — |
| **LiveContext** | `holding_count` |

**Öğrenme çıktıları** — ders sonunda okuyucu:
1. Saklamak, biriktirmek ve yatırmak eylemlerini **ayırt eder**.
2. Bir eylemin yatırım sayılması için gereken iki unsuru (sermaye bir kullanıma
   verilir + getiri belirsizdir) örnek üzerinde **gösterir**.
3. Yatırımı şans oyunundan ayıran farkı (kazancın kaynağı: **üretilen değer** mi,
   **sıfır toplamlı bahis** mi) **açıklar**.
4. *(Deep)* Getirinin neden var olduğunu risk primi çerçevesinde **açıklar**.

**Aşama planı (13)**
| # | Katman/Tür | Konu | Figür |
|---|---|---|---|
| 1 | Core/Explain | **Bu derste ne öğreneceksin?** | — |
| 2 | Core/Explain | Üç eylem: saklamak · biriktirmek · yatırmak | 🖼 `three-actions` **(çok panelli)** |
| 3 | Core/Example | 10.000 ₺'nin üç yolu — 1 yıl sonra ne değişti | 🖼 `ten-thousand-three-paths` **(çok panelli)** |
| 4 | Core/Explain | Yatırımın iki unsuru | 🖼 `capital-to-use` |
| 5 | Core/Example | "Parayı çalıştırmak" ne demek — paran bir fırının fırınına gider | 🖼 `money-at-work` **(çok panelli)** |
| 6 | Core/Trap | "Yatırım = kazanç garantisi" yanılgısı | 🖼 `no-guarantee` |
| 7 | Context/Explain | Yatırım · spekülasyon · şans oyunu: kazanç nereden geliyor | 🖼 `value-vs-zero-sum` **(çok panelli)** |
| 8 | Context/Example | Aynı varlık, iki davranış: 10 yıl tutmak vs 2 gün tutmak | 🖼 `hold-vs-flip` |
| 9 | Context/Trap | "Kısa vadede çok kazanan = iyi yatırımcı" (sonuç ≠ süreç) | — |
| 10 | Deep/Explain | Getiri neden var: fırsat maliyeti + risk primi | 🖼 `risk-premium-intro` |
| 11 | Deep/Example | Aynı işe iki finansman: ortak olmak vs borç vermek → S0-L5 köprüsü | — |
| 12 | Core/LiveContext | "Senin portföyünde" — kaç kalem var / demo | — |
| 13 | Core/Source | Bu bilgiler nereden geliyor? | — |

**İşlenmiş örnekler:** (a) 10.000 ₺ · kasa / vadeli hesap / şirket ortaklığı —
her yolda bir yıl sonra elde ne var, hangisinde belirsizlik vardı;
(b) fırın örneği: paranın hangi somut kullanıma dönüştüğü adım adım.

**Tuzaklar:** "Yatırım kazanç garantisidir" · "Kısa vadede kazanan iyi
yatırımcıdır" · "Yastık altı da bir yatırımdır".

**Figür planı (7):** `three-actions`✳ · `ten-thousand-three-paths`✳ ·
`capital-to-use` · `money-at-work`✳ · `no-guarantee` · `value-vs-zero-sum`✳ ·
`hold-vs-flip` · `risk-premium-intro` — ✳ = çok panelli.

**Değerlendirme (9):**
| Zorluk | Ölçtüğü çıktı |
|---|---|
| Easy ×3 | Ç1 (üç eylemi ayırt etme) · tanım hatırlama |
| Medium ×3 | Ç2 (verilen senaryo yatırım mı) · Ç3 (kazancın kaynağı) |
| Hard ×3 | Ç3 ayırt etme (spekülasyon vs yatırım, aynı varlıkta) · Ç4 |

**Kaynaklar (`Source`):** SPK — yatırımcı bilgilendirme yayınları (yatırım ve
sermaye piyasası aracı tanımları) ⟨belge + tarih doğrulanacak⟩ · TCMB terimler
sözlüğü ⟨doğrulanacak⟩.

**⚠ Yasal not:** Ders hiçbir varlık sınıfını önermez; "şirket ortaklığı"
örneği **soyut** kalır (şirket adı, sembol yok). Spekülasyon anlatımı kişileri
değil **davranışı** tarif eder.

---

#### S0-L2 · Paranın haritası — gelir, gider, birikim

| | |
|---|---|
| **Slug** | `paranin-haritasi` |
| **Seviye · süre** | Başlangıç · ~6 dk |
| **Ön bilgi** | `saving-vs-investing` (S0-L1) |
| **Tanıtılan** | `income-expense` · `savings-rate` · `pay-yourself-first` |
| **Pekiştirilen** | `saving-vs-investing` |
| **LiveContext** | `cash_weight` |

**Öğrenme çıktıları:**
1. Geliri, zorunlu gideri, isteğe bağlı gideri ve birikimi **ayırt eder**.
2. Birikim oranını (birikim ÷ gelir) **hesaplar**.
3. Yatırılacak paranın nereden çıktığını kendi cümlesiyle **açıklar**.
4. *(Deep)* Erken dönemde birikim oranının getiri oranından neden daha güçlü
   bir kaldıraç olduğunu **karşılaştırır**.

**Aşama planı (13)**
| # | Katman/Tür | Konu | Figür |
|---|---|---|---|
| 1 | Core/Explain | **Bu derste ne öğreneceksin?** | — |
| 2 | Core/Explain | Para nereye gidiyor: üç kova | 🖼 `three-buckets` **(çok panelli)** |
| 3 | Core/Example | Bir aylık dağılım — kalan ne kadar | 🖼 `monthly-split` |
| 4 | Core/Explain | Birikim oranı = birikim ÷ gelir | 🖼 `savings-rate-bar` |
| 5 | Core/Example | Aynı gelir, iki oran → 12 ayda fark | 🖼 `two-savers` **(çok panelli)** |
| 6 | Core/Trap | "Önce harca, kalanı biriktir" → kalan kalmaz | 🖼 `leftover-trap` |
| 7 | Context/Explain | Sırayı ters çevirmek: önce ayır | 🖼 `pay-yourself-first` |
| 8 | Context/Example | Düzensiz gelir (serbest çalışan): oran yerine aralık | — |
| 9 | Context/Trap | "Zam alınca birikim kendiliğinden artar" (yaşam tarzı enflasyonu) | 🖼 `lifestyle-creep` |
| 10 | Deep/Explain | Oran mı, getiri mi: erken dönemin kaldıracı | 🖼 `rate-vs-return-lever` **(çok panelli)** |
| 11 | Deep/Example | 10 yılda: oranı ikiye katlamak vs getiriyi ikiye katlamak | — |
| 12 | Core/LiveContext | Portföyünün ne kadarı nakitte | — |
| 13 | Core/Source | Bu bilgiler nereden geliyor? | — |

**İşlenmiş örnekler:** (a) aylık gelir → üç kovaya bölünüş → birikim oranı
aritmetiği; (b) iki kişi 12 ay boyunca, biriken tutar farkı adım adım.

**Tuzaklar:** "Kalanı biriktiririm" · "Zamla birikim otomatik artar" ·
"Birikim oranı düşükse yatırım anlamsızdır".

**Figür planı (7):** `three-buckets`✳ · `monthly-split` · `savings-rate-bar` ·
`two-savers`✳ · `leftover-trap` · `pay-yourself-first` · `lifestyle-creep` ·
`rate-vs-return-lever`✳.

**Değerlendirme (9):** Easy — kova ayrımı, oran tanımı · Medium — verilen
rakamlarla oran hesabı, tuzak tanıma · Hard — iki senaryonun 10 yıllık
karşılaştırması, düzensiz gelirde oranın yorumu.

**Kaynaklar:** — *(evrensel kavram; kurumsal veri içermez)* `Source` bloğu bu
derste **yöntem kaynağı** verir: kullanılan tüm rakamların **kurgusal ve etiketli**
olduğu açıkça yazılır (§6.3).

**⚠ Yasal not:** ⚠️ **Kişisel finans sınırı.** Hedef birikim oranı
**dayatılmaz** ("gelirinin %20'sini biriktir" ❌). Ders yalnız *nasıl hesaplanır*
ve *hangi çerçeveler var* der. Karar okuyucunundur.

---

#### S0-L3 · Acil durum fonu ve borç

| | |
|---|---|
| **Slug** | `acil-durum-fonu-ve-borc` |
| **Seviye · süre** | Başlangıç · ~7 dk |
| **Ön bilgi** | `income-expense`, `savings-rate` (S0-L2) |
| **Tanıtılan** | `emergency-fund` · `debt-cost` · `opportunity-cost` |
| **Pekiştirilen** | `saving-vs-investing` · `savings-rate` |
| **LiveContext** | `cash_weight` |

**Öğrenme çıktıları:**
1. Acil durum fonunun işlevini (varlık satmadan şoku karşılamak) **açıklar**.
2. Aylık orandan yıllık maliyete geçerek borcun maliyetini **hesaplar**.
3. Bir lirayı borca mı yatırıma mı vermenin *fırsat maliyeti* çerçevesini
   **kurar** — karar vermeden.
4. *(Deep)* Kesin maliyet ile belirsiz getirinin neden doğrudan
   karşılaştırılamadığını **gösterir**.

**Aşama planı (13)**
| # | Katman/Tür | Konu | Figür |
|---|---|---|---|
| 1 | Core/Explain | **Bu derste ne öğreneceksin?** | — |
| 2 | Core/Explain | Şok nedir: beklenmedik, ertelenemez gider | 🖼 `shock-event` |
| 3 | Core/Example | Tamponu olan ve olmayan iki kişi, aynı şok | 🖼 `with-without-buffer` **(çok panelli)** |
| 4 | Core/Explain | Tamponun üç özelliği: erişilebilir · oynamayan · ayrı | 🖼 `buffer-traits` |
| 5 | Core/Trap | "Acil durum fonu da getiri getirsin" → likidite feda edilir | 🖼 `liquidity-traded-away` |
| 6 | Core/Explain | Borcun maliyeti: aylık oran ≠ yıllık maliyet | 🖼 `monthly-to-yearly` |
| 7 | Core/Example | İşlenmiş örnek: aylık orandan yıllık bileşik maliyete (aritmetik) | 🖼 `debt-cost-ladder` |
| 8 | Context/Explain | Fırsat maliyeti: bir lira iki işi aynı anda yapamaz | 🖼 `one-lira-two-jobs` **(çok panelli)** |
| 9 | Context/Example | Karşılaştırma **çerçevesi** — kesin maliyet ↔ belirsiz getiri | — |
| 10 | Context/Trap | "Getiri faizden yüksekse borç iyidir" → belirsizlik unutulur | 🖼 `certain-vs-uncertain` **(çok panelli)** |
| 11 | Deep/Explain | Nakit tutmanın iki maliyeti: erime **ve** likidite değeri | 🖼 `two-costs-of-cash` |
| 12 | Core/LiveContext | Portföyünün nakit ağırlığı | — |
| 13 | Core/Source | Bu bilgiler nereden geliyor? | — |

**İşlenmiş örnekler:** (a) aynı şok, iki sonuç — tamponu olmayan kişi varlığını
**zamanlamadan bağımsız** satmak zorunda kalır; (b) aylık %X → yıllık bileşik
maliyet dönüşümü, adım adım.

**Tuzaklar:** "Fon da kazandırsın" · "Faizden yüksek getiri varsa borç iyidir"
(kesin ↔ belirsiz asimetrisi) · "Nakit tutmak hiçbir şey kaybettirmez".

**Figür planı (7):** `shock-event` · `with-without-buffer`✳ · `buffer-traits` ·
`liquidity-traded-away` · `monthly-to-yearly` · `debt-cost-ladder` ·
`one-lira-two-jobs`✳ · `certain-vs-uncertain`✳ · `two-costs-of-cash`.

**Değerlendirme (9):** Easy — tamponun işlevi, üç özelliği · Medium — aylık→yıllık
maliyet hesabı, fırsat maliyeti tanıma · Hard — kesin/belirsiz asimetrisini
içeren senaryo, "fon getiri getirsin" tuzağının sonucu.

**Kaynaklar:** Tüketici kredisi/kredi kartı maliyetinin yıllık orana çevrilmesine
dair **resmî tanım** — TCMB / BDDK ⟨belge + tarih doğrulanacak⟩.

**⚠ Yasal not:** 🔴 **Set 0'ın en hassas dersi.** "Borcunu kapat", "şu kadar
aylık gider kadar fon kur" **denmez** — bunlar kişisel finansal yönlendirmedir.
Ders yalnızca (a) maliyetin nasıl hesaplandığını, (b) kesin maliyet ile belirsiz
getirinin **aynı türden büyüklükler olmadığını** olgu olarak gösterir. Somut
oran/limit verilmez; verilen her kurumsal rakam kaynaklı ve tarihlidir.

---

#### S0-L4 · Bekleyen para neden erir?

| | |
|---|---|
| **Slug** | `bekleyen-para-neden-erir` |
| **Seviye · süre** | Başlangıç · ~7 dk |
| **Ön bilgi** | `saving-vs-investing` (S0-L1) |
| **Tanıtılan** | `inflation` · `purchasing-power` · `price-index` |
| **Pekiştirilen** | `saving-vs-investing` |
| **LiveContext** | `inflation_12m` |

> **Spiral notu:** Bu ders enflasyonu **formülsüz** verir. Formül
> (`reel = (1+n)/(1+e) − 1`) **S1-L1'e aittir** — burada hesap yapılmaz,
> sezgi kurulur (§2.3).

**Öğrenme çıktıları:**
1. Enflasyonu "aynı sepetin fiyatının artması" olarak **tanımlar**.
2. Alım gücünü tutardan **ayırt eder**.
3. Fiyat endeksinin ne ölçtüğünü ve kişisel sepetten neden farklı olabileceğini
   **açıklar**.
4. Verilen oran ve süre için alım gücü erimesini **okur** *(etkileşimli araç)*.

**Aşama planı (13)**
| # | Katman/Tür | Konu | Figür |
|---|---|---|---|
| 1 | Core/Explain | **Bu derste ne öğreneceksin?** | — |
| 2 | Core/Explain | Aynı sepet, iki tarih | 🖼 `same-basket-two-dates` **(çok panelli)** |
| 3 | Core/Example | Sepet 1.000 → 1.400; elindeki 1.000 ne alır | 🖼 `basket-price-up` |
| 4 | Core/Explain | Tutar ≠ alım gücü | 🖼 `amount-vs-power` |
| 5 | Core/Trap | "Param aynı kaldı, kaybetmedim" | 🖼 `standing-still` |
| 6 | Core/Example | **Etkileşimli:** enflasyon kaydırıcısı (T6.18) | 🖼 `inflation-slider` *(fallback `erosion-curve`)* |
| 7 | Context/Explain | Fiyat endeksi nedir, sepet nasıl kurulur | 🖼 `index-basket` |
| 8 | Context/Example | Kişisel sepet farkı: kirada oturan ↔ ev sahibi | 🖼 `personal-basket` |
| 9 | Context/Trap | "Resmî oran = benim enflasyonum" | — |
| 10 | Deep/Explain | Bileşik erime: üst üste yıllar | 🖼 `compounded-erosion` **(çok panelli)** |
| 11 | Deep/Example | Alım gücünün yarıya inmesi kaç yıl sürer (aritmetik) | — |
| 12 | Core/LiveContext | Son 12 ayın TÜFE'si | — |
| 13 | Core/Source | Bu bilgiler nereden geliyor? | — |

**İşlenmiş örnekler:** (a) sepet fiyatı artışı → aynı parayla alınabilen miktar;
(b) yıllık erimenin üst üste binmesi (yıl yıl tablo, aritmetik görünür).

**Tuzaklar:** "Rakam aynıysa kayıp yok" · "Resmî oran benim enflasyonum" ·
"Enflasyon her kalemi eşit etkiler".

**Figür planı (7 + 1 etkileşimli):** `same-basket-two-dates`✳ · `basket-price-up` ·
`amount-vs-power` · `standing-still` · **`inflation-slider` (etkileşimli)** ·
`erosion-curve` *(fallback)* · `index-basket` · `personal-basket` ·
`compounded-erosion`✳.

**Değerlendirme (9):** Easy — enflasyon tanımı, alım gücü ayrımı · Medium —
verilen sepet fiyatıyla alım gücü okuma, endeksin ne ölçtüğü · Hard — kişisel
sepet sapması, bileşik erimenin doğrusal olmadığını görme.

**Kaynaklar:** **TÜİK — Tüketici Fiyat Endeksi (TÜFE)**: endeksin tanımı, sepet
kapsamı ve yayın takvimi ⟨belge + tarih doğrulanacak⟩. Canlı `inflation_12m`
değerinin **kaynağı ve tarihi** blokta görünür (§6.4).

**⚠ Yasal not:** Enflasyon **geçmiş/gerçekleşmiş** veriyle anlatılır; gelecek
oran tahmini yapılmaz. Kaydırıcı "şu oran **olursa**" der, "olacak" demez
(§6.5 · `15` §6.2).

---

#### S0-L5 · Nereye yatırılır? — varlık türleri turu

| | |
|---|---|
| **Slug** | `varlik-turleri-turu` |
| **Seviye · süre** | Başlangıç · ~8 dk |
| **Ön bilgi** | `investment` (S0-L1) |
| **Tanıtılan** | `asset-class` · `ownership-vs-lending` · `liquidity` |
| **Pekiştirilen** | `investment` |
| **LiveContext** | `asset_class_weights` |

**Öğrenme çıktıları:**
1. Varlık sınıfını **tanımlar** ve yaygın sınıfları **örnekler**.
2. Bir varlıkta **ortak mı alacaklı mı** olunduğunu **ayırt eder**.
3. Likiditeyi **tanımlar** ve varlıkları bu eksende **sıralar**.
4. Aynı paranın farklı sınıflarda **neyin parçasına** dönüştüğünü **açıklar**.

**Aşama planı (14)**
| # | Katman/Tür | Konu | Figür |
|---|---|---|---|
| 1 | Core/Explain | **Bu derste ne öğreneceksin?** | — |
| 2 | Core/Explain | Varlık sınıfı ne demek | 🖼 `asset-class-map` |
| 3 | Core/Explain | Mevduat: bankaya borç veriyorsun | 🖼 `deposit-lending` |
| 4 | Core/Explain | Hisse: şirkete ortak oluyorsun | 🖼 `equity-ownership` |
| 5 | Core/Example | Aynı 10.000 ₺ üç sınıfta neyin parçası oldu | 🖼 `same-money-three-forms` **(çok panelli)** |
| 6 | Core/Explain | Altın ve döviz: değer saklama aracı | 🖼 `store-of-value` |
| 7 | Core/Explain | Fon ve BES: paketlenmiş sepetler | 🖼 `fund-wrapper` **(çok panelli)** |
| 8 | Core/Trap | "Fon aldım = tek bir şey aldım" (fon bir sepettir) | — |
| 9 | Context/Explain | Ortaklık ↔ alacaklılık ekseni | 🖼 `ownership-lending-axis` **(çok panelli)** |
| 10 | Context/Explain | Likidite ekseni | 🖼 `liquidity-axis` |
| 11 | Context/Example | Aynı varlık, iki likidite: hızlı satış ↔ makas | 🖼 `bid-ask-spread` |
| 12 | Context/Trap | "Her varlık her an satılır" | — |
| 13 | Deep/Explain | Sınıf içi tür farkı (S4-L2 altın köprüsü) | — |
| 14 | Core/LiveContext + Source | Sınıf dağılımın · kaynaklar | — |

> Not: 14. satır **iki ayrı bloktur** (`LiveContext` + `Source`); tabloda yer
> kazanmak için birleştirilmiştir.

**İşlenmiş örnekler:** (a) 10.000 ₺'nin üç sınıftaki somut karşılığı — kimin
kullanımına geçti, karşılığında ne alındı; (b) alış-satış makasının aynı anda
alıp satınca ne kaybettirdiği (aritmetik).

**Tuzaklar:** "Fon tek bir varlıktır" · "Her varlık istenildiği an nakde döner"
· "Altın ve döviz de faiz getirir".

**Figür planı (8):** `asset-class-map` · `deposit-lending` · `equity-ownership` ·
`same-money-three-forms`✳ · `store-of-value` · `fund-wrapper`✳ ·
`ownership-lending-axis`✳ · `liquidity-axis` · `bid-ask-spread`.

**Değerlendirme (9):** Easy — sınıf tanıma, ortak/alacaklı ayrımı · Medium —
verilen varlıkta hangi eksende olunduğu, likidite sıralaması · Hard — fon =
sepet çıkarımı, makasın maliyeti.

**Kaynaklar:** SPK — sermaye piyasası araçlarının tanımları ⟨doğrulanacak⟩ ·
TEFAS — yatırım fonu tanım ve kategorileri ⟨doğrulanacak⟩ · TMSF — mevduat
sigortası kapsamı *(yalnız "alacaklılık" bağlamında değinilir; limit **S4-L3'te**)*
⟨doğrulanacak⟩.

**⚠ Yasal not:** Sınıflar **sıralanmaz, karşılaştırılmaz** — "hangisi daha iyi"
sorusu dersin dışındadır (`15` §3.4 enstrüman sıralaması yasağı). Likidite ekseni
bir **kalite** sıralaması değildir; metinde bu açıkça yazılır.

---

#### S0-L6 · Getiri nereden gelir?

| | |
|---|---|
| **Slug** | `getiri-nereden-gelir` |
| **Seviye · süre** | Başlangıç · ~7 dk |
| **Ön bilgi** | `asset-class`, `ownership-vs-lending` (S0-L5) |
| **Tanıtılan** | `return-source` · `capital-gain` · `cash-flow-return` |
| **Pekiştirilen** | `investment` · `ownership-vs-lending` · `opportunity-cost` |
| **LiveContext** | `price_change_12m` |

**Öğrenme çıktıları:**
1. Getirinin iki kaynağını (değer artışı · nakit akışı) **ayırt eder**.
2. Bir varlığın getirisinin hangi kaynaktan geldiğini örnekte **gösterir**.
3. Toplam getiriyi iki bileşenin toplamı olarak **hesaplar**.
4. *(Deep)* "Bedava getiri" iddiasının neden eksik bir cümle olduğunu
   (karşılığında ne verildiği) **açıklar**.

**Aşama planı (13)**
| # | Katman/Tür | Konu | Figür |
|---|---|---|---|
| 1 | Core/Explain | **Bu derste ne öğreneceksin?** | — |
| 2 | Core/Explain | İki kaynak: değer artışı + nakit akışı | 🖼 `two-return-sources` **(çok panelli)** |
| 3 | Core/Example | Kira örneği: değer artışı ve kira geliri ayrı ayrı | 🖼 `rent-plus-value` |
| 4 | Core/Explain | Değer artışı: fiyat değişimi | 🖼 `capital-gain-line` |
| 5 | Core/Explain | Nakit akışı: faiz · temettü · kira | 🖼 `cash-flow-drip` |
| 6 | Core/Example | Toplam getiri = %A + %B (aritmetik) | 🖼 `total-return-sum` **(çok panelli)** |
| 7 | Core/Trap | "Temettü bedava para" — fiyattan düşer | 🖼 `dividend-not-free` **(çok panelli)** |
| 8 | Context/Explain | Getirinin karşılığı: sermayeni kim kullanıyor, hangi riski alıyorsun | 🖼 `who-uses-your-capital` |
| 9 | Context/Example | Aynı şirkete ortak olmak ↔ borç vermek: getirinin şekli değişir | 🖼 `equity-vs-debt-return` **(çok panelli)** |
| 10 | Context/Trap | "Yüksek temettü her zaman iyi" — kaynağı kâr mı | — |
| 11 | Deep/Explain | Kâğıt üstünde kâr ↔ nakde dönen kâr (S2-L2 köprüsü) | 🖼 `paper-vs-realized` |
| 12 | Core/LiveContext | Portföyündeki 12 aylık fiyat değişimi | — |
| 13 | Core/Source | Bu bilgiler nereden geliyor? | — |

**İşlenmiş örnekler:** (a) bir kalemde değer artışı %A, nakit akışı %B → toplam
getiri; (b) temettü dağıtımının fiyata etkisi — dağıtım öncesi/sonrası aynı
toplam değer.

**Tuzaklar:** "Temettü bedava" · "Yüksek temettü her zaman iyi" · "Fiyat
değişmediyse getiri yoktur".

**Figür planı (8):** `two-return-sources`✳ · `rent-plus-value` ·
`capital-gain-line` · `cash-flow-drip` · `total-return-sum`✳ ·
`dividend-not-free`✳ · `who-uses-your-capital` · `equity-vs-debt-return`✳ ·
`paper-vs-realized`.

**Değerlendirme (9):** Easy — iki kaynağı tanıma · Medium — toplam getiri
hesabı, verilen varlıkta kaynağı belirleme · Hard — temettü tuzağı,
kâğıt üstü/gerçekleşmiş ayrımı.

**Kaynaklar:** KAP — temettü (kâr payı) dağıtım bildirimleri ve dağıtımın fiyata
yansıması ⟨doğrulanacak⟩ · SPK — kâr payı mevzuatı tanımları ⟨doğrulanacak⟩.

**⚠ Yasal not:** Örneklerde **şirket adı ve sembol geçmez**; "A şirketi" gibi
soyut etiket kullanılır. Temettü anlatımı bir strateji önerisine dönüşmez.

---

#### S0-L7 · Risk ne demek?

| | |
|---|---|
| **Slug** | `risk-ne-demek` |
| **Seviye · süre** | Başlangıç · ~7 dk |
| **Ön bilgi** | `investment` (S0-L1) · `return-source` (S0-L6) |
| **Tanıtılan** | `risk` · `volatility` · `guaranteed-return-fallacy` |
| **Pekiştirilen** | `risk-premium` · `speculation` |
| **LiveContext** | — *(karşılığı bir metrik yok)* |

**Öğrenme çıktıları:**
1. Riski **sonucun belirsizliği** olarak **tanımlar** ve kayıptan **ayırt eder**.
2. Oynaklığı örnek üzerinde **okur**.
3. "Garantili yüksek getiri" ifadesinin neden bir çelişki olduğunu **açıklar**.
4. Bir getiri vaadi karşısında sorulacak üç soruyu **sıralar**.

**Aşama planı (13)**
| # | Katman/Tür | Konu | Figür |
|---|---|---|---|
| 1 | Core/Explain | **Bu derste ne öğreneceksin?** | — |
| 2 | Core/Explain | Risk = belirsizlik (kayıp değil) | 🖼 `uncertainty-fan` |
| 3 | Core/Example | İki yol, aynı ortalama, farklı dalgalanma | 🖼 `two-paths-same-average` **(çok panelli)** |
| 4 | Core/Explain | Oynaklık nedir | 🖼 `volatility-band` |
| 5 | Core/Trap | "Risk = kaybetmek" yanılgısı | — |
| 6 | Core/Explain | Getiri neden risksiz olmaz: risk primi | 🖼 `risk-premium-step` |
| 7 | Core/Example | "Yıllık %X garanti" iddiası — para nereden geliyor | 🖼 `where-money-comes-from` **(çok panelli)** |
| 8 | Core/Trap | **"Garantili yüksek getiri" çelişkisi** | 🖼 `guarantee-contradiction` **(çok panelli)** |
| 9 | Context/Explain | Üç soru: kim ödüyor · kaynağı ne · kim güvence veriyor | 🖼 `three-questions` |
| 10 | Context/Example | Aynı vaadin iki hâli: kurumsal güvenceli ↔ güvencesiz | 🖼 `guaranteed-by-whom` |
| 11 | Context/Trap | "Herkes kazanıyor" — sürü ve kaçırma korkusu (S3-L2 köprüsü) | — |
| 12 | Deep/Explain | Risk kişiye göre değişmez; **taşınabilirliği** değişir (vade → S0-L8) | 🖼 `risk-carrying-capacity` |
| 13 | Core/Source | Bu bilgiler nereden geliyor? | — |

**İşlenmiş örnekler:** (a) iki yol aynı ortalamaya varır ama biri iki katı
dalgalanır — "ortalama aynı" ≠ "aynı deneyim"; (b) garanti vaadinin nakit
akışını takip etme: ödemeler kimin parasından yapılıyor.

**Tuzaklar:** "Risk = kayıp" · "Garantili yüksek getiri mümkün" · "Herkes
kazanıyorsa güvenlidir".

**Figür planı (8):** `uncertainty-fan` · `two-paths-same-average`✳ ·
`volatility-band` · `risk-premium-step` · `where-money-comes-from`✳ ·
`guarantee-contradiction`✳ · `three-questions` · `guaranteed-by-whom` ·
`risk-carrying-capacity`.

**Değerlendirme (9):** Easy — risk tanımı, kayıptan ayrımı · Medium — iki yolun
oynaklığını karşılaştırma, üç sorunun uygulanması · Hard — garanti çelişkisini
bir senaryoda teşhis, kurumsal güvence ↔ kişisel vaat ayrımı.

**Kaynaklar:** SPK — **yatırımcı uyarıları** ve yetkisiz/izinsiz faaliyetlere
ilişkin bilgilendirme ⟨belge + tarih doğrulanacak⟩ · TMSF — mevduat sigortasının
**neyi kapsadığı** (güvencenin kaynağı örneği) ⟨doğrulanacak⟩.

**⚠ Yasal not:** 🔴 Dolandırıcılık anlatımında **hiçbir kurum, kişi, platform
veya ürün adı geçmez**; yalnız **kalıp** tarif edilir. "Şuna güvenme" değil,
"şu soruları sor" formu kullanılır. Mevzuat terimleri (yetkisiz aracılık vb.)
yalnız SPK kaynağına bağlıysa yazılır.

---

#### S0-L8 · Vade, hedef ve portföy

| | |
|---|---|
| **Slug** | `vade-hedef-ve-portfoy` |
| **Seviye · süre** | Başlangıç · ~8 dk |
| **Ön bilgi** | `asset-class` (S0-L5) · `risk` (S0-L7) · `liquidity` (S0-L5) |
| **Tanıtılan** | `time-horizon` · `portfolio` · `position` · `weight` |
| **Pekiştirilen** | `emergency-fund` · `liquidity` · `risk` |
| **LiveContext** | `holding_count` · `asset_class_weights` · `concentration_top2` |

**Öğrenme çıktıları:**
1. Zaman ufkunu **tanımlar** ve bir hedefe vade **atar**.
2. Portföyü kalem · miktar · maliyet · değer · ağırlık alanlarıyla **okur**.
3. Bir portföyün ağırlıklarını **hesaplar**.
4. *(Deep)* Fiyat hareketinin ağırlıkları işlem yapılmadan değiştirdiğini
   **gösterir** (S1-L2 köprüsü).

**Aşama planı (14)**
| # | Katman/Tür | Konu | Figür |
|---|---|---|---|
| 1 | Core/Explain | **Bu derste ne öğreneceksin?** | — |
| 2 | Core/Explain | Vade: parayı ne zaman kullanacaksın | 🖼 `time-horizon-ruler` |
| 3 | Core/Example | Üç hedef, üç vade | 🖼 `three-goals` **(çok panelli)** |
| 4 | Core/Trap | "Uzun vade = beklemek" — vade ihtiyaçtan gelir, tercihten değil | — |
| 5 | Core/Explain | Portföy = kalemlerin toplamı | 🖼 `portfolio-anatomy` |
| 6 | Core/Explain | Bir kalemin beş alanı | 🖼 `position-fields` **(çok panelli)** |
| 7 | Core/Example | Üç kalemlik portföyün ağırlıkları (aritmetik) | 🖼 `weight-share` |
| 8 | Core/Trap | "Kalem sayısı çoksa çeşitlenmiştir" (S1-L2 köprüsü) | 🖼 `many-but-same` |
| 9 | Context/Explain | Nirengi ekranı: hangi sayı nerede | 🖼 `app-screen-map` **(çok panelli)** |
| 10 | Context/Example | Aynı portföy iki baz para biriminde (S2-L3 köprüsü) | 🖼 `two-base-currencies` |
| 11 | Context/Trap | "Toplam değer arttıysa her kalem kazandırdı" | — |
| 12 | Deep/Explain | Portföy statiktir sanılır: fiyat hareketi ağırlığı kaydırır | 🖼 `weights-move-by-themselves` **(çok panelli)** |
| 13 | Core/LiveContext | Kaç kalem · sınıf dağılımı · en büyük iki ağırlık | — |
| 14 | Core/Source | Bu bilgiler nereden geliyor? | — |

**İşlenmiş örnekler:** (a) üç kalemlik portföyde ağırlık hesabı (pay ÷ toplam,
yüzdeye çevirme); (b) fiyat hareketiyle ağırlığın işlem yapılmadan değişmesi.

**Tuzaklar:** "Uzun vade = beklemek" · "Kalem sayısı = çeşitlilik" · "Toplam
arttıysa her kalem kazandı".

**Figür planı (8):** `time-horizon-ruler` · `three-goals`✳ · `portfolio-anatomy` ·
`position-fields`✳ · `weight-share` · `many-but-same` · `app-screen-map`✳ ·
`two-base-currencies` · `weights-move-by-themselves`✳.

**Değerlendirme (9):** Easy — portföy/kalem/ağırlık tanımları · Medium — ağırlık
hesabı, vade atama · Hard — kalem sayısı ≠ çeşitlilik çıkarımı, ağırlık kayması.

**Kaynaklar:** Bu ders **uygulamanın kendi ekranını** anlattığı için kaynak
bloğu **hesap yöntemini** gösterir: ağırlık ve değer formülleri `CLAUDE.md` §6
(deterministik, kodda) → `Source` bloğu bunu ve "sayılar LLM tarafından
üretilmez" ilkesini yazar.

**⚠ Yasal not:** Hedef ve vade anlatımı **kişiye özel plan üretmez** — "senin
vaden şu olmalı" ❌. Ders yalnız *vade nasıl belirlenir* sorusunun çerçevesini
verir.

---

> **Set 0 iş yükü (gerçekçi tahmin):** **~62 yeni figür** (≈20'si çok panelli,
> 1'i etkileşimli) + **72 test sorusu** + 8 ders gövdesi.
> Bunu yönetilebilir kılmak için §8.3'teki **paylaşılan SVG öğeleri** (panel
> çerçevesi, sepet, madeni para, ok, kişi silueti, bar) önce yazılır; figürler
> bu öğelerden kurulur. Ders ders ilerlenir (T6.17a-h), her ders kendi turunda
> testleriyle birlikte kapanır.

---

### Set 1 — Yatırım Kavramları (5 ders · mevcut)

Track: `temeller` → **başlığı "Yatırım Kavramları" olur** (T6.16); slug korunur
(kayıtlı ilerleme ve bağlantılar kırılmasın).

> **Biçim notu:** Set 0 künyeleri aşama planını tabloyla verir; Set 1-4
> künyeleri aynı bilgiyi **numaralı liste** biçiminde taşır (`Katman/Tür —
> konu 🖼 figür`). İçerik alanları aynıdır.

---

#### S1-L1 · Enflasyon ve Reel Getiri — *mevcut (T6.11a)*

`enflasyon-ve-reel-getiri` · Başlangıç · ~7 dk · **13 aşama · 5 figür**
**Ön bilgi:** `inflation` · `purchasing-power` · `price-index` (S0-L4)
**Tanıtılan:** `nominal-return` · `real-return` — **Pekiştirilen:** `inflation` ·
`purchasing-power` · `price-index` — **LiveContext:** `real_return_12m` ·
`inflation_12m`

**Öğrenme çıktıları:**
1. Nominal ve reel getiriyi **ayırt eder**.
2. Reel getiriyi `(1+n)/(1+e) − 1` ile **hesaplar**.
3. Basit çıkarmanın neden yaklaşık olduğunu ve hatanın nerede büyüdüğünü **gösterir**.
4. Kişisel sepetin resmî endeksten farkını **açıklar**.
5. *(Deep)* Dönem seçiminin ölçülen getiriyi nasıl değiştirdiğini **analiz eder**.

**Mevcut figürler (5):** `real-vs-nominal` · `purchasing-power` ·
`subtraction-error` · `basket-difference` · `window-selection`.

**Eksik / yapılacak (T6.19):** açılış bloğu ("Bu derste ne öğreneceksin?") ·
`Source` bloğu · figür hedefi ≥4 **sağlanıyor**.

**Kaynaklar:** TÜİK — TÜFE (endeks tanımı + kullanılan dönem) ⟨doğrulanacak⟩.

**⚠ Yasal not:** Reel getiri **gerçekleşmiş** dönem için hesaplanır; gelecek
enflasyon varsayımı yapılmaz.

---

#### S1-L2 · Çeşitlendirme Neden Önemli? — *mevcut (T6.11b)*

`cesitlendirme-neden-onemli` · Başlangıç · ~8 dk · **13 aşama · 4 figür**
**Ön bilgi:** `portfolio` · `weight` (S0-L8) · `asset-class` (S0-L5) · `risk` (S0-L7)
**Tanıtılan:** `diversification` · `correlation` · `concentration-drift`
**Pekiştirilen:** `risk` · `asset-class` · `weight` — **LiveContext:**
`concentration_top2` · `holding_count`

**Öğrenme çıktıları:**
1. Çeşitlendirmeyi "riski farklı kaynaklara **yayma**" olarak **tanımlar**.
2. Korelasyonu +1 / 0 / −1 sezgisiyle **okur**.
3. Aynı sınıfta çok kalem tutmanın neden çeşitlendirme olmadığını **gösterir**.
4. Para birimi / coğrafya eksenini TR bağlamında **uygular** ("dört kalem, tek eksen").
5. *(Deep)* Ağırlık kaymasını — işlem yapmadan yoğunlaşmayı — **analiz eder**.

**Mevcut figürler (4):** `concentration` · `same-sector` · `correlation-paths` ·
`concentration-drift`.

**Eksik / yapılacak (T6.19):** açılış bloğu · `Source` bloğu.

**Kaynaklar:** Korelasyon ve çeşitlendirme **evrensel kavramlar** → `Source`
bloğu yöntem kaynağı verir (kullanılan tüm rakamlar kurgusal ve etiketli, §6.3).

**⚠ Yasal not:** "Şu kadar varlığa dağıt" **denmez**; ders yalnız ağırlığın
nerede toplandığını görmeyi öğretir (mevcut içerikte bu sınır korunuyor).

---

#### S1-L3 · F/K, PD/DD Nedir? — *içerik turu bekliyor (T6.11c)*

`fk-pddd-nedir` · Başlangıç · ~8 dk · **6 → 13 aşama** · **1 → ≥6 figür**
**Ön bilgi:** `ownership-vs-lending` · `return-source` · `cash-flow-return` (S0-L5/L6)
**Tanıtılan:** `pe-ratio` · `pb-ratio` · `dividend-yield` · `peer-comparison`
**Pekiştirilen:** `ownership-vs-lending` · `cash-flow-return` — **LiveContext:** —

**Öğrenme çıktıları:**
1. F/K'yı "1 lira kâr için ödenen fiyat" olarak **yorumlar**.
2. PD/DD'yi defter değeriyle ilişkilendirerek **okur**.
3. Temettü verimini **hesaplar**.
4. Oranların yalnız **emsal içinde** anlamlı olduğunu **gösterir**.
5. *(Deep)* Paydanın (kâr, defter değeri) oynamasının oranı nasıl bozduğunu **analiz eder**.

**Aşama planı (13):**
1. `Core/Explain` — **Bu derste ne öğreneceksin?**
2. `Core/Explain` — Fiyat tek başına bir şey söylemez 🖼 `price-alone-says-little`
3. `Core/Explain` — F/K: 1 lira kâr kaç liraya 🖼 `pe-price-per-lira` **(çok panelli)**
4. `Core/Example` — İşlenmiş örnek: fiyat ve hisse başı kârdan F/K
5. `Core/Trap` — "F/K düşük = ucuz" yanılgısı 🖼 `low-pe-not-cheap`
6. `Core/Explain` — PD/DD: defter değeri nedir 🖼 `book-value-scale`
7. `Core/Example` — Aynı PD/DD, iki farklı iş modeli 🖼 `same-ratio-two-firms` **(çok panelli)**
8. `Core/Explain` — Temettü verimi 🖼 `dividend-yield-slice`
9. `Core/Trap` — "Yüksek verim her zaman iyi" (fiyat düştüğü için de yükselir)
10. `Context/Explain` — Emsal karşılaştırma: oran bir **bant** içinde okunur 🖼 `ratio-context` *(mevcut)*
11. `Context/Example` — Sektör bantları farklıdır 🖼 `sector-bands`
12. `Deep/Explain` — Payda oynarsa: tek seferlik kâr/zarar oranı bozar 🖼 `earnings-jump-distorts` **(çok panelli)**
13. `Core/Source` — Bu bilgiler nereden geliyor?

**Tuzaklar:** "Düşük F/K = ucuz" · "Yüksek temettü verimi = iyi" · "Oranlar
sektörler arası karşılaştırılır".

**Figürler (7):** `price-alone-says-little` · `pe-price-per-lira`✳ ·
`low-pe-not-cheap` · `book-value-scale` · `same-ratio-two-firms`✳ ·
`dividend-yield-slice` · `ratio-context` *(mevcut)* · `sector-bands` ·
`earnings-jump-distorts`✳.

**Değerlendirme (9):** Easy — oran tanımları · Medium — verilen fiyat/kârdan
hesap, verim hesabı · Hard — düşük F/K tuzağı, payda bozulması, sektör bandı.

**Kaynaklar:** KAP — finansal tablo kalemleri (hisse başına kâr, öz kaynak)
⟨doğrulanacak⟩ · SPK — finansal raporlama tebliği tanımları ⟨doğrulanacak⟩.

**⚠ Yasal not:** 🔴 **Hisse dersi — sınıra en yakın Set 1 dersi.** Şirket adı,
sembol ve gerçek oran **geçmez**; tüm örnekler "A şirketi" soyutluğunda kalır.
Hiçbir oran için "şu değerin altı ucuzdur" eşiği verilmez.

---

#### S1-L4 · Risk ve Getiri İlişkisi — *içerik turu bekliyor (T6.11c)*

`risk-ve-getiri-iliskisi` · Başlangıç · ~7 dk · **6 → 13 aşama** · **1 → ≥6 figür**
**Ön bilgi:** `risk` · `volatility` · `risk-premium` (S0-L7) · `return-source` (S0-L6)
**Tanıtılan:** `risk-return` — **Pekiştirilen:** `risk` · `volatility` ·
`risk-premium` — **LiveContext:** —

**Öğrenme çıktıları:**
1. Risk ile beklenen getirinin neden birlikte hareket ettiğini **açıklar**.
2. İki yolun oynaklığını **karşılaştırır**.
3. Ortalama getirinin **yaşanan deneyimi** anlatmadığını **gösterir**.
4. *(Deep)* Kayıp sonrası toparlanma matematiğini **hesaplar** (−%50 → +%100).
5. *(Deep)* Getiri sırasının (sequence) sonucu nasıl değiştirdiğini **analiz eder**.

**Aşama planı (13):**
1. `Core/Explain` — **Bu derste ne öğreneceksin?**
2. `Core/Explain` — Neden bedava yüksek getiri yok 🖼 `risk-return-ladder`
3. `Core/Example` — Aynı ortalama, iki farklı yol 🖼 `volatility-paths` *(mevcut)*
4. `Core/Explain` — Oynaklık nasıl okunur 🖼 `band-width`
5. `Core/Trap` — "Ortalama getiri = benim getirim" 🖼 `average-vs-experience` **(çok panelli)**
6. `Core/Example` — İşlenmiş örnek: iki yılın ortalaması ile gerçek sonuç farkı
7. `Core/Trap` — "Geçmiş getiri geleceği gösterir" *(tahmin yasağı ile birebir)*
8. `Context/Explain` — Düşüşün derinliği: geri dönüş matematiği 🖼 `recovery-math` **(çok panelli)**
9. `Context/Example` — −%50 sonrası başa dönmek için gereken artış (aritmetik)
10. `Context/Trap` — "Düşen her şey geri gelir"
11. `Deep/Explain` — Sıra önemlidir: aynı getiriler farklı sırayla 🖼 `sequence-matters` **(çok panelli)**
12. `Deep/Example` — Aynı yıllık getiriler, iki farklı sıra → farklı deneyim
13. `Core/Source` — Bu bilgiler nereden geliyor?

**Figürler (6):** `risk-return-ladder` · `volatility-paths` *(mevcut)* ·
`band-width` · `average-vs-experience`✳ · `recovery-math`✳ · `sequence-matters`✳.

**Değerlendirme (9):** Easy — risk-getiri birlikteliği · Medium — oynaklık
karşılaştırma, toparlanma hesabı · Hard — ortalama/deneyim ayrımı, sıra etkisi.

**Kaynaklar:** Evrensel kavram → yöntem kaynağı (§6.3); toparlanma matematiği
`CLAUDE.md` §6 deterministik hesap ilkesine bağlanır.

**⚠ Yasal not:** "Düşen geri gelir / gelmez" **iddiası yok** — yalnız
aritmetik gösterilir. Geçmiş getiri anlatımı gelecek çıkarımına dönüşmez.

---

#### S1-L5 · Bileşik Getirinin Gücü — *içerik turu bekliyor (T6.11c)*

`bilesik-getirinin-gucu` · Başlangıç · ~7 dk · **6 → 13 aşama** · **1 → ≥6 figür**
**Ön bilgi:** `return-source` · `cash-flow-return` (S0-L6) · `time-horizon` (S0-L8)
**Tanıtılan:** `compound` · `reinvestment` — **Pekiştirilen:** `savings-rate` ·
`time-horizon` · `real-return` — **LiveContext:** `price_change_12m`

**Öğrenme çıktıları:**
1. Bileşik getiriyi "getirinin de getiri getirmesi" olarak **tanımlar**.
2. Çok yıllı büyümeyi **hesaplar**.
3. Yeniden yatırımın (dağıtmamanın) etkisini **gösterir**.
4. Sürenin neden en güçlü değişken olduğunu **analiz eder**.
5. *(Deep)* Enflasyonla birleşince **reel** bileşik etkiyi **yorumlar**.

**Aşama planı (13):**
1. `Core/Explain` — **Bu derste ne öğreneceksin?**
2. `Core/Explain` — Getirinin üzerine getiri 🖼 `interest-on-interest` **(çok panelli)**
3. `Core/Example` — İki yıl üst üste: +20.000 değil +24.000 (aritmetik)
4. `Core/Explain` — Eğri neden yukarı kıvrılır 🖼 `compound-curve` *(mevcut)*
5. `Core/Trap` — "İki katı süre = iki katı sonuç" (doğrusal sanma)
6. `Core/Example` — Katlanma süresi: kaç yılda ikiye katlar 🖼 `doubling-time`
7. `Context/Explain` — Yeniden yatırım: zinciri kırmamak 🖼 `withdraw-breaks-chain` **(çok panelli)**
8. `Context/Example` — Aynı getiri, biri çekiyor biri bırakıyor
9. `Context/Trap` — "Küçük tutarla başlamanın anlamı yok" 🖼 `time-beats-amount` **(çok panelli)**
10. `Deep/Explain` — Reel bileşik: enflasyon aynı mekanizmayla çalışır 🖼 `real-compound`
11. `Deep/Example` — Nominal bileşik ↔ reel bileşik yan yana
12. `Core/LiveContext` — Portföyündeki 12 aylık değişim
13. `Core/Source` — Bu bilgiler nereden geliyor?

**Figürler (6):** `interest-on-interest`✳ · `compound-curve` *(mevcut)* ·
`doubling-time` · `withdraw-breaks-chain`✳ · `time-beats-amount`✳ · `real-compound`.

**Değerlendirme (9):** Easy — bileşik tanımı · Medium — çok yıllı hesap,
katlanma süresi · Hard — doğrusal sanma tuzağı, reel bileşik yorumu.

**Kaynaklar:** Yöntem kaynağı (§6.3) + `CLAUDE.md` §6 formülleri.

**⚠ Yasal not:** Örnek oranlar **kurgusal ve etiketli**; "yıllık %X beklenebilir"
formu **yasak** (gelecek tahmini).

---

### Set 2 — Portföyünü Okumak (4 ders · Başlangıç→Gelişen)

Bu set spiralin **üçüncü turudur** (§2.3): kavramlar kullanıcının **kendi
rakamıyla** çalışır. Bu yüzden her dersin `LiveContext` bloğu doludur.

---

#### S2-L1 · Ağırlık ve yoğunlaşma

`agirlik-ve-yogunlasma` · Başlangıç→Gelişen · ~7 dk · 13 aşama
**Ön bilgi:** `portfolio` · `position` · `weight` (S0-L8) · `diversification` (S1-L2)
**Tanıtılan:** `concentration` · `top-n-weight` — **Pekiştirilen:** `weight` ·
`diversification` · `concentration-drift` · `emergency-fund`
**LiveContext:** `concentration_top2` · `holding_count` · `asset_class_weights`

**Öğrenme çıktıları:**
1. Yoğunlaşmayı **tanımlar** ve en büyük N ağırlığını **okur**.
2. Kendi portföyünün yoğunlaşmasını **hesaplar** ve **yorumlar**.
3. Kalem sayısı ile gerçek çeşitliliğin farkını **gösterir**.
4. *(Deep)* Fon/BES içindeki **örtüşmeyi** (aynı varlığa iki kapıdan maruz kalma)
   **analiz eder**.

**Aşama planı (13):** açılış → yoğunlaşma nedir 🖼 `concentration` *(mevcut)* →
işlenmiş örnek: en büyük iki ağırlık 🖼 `top2-bar` → tuzak "çok kalem =
çeşitlilik" 🖼 `many-but-same` *(S0-L8 ile paylaşılır)* → ağırlık haritası
🖼 `weight-treemap` **(çok panelli)** → örnek: aynı toplam, iki dağılım →
`Context` eşik değil **bant** okuma → örnek: zaman içinde kayma
🖼 `drift-over-time` **(çok panelli)** → tuzak "bir kez kurdum, tamamdır" →
`Deep` örtüşme 🖼 `hidden-overlap` **(çok panelli)** → örnek: fon içeriğiyle
doğrudan kalemin çakışması → `LiveContext` → `Source`.

**Figürler (6):** `concentration`*(mevcut)* · `top2-bar` · `many-but-same`*(paylaşılan)* ·
`weight-treemap`✳ · `drift-over-time`✳ · `hidden-overlap`✳.

**Değerlendirme (9):** Easy — yoğunlaşma tanımı · Medium — ağırlık ve top-2
hesabı · Hard — örtüşme çıkarımı, kayma yorumu.

**Kaynaklar:** Hesap yöntemi `CLAUDE.md` §6 (ağırlık formülü) + `15` §3.1
(`concentration_top2` kaynağı: `PortfolioAnonymizer`).

**⚠ Yasal not:** 🔴 "Yoğunlaşman yüksek, X eklemelisin" **yasak** (`15` §3.4).
Yalnız ölçüm ve **çerçeve** ("bu metrik genelde şu üzerinde yoğun sayılır").

---

#### S2-L2 · Maliyet ortalaması ve kademeli alım

`maliyet-ortalamasi` · Başlangıç→Gelişen · ~7 dk · 13 aşama
**Ön bilgi:** `position` (S0-L8) · `capital-gain` (S0-L6)
**Tanıtılan:** `cost-basis` · `cost-averaging` · `realized-unrealized`
**Pekiştirilen:** `capital-gain` · `position` — **LiveContext:** `cost_basis`

**Öğrenme çıktıları:**
1. Ağırlıklı ortalama maliyeti **hesaplar** (`Σ(miktar×fiyat)/Σmiktar`).
2. Kademeli alımın ortalama maliyeti nasıl değiştirdiğini **gösterir**.
3. Gerçekleşmiş ve gerçekleşmemiş kârı **ayırt eder**.
4. *(Deep)* Ortalama maliyetin bir **hedef fiyat olmadığını** **analiz eder**
   (S3-L3 çıpalama köprüsü).

**Aşama planı (13):** açılış → maliyet nedir 🖼 `avg-cost-scale` **(çok panelli)** →
işlenmiş örnek: iki alımın ağırlıklı ortalaması (aritmetik) → kademeli alım
🖼 `staged-buys` → örnek: aynı toplam tutar, iki farklı ortalama
🖼 `same-total-different-avg` **(çok panelli)** → tuzak "ortalamayı düşürmek
kâr demektir" → `Context` gerçekleşmiş ↔ gerçekleşmemiş 🖼 `paper-vs-realized-2` →
örnek: satmadan kâr/zarar → tuzak "kâğıt üstünde zarar, zarar değildir" →
`Deep` maliyet bir hedef değildir 🖼 `cost-is-not-target` **(çok panelli)** →
örnek: fiyat senin maliyetini bilmez → `LiveContext` → `Source`.

**Figürler (6):** `avg-cost-scale`✳ · `staged-buys` · `same-total-different-avg`✳ ·
`paper-vs-realized-2` · `cost-is-not-target`✳ · `price-doesnt-know-you`.

**Değerlendirme (9):** Easy — maliyet tanımı · Medium — ağırlıklı ortalama
hesabı, gerçekleşmiş/gerçekleşmemiş ayrımı · Hard — "ortalamayı düşürmek"
tuzağı, maliyet ≠ hedef.

**Kaynaklar:** `CLAUDE.md` §6 — ağırlıklı ortalama maliyet formülü; uygulamanın
maliyeti nasıl hesapladığı (`03-DATA-MODEL.md` §Holdings/Transactions).

**⚠ Yasal not:** "Düşerse ekle / ortalama düşür" **yasak** — kademeli alım bir
**tanım** olarak anlatılır, strateji önerisi olarak değil.

---

#### S2-L3 · Kur etkisi ve çoklu para birimi

`kur-etkisi` · Gelişen · ~8 dk · 13 aşama
**Ön bilgi:** `asset-class` (S0-L5) · `weight` (S0-L8) · `correlation` (S1-L2)
**Tanıtılan:** `base-currency` · `fx-effect` — **Pekiştirilen:** `correlation` ·
`asset-class` · `weight` — **LiveContext:** `asset_class_weights`

**Öğrenme çıktıları:**
1. Baz para birimini **tanımlar** ve neden gerektiğini **açıklar**.
2. Aynı varlığın iki para biriminde farklı getiri göstermesini **hesaplar**.
3. Toplam getiriyi **varlık** ve **kur** bileşenine **ayırır**.
4. *(Deep)* "Farklı kalemler ama tek para birimi ekseni" durumunu **analiz eder**.

**Aşama planı (13):** açılış → baz para birimi: hangi gözlükle bakıyorsun
🖼 `base-currency-lens` **(çok panelli)** → örnek: aynı varlık iki kurda
🖼 `same-asset-two-currencies` **(çok panelli)** → tuzak "getirim %X" (hangi para
biriminde?) → `Context` bileşenlere ayırma 🖼 `fx-decomposition` → işlenmiş örnek:
varlık +%A, kur +%B → toplam ≠ A+B (bileşik) → tuzak "kur farkı da kârdır"
(alım gücü sorusu) → `Context` dört kalem, tek eksen 🖼 `single-axis-four-items` →
örnek: hepsi TL'ye bağlı portföy → `Deep` kur ve enflasyon aynı şey değildir
🖼 `fx-vs-inflation` **(çok panelli)** → `LiveContext` → `Source`.

**Figürler (6):** `base-currency-lens`✳ · `same-asset-two-currencies`✳ ·
`fx-decomposition` · `single-axis-four-items` · `fx-vs-inflation`✳ · `currency-drift`.

**Değerlendirme (9):** Easy — baz para birimi tanımı · Medium — iki para
biriminde getiri hesabı, bileşen ayrıştırma · Hard — bileşik etki (A+B değil),
kur ≠ enflasyon.

**Kaynaklar:** TCMB — döviz kurları (kaynak ve yayın saati) ⟨doğrulanacak⟩ ·
`CLAUDE.md` §6 — para birimi dönüşüm formülü.

**⚠ Yasal not:** "Dövize geç / TL'de kal" **yasak**. Para birimi bir **ölçüm
ekseni** olarak anlatılır, tercih olarak değil.

---

#### S2-L4 · Getiriyi doğru ölçmek

`getiriyi-dogru-olcmek` · Gelişen · ~8 dk · 13 aşama
**Ön bilgi:** `nominal-return` · `real-return` (S1-L1) · `compound` (S1-L5)
**Tanıtılan:** `period-selection` · `annualization` — **Pekiştirilen:**
`real-return` · `compound` · `volatility` · `price-index`
**LiveContext:** `real_return_12m` · `price_change_12m`

**Öğrenme çıktıları:**
1. Dönem seçiminin ölçülen getiriyi nasıl değiştirdiğini **gösterir**.
2. Yıllıklandırmayı **hesaplar**.
3. Nominal/reel ayrımını **kendi verisine uygular**.
4. *(Deep)* Kısa dönemi yıllıklandırmanın neden yanıltıcı olduğunu **analiz eder**.

**Aşama planı (13):** açılış → "getirim ne kadar?" eksik bir sorudur →
dönem penceresi 🖼 `window-selection` *(mevcut, paylaşılan)* → işlenmiş örnek:
aynı seri, üç farklı başlangıç → tuzak: başlangıcı seçmek 🖼 `cherry-picked-start`
**(çok panelli)** → `Context` yıllıklandırma nedir 🖼 `annualize-scale` →
işlenmiş örnek: 3 aylık %X → yıllık karşılığı (bileşik) → tuzak: kısa dönemi
yıllıklandırmak 🖼 `annualize-trap` **(çok panelli)** → `Context` nominal ↔ reel
yan yana 🖼 `nominal-real-side-by-side` **(çok panelli)** → örnek: aynı dönem iki
ölçüm → `Deep` katkı/çekiş varsa ölçüm değişir (para-ağırlıklı ↔ zaman-ağırlıklı,
sezgisel) → `LiveContext` → `Source`.

**Figürler (6):** `window-selection`*(paylaşılan)* · `cherry-picked-start`✳ ·
`annualize-scale` · `annualize-trap`✳ · `nominal-real-side-by-side`✳ ·
`flows-change-measurement`.

**Değerlendirme (9):** Easy — nominal/reel ayrımı · Medium — yıllıklandırma
hesabı, dönem etkisi · Hard — kiraz toplama tuzağı, katkı/çekişin ölçüme etkisi.

**Kaynaklar:** TÜİK — TÜFE dönem verisi ⟨doğrulanacak⟩ · `CLAUDE.md` §6 —
reel getiri formülü · `15` §3.1 — `real_return_12m` kaynağı.

**⚠ Yasal not:** Tüm ölçümler **gerçekleşmiş** dönem içindir; "yıllık %X
bekleyebilirsin" formu yasak.

---

### Set 3 — Davranış (4 ders)

> **`RiskAttitude` yalnız bu setin ders SIRASINI etkiler** (`15` §1.1) —
> etiket hiçbir yerde görünmez, metinde "senin profilin" denmez.
> **Ton kuralı:** ders okuyucuyu **teşhis etmez**. "Sen şunu yapıyorsun" ❌ →
> "İnsanlar genelde şu kalıba düşer" ✅. Davranış aynası (kendi işlem geçmişi
> üzerinden gözlem) **bu setin değil, T8 A5'in** işidir ve hukuk görüşü ister.

---

#### S3-L1 · Kayıptan kaçınma

`kayiptan-kacinma` · Gelişen · ~7 dk · 13 aşama
**Ön bilgi:** `risk` · `volatility` (S0-L7) · `realized-unrealized` (S2-L2)
**Tanıtılan:** `loss-aversion` · `disposition-effect` — **Pekiştirilen:**
`risk` · `realized-unrealized` · `risk-return` — **LiveContext:** —

**Öğrenme çıktıları:**
1. Kayıptan kaçınmayı **tanımlar**.
2. Aynı büyüklükteki kazanç ve kaybın neden farklı hissettirdiğini **örnekler**.
3. Kazananı erken satma / kaybedeni tutma kalıbını **teşhis eder**.
4. *(Deep)* Aynı seçeneğin **çerçevelenişinin** kararı nasıl değiştirdiğini **analiz eder**.

**Aşama planı (13):** açılış → kayıp ve kazanç aynı ağırlıkta hissedilmez
🖼 `pain-vs-pleasure` **(çok panelli)** → işlenmiş örnek: +1.000 ↔ −1.000 aynı
kişi → tuzak "zarar satılmazsa zarar değildir" 🖼 `paper-loss-feels-real` →
`Context` kalıp: kazananı sat, kaybedeni tut
🖼 `sell-winners-hold-losers` **(çok panelli)** → örnek: iki kalem, aynı karar anı →
tuzak "geri gelmesini beklemek bedava değildir" (fırsat maliyeti köprüsü) →
`Context` asimetri terazisi 🖼 `asymmetry-scale` → `Deep` çerçeveleme
🖼 `frame-changes-choice` **(çok panelli)** → örnek: aynı sayı, iki cümle →
`Deep` kalıbın ölçülen getiriye etkisi → `Source`.

**Figürler (6):** `pain-vs-pleasure`✳ · `paper-loss-feels-real` ·
`sell-winners-hold-losers`✳ · `asymmetry-scale` · `frame-changes-choice`✳ ·
`opportunity-of-waiting`.

**Değerlendirme (9):** Easy — tanım · Medium — kalıbı senaryoda tanıma ·
Hard — çerçeveleme etkisi, kâğıt üstü zarar tuzağı.

**Kaynaklar:** Davranışsal finans literatürünün **yerleşik bulguları**;
`Source` bloğu birincil kaynağı (akademik çalışma adı + yıl) verir
⟨doğrulanacak — §6.2'ye göre akademik kaynak kabul edilir⟩.

**⚠ Yasal not:** 🔴 "Kaybedeni sat" / "kazananı tut" **yasak** — her ikisi de
alım-satım yönlendirmesidir. Ders kalıbı **tarif eder**, tepki önermez.

---

#### S3-L2 · FOMO ve sürü davranışı

`fomo-ve-suru` · Gelişen · ~7 dk · 13 aşama
**Ön bilgi:** `speculation` (S0-L1) · `guaranteed-return-fallacy` · `risk` (S0-L7)
**Tanıtılan:** `fomo` · `herding` — **Pekiştirilen:** `speculation` ·
`guaranteed-return-fallacy` · `concentration` — **LiveContext:** —

**Öğrenme çıktıları:**
1. FOMO'yu ve sürü davranışını **tanımlar**.
2. "Herkes alıyor" bilgisinin neden bir **veri olmadığını** **açıklar**.
3. Hikâye ile rakamı **ayırt eder**.
4. *(Deep)* Geç katılımın neden farklı bir risk profili taşıdığını **analiz eder**.

**Aşama planı (13):** açılış → kaçırma korkusu nedir 🖼 `fear-of-missing` →
işlenmiş örnek: bir tanıdığın kazancı duyulduğunda → `Core` sürü: ok yönü
🖼 `crowd-arrow` **(çok panelli)** → tuzak "herkes alıyorsa doğrudur" →
`Context` hikâye ↔ rakam 🖼 `story-vs-numbers` **(çok panelli)** → örnek: aynı
varlık, iki anlatım → tuzak "geç kalmak = fırsatı kaçırmak" → `Context` geç
katılım 🖼 `late-entry` **(çok panelli)** → örnek: aynı varlığa iki farklı anda
girmek → `Deep` hayatta kalma yanlılığı: kazananların duyulması
🖼 `survivorship` → `Source`.

**Figürler (6):** `fear-of-missing` · `crowd-arrow`✳ · `story-vs-numbers`✳ ·
`late-entry`✳ · `survivorship` · `who-tells-the-story`.

**Değerlendirme (9):** Easy — tanımlar · Medium — hikâye/rakam ayrımı, kalıp
tanıma · Hard — hayatta kalma yanlılığı, geç katılımın risk farkı.

**Kaynaklar:** Davranışsal finans literatürü ⟨doğrulanacak⟩ · SPK yatırımcı
uyarıları (sosyal medya kaynaklı yönlendirme) ⟨doğrulanacak⟩.

**⚠ Yasal not:** Hiçbir varlık, platform, kişi veya dönem **örnek olay olarak
adlandırılmaz** (gerçek bir balon/olay anlatımı zımni yorum üretir). Kalıplar
soyut anlatılır.

---

#### S3-L3 · Çıpalama ve batık maliyet

`cipalama-ve-batik-maliyet` · Gelişen · ~7 dk · 13 aşama
**Ön bilgi:** `cost-basis` (S2-L2) · `opportunity-cost` (S0-L3) · `period-selection` (S2-L4)
**Tanıtılan:** `anchoring` · `sunk-cost` — **Pekiştirilen:** `cost-basis` ·
`loss-aversion` · `opportunity-cost` — **LiveContext:** `cost_basis`

**Öğrenme çıktıları:**
1. Çıpalamayı **tanımlar** ve alış fiyatının nasıl çıpaya dönüştüğünü **gösterir**.
2. Batık maliyeti **tanımlar** ve geleceğe ait kararla ilişkisiz olduğunu **açıklar**.
3. Aynı varlığı farklı maliyetlerle tutan iki kişinin aynı geleceğe baktığını
   **karşılaştırır**.
4. *(Deep)* "Başa baş" düşüncesinin ölçüm dönemini nasıl çarpıttığını **analiz eder**.

**Aşama planı (13):** açılış → çıpa nedir 🖼 `anchor-price` **(çok panelli)** →
işlenmiş örnek: alış fiyatı kararın merkezine oturuyor → tuzak "başa baş gelince
satarım" → `Context` fiyat senin maliyetini bilmez
🖼 `price-doesnt-know-you` *(S2-L2 ile paylaşılır)* → örnek: iki yatırımcı, aynı
varlık, iki maliyet 🖼 `two-investors-same-asset` **(çok panelli)** → `Context`
batık maliyet 🖼 `sunk-cost-hole` **(çok panelli)** → örnek: harcanan para geri
gelmiyor → tuzak "bu kadar bekledim, biraz daha beklerim" → `Deep` soruyu
yeniden kurmak: "bugün olsa yine alır mıydım" **çerçevesi** 🖼 `reset-the-question`
→ `LiveContext` → `Source`.

**Figürler (6):** `anchor-price`✳ · `price-doesnt-know-you`*(paylaşılan)* ·
`two-investors-same-asset`✳ · `sunk-cost-hole`✳ · `reset-the-question` ·
`breakeven-illusion`.

**Değerlendirme (9):** Easy — tanımlar · Medium — çıpayı senaryoda tanıma ·
Hard — iki yatırımcı çıkarımı, başa baş yanılsaması.

**Kaynaklar:** Davranışsal finans literatürü ⟨doğrulanacak⟩.

**⚠ Yasal not:** 🔴 "Bugün olsa alır mıydım" sorusu bir **düşünme çerçevesi**
olarak sunulur; cevabı **verilmez** ve "öyleyse sat" çıkarımı yazılmaz.

---

#### S3-L4 · Aşırı işlem ve gizli maliyetler

`asiri-islem-ve-gizli-maliyetler` · Gelişen · ~7 dk · 13 aşama
**Ön bilgi:** `liquidity` (S0-L5) · `cost-averaging` (S2-L2) · `disposition-effect` (S3-L1)
**Tanıtılan:** `overtrading` · `transaction-cost` — **Pekiştirilen:** `liquidity` ·
`fomo` · `disposition-effect` — **LiveContext:** —

**Öğrenme çıktıları:**
1. İşlem maliyetinin bileşenlerini (komisyon, makas, vergi/masraf) **sıralar**.
2. Bir gidiş-dönüş işlemin maliyetini **hesaplar**.
3. İşlem sıklığı arttıkça maliyetin nasıl biriktiğini **gösterir**.
4. *(Deep)* "Hareketli olmak = iyi yönetmek" varsayımını **analiz eder**.

**Aşama planı (13):** açılış → görünmeyen maliyetler 🖼 `hidden-costs-list` →
`Core` makas nedir 🖼 `spread-bite` **(çok panelli)** → işlenmiş örnek: al-sat
gidiş-dönüş maliyeti (aritmetik) 🖼 `cost-per-round-trip` → tuzak "komisyon sıfır
demek maliyet sıfır demek" → `Context` maliyet birikir
🖼 `trades-stack-up` **(çok panelli)** → örnek: yılda 4 ↔ 40 işlem → tuzak
"kaybı hızlı kapatmak için işlem yapmak" (S3-L1 köprüsü) → `Context` faaliyet ↔
sonuç 🖼 `activity-vs-outcome` **(çok panelli)** → `Deep` maliyet bileşik etkiyi
tersinden yer (S1-L5 köprüsü) 🖼 `cost-compounds-too` → `Source`.

**Figürler (6):** `hidden-costs-list` · `spread-bite`✳ · `cost-per-round-trip` ·
`trades-stack-up`✳ · `activity-vs-outcome`✳ · `cost-compounds-too`.

**Değerlendirme (9):** Easy — maliyet bileşenleri · Medium — gidiş-dönüş
maliyeti hesabı · Hard — birikimin bileşik etkisi, faaliyet/sonuç ayrımı.

**Kaynaklar:** Aracı kurum masraf kalemlerinin **tanımları** (komisyon, makas)
— SPK/Borsa İstanbul tanımları ⟨doğrulanacak⟩. **Belirli kurum tarifesi
yazılmaz** (değişken + reklam sayılabilir).

**⚠ Yasal not:** "Az işlem yap" bir **öneri değildir** — ders maliyetin
matematiğini gösterir, işlem sıklığı kararını okuyucuya bırakır.

---

### Set 4 — Türkiye Gerçekleri (4 ders · Gelişen→İleri)

> **Bu setin tamamı yıllık gözden geçirmeye tabidir** (§6.4): oranlar, limitler
> ve mevzuat referansları değişir. Her künyede kaynak + tarih zorunludur.

---

#### S4-L1 · BES'i doğru kullanmak

`bes-i-dogru-kullanmak` · Gelişen · ~8 dk · 13 aşama
**Ön bilgi:** `time-horizon` (S0-L8) · `compound` (S1-L5) · `asset-class` (S0-L5)
**Tanıtılan:** `bes` · `state-contribution` · `vesting` — **Pekiştirilen:**
`compound` · `time-horizon` — **LiveContext:** `bes_state_share`

**Öğrenme çıktıları:**
1. BES'in üç bileşenini (kendi katkın · devlet katkısı · fon) **tanımlar**.
2. Devlet katkısının neden **ayrı bir kalem** olarak izlendiğini **açıklar**.
3. Hak kazanma (vesting) takvimini **okur**.
4. *(Deep)* Erken çıkışın hak kazanılmamış tutara etkisini **analiz eder**.

**Aşama planı (13):** açılış → BES nedir: iki cep 🖼 `bes-two-pockets` **(çok
panelli)** → işlenmiş örnek: katkı + devlet katkısı ayrı ayrı → `Core` devlet
katkısı bir **hediye değil, koşullu bir kalem** 🖼 `state-contribution-bar` →
tuzak "devlet katkısı hemen benim" → `Context` hak kazanma takvimi
🖼 `vesting-timeline` **(çok panelli)** → örnek: farklı sürelerde hak kazanılan
oran → tuzak "istediğim an tam çıkarım" → `Context` fonun içi: BES bir sepet
taşır 🖼 `fund-inside-bes` → `Deep` erken çıkış etkisi
🖼 `early-exit-effect` **(çok panelli)** → örnek: uzun vade + bileşik etkinin
kesilmesi → `LiveContext` → `Source`.

**Figürler (6):** `bes-two-pockets`✳ · `state-contribution-bar` ·
`vesting-timeline`✳ · `fund-inside-bes` · `early-exit-effect`✳ · `bes-timeline-summary`.

**Değerlendirme (9):** Easy — bileşenler · Medium — devlet katkısının ayrı
izlenmesi, vesting okuma · Hard — erken çıkış etkisi, bileşik kesintisi.

**Kaynaklar:** 🔴 **Zorunlu ve tarihli** — SEDDK (Sigortacılık ve Özel Emeklilik
Düzenleme ve Denetleme Kurumu) ve Hazine ve Maliye Bakanlığı: devlet katkısı
oranı, üst sınırı ve hak kazanma süreleri ⟨oran/süre/belge **doğrulanacak**;
**yıllık gözden geçirme**⟩.

**⚠ Yasal not:** 🔴 "BES'e gir / çık / şu fona geç" **yasak**. Devlet katkısı
oranı gibi **her rakam** kaynaklı ve tarihli yazılır; kaynaksız oran metne
girmez (§9-M1).

---

#### S4-L2 · Altın kültürü — gram, çeyrek, ayar

`altin-kulturu` · Gelişen · ~8 dk · 13 aşama
**Ön bilgi:** `asset-class` · `liquidity` (S0-L5) · `base-currency` · `fx-effect` (S2-L3)
**Tanıtılan:** `gold-forms` · `making-charge` — **Pekiştirilen:** `liquidity` ·
`fx-effect` · `base-currency` — **LiveContext:** `asset_class_weights`

**Öğrenme çıktıları:**
1. Gram / çeyrek / ziynet ve ayar (saflık) farkını **tanımlar**.
2. İşçilik payının alış-satış farkına etkisini **hesaplar**.
3. Ziynet altını ile yatırım altınını likidite ekseninde **ayırt eder**.
4. *(Deep)* TL cinsi altın getirisini **ons** ve **kur** bileşenine **ayırır**.

**Aşama planı (13):** açılış → altının biçimleri 🖼 `gold-forms-row` **(çok
panelli)** → `Core` ayar = saflık 🖼 `karat-purity` → işlenmiş örnek: aynı gram,
farklı ayar → `Core` işçilik payı 🖼 `making-charge-wedge` **(çok panelli)** →
işlenmiş örnek: alıp hemen satmanın maliyeti (aritmetik) → tuzak "altın hep aynı
altındır" → `Context` düğün/ziynet altını: duygusal ve likit değil
🖼 `wedding-gold` → tuzak "ziynet de yatırımdır" → `Context` TL altın fiyatı iki
bileşenlidir 🖼 `ounce-and-fx` **(çok panelli)** → `Deep` örnek: ons sabitken
kurla değişen TL fiyatı → `LiveContext` → `Source`.

**Figürler (6):** `gold-forms-row`✳ · `karat-purity` · `making-charge-wedge`✳ ·
`wedding-gold` · `ounce-and-fx`✳ · `gold-decomposition`.

**Değerlendirme (9):** Easy — ayar/biçim tanımı · Medium — işçilik maliyeti
hesabı, likidite ayrımı · Hard — ons/kur ayrıştırması.

**Kaynaklar:** Darphane ve Damga Matbaası — ayar/saflık tanımları
⟨doğrulanacak⟩ · Borsa İstanbul Kıymetli Madenler Piyasası — işlem gören
standart altın tanımı ⟨doğrulanacak⟩ · TCMB — kur (ons→TL bileşeni)
⟨doğrulanacak⟩.

**⚠ Yasal not:** "Altın al / bozdur" **yasak**. Ders kültürel bir varlığı
**tanımlar ve maliyetini gösterir**; bir varlık sınıfını diğerine tercih
ettirmez (`15` §3.4).

---

#### S4-L3 · Enflasyon ortamında birikim

`enflasyonda-birikim` · Gelişen→İleri · ~8 dk · 13 aşama
**Ön bilgi:** `inflation` · `purchasing-power` (S0-L4) · `real-return` (S1-L1) · `liquidity` (S0-L5)
**Tanıtılan:** `deposit-insurance` · `saving-in-inflation` — **Pekiştirilen:**
`real-return` · `nominal-return` · `purchasing-power` · `guaranteed-return-fallacy`
**LiveContext:** `inflation_12m` · `real_return_12m` · `cash_weight`

**Öğrenme çıktıları:**
1. Nominal faiz ile reel sonucu **ayırt eder**.
2. Mevduat sigortasının **neyi kapsadığını** ve neyi kapsamadığını **açıklar**.
3. "Enflasyonun altında kalmak" durumunu kendi verisinde **okur**.
4. *(Deep)* Likidite güvencesi ile reel koruma arasındaki gerilimi **analiz eder**.

**Aşama planı (13):** açılış → nominal oran ↔ enflasyon
🖼 `nominal-rate-vs-inflation` **(çok panelli)** → işlenmiş örnek: nominal %A,
enflasyon %B → reel sonuç (S1-L1 formülü uygulanır) → tuzak "faiz yüksekse
kazandım" → `Core` güvence kimden geliyor 🖼 `insurance-scope` → örnek: sigortanın
kapsadığı ↔ kapsamadığı → tuzak "her birikim aracı sigortalıdır" → `Context`
alım gücü sonucu 🖼 `real-result-bar` → örnek: bir yıl sonunda sepet karşılığı →
`Deep` likidite ↔ reel koruma gerilimi 🖼 `liquidity-vs-protection` **(çok
panelli)** → `Deep` erime birikimde de çalışır 🖼 `erosion-in-savings` **(çok
panelli)** → `LiveContext` → `Source`.

**Figürler (6):** `nominal-rate-vs-inflation`✳ · `insurance-scope` ·
`real-result-bar` · `liquidity-vs-protection`✳ · `erosion-in-savings`✳ ·
`rate-vs-basket`.

**Değerlendirme (9):** Easy — nominal/reel ayrımı, sigorta kavramı · Medium —
reel sonuç hesabı, kapsam okuma · Hard — likidite/koruma gerilimi, "yüksek faiz"
tuzağı.

**Kaynaklar:** 🔴 **Zorunlu ve tarihli** — TMSF: mevduat sigortası kapsamı ve
limiti ⟨limit **doğrulanacak**; **yıllık gözden geçirme**⟩ · TÜİK TÜFE
⟨doğrulanacak⟩ · TCMB ⟨doğrulanacak⟩.

**⚠ Yasal not:** 🔴 "Mevduat yerine X" / "şu araca geç" **yasak**. Sigorta
kapsamı **olgu** olarak verilir; hiçbir araç güvenli/güvensiz diye
etiketlenmez.

---

#### S4-L4 · Fon okuma — TEFAS ve gider oranı

`fon-okuma` · İleri · ~8 dk · 13 aşama · *(T7.5 fon verisine bağımlı)*
**Ön bilgi:** `asset-class` · `ownership-vs-lending` (S0-L5) · `peer-comparison` (S1-L3) ·
`transaction-cost` (S3-L4) · `compound` (S1-L5)
**Tanıtılan:** `fund-expense-ratio` · `tefas` — **Pekiştirilen:**
`peer-comparison` · `transaction-cost` · `compound` · `reinvestment`
**LiveContext:** `asset_class_weights`

**Öğrenme çıktıları:**
1. Fonun bir **sepet** olduğunu ve içeriğinin nereden okunacağını **gösterir**.
2. Gider oranını **tanımlar** ve getiriye etkisini **hesaplar**.
3. Bir fon künyesinde bakılacak alanları **sıralar**.
4. *(Deep)* Gider oranının bileşik etkiyle uzun vadede nasıl büyüdüğünü **analiz eder**.

**Aşama planı (13):** açılış → fonun içini açmak 🖼 `fund-basket-open` **(çok
panelli)** → `Core` künyede ne yazar 🖼 `tefas-fact-sheet` → işlenmiş örnek: bir
künyenin alanları okunur → tuzak "fon adı içeriğini anlatır" → `Core` gider oranı
🖼 `expense-ratio-bite` **(çok panelli)** → işlenmiş örnek: %X gider, 1 yıllık
etkisi (aritmetik) → tuzak "gider oranı küçük bir rakam" → `Context` kategori
içinde karşılaştırma (emsal) 🖼 `category-comparison` → tuzak "geçmiş getiriye
göre seçmek" *(tahmin yasağıyla birebir)* → `Deep` gider bileşikleşir
🖼 `expense-compounds` **(çok panelli)** → örnek: 10 yılda birikimli etki →
`LiveContext` → `Source`.

**Figürler (6):** `fund-basket-open`✳ · `tefas-fact-sheet` · `expense-ratio-bite`✳ ·
`category-comparison` · `expense-compounds`✳ · `name-vs-content`.

**Değerlendirme (9):** Easy — fon = sepet, gider oranı tanımı · Medium — gider
etkisinin hesabı, künye okuma · Hard — bileşik gider etkisi, geçmiş getiriye
göre seçme tuzağı.

**Kaynaklar:** 🔴 TEFAS — fon künyesi alanları ve kategori tanımları
⟨doğrulanacak⟩ · SPK — yatırım fonlarına ilişkin tebliğ (gider kalemleri)
⟨doğrulanacak; **yıllık gözden geçirme**⟩.

**⚠ Yasal not:** 🔴 **Hiçbir fon adı/kodu geçmez.** "Ucuz fon iyidir" **yasak** —
gider oranının **matematiği** gösterilir, seçim önerilmez. Kategori
karşılaştırması bir sıralama değil, **emsal bandı** okuma alıştırmasıdır.

---

## 6. Kaynak ve doğruluk politikası

> **Neden:** Bu bir *ciddi bilgi* eğitimidir. Yanlış bir rakam, finans
> uygulamasında en büyük güven kırıcıdır (`CLAUDE.md` §3.1). `14` §B1 zaten
> **"kaynak daima görünür"** diyor — bu bölüm o ilkeyi içeriğe bağlar.

### 6.1 `Source` bloğu — her derste zorunlu

Her dersin **son aşaması** `SectionKind.Source` bloğudur:
**"Bu bilgiler nereden geliyor?"**

`SectionKind.Source` bugün enum'da tanımlı ama **hiç kullanılmıyor**
([`Enums.cs:188`](../../backend/src/Finans.Domain/Enums.cs:188)) — T6.19 bunu
devreye alır.

Blok üç şeyi söyler:
1. **Kurumsal kaynaklar** — kurum · belge · tarih.
2. **Kurgusal sayı beyanı** — "bu derste kullanılan tüm rakamlar örnek amaçlıdır"
   (§6.3).
3. **Hesap yöntemi** — sayılar nasıl hesaplandı (`CLAUDE.md` §6 formülleri),
   **LLM tarafından üretilmediği** (`14` §4-A3 şeffaflığı).

### 6.2 Kabul edilen kaynaklar

| Alan | Kurum |
|---|---|
| Enflasyon, fiyat endeksi | **TÜİK** |
| Kur, faiz, ödeme sistemleri | **TCMB** |
| Sermaye piyasası tanım/mevzuatı, yatırımcı uyarıları | **SPK** |
| Bankacılık düzenlemesi | **BDDK** |
| Mevduat sigortası | **TMSF** |
| Bireysel emeklilik | **SEDDK** · **Hazine ve Maliye Bakanlığı** |
| Yatırım fonları | **TEFAS** · SPK tebliğleri |
| Şirket bildirimleri, finansal tablolar | **KAP** |
| Borsa, kıymetli madenler | **Borsa İstanbul** |
| Davranışsal finans | Hakemli akademik yayın (yazar + yıl) |

**Kabul edilmez:** haber sitesi · blog · sosyal medya · aracı kurum yorumu ·
LLM çıktısı. *(Aracı kurum tarifesi ayrıca reklam riski taşır — §S3-L4.)*

### 6.3 Hangi iddia kaynak ister?

| Kaynak **ister** | Kaynak **istemez** |
|---|---|
| Kurumsal tanım ("mevduat sigortası nedir") | Evrensel kavram anlatımı ("çeşitlendirme riski yayar") |
| Oran, limit, eşik (devlet katkısı, sigorta limiti) | **Açıkça etiketli kurgusal** örnek sayısı |
| Resmî istatistik (TÜFE, kur) | Aritmetik türetme (formülün kendi sonucu) |
| Mevzuat hükmü, tebliğ | Uygulamanın kendi hesabı → yöntem beyanı yeter |

**Kurgusal sayı kuralı:** Örneklerdeki rakamlar gerçek veri **değildir** ve
metinde bu görünür ("örnek olarak", "diyelim ki"). Gerçek veri yalnız
`LiveContext` bloğunda ve kaynaklı olarak geçer. Enstrüman adı hâlâ **yasak**
(`15` §3.4) — örnekler "A yatırımı" soyutluğunda kalır.

### 6.4 Tarih damgası ve gözden geçirme

- Her kurumsal kaynak **tarihlidir**: kurum · belge · *veri tarihi*.
- **Yıllık gözden geçirme zorunlu** olan içerikler: Set 4'ün tamamı ·
  devlet katkısı oranı · mevduat sigortası limiti · TÜFE referansları ·
  fon gider kalemleri.
- Gözden geçirme kaydı bu dokümanın §10'unda tutulur.
- ⟨doğrulanacak⟩ işareti, künyede **kaynağın kesin belgesinin henüz
  doğrulanmadığını** gösterir. **İçerik yazılırken bu işaret kalkmadan ders
  yayınlanmaz** (§9-İ2).

### 6.5 Kaynak ≠ tahmin izni

Kaynaklı veri bile **geçmişe** aittir. Kaynak göstermek şu formları **açmaz**:
- ❌ "TÜİK'e göre enflasyon %X, yani gelecek yıl …"
- ❌ "Kaynak: TEFAS — bu fon kategorisi daha iyi performans gösterdi"
- ✅ "TÜİK TÜFE, 12 aylık gerçekleşme (⟨tarih⟩): %X"

---

## 7. Değerlendirme tasarımı

### 7.1 Soru dağılımı

Her ders **9 soru** taşır: **3 Easy · 3 Medium · 3 Hard**
(`QuizQuestion.Difficulty`, T6.11a). Seviye filtresi hem gösterimi hem
**puanlamayı** etkiler: Başlangıç yalnız Easy · Gelişen +Medium · İleri hepsi.

| Zorluk | Bloom | Ne sorar |
|---|---|---|
| **Easy** | Hatırla / Anla | Tanım, ayrım, kavramın ne olduğu |
| **Medium** | Uygula | Verilen rakamla hesap, kavramı senaryoda kullanma |
| **Hard** | Analiz et | Tuzağı teşhis, iki durumu karşılaştırma, sınırı görme |

### 7.2 Soru ↔ çıktı eşlemesi

**Her soru bir öğrenme çıktısını ölçer.** Ölçülmeyen çıktı kalırsa ya soru
eklenir ya çıktı gereksizdir. Künyelerdeki *Değerlendirme* satırı bu eşlemeyi
verir.

### 7.3 Çeldirici kuralı

**Çeldiriciler rastgele değildir — dersteki `Trap` bloklarından türetilir.**
Bir yanılgı derste tuzak olarak işlendiyse, testte çeldirici olarak geri gelir.
Bu, testi "hatırlama sınavı" olmaktan çıkarıp **yanılgı düzeltme** aracına
çevirir.

- Her sorunun **tam bir doğru** şıkkı olur.
- Her soruda **eğitici `Explanation`** bulunur; doğru şık ve açıklama **yalnız
  deneme sonucunda** açılır (T5E.3 sözleşmesi).
- "Yukarıdakilerin hepsi" / "hiçbiri" **kullanılmaz** (ayrım gücü düşük).

### 7.4 Geçme ve ilerleme

Ders **testi geçmeden tamamlanmaz** (T6.6; sunucuda `quiz_not_passed`).
Test **ayrı ve son adımdır** (T6.10). Utandırmama ilkesi (`14` §4-A2)
bağlayıcıdır: yanlışta puan/kırmızı vurgusu değil, açıklama gösterilir.

---

## 8. Görsel plan

### 8.1 Figür türleri

| Tür | Ne zaman | Örnek |
|---|---|---|
| **Tek sahne** | Tek bir ilişkiyi göstermek | `savings-rate-bar` |
| **Çok panelli anlatı** | Önce→sonra, karşılaştırma, süreç | `with-without-buffer` |
| **Etkileşimli** | Kullanıcı bir değişkeni oynatınca sonucu görmeli | `inflation-slider` |

**Set 0 kuralı:** ders başına **≥6 figür**, **≥1 çok panelli**; her anlatım
aşamasında görsel olması hedeftir (`15` §6.1).

### 8.2 Çok panelli kalıp

Tek `<svg>` içinde 2-4 panel; her panelin **kendi başlığı** olur; paneller
soldan sağa (mobilde alt alta) **zaman/neden-sonuç** sırasında okunur.
`aria-label` panellerin **hepsini** özetler (parça parça değil).

### 8.3 Paylaşılan öğeler (önce yazılır)

~62 figürü tek tek sıfırdan çizmek sürdürülemez. Önce **paylaşılan SVG
öğeleri** yazılır ve figürler bunlardan kurulur:

`Panel` (çerçeve + başlık) · `Coin` / `Note` (para) · `Basket` (sepet) ·
`PersonGlyph` (kişi silueti) · `Arrow` (yön/akış) · `Bar` / `StackedBar` ·
`AxisLine` · `Callout` (vurgu balonu).

### 8.4 Değişmeyen kurallar

- **Kütüphane yok** — elle yazılmış SVG ([`LessonFigure.tsx`](../../web/src/components/LessonFigure.tsx)).
- **Tema değişkenleri** kullanılır; sabit renk kodu yazılmaz (açık/koyu tema).
- `role="img"` + açıklayıcı `aria-label`; figür **metnin yerine geçmez**,
  metin figürsüz de eksiksizdir (bilinmeyen anahtar sessizce düşer).
- Figür **sola hizalı** (T6.7'de ortalama metinden kopuk göründü).
- Anahtar adı: `kebab-case`, İngilizce, semantik. **Ders içinde tekil**;
  farklı dersler aynı figürü **paylaşabilir** (örn. `window-selection`).

---

## 9. Yapısal sözleşme

Müfredatın içerikten sapmaması için kuralların bir kısmı **teste bağlanır**.
Geri kalanı açıkça **insan gözden geçirmesidir** — bu ayrım kasıtlıdır, "test
yeşil = içerik doğru" yanılsaması yaratılmaz.

### 9.1 Makineyle doğrulanan (M) — `EducationSeedTests`

| # | Kural |
|---|---|
| **M1** | Her yayımlanmış ders **≥1 `Source` bloğu** taşır |
| **M2** | Her dersin **ilk bloğu** açılış bloğudur ("Bu derste ne öğreneceksin?") ve **≥3 madde** içerir |
| **M3** | Figür eşiği: **Set 0 ≥6** (≥1 çok panelli) · diğer setler **≥2** *(müfredat hedefi ≥4; mevcut S1-L3/L4/L5 T6.11c'de yükseltilir — test bugün kırmızıya düşmez)* |
| **M4** | Figür anahtarları **ders içinde tekil** ve **hepsi `LessonFigure.tsx` kayıt defterinde tanımlı** *(TSX'ten regex ile okunur)* |
| **M5** | Quiz: ders başına **9 soru**, her zorluktan **≥3**, her soruda **tam 1 doğru** + `Explanation` dolu |
| **M6** | Her dersin **≥1 `ConceptTag`**'i var **ve** her `ConceptTag` **en az bir derste** tanıtılıyor (boşta kavram yok) |
| **M7** | Tavsiye / tahmin / **enstrüman sıralaması** taraması — `Source` blokları **dahil** *(mevcut test genişletilir)* |
| **M8** | MiniMarkdown alt kümesi dışına çıkılmaz *(link, T6.8 indikten sonra izinli)* |
| **M9** | Ön-koşul zinciri **track içinde** kalır; **setler arası sert kilit yok** (`15` §6.3) |

### 9.2 İnsan gözden geçirmesi (İ) — test edilemez

| # | Kural |
|---|---|
| **İ1** | Öğrenme çıktısı dersin **gerçek içeriğini** karşılıyor mu |
| **İ2** | **Kaynak iddiayı gerçekten destekliyor mu** — her ⟨doğrulanacak⟩ işareti kalkmadan ders yayımlanmaz |
| **İ3** | **İşlenmiş örneklerin aritmetiği** — her örnek yazım sırasında yeniden hesaplanır |
| **İ4** | Yasal sınır yorumu — 🔴 işaretli dersler (S0-L3 · S0-L7 · S1-L3 · S2-L1 · S3-L1 · S3-L3 · S4-L1 · S4-L3 · S4-L4) ayrıca okunur |
| **İ5** | Ton: davranış dersleri okuyucuyu **teşhis etmiyor** mu |

### 9.3 Sıralama kısıtı

**Testler içerikten önce yazılmaz.** M1/M2 bugünkü seed'de karşılanmıyor
(`Source` bloğu ve açılış bloğu yok) → önce **T6.19** içeriği indirir, sonra
**T6.20** testleri yazılır. Kırmızı test bırakılmaz (`CLAUDE.md` §12).

---

## 10. Sürüm ve gözden geçirme

| Değişiklik | Ne güncellenir |
|---|---|
| Ders içeriği düzeltildi | `EducationContent.cs` → seed **mutabakatı** çalışan DB'ye indirir (T6.7) |
| Yeni ders eklendi | §3 kavram haritası + §5 künye + `SeedData` + ön-koşul zinciri |
| Kavram eklendi/çıkarıldı | §3 tablosu + `ConceptTag` seed + T6.3 sözlüğü |
| Kurumsal oran/limit değişti | İlgili künye *Kaynaklar* + `Source` bloğu + **tarih** |
| Figür eklendi | §5 künye figür planı + `LessonFigure.tsx` kayıt defteri *(M4 bunu zorlar)* |

**Yıllık gözden geçirme (her yıl, Set 4 + tüm ⟨doğrulanacak⟩ işaretleri):**

| Tarih | Kapsam | Yapan | Not |
|---|---|---|---|
| — | *(ilk gözden geçirme Set 4 yazıldığında)* | — | — |

---

## 11. Kararlar (2026-07-22 · ürün sahibi)

| # | Soru | Karar | Gerekçe |
|---|---|---|---|
| 1 | Müfredat kapsamı | **25 dersin tamamı ders bazında tam detayda** | Kavram tekrarı/boşluğu ancak tüm harita bir arada görülünce fark edilir (§3) |
| 2 | Doğruluk politikası | **Her olgusal iddia kaynaklı; `Source` bloğu devreye alınır** | "Ciddi bilgi" iddiasının karşılığı; `14` §B1 "kaynak daima görünür" ilkesi zaten vardı ama uygulanmamıştı |
| 3 | Biçim | **Doküman + testle doğrulanan yapısal sözleşme** | Müfredat ile seed'in zamanla ayrışmasını engeller (§9) |

---

*Kardeş dokümanlar: [`15-EDUCATION-PLAN.md`](15-EDUCATION-PLAN.md) (tasarım) ·
[`14-PRODUCT-STRATEGY.md`](14-PRODUCT-STRATEGY.md) (vizyon) ·
[`08-BACKLOG.md`](08-BACKLOG.md) (görev sırası) ·
[`09-TESTING-STRATEGY.md`](09-TESTING-STRATEGY.md) (senaryo kataloğu).
Yasal kısıt: `CLAUDE.md` §2.*
