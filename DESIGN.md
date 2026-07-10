# DESIGN.md — Tasarım Rehberi

> Onaylanan v0 taslağının tasarım sistemi. Renkler, tipografi ve bileşen
> kalıpları burada. React Native'e taşırken § 6'daki eşleme notlarına bak
> (CSS'teki bazı şeyler RN'de farklı çalışır).

---

## 1. Tasarım Felsefesi (v2 — "Gece")

> 🔄 **v2 (2026-07-10):** Tema, ui-ux-pro-max tasarım sistemi turuyla
> **"Modern Dark (Cinema)"** ailesine geçti — gece mavisi zemin + indigo vurgu +
> ambient glow + motion katmanı. v1 (sıcak kömür + altın) git geçmişinde.

- **Ton:** Modern, odaklı, premium fintech. Karanlık sinematik atmosfer;
  öğrenmek isteyen birine yakın duran, teknik ama davetkâr.
- **Tema:** Koyu (dark), gece mavisi zemin + indigo vurgu + yavaş salınan
  ambient ışık lekeleri. **Saf siyah yok** (#000 OLED smear + sertlik).
- **Motion:** Hareket anlam taşır (giriş hiyerarşisi, basılı geri bildirim);
  süsleme değil. 150-300ms mikro, ≤640ms giriş; `Bezier(0.16,1,0.3,1)` easing;
  yalnız transform/opacity; `prefers-reduced-motion` her animasyonu durdurur.
- **Yaklaşım:** Rafine minimalizm. Bolca nefes alanı, net hiyerarşi, abartısız
  ama özenli mikro-detaylar. "AI slop" jenerik görünümden kaçın.

---

## 2. Renk Paleti (CSS değişkenleri)

```css
--bg:          #0B0F1E;   /* ana zemin (gece mavisi — saf siyah DEĞİL) */
--panel:       #131A30;   /* kart yüzeyi                          */
--panel-2:     #1A2240;   /* hover / ikincil yüzey                */
--line:        #26304F;   /* hairline kenarlık / ayraç (dekoratif)*/
--line-strong: #42507E;   /* form kontrolü kenarlığı (görünür)    */

--accent:      #8A94DC;   /* birincil vurgu (sakin indigo) — CTA/nav/link */
--accent-soft: #A6AEE8;   /* yumuşak indigo (hover / ikincil)     */

--mint:        #45D5A2;   /* pozitif / kâr (yeşil)                */
--coral:       #F97F7F;   /* negatif / zarar (mercan)             */

--text:        #EEF2FF;   /* birincil metin (soğuk beyaz)         */
--text-soft:   #CBD5F0;   /* yumuşak gövde metni (kart paragrafı) */
--muted:       #97A3C9;   /* ikincil metin                        */
--muted-2:     #6B7699;   /* en soluk metin / placeholder (≥3.8:1)*/

/* Kategorik varlık/para birimi renkleri (grafik & rozetler & ikon kutuları).
   TEK KAYNAK: packages/shared/src/theme (assetMeta bunlardan beslenir). */
--gold:        #E4C06A;   /* Altın varlığı (kategorik — vurgu DEĞİL) */
--usd:         #66C7EA;   /* USD (camgöbeği)                      */
--eur:         #9BAAF3;   /* EUR (periwinkle)                     */
--fx:          #A3CE6E;   /* döviz varlık sınıfı (dolar yeşili)   */
--stock:       #4FA3F7;   /* hisse (gök mavisi)                   */
--fund:        #38CFC4;   /* fon (turkuaz)                        */
--bes:         #C08AE8;   /* BES (mor)                            */
--cash:        #94A0B8;   /* nakit (soğuk gri)                    */
```

**Kullanım kuralı:** Pozitif sayılar `--mint`, negatif `--coral`, vurgular
`--accent`. Renk yoğunluğunu az tut; baskın gece zemini + keskin indigo
aksanlar + ambient glow (indigo/turkuaz radyal lekeler, `body::before`).

**Kontrast kuralı (WCAG):** metin panel üstünde ≥4.5:1 (`--muted` 6.9:1,
`--accent` metin olarak 6.0:1); grafik/veri renkleri ≥3:1 (kategoriklerin tümü
≥6.5:1). Aynı türden birden çok grafik dilimi ton varyantıyla ayrışır
(`assetMeta.sliceColors`). Kâr/zarar asla yalnız renkle verilmez — her zaman
+/− işareti ve yüzdeyle birlikte (renk körlüğü).

---

## 3. Tipografi

İki font, self-hosted @fontsource-variable (ikisi de latin-ext / Türkçe destekler):

```
Display / başlıklar : 'Space Grotesk'  (geometrik grotesk, teknik karakter)
Gövde / sayılar      : 'Inter'          (nötr grotesk, mükemmel okunurluk, tabular nums)
```

- Büyük değerler ve başlıklar **Space Grotesk 600**, hafif negatif letter-spacing.
- Tüm metin gövdesi **Inter**.
- **Sayılar her yerde `font-variant-numeric: tabular-nums`** — finansal
  rakamların hizalı durması için şart.

Ölçek (taslaktaki kullanım):
| Eleman              | Font          | Boyut/Ağırlık       |
|---------------------|---------------|---------------------|
| Hero değer          | Space Grotesk | 38px / 600          |
| Ekran başlığı (h3)  | Space Grotesk | 18px / 600          |
| Kart başlığı        | Inter         | 14px / 700          |
| Gövde metni         | Inter         | 13px / 400-500      |
| Etiket / muted      | Inter         | 11-12px / 600       |

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
