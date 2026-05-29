// Tasarım token'ları (DESIGN.md). İSKELET — T0.9'da DESIGN.md'den tam doldurulur
// (renk/tipografi/aralık token'ları + CSS değişken üretimi). Web ve mobil tüketir.

/** Token sözlüğü tipi — T0.9'da gerçek değerlerle genişler. */
export interface ThemeTokens {
  color: Record<string, string>;
  space: Record<string, string>;
  font: Record<string, string>;
}

/** Geçici boş iskelet; T0.9'da DESIGN.md token'larıyla doldurulacak. */
export const tokens: ThemeTokens = {
  color: {},
  space: {},
  font: {},
};
