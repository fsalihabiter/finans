import { formatCurrency, formatPercent } from "@finans/shared";
import type { AllocationSlice, AssetType, CurrencyCode } from "@finans/shared";

/** Varlık türü → tasarım token rengi (DESIGN.md §2). */
const ASSET_COLOR: Record<AssetType, string> = {
  Gold: "var(--gold, #e0b255)",
  Fx: "var(--usd, #7fb7d6)",
  Bes: "var(--bes, #b98ad9)",
  Cash: "var(--cash, #9ca7a0)",
  Stock: "var(--mint, #5fc9a0)",
  Fund: "var(--gold-soft, #caa05a)",
};

const R = 60;
const STROKE = 22;
const CIRC = 2 * Math.PI * R;

/**
 * Portföy dağılımı — SVG donut + lejant (13 §4). Her dilim ağırlığı kadar yay
 * kaplar; ağırlık backend'den gelir (weight = değer / toplam). Hesap yok.
 */
export function AllocationDonut({
  allocation,
  baseCurrency,
}: {
  allocation: AllocationSlice[];
  baseCurrency: CurrencyCode;
}) {
  if (allocation.length === 0) return null;

  let acc = 0;
  const segments = allocation.map((slice) => {
    const length = slice.weight * CIRC;
    const dashoffset = -acc;
    acc += length;
    return { slice, length, dashoffset };
  });

  const label = allocation
    .map((s) => `${s.name} ${formatPercent(s.weight, 1, true, false)}`)
    .join(", ");

  return (
    <section className="alloc" aria-label="Varlık dağılımı">
      <svg className="alloc-donut" viewBox="0 0 160 160" role="img" aria-label={label}>
        <g transform="translate(80,80) rotate(-90)">
          <circle r={R} fill="none" stroke="var(--line, #322b22)" strokeWidth={STROKE} />
          {segments.map(({ slice, length, dashoffset }) => (
            <circle
              key={slice.assetType + slice.name}
              r={R}
              fill="none"
              stroke={ASSET_COLOR[slice.assetType]}
              strokeWidth={STROKE}
              strokeDasharray={`${length} ${CIRC - length}`}
              strokeDashoffset={dashoffset}
            />
          ))}
        </g>
      </svg>

      <ul className="alloc-legend">
        {allocation.map((slice) => (
          <li key={slice.assetType + slice.name}>
            <span className="alloc-swatch" style={{ background: ASSET_COLOR[slice.assetType] }} />
            <span className="alloc-name">{slice.name}</span>
            <span className="alloc-weight">{formatPercent(slice.weight, 1, true, false)}</span>
            <span className="alloc-value muted">{formatCurrency(slice.value, baseCurrency)}</span>
          </li>
        ))}
      </ul>
    </section>
  );
}
