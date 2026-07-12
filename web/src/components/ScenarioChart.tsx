import { useId } from "react";
import { formatCurrency, formatDate } from "@finans/shared";
import type { CurrencyCode, ScenarioPoint } from "@finans/shared";
import { useChartHover } from "../lib/useChartHover";

/**
 * Senaryo karşılaştırma grafiği (T5.4) — ÜÇ seri: pozisyonun gerçek değeri (dolgulu ana
 * çizgi) + nakitte dursaydı (yatırılan, kesikli) + alım gücü eşiği (enflasyon düzeltmeli
 * yatırılan, noktalı amber). Çizgiler GÖSTERİR, yorumlamaz — tavsiye/tahmin YOK
 * (CLAUDE.md §2). Hover: tarih + üç serinin değeri.
 */
export function ScenarioChart({
  points,
  currency,
}: {
  points: ScenarioPoint[];
  currency: CurrencyCode;
}) {
  const gradientId = useId();
  const width = 900;
  const height = 260;
  const padX = 6;
  const padY = 12;
  const hover = useChartHover(points.length, padX / width);
  if (points.length < 2) return null;

  const hasThreshold = points.some((p) => p.inflationAdjustedCost != null);

  // Ölçek üç serinin ortak min/max'ı — hepsi kadraja sığar.
  const all = points.flatMap((p) =>
    p.inflationAdjustedCost != null ? [p.value, p.cost, p.inflationAdjustedCost] : [p.value, p.cost]);
  const min = Math.min(...all);
  const max = Math.max(...all);
  const span = max - min || 1;

  const x = (i: number) => padX + (i * (width - 2 * padX)) / (points.length - 1);
  const y = (v: number) => height - padY - ((v - min) / span) * (height - 2 * padY);

  const path = (pick: (p: ScenarioPoint) => number | null | undefined) =>
    points
      .map((p, i) => {
        const v = pick(p);
        return v == null ? null : `${i === 0 ? "M" : "L"}${x(i).toFixed(1)} ${y(v).toFixed(1)}`;
      })
      .filter(Boolean)
      .join(" ");

  const valueLine = path((p) => p.value);
  const costLine = path((p) => p.cost);
  const thresholdLine = hasThreshold ? path((p) => p.inflationAdjustedCost) : "";
  const area = `${valueLine} L${x(points.length - 1).toFixed(1)} ${height} L${x(0).toFixed(1)} ${height} Z`;

  const last = points[points.length - 1];
  const positive = last.value >= last.cost;
  const stroke = positive ? "var(--mint, #45d5a2)" : "var(--coral, #f97f7f)";
  const amber = "var(--amber, #e8b34c)";

  const hovered = hover.index !== null ? points[hover.index] : null;
  const xPct = hover.index !== null ? (x(hover.index) / width) * 100 : 0;
  const tipPct = Math.min(85, Math.max(15, xPct));

  return (
    <figure className="price-chart scenario-chart">
      <div
        className="chart-hover-area"
        onPointerMove={hover.onPointerMove}
        onPointerLeave={hover.onPointerLeave}
      >
        <svg viewBox={`0 0 ${width} ${height}`} aria-hidden="true" focusable="false" preserveAspectRatio="none">
          <defs>
            <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={stroke} stopOpacity="0.2" />
              <stop offset="100%" stopColor={stroke} stopOpacity="0" />
            </linearGradient>
          </defs>
          <path className="spark-area" d={area} fill={`url(#${gradientId})`} />
          {hasThreshold && (
            <path
              className="sc-threshold-line"
              d={thresholdLine}
              fill="none"
              stroke={amber}
              strokeWidth="1.5"
              strokeDasharray="2 5"
              strokeLinecap="round"
              strokeLinejoin="round"
              vectorEffect="non-scaling-stroke"
            />
          )}
          <path
            className="vh-cost-line"
            d={costLine}
            fill="none"
            stroke="var(--muted, #97a3c9)"
            strokeWidth="1.5"
            strokeDasharray="5 5"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
          />
          <path
            className="spark-line"
            d={valueLine}
            fill="none"
            stroke={stroke}
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
            pathLength={1}
            vectorEffect="non-scaling-stroke"
          />
        </svg>

        {hovered && (
          <div className="chart-hover" aria-hidden="true">
            <i className="ch-crosshair" style={{ left: `${xPct}%` }} />
            <i
              className="ch-dot"
              style={{ left: `${xPct}%`, top: `${(y(hovered.value) / height) * 100}%`, background: stroke }}
            />
            <i
              className="ch-dot cost"
              style={{ left: `${xPct}%`, top: `${(y(hovered.cost) / height) * 100}%` }}
            />
            <div className="ch-tip" style={{ left: `${tipPct}%` }}>
              <div className="ch-tip-date tnum">{formatDate(hovered.date)}</div>
              <div className="ch-tip-row">
                <i className="vh-dot value" style={{ background: stroke }} /> Değeri
                <b className="tnum">{formatCurrency(hovered.value, currency)}</b>
              </div>
              <div className="ch-tip-row">
                <i className="vh-dot cost" /> Nakitte dursaydı
                <b className="tnum">{formatCurrency(hovered.cost, currency)}</b>
              </div>
              {hovered.inflationAdjustedCost != null && (
                <div className="ch-tip-row">
                  <i className="vh-dot threshold" /> Alım gücü eşiği
                  <b className="tnum">{formatCurrency(hovered.inflationAdjustedCost, currency)}</b>
                </div>
              )}
            </div>
          </div>
        )}
      </div>

      <div className="price-chart-scale tnum" aria-hidden="true">
        <span>{formatCurrency(max, currency)}</span>
        <span>{formatCurrency(min, currency)}</span>
      </div>
      <figcaption className="price-chart-dates tnum">
        <span>{formatDate(points[0].date)}</span>
        <span className="vh-legend" aria-hidden="true">
          <i className="vh-dot value" style={{ background: stroke }} /> Değeri
          <i className="vh-dot cost" /> Nakitte dursaydı
          {hasThreshold && (
            <>
              <i className="vh-dot threshold" /> Alım gücü eşiği
            </>
          )}
        </span>
        <span>{formatDate(points[points.length - 1].date)}</span>
      </figcaption>
    </figure>
  );
}
