// Türkçe (tr-TR) gösterim biçimleyicileri (NFR-7).
// Hesap DEĞİL — backend ham/decimal sayıyı verir, burada yalnızca biçimlenir.
// Yuvarlama yalnızca gösterimde; ham değer hiç değiştirilmez.

import type { CurrencyCode } from "../types/index";

const CURRENCY_FRACTION_DIGITS = 2;

/**
 * Para tutarını tr-TR biçiminde döndürür (binlik `.`, ondalık `,`).
 * Örn. formatCurrency(422970.5, "TRY") → "₺422.970,50"
 */
export function formatCurrency(value: number, currency: CurrencyCode = "TRY"): string {
  return new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency,
    minimumFractionDigits: CURRENCY_FRACTION_DIGITS,
    maximumFractionDigits: CURRENCY_FRACTION_DIGITS,
  }).format(value);
}

/**
 * Oranı yüzde olarak tr-TR biçiminde döndürür.
 * Girdi oran (0.516) ya da hazır yüzde olabilir; `asRatio` ile belirtilir.
 * `signed` true ise pozitifte "+" eklenir (getiri için); ağırlık/oran gösteriminde false.
 * Örn. formatPercent(0.516) → "+%51,6" · formatPercent(0.405, 1, true, false) → "%40,5"
 */
export function formatPercent(
  value: number,
  fractionDigits = 1,
  asRatio = true,
  signed = true,
): string {
  const pct = asRatio ? value * 100 : value;
  const sign = signed && pct > 0 ? "+" : "";
  return `${sign}%${new Intl.NumberFormat("tr-TR", {
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits,
  }).format(pct)}`;
}
