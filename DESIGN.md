# DESIGN.md — Tasarım Rehberi

> Onaylanan v0 taslağının tasarım sistemi. Renkler, tipografi ve bileşen
> kalıpları burada. React Native'e taşırken § 6'daki eşleme notlarına bak
> (CSS'teki bazı şeyler RN'de farklı çalışır).

---

## 1. Tasarım Felsefesi

- **Ton:** Sıcak, güven veren, editöryel-zarif. Soğuk/kurumsal "Wall Street"
  değil; öğrenmek isteyen birine yakın duran, premium ama davetkâr.
- **Tema:** Koyu (dark), sıcak kömür/koyu kahve zemin + altın vurgu.
  Altın hem temaya (portföydeki altın varlığı) hem yatırım çağrışımına uyar.
- **Yaklaşım:** Rafine minimalizm. Bolca nefes alanı, net hiyerarşi, abartısız
  ama özenli mikro-detaylar. "AI slop" jenerik görünümden kaçın.

---

## 2. Renk Paleti (CSS değişkenleri)

```css
--bg:        #14110D;   /* ana zemin (sıcak kömür)           */
--panel:     #1C1813;   /* kart yüzeyi                        */
--panel-2:   #241F18;   /* hover / ikincil yüzey              */
--line:      #322B22;   /* kenarlık / ayraç                   */

--gold:      #E0B255;   /* birincil vurgu (altın)             */
--gold-soft: #CAA05A;   /* yumuşak altın                      */

--mint:      #5FC9A0;   /* pozitif / kâr (yeşil)              */
--coral:     #E58E6E;   /* negatif / zarar (mercan)           */

--text:      #F3EDE2;   /* birincil metin (sıcak beyaz)       */
--muted:     #A89C89;   /* ikincil metin                      */
--muted-2:   #6F6557;   /* en soluk metin / placeholder       */

/* Varlık sınıfı renkleri (grafik & rozetler) */
--usd:       #7FB7D6;   /* dolar / döviz (mavi)               */
--bes:       #B98AD9;   /* BES (mor)                          */
--cash:      #9CA7A0;   /* nakit (gri-yeşil)                  */
/* altın için --gold kullanılır */
```

**Kullanım kuralı:** Pozitif sayılar `--mint`, negatif `--coral`, vurgular
`--gold`. Renk yoğunluğunu az tut; baskın koyu zemin + keskin altın aksanlar.

---

## 3. Tipografi

İki font, Google Fonts (ikisi de Türkçe karakter destekler):

```
Display / başlıklar : 'Fraunces'        (serif, karakterli, opsz 9..144)
Gövde / sayılar      : 'Hanken Grotesk'  (grotesk, temiz, tabular nums)
```

- Büyük değerler ve başlıklar **Fraunces 600**, hafif negatif letter-spacing.
- Tüm metin gövdesi **Hanken Grotesk**.
- **Sayılar her yerde `font-variant-numeric: tabular-nums`** — finansal
  rakamların hizalı durması için şart.

Ölçek (taslaktaki kullanım):
| Eleman              | Font     | Boyut/Ağırlık       |
|---------------------|----------|---------------------|
| Hero değer          | Fraunces | 38px / 600          |
| Ekran başlığı (h3)  | Fraunces | 18px / 600          |
| Kart başlığı        | Hanken   | 14px / 700          |
| Gövde metni         | Hanken   | 13px / 400-500      |
| Etiket / muted      | Hanken   | 11-12px / 600       |

---

## 4. Şekil / Boşluk / Gölge Token'ları

```
Köşe yarıçapı:
  kart           : 20-26px
  küçük rozet/ic : 13-14px
  pill / chip    : 100px (tam yuvarlak) veya 13px
Boşluk:
  ekran iç padding : 20px yatay
  kartlar arası    : 11-13px
  bölüm üstü       : 26px
Gölge:
  hero kart : 0 18px 40px -22px rgba(0,0,0,.9)
  fab/orb   : altın renkli yumuşak glow
```

