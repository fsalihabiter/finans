import { tokens } from "@finans/shared";
import type { AssetType, CurrencyCode } from "@finans/shared";

const C = tokens.color;

/** Varlık türü → görsel kimlik (ikon + renk). Donut, tablo, detay ve ekleme formunda ortak.
 *  Renkler @finans/shared token'larından gelir (tek kaynak — bileşende ham hex yok). */
export const ASSET_META: Record<AssetType, { icon: string; color: string; label: string }> = {
  Gold: { icon: "🪙", color: C.gold, label: "Altın" },
  Fx: { icon: "💵", color: C.fx, label: "Döviz" },
  Stock: { icon: "📈", color: C.stock, label: "Hisse" },
  Fund: { icon: "📊", color: C.fund, label: "Fon" },
  Bes: { icon: "🏦", color: C.bes, label: "BES" },
  Cash: { icon: "💰", color: C.cash, label: "Nakit" },
};

export const CURRENCY_COLOR: Record<CurrencyCode, string> = {
  TRY: C.gold,
  USD: C.usd,
  EUR: C.eur,
};

/** rgba arka plan (ikon kutusu için) — meta rengini düşük opaklıkta verir. */
export function softBg(color: string, alpha = 0.15): string {
  const m = color.replace("#", "");
  const r = parseInt(m.slice(0, 2), 16);
  const g = parseInt(m.slice(2, 4), 16);
  const b = parseInt(m.slice(4, 6), 16);
  return `rgba(${r},${g},${b},${alpha})`;
}

/** Rengi beyaza (amount>0) ya da zemine (amount<0) doğru karıştırır — deterministik ton varyantı. */
export function shade(color: string, amount: number): string {
  const m = color.replace("#", "");
  const target = amount >= 0 ? 255 : 20; // koyulaştırma zemin sıcaklığını korur
  const t = Math.abs(amount);
  const mix = (v: number) => Math.round(v + (target - v) * t);
  const [r, g, b] = [0, 2, 4].map((i) => mix(parseInt(m.slice(i, i + 2), 16)));
  const hex = (v: number) => v.toString(16).padStart(2, "0");
  return `#${hex(r)}${hex(g)}${hex(b)}`;
}

/** Aynı varlık türü birden çok dilimde tekrar ediyorsa (örn. iki Fx: USD + EUR)
 *  her tekrara ayırt edilebilir bir ton varyantı verir — donut ve lejant aynı diziyi kullanır. */
const VARIANT_STEPS = [0, 0.22, -0.28, 0.42] as const;

export function sliceColors(slices: ReadonlyArray<{ assetType: AssetType }>): string[] {
  const seen = new Map<AssetType, number>();
  return slices.map(({ assetType }) => {
    const n = seen.get(assetType) ?? 0;
    seen.set(assetType, n + 1);
    return shade(ASSET_META[assetType].color, VARIANT_STEPS[n % VARIANT_STEPS.length]);
  });
}
