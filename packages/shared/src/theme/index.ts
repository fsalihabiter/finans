// Tasarım token'ları (DESIGN.md §2-4). TEK KAYNAK: web ve mobil buradan beslenir.
// Web `cssVariables()` ile :root değişkenleri üretir; mobil (RN) `tokens`'ı
// doğrudan JS objesi olarak kullanır (DESIGN.md §6).

// ── Nirengi v2 — "Gece" (Modern Dark / Cinema) ────────────────────────────────
// ui-ux-pro-max tasarım sistemi turu (2026-07-10): koyu gece mavisi zemin + indigo
// vurgu + ambient glow. Saf siyah YOK (OLED smear). Tüm metin/panel çiftleri
// WCAG doğrulamalı (accent 6.0:1, muted 6.9:1). v1 (kömür+altın) git geçmişinde.
export const tokens = {
  color: {
    bg: "#0B0F1E", // ana zemin (gece mavisi — saf siyah değil)
    panel: "#131A30", // kart yüzeyi
    panel2: "#1A2240", // hover / ikincil yüzey
    line: "#26304F", // hairline kenarlık / ayraç (dekoratif)
    lineStrong: "#42507E", // form kontrolü kenarlığı (girdi sınırı görünür olmalı)
    accent: "#8A94DC", // birincil vurgu (sakin indigo — CTA, aktif nav, linkler (desatüre: parlak mavi göz yoruyordu)
    accentSoft: "#A6AEE8", // yumuşak indigo (hover / ikincil vurgu)
    mint: "#45D5A2", // pozitif / kâr
    coral: "#F97F7F", // negatif / zarar
    text: "#EEF2FF", // birincil metin (soğuk beyaz)
    textSoft: "#CBD5F0", // yumuşak gövde metni (kart içi paragraf)
    muted: "#97A3C9", // ikincil metin
    muted2: "#6B7699", // en soluk metin / placeholder (panel üstünde ≥3.8:1)
    // Kategorik varlık/para birimi renkleri — grafik + rozet + ikon kutusu tek kaynağı.
    // Hepsi panel üstünde ≥6.5:1 taşır; ton ayrımı donut'ta yan yana okunur.
    gold: "#E4C06A", // Altın varlığı (kategorik — artık birincil vurgu DEĞİL)
    usd: "#66C7EA", // USD (camgöbeği)
    eur: "#9BAAF3", // EUR (periwinkle) — BES morundan ve USD'den ayrık
    fx: "#A3CE6E", // döviz varlık sınıfı (dolar yeşili)
    stock: "#4FA3F7", // hisse (gök mavisi) — accent indigosundan ayrık
    fund: "#38CFC4", // fon (turkuaz)
    bes: "#C08AE8", // BES (mor)
    cash: "#94A0B8", // nakit (soğuk gri)
  },
  font: {
    // 'Space Grotesk Variable' / 'Inter Variable' = @fontsource-variable (web);
    // mobil (expo-font) statik adları kullanır. İkisi de latin-ext (Türkçe) destekler.
    display: "'Space Grotesk Variable', 'Space Grotesk', 'Segoe UI', system-ui, sans-serif",
    body: "'Inter Variable', 'Inter', system-ui, -apple-system, 'Segoe UI', sans-serif",
  },
  radius: {
    card: "22px",
    badge: "14px",
    pill: "100px",
  },
  space: {
    screenX: "20px",
    cardGap: "12px",
    sectionTop: "26px",
  },
  shadow: {
    hero: "0 18px 40px -22px rgba(0,0,0,.9)",
  },
} as const;

export type Tokens = typeof tokens;

/** camelCase / sayı sınırlarını kebab'a çevirir: goldSoft→gold-soft, panel2→panel-2. */
function kebab(key: string): string {
  return key
    .replace(/([a-zA-Z])([0-9])/g, "$1-$2")
    .replace(/([a-z])([A-Z])/g, "$1-$2")
    .toLowerCase();
}

/**
 * Token'lardan CSS değişken bildirimleri üretir (web). Renkler ham (`--bg`),
 * diğer gruplar öneklidir (`--font-*`, `--radius-*`, `--space-*`, `--shadow-*`).
 */
export function cssVariables(selector = ":root"): string {
  const lines: string[] = [];
  for (const [k, v] of Object.entries(tokens.color)) lines.push(`  --${kebab(k)}: ${v};`);
  for (const [k, v] of Object.entries(tokens.font)) lines.push(`  --font-${kebab(k)}: ${v};`);
  for (const [k, v] of Object.entries(tokens.radius)) lines.push(`  --radius-${kebab(k)}: ${v};`);
  for (const [k, v] of Object.entries(tokens.space)) lines.push(`  --space-${kebab(k)}: ${v};`);
  for (const [k, v] of Object.entries(tokens.shadow)) lines.push(`  --shadow-${kebab(k)}: ${v};`);
  return `${selector} {\n${lines.join("\n")}\n}`;
}
