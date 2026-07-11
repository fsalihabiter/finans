import { formatCurrency, formatNumber, formatPercent } from "@finans/shared";
import type { CurrencyCode } from "@finans/shared";
import { useCountUp } from "../lib/useCountUp";

/* Sayaç gösterim ailesi: sayısal değer 0'dan (güncellemede önceki değerden)
   hedefe "sayarak" yükselir (useCountUp) — her sayfa açılışında/yenilemesinde.
   Yalnız GÖSTERİM — ara değerler hiçbir hesaba girmez; testte hedef anında görünür. */

/** Para değeri sayaçla yüklenir; her karede tr-TR formatCurrency ile biçimlenir. */
export function CountUpCurrency({ value, currency }: { value: number; currency: CurrencyCode }) {
  return <>{formatCurrency(useCountUp(value), currency)}</>;
}

/** Yüzde/oran sayaçla yükselir; parametreler formatPercent ile birebir aynı. */
export function CountUpPercent({
  value,
  fractionDigits = 1,
  asRatio = true,
  signed = true,
}: {
  value: number;
  fractionDigits?: number;
  asRatio?: boolean;
  signed?: boolean;
}) {
  return <>{formatPercent(useCountUp(value), fractionDigits, asRatio, signed)}</>;
}

/** Düz sayı (örn. pozisyon adedi) sayaçla yükselir; tr-TR formatNumber biçimi. */
export function CountUpNumber({
  value,
  maxFractionDigits = 0,
}: {
  value: number;
  maxFractionDigits?: number;
}) {
  return <>{formatNumber(useCountUp(value), maxFractionDigits)}</>;
}
