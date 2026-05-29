# 05 — Mobil Şartname (Mobile Spec)

> `portfoy-uygulamasi-taslak.html` taslağından **ekran ekran türetilmiş**
> şartname. Görsel kaynak: `.claude/skills/run-finans-prototype/shots/` (render
> için bkz. `run-finans-prototype/SKILL.md`). Tasarım token'ları `DESIGN.md`'de;
> burada **davranış, veri bağlama ve bileşen sözleşmesi** var.

> 📌 **Sıra notu:** Mobil, **web'den sonra** gelen koldur (birincil yüzey web —
> [`13-WEB-FRONTEND.md`](13-WEB-FRONTEND.md)). Bu şartnamedeki ekran/akış/davranış
> mantığı önce web'de hayata geçer; mobil aynı API + `@finans/shared` paketini
> (tip/token/format) paylaşarak uygular. Tasarım dili (`DESIGN.md`) iki yüzeyde
> ortak; düzen platforma özel.

---

## 1. Navigasyon İskeleti

```
TabNavigator (bottom, 4 sekme + ortada FAB)
├── Portföy   (PortfolioScreen)        [varsayılan]
├── Analiz    (AnalysisScreen)
├── (FAB +)   → AddAssetSheet (modal/overlay, alttan kayar)
├── Hisse     (StockScreen)
└── Eğitim    (EducationScreen)

Overlay'ler (alttan yukarı kayan tam ekran sheet):
├── AssetDetailSheet  (holding satırına dokununca)
└── AddAssetSheet     (FAB'a dokununca)
```

- Aktif sekme **altın** (`--gold`), pasif **muted-2**. (`DESIGN.md` § 5.)
- Sekme geçişinde hafif fade + yukarı kayma (`DESIGN.md` § 7).
- Overlay açılış: `translateY(100%→0)`, `cubic-bezier(.22,.9,.3,1)`.

> RN karşılığı: `@react-navigation/bottom-tabs` + ortadaki FAB için özel
> `tabBar`. Overlay'ler `@react-navigation` modal stack veya
> `react-native-reanimated` bottom-sheet.

---

## 2. Tema Token'ları (önce bunu kur — Faz 0)

