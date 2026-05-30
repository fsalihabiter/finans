import { Link } from "react-router-dom";
import { formatCurrency, formatNumber, formatPercent } from "@finans/shared";
import type { CurrencyCode, Holding } from "@finans/shared";
import { ASSET_META, softBg } from "../lib/assetMeta";

function tone(value: number | null): string {
  if (value === null || value === 0) return "";
  return value > 0 ? "up" : "down";
}

const money = (value: number | null, ccy: CurrencyCode) =>
  value === null ? "—" : formatCurrency(value, ccy);

/**
 * Pozisyon tablosu (13 §4): ikon + ad/alt-bilgi, ağırlık çubuğu, renkli getiri.
 * Ad hücresi varlık detayına (`/holdings/:id`) bağlanır. Geniş ekranda tablo,
 * dar ekranda yatay kaydırma.
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
            <th scope="col" className="num">Maliyet</th>
            <th scope="col" className="num">Değer</th>
            <th scope="col" className="num">Ağırlık</th>
            <th scope="col" className="num">Getiri</th>
          </tr>
        </thead>
        <tbody>
          {holdings.map((h) => {
            const meta = ASSET_META[h.assetType];
            return (
              <tr key={h.id}>
                <td>
                  <Link to={`/holdings/${h.id}`} className="holding-link">
                    <div className="asset-cell">
                      <div className="asset-ic" style={{ background: softBg(meta.color) }}>{meta.icon}</div>
                      <div>
                        <div className="asset-nm">
                          {h.name}
                          {h.symbol && <span className="muted"> {h.symbol}</span>}
                        </div>
                        <div className="asset-sub tnum">
                          ort. {formatCurrency(h.avgCost, h.currency)}/{h.unit}
                        </div>
                      </div>
                    </div>
                  </Link>
                </td>
                <td className="num tnum">
                  {formatNumber(h.quantity)} <span className="muted">{h.unit}</span>
                </td>
                <td className="num tnum">{formatCurrency(h.totalCost, baseCurrency)}</td>
                <td className="num tnum">{money(h.currentValue, baseCurrency)}</td>
                <td className="num">
                  <div className="weight-bar">
                    <i style={{ width: `${Math.min(h.weight * 100, 100)}%`, background: meta.color }} />
                  </div>
                </td>
                <td className={`num tnum ${tone(h.returnRatio)}`}>
                  {h.returnRatio === null ? "—" : formatPercent(h.returnRatio)}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
