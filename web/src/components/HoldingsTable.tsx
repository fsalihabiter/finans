import { Link } from "react-router-dom";
import { formatCurrency, formatNumber, formatPercent } from "@finans/shared";
import type { CurrencyCode, Holding } from "@finans/shared";

function tone(value: number | null): string {
  if (value === null || value === 0) return "";
  return value > 0 ? "pos" : "neg";
}

const money = (value: number | null, ccy: CurrencyCode) =>
  value === null ? "—" : formatCurrency(value, ccy);

const ratio = (value: number | null) => (value === null ? "—" : formatPercent(value));

/**
 * Pozisyon tablosu (13 §4). Geniş ekranda gerçek tablo; dar ekranda yatay kaydırma.
 * Birim fiyatlar varlığın pb'sinde, toplulaştırmalar baz pb'de (backend). Hesap yok.
 * Ad hücresi varlık detayına (`/holdings/:id`) bağlanır.
 */
export function HoldingsTable({
  holdings,
  baseCurrency,
}: {
  holdings: Holding[];
  baseCurrency: CurrencyCode;
}) {
  if (holdings.length === 0) return null;

  return (
    <div className="holdings-wrap">
      <table className="holdings-table">
        <thead>
          <tr>
            <th scope="col">Varlık</th>
            <th scope="col" className="num">Miktar</th>
            <th scope="col" className="num">Ort. maliyet</th>
            <th scope="col" className="num">Güncel fiyat</th>
            <th scope="col" className="num">Değer</th>
            <th scope="col" className="num">Kâr</th>
            <th scope="col" className="num">Getiri</th>
            <th scope="col" className="num">Ağırlık</th>
          </tr>
        </thead>
        <tbody>
          {holdings.map((h) => (
            <tr key={h.id}>
              <td>
                <Link to={`/holdings/${h.id}`} className="holding-link">
                  {h.name}
                </Link>
                {h.symbol && <span className="muted holding-symbol"> {h.symbol}</span>}
              </td>
              <td className="num">
                {formatNumber(h.quantity)} <span className="muted">{h.unit}</span>
              </td>
              <td className="num">{formatCurrency(h.avgCost, h.currency)}</td>
              <td className="num">{money(h.currentPrice, h.currency)}</td>
              <td className="num">{money(h.currentValue, baseCurrency)}</td>
              <td className={`num ${tone(h.profit)}`}>{money(h.profit, baseCurrency)}</td>
              <td className={`num ${tone(h.returnRatio)}`}>{ratio(h.returnRatio)}</td>
              <td className="num">{formatPercent(h.weight, 1, true, false)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
