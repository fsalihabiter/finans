import { formatCurrency } from "@finans/shared";
import type { PriceDto } from "@finans/shared";

/** Enstrümanın kullanıcıya görünen etiketi (altın → "Gram altın", döviz → kod). */
function label(p: PriceDto): string {
  return p.kind === "Gold" ? "Gram altın" : p.currency;
}

/**
 * Canlı fiyat çipleri (altın/döviz) — salt gösterim (T2.6). `stale` ise değer son
 * bilinen ("yaklaşık"). Fiyatlar `quoteCurrency` (Faz 2: TRY) cinsinden biçimlenir.
 */
export function LivePrices({ prices }: { prices: PriceDto[] }) {
  if (prices.length === 0) return null;

  return (
    <div className="price-chips" aria-label="Canlı fiyatlar">
      {prices.map((p) => (
        <span key={`${p.kind}-${p.currency}`} className={`price-chip${p.stale ? " stale" : ""}`}>
          <span className="pc-k">{label(p)}</span>
          <b className="tnum">{formatCurrency(p.price, p.quoteCurrency)}</b>
          {p.stale && (
            <span className="pc-stale" title="Son bilinen fiyat (canlı kaynak ulaşılamadı)">
              ~yaklaşık
            </span>
          )}
        </span>
      ))}
    </div>
  );
}
