// Türkçe (tr-TR) gösterim biçimleyicileri (NFR-7).
// Hesap DEĞİL — backend ham/decimal sayıyı verir, burada yalnızca biçimlenir.
// Yuvarlama yalnızca gösterimde; ham değer hiç değiştirilmez.

import type { CurrencyCode } from "../types/index";

const CURRENCY_FRACTION_DIGITS = 2;

/**
 * Sade sayı (miktar vb.) tr-TR biçimi: binlik `.`, ondalık `,`, sondaki sıfırlar atılır.
 * Örn. formatNumber(2000) → "2.000" · formatNumber(40.5) → "40,5"
 */
export function formatNumber(value: number, maxFractionDigits = 4): string {
  return new Intl.NumberFormat("tr-TR", { maximumFractionDigits: maxFractionDigits }).format(value);
}

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
 * ISO tarihi tr-TR <b>noktalı sayısal</b> biçimde: "01.03.2026" (gg.aa.yyyy). Geçersizse "—".
 * Tüm projede tek tarih gösterim biçimi (NFR-7).
 */
export function formatDate(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime())
    ? "—"
    : new Intl.DateTimeFormat("tr-TR", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        timeZone: "UTC", // tarih takvim-günü; izleyenin saat dilimi günü kaydırmasın
      }).format(d);
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
