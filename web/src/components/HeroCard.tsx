import { formatCurrency, formatPercent } from "@finans/shared";
import type { PortfolioSummary } from "@finans/shared";

/** Oran değerini biçimler; null ise "—" (hesaplanamadı). */
function ratioText(value: number | null): string {
  return value === null ? "—" : formatPercent(value);
}

function toneClass(value: number | null): string {
  if (value === null || value === 0) return "";
  return value > 0 ? "pos" : "neg";
}

/**
 * Portföyün üst kartı (13 §4): büyük toplam değer + net kâr / getiri / reel getiri /
 * maliyet. Tüm sayılar backend'den ham gelir; burada yalnızca tr-TR biçimlenir (NFR-7).
 */
export function HeroCard({ summary }: { summary: PortfolioSummary }) {
  const { baseCurrency, totalValue, netProfit, totalCost, returnRatio, realReturnRatio } = summary;
  const profitSign = netProfit > 0 ? "+" : "";

  return (
    <section className="hero-card" aria-label="Portföy özeti">
      <div className="hero-main">
        <span className="hero-label">Toplam değer</span>
        <strong className="hero-value">{formatCurrency(totalValue, baseCurrency)}</strong>
      </div>
      <dl className="hero-stats">
        <div>
          <dt>Net kâr</dt>
          <dd className={toneClass(netProfit)}>
            {profitSign}
            {formatCurrency(netProfit, baseCurrency)}
          </dd>
        </div>
        <div>
          <dt>Getiri</dt>
          <dd className={toneClass(returnRatio)}>{ratioText(returnRatio)}</dd>
        </div>
        <div>
          <dt>Reel getiri</dt>
          <dd className={toneClass(realReturnRatio)}>{ratioText(realReturnRatio)}</dd>
        </div>
        <div>
          <dt>Toplam maliyet</dt>
          <dd>{formatCurrency(totalCost, baseCurrency)}</dd>
        </div>
      </dl>
    </section>
  );
}
