# 15 — Eğitim Derinliği, Kişiselleştirme ve Canlı Veri Planı

> **Tarih:** 2026-07-19 · **Durum:** ✅ Onaylandı ve `08-BACKLOG.md` Faz 6'ya
> işlendi (kararlar §9)
> **Kapsam:** Eğitim modülünün MVP iskeletinden (T5E.1–T5E.4) *uyarlanabilir,
> örneklendirilmiş ve gerçek veriyle bağlanmış* bir müfredata evrimi.
> **Bağlam:** `14` §4-A1/A2/A4 vizyonu · mevcut şema `03` §E · uçlar `04` §7.5
> **Yasal çerçeve:** `CLAUDE.md` §2 (yatırım tavsiyesi DEĞİL) — bu doküman
> boyunca en sık başvurulan kısıt budur.

---

## 1. Çekirdek Tasarım Kararı: derinliği RİSK değil BİLGİ belirler

Ürün sahibinin isteği: *"eğitime başlamadan yatırımcının risk seviyesini
belirleyerek eğitim konularının derinliğinin belirlenmesi."*

Niyet doğru (kişiselleştirilmiş derinlik), ancak **risk toleransı derinliğin
yanlış girdisidir** ve bunu olduğu gibi uygularsak ürün ters çalışır:

> Risk iştahı yüksek ama bilgisi sıfır kullanıcı — finansal okuryazarlıkta en
> kırılgan profil budur. "Risk seviyesi yüksek → derin içerik" kuralı, en az
> bilen kullanıcıya en ağır içeriği verir. Tam tersi gerekir.

Bu yüzden profil **tek eksen değil, üç dik eksen** olarak modellenir:

| Eksen | Ölçer | Neyi belirler | Yasal durum |
|---|---|---|---|
| **Bilgi seviyesi** (`LiteracyLevel`) | 4 nesnel tanılama sorusu | İçerik **derinliği** (L1/L2/L3) | ✅ Tamamen güvenli |
| **Risk tutumu** (`RiskAttitude`) | 4 senaryo sorusu (doğru cevap yok) | İçerik **vurgusu ve sırası** (hangi davranış dersi öne çıkar) — **kullanıcıya görünmez** | ⚠️ Sınırlı — §1.1 |
| **Portföy gerçekliği** | Mevcut hesaplanmış metrikler | **Örnekler** ("Senin portföyünde") | ✅ Olgu |

### 1.1 Risk tutumu için SPK sınırı — kırmızı çizgi

Türkiye'de **yerindelik/uygunluk testi** SPK lisanslı aracıların yatırım
hizmeti öncesi yaptığı düzenlenmiş bir araçtır. Nirengi bunu **taklit etmez.**

`RiskAttitude` çıktısı **asla** şu forma girmez:

- ❌ "Profilin: Dengeli → %40 hisse, %30 tahvil uygundur"
- ❌ "Risk profiline göre portföyün fazla agresif"
- ❌ Herhangi bir varlık dağılımı yüzdesi öneren cümle

Yalnızca şu forma girer:

- ✅ "Dalgalanma karşısında hızlı tepki verdiğini söyledin — bu derste
  *kayıptan kaçınma* önyargısını inceleyeceğiz."
- ✅ Davranış derslerinin sıralamasını değiştirmek (görünmez etki)

**Uygulama kuralı:** `RiskAttitude` yalnızca (a) ders sıralaması ve (b) davranış
dersi metin varyantı seçiminde kullanılır; portföy ekranlarına, LLM
prompt'larının sayısal bağlamına veya herhangi bir dağılım çıktısına **girmez.**
Bu kısıt teste bağlanır (bkz. §7 SC-E4).

**KARAR (2026-07-19): `RiskAttitude` kullanıcıya hiç gösterilmez.** Etiket
("Temkinli/Dengeli/Atılgan") ne profil ekranında ne ders başlığında görünür;
yalnızca içeriğin sırasını ve metin varyantını sessizce etkiler. Gerekçe:
görünür bir risk etiketi, kullanıcı tarafından bir *yatırımcı sınıflandırması*
olarak okunur ve yerindelik testi çağrışımı yapar. Görünmez tutmak hem SPK
sınırından uzaklaştırır hem de kullanıcının kendini bir kutuya yerleştirip
öğrenmeyi bırakmasını engeller.

---

## 2. Katmanlı İçerik Mimarisi

Mevcut `Lesson.BodyMarkdown` tek bloktur → derinlik ayarlanamaz. Çözüm zaten
şemada duruyor: **`LessonSection` tablosu var ama kullanılmıyor.**

