import { describe, it, expect } from "vitest";
import { formatCurrency, formatDate, formatPercent } from "./index";

describe("formatDate (tr-TR noktalı)", () => {
  it("ISO tarihi gg.aa.yyyy biçiminde döndürür", () => {
    expect(formatDate("2026-03-01T00:00:00Z")).toBe("01.03.2026");
    expect(formatDate("2025-08-25T10:00:00Z")).toBe("25.08.2025");
  });

  it("geçersiz tarihte — döndürür", () => {
    expect(formatDate("yok")).toBe("—");
  });
});

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
