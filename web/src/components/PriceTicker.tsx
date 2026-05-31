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
 * Canlı fiyat şeridi (T2.6+): altın/döviz değerleri ayrılmış sütunlarda + kaynak etiketi.
 * Az sayıda değer olduğu için **statik** (kaydırma yok → tekrar/karışıklık yok); dar
 * ekranda satır kaydırır. `stale` → "~yaklaşık". Salt gösterim.
 */
export function PriceTicker({ prices }: { prices: PriceDto[] }) {
  if (prices.length === 0) return null;

  return (
    <div className="price-bar" aria-label="Canlı fiyatlar">
      <div className="pb-items">
        {prices.map((p) => (
          <span className={`pb-item${p.stale ? " stale" : ""}`} key={`${p.kind}-${p.currency}`}>
            <span className="pb-ic" aria-hidden="true">{iconFor(p)}</span>
            <span className="pb-k">{labelFor(p)}</span>
            <b className="tnum">{formatCurrency(p.price, p.quoteCurrency)}</b>
            {p.stale && <span className="pb-stale">~yaklaşık</span>}
          </span>
        ))}
      </div>
      <span className="pb-src">{SOURCE_NOTE}</span>
    </div>
  );
}