### 2.1 Şema eklemesi

```
LessonSection += DepthTier   { Core, Context, Deep }      // derinlik katmanı
LessonSection += SectionKind { Explain, Example, Trap,    // blok türü
                               LiveContext, Source }
```

`Lesson.BodyMarkdown` **korunur** → `Core/Explain` bölümü yoksa geriye dönük
fallback olarak render edilir (mevcut 5 ders kırılmaz).

### 2.2 Derinlik katmanları

| Katman | Kime | Uzunluk | İçerik |
|---|---|---|---|
| **L1 Core** | Herkes | ~150 kelime | Kavram nedir, neden önemli. Jargonsuz. |
| **L2 Context** | Gelişen + İleri | ~300 kelime | Nasıl hesaplanır, ne zaman yanıltır, sınırları. |
| **L3 Deep** | İleri | ~400 kelime | Formül, kenar durum, TR'ye özgü incelik. |

Render kuralı: kullanıcının seviyesine **kadar** olan katmanlar gösterilir
(İleri kullanıcı L1+L2+L3 görür, L1 katlanmış/özet olarak). Alt seviyedeki
kullanıcı L2/L3'ü **"Daha derine in"** açılır bloğuyla isteğe bağlı açabilir —
**tavan kapatılmaz**, sadece varsayılan değişir.

### 2.3 Blok türleri (derinlikten dik)

Her ders, derinlik katmanlarından bağımsız olarak şu blokları taşır:

1. **Explain** — anlatım (katmanlı)
2. **Example (jenerik)** — statik, herkes için aynı, güvenli sayılar
3. **Trap** — yaygın yanlış anlama / davranışsal tuzak
4. **LiveContext** — "Senin portföyünde" (§3)
5. **Source** — sayılar nereden geldi (`14` §4-A3 / T7.9 şeffaflığı)

---

## 3. Canlı Veri Bağlamı — "Senin portföyünde"

### 3.1 Sözleşme

Her ders ihtiyaç duyduğu metrikleri **bildirir**; backend deterministik olarak
çözer. LLM bu sayıları **üretmez** (`CLAUDE.md` §3.1).

```
Lesson += RequiredContextKeys : ContextKey[]
```

| ContextKey | Kaynak (mevcut kodda hazır) | Örnek kullanım |
|---|---|---|
| `concentration_top2` | `AnonymizedPortfolioSummary.ConcentrationTop2` | Çeşitlendirme dersi |
| `real_return_12m` | `PortfolioSummary.RealReturnRatio` | Reel getiri dersi |
| `asset_class_weights` | `PortfolioSummary.Allocation[]` | Ağırlık dersi |
| `cash_weight` | `AnonymizedPortfolioSummary.CashWeight` | Nakit/likidite |
| `bes_state_share` | `AnonymizedBesBreakdown.StateShare` | BES dersi |
| `holding_count` | `AnonymizedPortfolioSummary.HoldingCount` | Yoğunlaşma |
| `inflation_12m` | TÜİK TÜFE (dış) | Enflasyon dersi |
| `price_change_12m` | Fiyat geçmişi (Faz 5) | Bileşik getiri |

> Not: bu metriklerin **tamamı zaten hesaplanıyor** — `PortfolioAnonymizer`
> ve `PortfolioService` içinde. Yeni hesap yazılmıyor, mevcut çıktı bağlanıyor.

### 3.2 Üç durumlu render (zorunlu)

**KARAR (2026-07-19): onboarding sırası = (c) demo portföyle eğitim → sonra
kendi verisine geçiş.** Yeni kullanıcı eğitime portföy girmeden başlar; canlı
bağlam blokları **açıkça etiketlenmiş demo portföy** üzerinden çalışır.

| Durum | Koşul | Davranış |
|---|---|---|
| `Own` | Kullanıcının gerçek metrikleri mevcut | Gerçek sayılarla kişisel blok |
| `Demo` | Portföy boş / <2 kalem / metrik yok | **Demo portföy** sayılarıyla aynı blok + belirgin "örnek portföy" rozeti + "kendi verinle gör" yönlendirmesi. **Ders kilitlenmez.** |
| `Stale` | Kendi verisi var ama fiyat bayat | Sayı + "şu tarihe ait" damgası |

**Demo modu tasarımın birinci sınıf vatandaşıdır** — dersin pedagojik değeri
(kavramı somut sayıyla görmek) portföyü olmayan kullanıcıda da korunur.