`mobile/src/theme/colors.ts` (DESIGN.md § 2'den):
```ts
export const colors = {
  bg: '#14110D', panel: '#1C1813', panel2: '#241F18', line: '#322B22',
  gold: '#E0B255', goldSoft: '#CAA05A',
  mint: '#5FC9A0', coral: '#E58E6E',
  text: '#F3EDE2', muted: '#A89C89', muted2: '#6F6557',
  usd: '#7FB7D6', bes: '#B98AD9', cash: '#9CA7A0',
} as const;
```
- `typography.ts`: Fraunces (başlık/değer, 600), Hanken Grotesk (gövde).
  Sayılar **tabular-nums** (`fontVariant: ['tabular-nums']`).
- `spacing.ts`: kart radius 20-26, ekran yatay padding 20, kartlar arası 11-13.
- Fontlar `expo-font` ile yüklenir (Fraunces, Hanken Grotesk).

> **Kural:** Renk/font/boşluk **sadece** bu token'lardan gelir. Hiçbir bileşende
> hardcoded hex yok (DESIGN.md § 6 önerisi).

---

## 3. Ekran: Portföy (`PortfolioScreen`)

**Veri:** `GET /api/portfolio/summary` + `GET /api/holdings` (react-query).
**Görsel:** `shots/portfoy.png`.

| Bileşen | İçerik | Veri kaynağı | Not |
|---------|--------|--------------|-----|
| `Greeting` | "İyi akşamlar, Yatırımcı 👋" | saat bazlı statik | Faz 5'te kullanıcı adı |
| `HeroCard` | toplam değer, net kâr pill, (maliyet/net kâr/getiri) | `summary.totalValue/netProfit/returnRatio/totalCost` | kâr+ → `mint`, − → `coral` + ok |
| `AllocationDonut` | halka + ortada varlık sayısı + legend (% ile) | `summary.allocation[]` | **`react-native-svg`** (conic-gradient yok). Renk: type→token |
| `HoldingRow` (liste) | ikon, ad, alt bilgi, değer, getiri% | `holdings[]` | dokun → `AssetDetailSheet` |
| `Nudge` | eğitici not | Faz 2: `GET /nudges`, Faz 1: statik/yok | altın tonlu kutu |

**Etkileşim:** pull-to-refresh → summary+holdings yeniden çek (Faz 2 canlı fiyat).
Holding satırına dokun → ilgili `holdingId` ile detay sheet açılır.

**`HoldingRow` props sözleşmesi:**
```ts
type HoldingRowProps = {
  icon: string;            // emoji veya asset ikonu
  iconBg: string;          // token rengi (.15 opaklık)
  name: string;            // "Altın"
  subtitle: string;        // "40 gr · ort. 4.546 ₺/gr"
  valueText: string;       // formatCurrency(currentValue)
  returnText: string | null; // "+%43,0" | null (nakit)
  trend: 'up' | 'down' | 'flat';
  onPress: () => void;
};
```

---

## 4. Ekran: Analiz (`AnalysisScreen`)

**Veri:** Faz 3 → `GET /api/portfolio/commentary`. Faz 1-2'de placeholder/kural.
**Görsel:** `shots/analiz.png`.

| Bileşen | İçerik | Not |
|---------|--------|-----|
| `AiHeader` | orb ikon + "Portföy Yorumu" + tarih | |
| `Disclaimer` | **"yatırım tavsiyesi değildir"** | **HER ZAMAN görünür** (NFR-2) |
| `AnalysisCard` (n adet) | emoji + başlık + paragraf; ops. `meter` veya `tagrow` | `commentary.cards[]` |

**`AnalysisCard` `commentary` kart şemasıyla birebir** (bkz. `04` § 6):
```ts
type AnalysisCard = {
  emoji: string; title: string; body: string;
  meter?: { value: number; lowLabel: string; highLabel: string };
  tags?: string[];
};
```
- LLM yanıtı gelene kadar **skeleton/loading** (yanıt birkaç saniye sürebilir).
- Parse/LLM hatası → fallback metin, ekran çökmez (FR-3.2).

> **Sınır:** Bu ekrandaki hiçbir metin "al/sat/yükselir" diyemez. Korkuluklar
> backend prompt'unda (`07`), ama mobil de disclaimer'ı **kaldıramaz**.

---

## 5. Ekran: Hisse (`StockScreen`)

**Veri:** Faz 4 → `GET /api/stocks/{symbol}/metrics` + `/explain`.
**Görsel:** `shots/hisse.png`.

| Bileşen | İçerik | Not |
|---------|--------|-----|
| `SearchBar` | sembol arama | Faz 4 |
| `StockHeader` | logo, ad, sembol, fiyat, değişim% | `metrics` |
| `MetricGrid` (2x2) | F/K, PD/DD, temettü verimi, kâr büyümesi + sektör etiketi | `metrics.metrics` + `sectorContext` |
| `AiHeader` + `Disclaimer` | "Bu rakamlar ne anlatıyor?" + tavsiye değil | |
| `AnalysisCard` (n) | metrik açıklamaları | `/explain` (commentary şeması) |

- Sektör etiketi rengi: `above/high` → `coral`, `positive` → `mint`,
  `low/neutral` → `muted`.
- Her metrikte `?` ipucu (dokun → kısa açıklama tooltip/sheet).

---

## 6. Ekran: Eğitim (`EducationScreen`)

**Veri:** Faz 5 (içerik). Faz 1-4'te statik liste kabul.
**Görsel:** `shots/egitim.png`.

| Bileşen | İçerik | Not |
|---------|--------|-----|
| `ProgressTrack` | tamamlanan/sıradaki/kilitli segmentler | |
| `LessonRow` (liste) | numara, başlık, açıklama, süre, durum | durum: ✓/sırada/🔒 |

> Analiz/hisse kartlarından ilgili derse **derin bağlantı** (örn. "Çeşitlendirme"
> dersine git) ileride değerlidir.

---

## 7. Overlay: Varlık Detayı (`AssetDetailSheet`)

**Veri:** `GET /api/holdings/{id}`. **Görsel:** `shots/overlay-detail.png`.

| Bileşen | İçerik |
|---------|--------|
| `DetailHero` | ikon, güncel değer, kâr/zarar (tutar + %) |
| `DetailRow` (liste) | Miktar, Ort. maliyet, Güncel fiyat, Toplam maliyet, Portföy ağırlığı |
| `Nudge` | varlığa özel eğitici not (Faz 2+) |

- Üstte geri (`←`) butonu → sheet kapanır.
- BES detayında **devlet katkısı ayrı satır** (FR-1.5).

---

## 8. Overlay: Varlık Ekle (`AddAssetSheet`)

**Aksiyon:** `POST /api/holdings`. **Görsel:** `shots/overlay-add.png`.

| Alan | Tip | Doğrulama |
|------|-----|-----------|
| Varlık türü | chip seçimi (Altın/Döviz/Hisse/Fon/BES/Nakit) | zorunlu |
| Para birimi | chip (TRY/USD/EUR) | zorunlu |
| Miktar | sayı girişi | > 0 |
| Ortalama maliyet (birim başına) | sayı girişi | ≥ 0 (nakitte gizlenebilir) |
| Alış tarihi | tarih seçici | opsiyonel |

- Kaydet → `POST /api/holdings` (ilk transaction ile). Başarıda sheet kapanır,
  portföy listesi invalidate edilir (react-query).
- Hata → form üstünde TR mesaj (API hata sözleşmesi, `04` § 2).
- **Tür → alan görünürlüğü:** Nakit'te "ort. maliyet" gizli; Hisse'de sembol
  arama görünür (Faz 4 ile entegre).

---

## 9. Ortak Bileşen Envanteri

| Bileşen | Kullanıldığı ekran | Öncelik (faz) |
|---------|--------------------|---------------|
| `Disclaimer` | Analiz, Hisse | 3/4 — ama erken yaz, her yere lazım |
| `AnalysisCard` | Analiz, Hisse | 3 |
| `Nudge` | Portföy, Detay | 2 |
| `HeroCard` / `DetailHero` | Portföy / Detay | 1 |
| `AllocationDonut` | Portföy | 1 |
| `HoldingRow` | Portföy | 1 |
| `Chip` (seçilebilir) | AddAsset | 1 |
| `MetricCard` + `MetricGrid` | Hisse | 4 |
| `formatCurrency` / `formatPercent` util | her yer | 0 (önce bunu yaz) |

---

## 10. Formatlama Kuralı (NFR-7) — tek yerden

```ts
// utils/format.ts
export const formatCurrency = (v: number, cur = '₺') =>
  new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 0, maximumFractionDigits: 2 })
    .format(v) + ' ' + cur;            // 641403 → "641.403 ₺"

export const formatPercent = (ratio: number) =>
  '%' + new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 1, maximumFractionDigits: 1 })
    .format(ratio * 100);              // 0.516 → "%51,6"
```
> Backend ham sayı (`0.516`) döner; **mobil formatlar**. Hesap mobilde **yok**.

---

## 11. Erişilebilirlik (DESIGN.md § 8)

- Dokunma hedefi ≥ 44dp.
- Kâr/zarar **renk + ok** birlikte (renk tek başına anlam taşımaz).
- Metin kontrastı korunur (sıcak beyaz / koyu zemin).
