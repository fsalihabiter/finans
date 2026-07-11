import { useId } from "react";
import { formatCurrency, formatDate } from "@finans/shared";
import type { CurrencyCode, StockPricePoint } from "@finans/shared";

/**
 * Fiyat geçmişi çizgi grafiği (T4.5) — Sparkline'ın büyük kardeşi: gradyan alan + çizim
 * animasyonu + min/max fiyat ve başlangıç/bitiş tarih etiketleri. Geçmiş gösterimi;
 * tahmin çizgisi/oku YOK (CLAUDE.md §2). Renk dönem değişiminin yönüne göre (bilgi).
 */
export function PriceChart({
  points,
  currency,
  positive = true,
}: {
  points: StockPricePoint[];
  currency: string;
  positive?: boolean;
}) {
  const gradientId = useId();
  if (points.length < 2) return null;

  const width = 900;
  const height = 240;
  const padX = 6;
  const padY = 12;

  const closes = points.map((p) => p.close);
  const min = Math.min(...closes);
  const max = Math.max(...closes);
  const span = max - min || 1;

  const coords = points.map((p, i) => ({
    x: padX + (i * (width - 2 * padX)) / (points.length - 1),
    y: height - padY - ((p.close - min) / span) * (height - 2 * padY),
  }));
  const line = coords
    .map((c, i) => `${i === 0 ? "M" : "L"}${c.x.toFixed(1)} ${c.y.toFixed(1)}`)
    .join(" ");
  const area = `${line} L${coords[coords.length - 1].x.toFixed(1)} ${height} L${coords[0].x.toFixed(1)} ${height} Z`;

  const stroke = positive ? "var(--mint, #45d5a2)" : "var(--coral, #f97f7f)";
  const ccy = currency as CurrencyCode;

  return (
    <figure className="price-chart">
      <svg viewBox={`0 0 ${width} ${height}`} aria-hidden="true" focusable="false" preserveAspectRatio="none">
        <defs>
          <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={stroke} stopOpacity="0.22" />
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
          vectorEffect="non-scaling-stroke"
        />
      </svg>
      <div className="price-chart-scale tnum" aria-hidden="true">
        <span>{formatCurrency(max, ccy)}</span>
        <span>{formatCurrency(min, ccy)}</span>
      </div>
      <figcaption className="price-chart-dates tnum">
        <span>{formatDate(points[0].date)}</span>
        <span>{formatDate(points[points.length - 1].date)}</span>
      </figcaption>
    </figure>
  );
}