⚠️ **Güven kısıtı:** Demo veri gerçek veriyle **asla karıştırılamaz** olmalıdır
— blok kenarlığı/rozeti farklı, metin "örnek bir portföyde" diye açar, hiçbir
demo sayı kullanıcının kendi özet/pano ekranına sızmaz. Bu, `Demo` durumunun
testidir (SC-E3).

### 3.3 Şablon deseni

Cümle statik, sayı enjekte:

```markdown
### Senin portföyünde
En büyük iki varlığın portföyünün **%{concentration_top2}**'sini oluşturuyor.
Bu derste gördüğümüz *yoğunlaşma* tam olarak bunu ölçer.
```

LLM **isteğe bağlı ikinci katman** olarak yorum paragrafı ekleyebilir — mevcut
`CommentaryPrompts` deseni + tüm guard hattı (`CommentaryOutputGuard`,
`CommentaryLanguageGuard`) yeniden kullanılır. LLM çıkarsa ders yine çalışır.

### 3.4 Canlı veri karşılaştırma kuralları — izin/yasak listesi

| | Örnek |
|---|---|
| ✅ | "Senin yoğunlaşman %84. Bu metrik genelde %60 üzerinde 'yoğun' sayılır." (çerçeve) |
| ✅ | "Son 12 ayda TÜFE %X'ti; portföyün nominal %Y, reel %Z getirdi." (gerçekleşmiş) |
| ✅ | "Bu dersteki formülü kendi rakamınla çalıştırdık: ..." |
| ❌ | "Altın, dolardan daha iyi performans gösterdi." → **enstrüman sıralaması** = zımni yönlendirme |
| ❌ | "Yoğunlaşman yüksek, hisse eklemelisin." → dağılım tavsiyesi |
| ❌ | "Bu oran düzelirse getirin artar." → gelecek tahmini |

**Yeni guard gereksinimi:** mevcut `CommentaryOutputGuard` yönlendirme ve tahmin
kalıplarını yakalıyor; **enstrüman karşılaştırma/sıralama** kalıbı için kural
eklenmeli (örn. "X, Y'den daha iyi/kötü" + varlık adı). Bkz. §7 SC-E5.

---

## 4. Tanılama Testi (eğitim öncesi)

**8 soru, ~90 saniye, atlanabilir** (atlanırsa varsayılan: Başlangıç).
`14` §4-A2'nin "utandırmayan" ilkesi bağlayıcıdır: yanlış cevapta puan/kırmızı
gösterilmez, "şu dersle başlayalım" denir.

### 4.1 Bilgi soruları (4) → `LiteracyLevel`

Nesnel, tek doğru. Örnekler:

1. **Reel getiri sezgisi:** "100 TL'n var. Yıllık faiz %40, enflasyon %50.
   Yıl sonunda alım gücün ne olur?" *(arttı / azaldı / aynı)*
2. **Oran okuma:** "Bir şirketin F/K'sı 8, sektör ortalaması 20. Bu tek başına
   ne söyler?" *(ucuz olabilir / kesin ucuz / kârı düşük olabilir — birden fazla
   makul; doğru cevap "tek başına yeterli değil")*
3. **Çeşitlendirme:** "Tüm paran tek bir varlıkta. Bu neyi artırır?"
4. **Bileşik etki:** "Yıllık %20 getiren birikim 3 yılda kaça katlanır?"
   *(1.6x / 1.7x / 2x — yaklaşık)*

Skor → `Başlangıç (0-1)` · `Gelişen (2-3)` · `İleri (4)`

### 4.2 Risk tutumu soruları (4) → `RiskAttitude`

Senaryo tabanlı, **doğru cevap yok**, puanlanmaz:

1. "Portföyün bir ayda %20 düştü. İlk tepkin?" *(satarım / beklerim / eklerim)*
2. "Bir tanıdığın 3 ayda %200 kazandığı bir yatırımdan bahsediyor. Ne
   hissedersin?" *(kaçırdım / merak ederim / şüphelenirim)*
3. "Bu parayı ne zaman kullanmayı düşünüyorsun?" *(1 yıl / 1-5 yıl / 5+ yıl)*
4. "Değeri yarıya inen bir varlığı elde tutma sebebin ne olurdu?"

