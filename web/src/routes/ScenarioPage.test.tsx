import { afterEach, describe, expect, it, vi } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import { renderWithProviders } from "../test/renderWithProviders";
import { ScenarioPage } from "./ScenarioPage";

const holdings = [
  {
    id: "h-gold", assetType: "Gold", name: "Altın (gram)", symbol: "XAU", currency: "TRY",
    unit: "gram", quantity: 40, avgCost: 4546.275, currentPrice: 6500, totalCost: 181851,
    currentValue: 260000, profit: 78149, returnRatio: 0.43, weight: 0.9, bes: null,
  },
  {
    // Nakit seçenek DIŞI kalmalı (nakitte-nakit karşılaştırması anlamsız).
    id: "h-cash", assetType: "Cash", name: "Nakit (TL)", symbol: null, currency: "TRY",
    unit: "TRY", quantity: 6025, avgCost: 1, currentPrice: 1, totalCost: 6025,
    currentValue: 6025, profit: 0, returnRatio: 0, weight: 0.1, bes: null,
  },
];

const scenario = {
  holdingId: "h-gold",
  name: "Altın (gram)",
  assetType: "Gold",
  baseCurrency: "TRY",
  points: [
    { date: "2024-06-01", value: 181851, cost: 181851, inflationAdjustedCost: 181851 },
    { date: "2026-07-12", value: 260000, cost: 181851, inflationAdjustedCost: 250000 },
  ],
  summary: {
    currentValue: 260000,
    invested: 181851,
    difference: 78149,
    differenceRatio: 0.4297,
    inflationAdjustedInvested: 250000,
    annualInflationRate: 0.38,
  },
  firstDate: "2024-06-01",
  asOf: "2026-07-12T00:00:00Z",
};

function mockApi() {
  vi.stubGlobal(
    "fetch",
    vi.fn((url: string) => {
      let body: unknown = holdings;
      if (url.includes("/api/portfolio/scenario/")) body = scenario;
      return Promise.resolve({ ok: true, status: 200, json: async () => body } as Response);
    }),
  );
}

afterEach(() => vi.restoreAllMocks());

describe("ScenarioPage (T5.4)", () => {
  it("pozisyon seçici çipleri gösterir; nakit seçenek dışıdır", async () => {
    mockApi();
    renderWithProviders(<ScenarioPage />);

    expect(screen.getByRole("heading", { name: "Senaryo" })).toBeInTheDocument();

    await waitFor(() =>
      expect(screen.getByRole("button", { name: /Altın \(gram\)/ })).toBeInTheDocument());
    expect(screen.queryByRole("button", { name: /Nakit/ })).not.toBeInTheDocument();
  });

  it("karşılaştırma özetini, üç çizgili grafiği ve eğitici çerçeveyi gösterir", async () => {
    mockApi();
    const { container } = renderWithProviders(<ScenarioPage />);

    // Özet kartları: bugünkü değer, nakitte dursaydı, fark, alım gücü eşiği.
    await waitFor(() => expect(screen.getByText("Bugünkü değeri")).toBeInTheDocument());
    expect(screen.getAllByText(/260\.000/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/181\.851/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/250\.000/).length).toBeGreaterThan(0);
    expect(screen.getByText(/yıllık %38,0 enflasyonla/)).toBeInTheDocument();

    // Grafik üç çizgiyle çizilir (değer + kesikli yatırılan + noktalı eşik).
    expect(container.querySelector(".spark-line")).not.toBeNull();
    expect(container.querySelector(".vh-cost-line")).not.toBeNull();
    expect(container.querySelector(".sc-threshold-line")).not.toBeNull();

    // Kalıcı disclaimer (CLAUDE.md §2) + "geçmiş, tahmin değil" çerçevesi.
    expect(screen.getByText(/yatırım tavsiyesi değildir/i)).toBeInTheDocument();
    expect(screen.getByText(/gelecek performansın göstergesi\s+değildir/)).toBeInTheDocument();

    // Sayıların metin okuması (narrative) — deterministik şablon, tavsiye içermez.
    expect(screen.getByText(/pozisyonuna bugüne dek .* yatırdın/)).toBeInTheDocument();
    expect(screen.getByText(/al-sat önerisi değildir/)).toBeInTheDocument();
  });
});
