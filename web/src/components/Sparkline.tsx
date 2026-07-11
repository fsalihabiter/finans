import { useId } from "react";

/**
 * Mini değer serisi grafiği (sparkline): çizgi soldan sağa çizilerek belirir
 * (pathLength=1 + stroke-dashoffset, App.css .spark-line), altındaki gradyan
 * dolgu gecikmeli fade ile gelir (.spark-area). Dekoratif — sayılar metinde
 * zaten var, o yüzden aria-hidden.
 */
export function Sparkline({
  values,
  width = 240,
  height = 56,
  stroke = "var(--accent, #8a94dc)",
}: {
  values: number[];
  width?: number;
  height?: number;
  stroke?: string;
}) {
  const gradientId = useId();
  if (values.length < 2) return null;

  const min = Math.min(...values);
  const max = Math.max(...values);
  const span = max - min || 1;
  const pad = 3;
  const points = values.map((v, i) => ({
    x: pad + (i * (width - 2 * pad)) / (values.length - 1),
    y: height - pad - ((v - min) / span) * (height - 2 * pad),
  }));
  const line = points
    .map((p, i) => `${i === 0 ? "M" : "L"}${p.x.toFixed(1)} ${p.y.toFixed(1)}`)
    .join(" ");
  const area = `${line} L${points[points.length - 1].x.toFixed(1)} ${height} L${points[0].x.toFixed(1)} ${height} Z`;

  return (
    <svg
      className="sparkline"
      viewBox={`0 0 ${width} ${height}`}
      aria-hidden="true"
      focusable="false"
    >
      <defs>
        <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={stroke} stopOpacity="0.3" />
          <stop offset="100%" stopColor={stroke} stopOpacity="0" />
        </linearGradient>
      </defs>
      <path className="spark-area" d={area} fill={`url(#${gradientId})`} />
      <path
        className="spark-line"
        d={line}
        fill="none"
        stroke={stroke}
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        pathLength={1}
      />
    </svg>
  );
}
