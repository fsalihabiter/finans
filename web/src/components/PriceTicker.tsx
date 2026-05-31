import { formatCurrency } from "@finans/shared";
import type { PriceDto } from "@finans/shared";

const SOURCE_NOTE = "Kaynak: Frankfurter (döviz) · Truncgil (altın)";

function labelFor(p: PriceDto): string {
  return p.kind === "Gold" ? "Gram altın" : p.currency;
}

function iconFor(p: PriceDto): string {
  if (p.kind === "Gold") return "🪙";
  return p.currency === "USD" ? "💵" : "💶";
}

/**
 * Topbar'a gömülü kompakt canlı fiyat şeridi (T2.6+): az sayıda değer olduğu için
 * kutu/kaydırma yok, başlık ile araçlar arasında satır içi durur. Kaynak hover
 * ipucunda (title). `stale` → "~yaklaşık". Salt gösterim.
 */
export function PriceTicker({ prices }: { prices: PriceDto[] }) {
  if (prices.length === 0) return null;

  return (
    <div className="live-prices" aria-label="Canlı fiyatlar" title={SOURCE_NOTE}>
      {prices.map((p) => (
        <span className={`lp${p.stale ? " stale" : ""}`} key={`${p.kind}-${p.currency}`}>
          <span className="lp-ic" aria-hidden="true">{iconFor(p)}</span>
          <span className="lp-k">{labelFor(p)}</span>
          <b className="tnum">{formatCurrency(p.price, p.quoteCurrency)}</b>
          {p.stale && <span className="lp-stale">~yaklaşık</span>}
        </span>
      ))}
    </div>
  );
}