Çıktı: `Temkinli` · `Dengeli` · `Atılgan` — **yalnızca ders vurgusu için**
(§1.1 kısıtı) ve **kullanıcıya hiç gösterilmez** (karar 2026-07-19). Test
sonunda kullanıcı yalnızca "şu dersle başlayalım" yönlendirmesi görür; hangi
tutum sınıfına düştüğü bilgisi arayüze çıkmaz.

### 4.3 Şema

```
Users += LiteracyLevel : LessonLevel?     (null = ölçülmemiş)
Users += RiskAttitude  : RiskAttitude?    (null = ölçülmemiş)
Users += ProfiledAtUtc : DateTime?
```

Yeniden ölçüm istenebilir (profil dondurulmaz). `LiteracyLevel` ayrıca
**ustalıkla yükselir** (§5).

---

## 5. Uyarlanabilirlik: sabit profil değil, öğrenen profil

Tek seferlik test yeterli değildir — kullanıcı öğrendikçe derinlik artmalıdır.

```
UserConceptMastery (yeni tablo)
  UserId, ConceptTagId, MasteryScore (0-100), LastSeenAtUtc
```

- Quiz sonucu ilgili `ConceptTag`'lerin `MasteryScore`'unu günceller.
- Bir kavramda ustalık yüksekse → o kavramın L1 katmanı katlanmış gelir
  (tekrar okutmayız), L2/L3 açık gelir.
- `LastSeenAtUtc` **aralıklı tekrar** için: uzun süre görülmemiş kavram, ilgili
  ders açıldığında kısa hatırlatma bloğu olarak yeniden yüzeye çıkar.

Bu, `LiteracyLevel`'ı global bir etiketten **kavram bazlı bir haritaya**
dönüştürür — "haritayı okumayı öğretir" konumlandırmasıyla tutarlı.

---

## 6. Müfredat: 4 set, 17 ders

Mevcut 5 ders korunur, üzerine inşa edilir. (`14` §4-A1'deki 8 derslik ilk
liste bu yapıya dağıtıldı.)

### Set 1 — Temeller (5 ders · mevcut · Başlangıç)
1. Enflasyon ve Reel Getiri · 2. Çeşitlendirme · 3. F/K, PD/DD ·
4. Risk ve Getiri · 5. Bileşik Getiri

### Set 2 — Portföyünü Okumak (4 ders · Başlangıç→Gelişen)
6. Ağırlık ve yoğunlaşma *(`concentration_top2`)*
7. Maliyet ortalaması / kademeli alım *(`cost_basis`)*
8. Kur etkisi ve çoklu para birimi *(`asset_class_weights`)*
9. Getiriyi doğru ölçmek — nominal vs reel, dönem seçimi *(`real_return_12m`)*

### Set 3 — Davranış (4 ders · `RiskAttitude` sırayı belirler)
10. Kayıptan kaçınma · 11. FOMO ve sürü davranışı ·
12. Çıpalama ve maliyet takıntısı · 13. Aşırı işlem ve gizli maliyetler

### Set 4 — Türkiye Gerçekleri (4 ders · Gelişen→İleri)
14. BES'i doğru kullanmak *(`bes_state_share`)*
15. Altın kültürü — gram/çeyrek/22 ayar, düğün altını
16. Enflasyon ortamında birikim *(`inflation_12m`)*
17. Fon okuma — TEFAS, gider oranı *(T7.5 bağımlı)*

> **KARAR (2026-07-19): vergi dersi kapsam DIŞI.** Taslakta 18. ders olarak
> önerilen "Maliyet ve vergi farkındalığı" çıkarıldı — mali müşavirlik alanına
> temas ediyor ve `CLAUDE.md` §2'nin savunduğu net sınırı bulanıklaştırıyor.
> İleride gündeme gelirse `14` §6 hukuk merceğinden geçmesi şarttır.

---

## 7. Test Senaryoları (`09` §5'e eklenecek)

