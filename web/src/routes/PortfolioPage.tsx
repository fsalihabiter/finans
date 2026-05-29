import { formatCurrency, formatPercent } from "@finans/shared";

/**
 * Portföy sayfası — Faz 0 yer tutucu. Gerçek HeroCard/Donut/Holdings T1.11+'da.
 * `@finans/shared/format`'in web'de çalıştığını da gösterir (token + format bağı).
 */
export function PortfolioPage() {
  // Örnek sayılar yalnızca format util'ini sergilemek için (taslak değerleri).
  return (
    <section>
      <h1>Portföy</h1>
      <p className="muted">
        Faz 0 iskeleti. Varlık özeti, dağılım grafiği ve holdings tablosu Faz 1'de
        gelir.
      </p>
      <div className="demo-figures">
        <div>
          <span className="muted">Toplam değer</span>
          <strong>{formatCurrency(641403)}</strong>
        </div>
        <div>
          <span className="muted">Net kâr</span>
          <strong className="pos">+{formatCurrency(218433)}</strong>
        </div>
        <div>
          <span className="muted">Getiri</span>
          <strong className="pos">{formatPercent(0.516)}</strong>
        </div>
      </div>
    </section>
  );
}
