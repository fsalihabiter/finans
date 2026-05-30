import { describe, it, expect } from "vitest";
import { formatCurrency, formatPercent } from "./index";

describe("formatCurrency (tr-TR)", () => {
  it("binlik ayraç nokta, ondalık virgül kullanır", () => {
    // Sembol konumu ICU sürümüne göre değişebilir → sayısal kısmı doğrula.
    expect(formatCurrency(422970.5)).toContain("422.970,50");
  });

  it("tam sayıyı iki ondalıkla gösterir", () => {
    expect(formatCurrency(641403)).toContain("641.403,00");
  });
});

describe("formatPercent (tr-TR)", () => {
  it("oranı yüzdeye çevirir ve işaret ekler", () => {
    expect(formatPercent(0.516)).toBe("+%51,6");
  });

  it("hazır yüzde değerini asRatio=false ile biçimler", () => {
    expect(formatPercent(43, 0, false)).toBe("+%43");
  });

  it("signed=false ile pozitifte işaret eklemez (ağırlık/oran)", () => {
    expect(formatPercent(0.405, 1, true, false)).toBe("%40,5");
  });
});
