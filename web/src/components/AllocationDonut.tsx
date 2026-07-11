import type { CSSProperties } from "react";
import { formatPercent } from "@finans/shared";
import type { AllocationSlice, CurrencyCode } from "@finans/shared";
import { sliceColors } from "../lib/assetMeta";

/* Dilim çizim animasyonu zamanlaması: her dilim, kendinden önceki dilimlerin payı
   kadar bekler ve kendi payı kadar sürede çizilir (linear) → bütün, tek kalemde
   çevre boyunca çizilmiş gibi okunur (App.css @keyframes donut-draw). */
const DRAW_TOTAL_MS = 640;
const DRAW_START_MS = 160;

const R = 60;
const STROKE = 26;
const CIRC = 2 * Math.PI * R;

/**
 * Portföy dağılımı — SVG donut + lejant (13 §4). Her dilim ağırlığı kadar yay kaplar;
 * varlık-türü renkleri ortak `ASSET_META`'dan. Merkezde varlık sayısı.
 */
export function AllocationDonut({
  allocation,
}: {
  allocation: AllocationSlice[];
  baseCurrency?: CurrencyCode;
}) {
  if (allocation.length === 0) return null;

  // Her dilimin başlangıcı, önceki dilimlerin toplam yayı kadar kaydırılır.
  // (Render sırasında değişken mutasyonundan kaçınılır — n küçük, varlık türü ≤ 6.)
  // Aynı türden tekrar eden dilimler (iki Fx gibi) ton varyantıyla ayrışır.
  const colors = sliceColors(allocation);
  const segments = allocation.map((slice, i) => {
    const length = slice.weight * CIRC;
    const dashoffset = -allocation.slice(0, i).reduce((sum, s) => sum + s.weight * CIRC, 0);
    return { slice, length, dashoffset, color: colors[i] };
  });

  const label = allocation.map((s) => `${s.name} ${formatPercent(s.weight, 1, true, false)}`).join(", ");

  return (
    <div className="alloc">
      <svg className="alloc-donut" viewBox="0 0 160 160" role="img" aria-label={label}>
        <g transform="translate(80,80) rotate(-90)">
          <circle r={R} fill="none" stroke="var(--line, #322b22)" strokeWidth={STROKE} />
          {segments.map(({ slice, length, dashoffset, color }) => (
            <circle
              key={slice.assetType + slice.name}
              className="alloc-seg"
              r={R}
              fill="none"
              stroke={color}
              strokeWidth={STROKE}
              strokeDasharray={`${length} ${CIRC - length}`}
              strokeDashoffset={dashoffset}
              style={{
                "--seg-delay": `${Math.round(DRAW_START_MS + (-dashoffset / CIRC) * DRAW_TOTAL_MS)}ms`,
                "--seg-dur": `${Math.max(90, Math.round(slice.weight * DRAW_TOTAL_MS))}ms`,
              } as CSSProperties}
            />
          ))}
        </g>
        <text x="80" y="74" textAnchor="middle" fill="var(--text, #f3ede2)"
          style={{ font: "600 26px var(--font-display, Georgia, serif)" }}>
          {allocation.length}
        </text>
        <text x="80" y="92" textAnchor="middle" fill="var(--muted, #a89c89)"
          style={{ fontSize: "10px", letterSpacing: "0.06em" }}>
          VARLIK
        </text>
      </svg>

      <ul className="alloc-legend">
        {allocation.map((slice, i) => (
          <li key={slice.assetType + slice.name}>
            <span className="alloc-swatch" style={{ background: colors[i] }} />
            <span className="alloc-name">{slice.name}</span>
            <span className="alloc-weight tnum">{formatPercent(slice.weight, 1, true, false)}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
