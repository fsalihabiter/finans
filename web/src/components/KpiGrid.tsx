import type { PortfolioSummary } from "@finans/shared";
import { CountUpCurrency, CountUpNumber, CountUpPercent } from "./CountUp";
import { InfoTip } from "./InfoTip";

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
        <div className="v tnum"><CountUpCurrency value={totalValue} currency={baseCurrency} /></div>
        <div className={`sub tnum ${tone(netProfit)}`}>
          <span className="delta-arrow" aria-hidden="true">{netProfit >= 0 ? "▲" : "▼"}</span>
          <span>
            {profitSign}
            <CountUpCurrency value={netProfit} currency={baseCurrency} />
            {returnRatio !== null && (
              <>
                {" · "}
                <CountUpPercent value={returnRatio} />
              </>
            )}
          </span>
        </div>
      </div>

      <div className="kpi">
        <div className="k">Toplam Maliyet</div>
        <div className="v tnum"><CountUpCurrency value={totalCost} currency={baseCurrency} /></div>
        <div className="sub muted"><CountUpNumber value={positionCount} /> pozisyon</div>
      </div>

      <div className="kpi">
        <div className="k">
          Net Kâr / Zarar
          <InfoTip label="Net kâr/zarar">
            Güncel değer eksi toplam maliyet. Henüz satmasan da "kâğıt üstündeki"
            kazanç/kaybını gösterir.
          </InfoTip>
        </div>
        <div className={`v tnum ${tone(netProfit)}`}>
          {profitSign}
          <CountUpCurrency value={netProfit} currency={baseCurrency} />
        </div>
        <div className={`sub ${tone(netProfit)}`}>tüm zamanlar</div>
      </div>

      <div className="kpi">
        <div className="k">
          Getiri
          <InfoTip label="Reel getiri">
            Getiri, paranın yüzde kaç büyüdüğüdür. Reel getiri ise enflasyondan
            arındırılmış halidir — satın alma gücün gerçekte arttı mı azaldı mı.
          </InfoTip>
        </div>
        <div className={`v tnum ${tone(returnRatio)}`}>
          {returnRatio === null ? "—" : <CountUpPercent value={returnRatio} />}
        </div>
        <div className="sub muted tnum">
          {realReturnRatio === null ? (
            "reel —"
          ) : (
            <>
              reel <CountUpPercent value={realReturnRatio} />
            </>
          )}
        </div>
      </div>
    </div>
  );
}
