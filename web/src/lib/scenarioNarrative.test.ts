import { describe, expect, it } from "vitest";
import { buildScenarioNarrative } from "./scenarioNarrative";
import type { ScenarioComparison } from "@finans/shared";

function scenario(overrides: Partial<ScenarioComparison["summary"]>): ScenarioComparison {
  return {
    holdingId: "h1",
    name: "Altın (gram)",
    assetType: "Gold",
    baseCurrency: "TRY",
    points: [],
    summary: {
      currentValue: 273924.2,
      invested: 206144.8,
      difference: 67779.4,
      differenceRatio: 0.3288,
      inflationAdjustedInvested: 346835.49,
      annualInflationRate: 0.38,
      ...overrides,
    },
    firstDate: "2024-11-29",
    asOf: "2026-07-12T00:00:00Z",
  };
}

describe("buildScenarioNarrative (T5.4)", () => {
  it("kârda + eşik altında: nominal önde ama alım gücü azalmış — iki mesaj birden", () => {
    const text = buildScenarioNarrative(scenario({}));

    expect(text).toContain("₺206.144,80 yatırdın");
    expect(text).toContain("bugünkü değeri ₺273.924,20");
    expect(text).toContain("nominal ₺67.779,40 (+%32,9) önde");
    expect(text).toContain("eşiğin ALTINDA");
    expect(text).toContain("alım gücü azalmış");
    // Kalıcı çerçeve: tavsiye/tahmin değil (CLAUDE.md §2).
    expect(text).toContain("al-sat önerisi değildir");
  });

  it("eşiğin üzerindeyse alım gücünü korumuş der", () => {
    const text = buildScenarioNarrative(
      scenario({ inflationAdjustedInvested: 250000 }),
    );
    expect(text).toContain("eşiğin ÜZERİNDE");
    expect(text).toContain("alım gücünü korumuş");
  });

  it("zarardaysa nakde göre geride der", () => {
    const text = buildScenarioNarrative(
      scenario({ currentValue: 180000, difference: -26144.8, differenceRatio: -0.1268 }),
    );
    expect(text).toContain("nominal ₺26.144,80 (%-12,7) geride");
  });

  it("enflasyon verisi yoksa eşik cümlesi kurulmaz", () => {
    const text = buildScenarioNarrative(
      scenario({ inflationAdjustedInvested: null, annualInflationRate: null }),
    );
    expect(text).not.toContain("Alım gücü eşiği");
    expect(text).toContain("al-sat önerisi değildir"); // çerçeve her koşulda
  });
});
