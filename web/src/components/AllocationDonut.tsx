import { formatPercent } from "@finans/shared";
import type { AllocationSlice, CurrencyCode } from "@finans/shared";
import { ASSET_META } from "../lib/assetMeta";

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

  let acc = 0;
  const segments = allocation.map((slice) => {
    const length = slice.weight * CIRC;
    const dashoffset = -acc;
    acc += length;
    return { slice, length, dashoffset };
  });

  const label = allocation.map((s) => `${s.name} ${formatPercent(s.weight, 1, true, false)}`).join(", ");

  return (
    <div className="alloc">
      <svg className="alloc-donut" viewBox="0 0 160 160" role="img" aria-label={label}>
        <g transform="translate(80,80) rotate(-90)">
          <circle r={R} fill="none" stroke="var(--line, #322b22)" strokeWidth={STROKE} />
          {segments.map(({ slice, length, dashoffset }) => (
            <circle
              key={slice.assetType + slice.name}
              r={R}
              fill="none"
              stroke={ASSET_META[slice.assetType].color}
              strokeWidth={STROKE}
              strokeDasharray={`${length} ${CIRC - length}`}
              strokeDashoffset={dashoffset}
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
        {allocation.map((slice) => (
          <li key={slice.assetType + slice.name}>
            <span className="alloc-swatch" style={{ background: ASSET_META[slice.assetType].color }} />
            <span className="alloc-name">{slice.name}</span>
            <span className="alloc-weight tnum">{formatPercent(slice.weight, 1, true, false)}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
