// Tasarım token'ları (DESIGN.md §2-4). TEK KAYNAK: web ve mobil buradan beslenir.
// Web `cssVariables()` ile :root değişkenleri üretir; mobil (RN) `tokens`'ı
// doğrudan JS objesi olarak kullanır (DESIGN.md §6).

export const tokens = {
  color: {
    bg: "#14110D", // ana zemin (sıcak kömür)
    panel: "#1C1813", // kart yüzeyi
    panel2: "#241F18", // hover / ikincil yüzey
    line: "#322B22", // kenarlık / ayraç
    gold: "#E0B255", // birincil vurgu (altın)
    goldSoft: "#CAA05A", // yumuşak altın
    mint: "#5FC9A0", // pozitif / kâr
    coral: "#E58E6E", // negatif / zarar
    text: "#F3EDE2", // birincil metin (sıcak beyaz)
    muted: "#A89C89", // ikincil metin
    muted2: "#6F6557", // en soluk metin / placeholder
    usd: "#7FB7D6", // dolar / döviz (mavi)
    bes: "#B98AD9", // BES (mor)
    cash: "#9CA7A0", // nakit (gri-yeşil)
  },
  font: {
    // 'Fraunces Variable' = @fontsource-variable (web); 'Fraunces' = mobil expo-font.
    display: "'Fraunces Variable', 'Fraunces', Georgia, 'Times New Roman', serif",
    body: "'Hanken Grotesk Variable', 'Hanken Grotesk', system-ui, -apple-system, sans-serif",
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
