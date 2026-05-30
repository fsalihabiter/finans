import { formatCurrency, formatPercent } from "@finans/shared";
import type { PortfolioSummary } from "@finans/shared";

function tone(v: number | null): string {
  if (v === null || v === 0) return "";
  return v > 0 ? "up" : "down";
}

/** Üst KPI şeridi (taslak referansı): hero toplam değer + maliyet + net kâr + getiri. */
export function KpiGrid({
  summary,
  positionCount,
}: {
  summary: PortfolioSummary;
  positionCount: number;
}) {
  const { baseCurrency, totalValue, totalCost, netProfit, returnRatio, realReturnRatio } = summary;
  const profitSign = netProfit > 0 ? "+" : "";

  return (
    <div className="kpis">
      <div className="kpi hero">
        <div className="k">Toplam Portföy Değeri</div>
        <div className="v tnum">{formatCurrency(totalValue, baseCurrency)}</div>
        <div className={`sub tnum ${tone(netProfit)}`}>
          {profitSign}
          {formatCurrency(netProfit, baseCurrency)}
          {returnRatio !== null && ` · ${formatPercent(returnRatio)}`}
        </div>
      </div>

      <div className="kpi">
        <div className="k">Toplam Maliyet</div>
        <div className="v tnum">{formatCurrency(totalCost, baseCurrency)}</div>
        <div className="sub muted">{positionCount} pozisyon</div>
      </div>

      <div className="kpi">
        <div className="k">Net Kâr / Zarar</div>
        <div className={`v tnum ${tone(netProfit)}`}>
          {profitSign}
          {formatCurrency(netProfit, baseCurrency)}
        </div>
        <div className={`sub ${tone(netProfit)}`}>tüm zamanlar</div>
      </div>

      <div className="kpi">
        <div className="k">Getiri</div>
        <div className={`v tnum ${tone(returnRatio)}`}>
          {returnRatio === null ? "—" : formatPercent(returnRatio)}
        </div>
        <div className="sub muted tnum">
          {realReturnRatio === null ? "reel —" : `reel ${formatPercent(realReturnRatio)}`}
        </div>
      </div>
    </div>
  );
}