Atmosfer: zeminde iki radial-gradient (sağ üstte sıcak altın halesi, sol altta
soğuk yeşil) ile derinlik. Düz tek renk zeminden kaçın.

---

## 5. Bileşen Kalıpları

- **Hero kart:** Toplam değer + net kâr pill'i + alt satırda maliyet/kâr/getiri
  üçlüsü. Sağ üstte altın radial hale.
- **Dağılım (donut):** Conic-gradient halka + ortada özet. Yanında renk
  noktalı legend.
- **Holding satırı:** `[ikon] [ad + alt bilgi] [değer + getiri%]`. Dokununca
  detay ekranı açılır.
- **Nudge (bilgilendirme):** Altın tonlu, ampul ikonlu küçük eğitici kutu.
  Bağlama duyarlı ipuçları için.
- **Analiz kartı (acard):** Emoji + başlık + paragraf; bazılarında ölçek
  çubuğu (meter) veya etiket satırı (tagrow).
- **Disclaimer (disc):** Kesik kenarlıklı, "tavsiye değildir" çerçevesi —
  analiz ve hisse ekranlarında her zaman görünür.
- **Hisse metrikleri:** 2x2 grid; her metrikte değer + sektöre göre etiket
  (yüksek/düşük/pozitif).
- **Alt navigasyon:** 4 sekme (Portföy, Analiz, Hisse, Eğitim) + ortada altın
  FAB (varlık ekle). Aktif sekme altın renkli.
- **Overlay:** Detay ve "varlık ekle" alttan yukarı kayan tam ekran katman.

---

## 6. React Native'e Taşıma Notları ⚠️

CSS taslağındaki bazı şeyler RN'de **birebir çalışmaz**. Eşlemeler:

| Web (CSS)                    | React Native karşılığı                         |
|------------------------------|------------------------------------------------|
| `conic-gradient` (donut)     | `react-native-svg` ile çiz veya hazır grafik kütüphanesi (`react-native-gifted-charts` / `victory-native`) |
| `linear/radial-gradient`     | `expo-linear-gradient` veya `react-native-linear-gradient` |
| `backdrop-filter: blur`      | `@react-native-community/blur` (BlurView)      |
| CSS değişkenleri (`:root`)   | JS'te bir `theme.ts` / `colors.ts` token dosyası |
| `box-shadow`                 | iOS: `shadow*`; Android: `elevation` (ayrı ayrı)|
| `:hover`                     | RN'de hover yok → `Pressable` + `onPressIn` durumları |
| `position:fixed/absolute`    | RN'de `absolute` var, `fixed` yok              |
| px birimleri                 | RN'de birimsiz sayı (dp)                        |
| Google Fonts `<link>`        | `expo-font` ile fontları yükle (Fraunces, Hanken Grotesk) |

**Öneri:** Renk/tipografi/boşluk token'larını `mobile/src/theme/` altında tek
bir dosyada topla; tüm bileşenler oradan beslensin. Böylece tema tek yerden
yönetilir (web taslağındaki `:root` mantığının RN karşılığı).

---

## 7. Etkileşim / Animasyon

- Ekran geçişlerinde hafif fade + yukarı kayma (taslaktaki `fade` animasyonu).
- Overlay'ler alttan yukarı `cubic-bezier(.22,.9,.3,1)` ile kayar.
- Holding satırına dokununca hafif sağa kayma + yüzey rengi değişimi.
- Abartma; tek iyi kurgulanmış giriş animasyonu, dağınık mikro-etkileşimlerden
  iyidir.

---

## 8. Erişilebilirlik

- Metin kontrastını koru (sıcak beyaz `--text` koyu zeminde yeterli).
- Dokunma hedefleri en az ~44px.
- Renk tek başına anlam taşımasın (kâr/zarar için renk + ok işareti birlikte).
