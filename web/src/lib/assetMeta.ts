import type { AssetType, CurrencyCode } from "@finans/shared";

/** Varlık türü → görsel kimlik (ikon + renk). Donut, tablo, detay ve ekleme formunda ortak. */
export const ASSET_META: Record<AssetType, { icon: string; color: string; label: string }> = {
  Gold: { icon: "🪙", color: "#E0B255", label: "Altın" },
  Fx: { icon: "💵", color: "#A8C36A", label: "Döviz" },
  Stock: { icon: "📈", color: "#6EA8FE", label: "Hisse" },
  Fund: { icon: "📊", color: "#3FB9CE", label: "Fon" },
  Bes: { icon: "🏦", color: "#B98AD9", label: "BES" },
  Cash: { icon: "💰", color: "#9CA7A0", label: "Nakit" },
};

export const CURRENCY_COLOR: Record<CurrencyCode, string> = {
  TRY: "#E0B255",
  USD: "#6EA8FE",
  EUR: "#B98AD9",
};

/** rgba arka plan (ikon kutusu için) — meta rengini düşük opaklıkta verir. */
export function softBg(color: string, alpha = 0.15): string {
  const m = color.replace("#", "");
  const r = parseInt(m.slice(0, 2), 16);
  const g = parseInt(m.slice(2, 4), 16);
  const b = parseInt(m.slice(4, 6), 16);
  return `rgba(${r},${g},${b},${alpha})`;
}
