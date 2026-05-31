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

function row(prices: PriceDto[]) {
  return (
    <>
      {prices.map((p) => (
        <span className={`tk-item${p.stale ? " stale" : ""}`} key={`${p.kind}-${p.currency}`}>
          <span className="tk-ic" aria-hidden="true">{iconFor(p)}</span>
          <span className="tk-k">{labelFor(p)}</span>
          <b className="tnum">{formatCurrency(p.price, p.quoteCurrency)}</b>
          {p.stale && <span className="tk-stale">~yaklaşık</span>}
        </span>
      ))}
      <span className="tk-src">{SOURCE_NOTE}</span>
    </>
  );
}

/**
 * Canlı fiyat kayan-yazısı (T2.6+, kullanıcı isteği): altın/döviz değerleri + kaynak,
 * kesintisiz akar (içerik iki kez → CSS marquee). Hover'da durur; reduced-motion'da
 * akış kapanır (a11y). `stale` → "~yaklaşık". Salt gösterim.
 */
export function PriceTicker({ prices }: { prices: PriceDto[] }) {
  if (prices.length === 0) return null;

  return (
    <div className="ticker" aria-label="Canlı fiyatlar">
      <div className="ticker-track">
        <div className="ticker-seq">{row(prices)}</div>
        <div className="ticker-seq" aria-hidden="true">{row(prices)}</div>
      </div>
    </div>
  );
}
