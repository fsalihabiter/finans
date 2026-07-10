/**
 * Nirengi marka glyph'i: yükselen nirengi/grafik çizgisi (4 düğüm — çık → tepe →
 * geri çekilme → daha yüksek zirve). `.brand-mark` (indigo gradient kutu — v2 Gece)
 * içinde koyu glyph olarak görünür; favicon ile aynı şekil. Glyph kutuyu dolduracak
 * boyutta (iç dolgu az). Bkz. [[brand-name-nirengi]].
 */
export function BrandMark() {
  return (
    <svg viewBox="0 0 64 64" width="32" height="32" aria-hidden="true">
      <path
        d="M11 53 L26 29 L37 43 L53 11"
        fill="none"
        stroke="#0B0F1E"
        strokeWidth="5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <g fill="#0B0F1E">
        <circle cx="11" cy="53" r="5.5" />
        <circle cx="26" cy="29" r="5.5" />
        <circle cx="37" cy="43" r="5.5" />
        <circle cx="53" cy="11" r="5.5" />
      </g>
    </svg>
  );
}