| ID | Senaryo |
|---|---|
| SC-E1 | Başlangıç kullanıcı ders açar → yalnız L1 render; "Daha derine in" ile L2 açılır |
| SC-E2 | İleri kullanıcı aynı dersi açar → L1 katlanmış, L2+L3 açık |
| SC-E3 | Portföyü boş kullanıcı → `Demo`: demo portföy sayıları + belirgin "örnek portföy" rozeti; ders kilitlenmez; **demo sayı kullanıcının pano/özet ekranına sızmaz** |
| SC-E4 | **`RiskAttitude` hiçbir dağılım/portföy çıktısına sızmaz VE hiçbir API yanıtında/arayüzde görünmez** (kod + çıktı taraması) |
| SC-E5 | LLM ders yorumu enstrüman sıralaması üretirse guard kartı düşürür |
| SC-E6 | Quiz geçilince ilgili `ConceptTag` `MasteryScore` artar; L1 katlanır |
| SC-E7 | Tanılama atlanır → `LiteracyLevel=null` → Başlangıç gibi davranılır, hata yok |
| SC-E8 | `LiveContext` sayıları deterministik: aynı portföy → aynı çıktı (LLM'siz) |
| SC-E9 | Bayat fiyat → `Stale` damgası görünür |
| SC-E10 | IDOR: A kullanıcısının `MasteryScore`/profili B'ye sızmaz |

---

## 8. Görev Kırılımı (`08-BACKLOG.md` Faz 6'ya işlendi)

Mevcut T6.1–T6.4 korunmuş, yeni işler T6.5'ten devam etmiştir.

| ID | Görev | Bağımlılık |
|---|---|---|
| T6.1 | *(mevcut, genişletildi)* İlk 5 dersin **L1/L2/L3** gövdeleri + jenerik örnek + tuzak blokları | T5E.2 |
| T6.2 | *(mevcut, genişletildi)* "Senin portföyünde" bağlam API'si → **`LessonContextService`**: `ContextKey` → deterministik değer, **3 durum** (`Own/Demo/Stale`) | T5E.3, T1.7 |
| T6.3 | *(mevcut)* Kavram sözlüğü | T5E.4 |
| T6.4 | *(mevcut)* İlerleme mekaniği: rozet + streak | T5E.4 |
| T6.5 | **Katmanlı içerik şeması:** `LessonSection.DepthTier/SectionKind` + migration + geriye dönük `BodyMarkdown` fallback | T5E.1 |
| T6.6 | **Tanılama testi** (8 soru: 4 bilgi + 4 senaryo) + `Users.LiteracyLevel/RiskAttitude/ProfiledAtUtc`; atlanabilir; `RiskAttitude` **arayüze çıkmaz** — ⚠ **eski T7.1'i devralır** | T6.5 |
| T6.7 | **Uyarlanabilir render** (web): seviyeye göre katman + "Daha derine in" (tavan kapatılmaz) | T6.5, T6.6 |
| T6.8 | `MiniMarkdown` genişletme: **tablo + link** (hâlâ `dangerouslySetInnerHTML` YOK) | — |
| T6.9 | `UserConceptMastery` + quiz→ustalık akışı + aralıklı tekrar | T6.6 |
| T6.10 | **Eğitim demo bağlam portföyü** (karar 1c): salt-okunur örnek portföy + belirgin rozet; demo sayı kendi pano/özetine sızmaz | T6.2 |
| T6.11 | Set 2 içerikleri — Portföyünü Okumak (4 ders) | T6.1 |
| T6.12 | Set 3 içerikleri — Davranış (4 ders) + `RiskAttitude` sıralaması | T6.6 |
| T6.13 | Set 4 içerikleri — Türkiye Gerçekleri (4 ders; vergi hariç) | T6.11 |
| T6.14 | LLM ders yorumu katmanı (opsiyonel) + **enstrüman-sıralama guard'ı** | T6.2 |

**Plan etkisi:** T7.1 (okuryazarlık profili) → **T6.6 olarak Faz 6'ya taşındı**,
çünkü uyarlanabilir derinliğin ön koşuludur. Faz 7'de yerine referans satırı
bırakıldı. T7.7 (demo/misafir modu) Faz 7'de kalır; T6.10 onun eğitim-içi
dar dilimidir.

---

## 9. Kararlar (2026-07-19 · ürün sahibi)

| # | Soru | Karar | Gerekçe |
|---|---|---|---|
| 1 | Onboarding sırası | **(c) Demo portföyle eğitim → sonra kendi verisi** | Ders pedagojik değerini portföysüz kullanıcıda da korur; portföy girişi ön koşul olmaz |
| 2 | `RiskAttitude` görünürlüğü | **Görünmesin** | Görünür etiket "yatırımcı sınıflandırması" olarak okunur (yerindelik çağrışımı); ayrıca kullanıcıyı kutuya hapseder |
| 3 | Vergi dersi (18) | **Alınmasın** | Mali müşavirlik alanına temas ediyor; §2 sınırını bulanıklaştırır |

---

*Kaynak vizyon: `14` §4-A1/A2/A4 · Şema: `03` §E · Uçlar: `04` §7.5 ·
Yasal kısıt: `CLAUDE.md` §2, `01` NFR-2*
