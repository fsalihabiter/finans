import { afterEach, describe, expect, it, vi } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import { renderWithProviders } from "../test/renderWithProviders";
import { PerformancePage } from "./PerformancePage";

const summary = {
  baseCurrency: "TRY",
  totalValue: 260000,
  totalCost: 181851,
  netProfit: 78149,
  returnRatio: 0.43,
  realReturnRatio: null,
  allocation: [{ assetType: "Gold", name: "Altın", value: 260000, weight: 1 }],
  asOf: "2026-05-30T00:00:00Z",
};

const holdings = [
  {
    id: "h1", assetType: "Gold", name: "Altın", symbol: null, currency: "TRY", unit: "gram",
    quantity: 40, avgCost: 4546, currentPrice: 6500, totalCost: 181851, currentValue: 260000,
    profit: 78149, returnRatio: 0.43, weight: 1, bes: null,
  },
];

function mockApi() {
  vi.stubGlobal(
    "fetch",
    vi.fn((url: string) => {
      const body = url.includes("/api/holdings") ? holdings : summary;
      return Promise.resolve({ ok: true, status: 200, json: async () => body } as Response);
    }),
  );
}

afterEach(() => vi.restoreAllMocks());

describe("PerformancePage", () => {
  it("dönem sekmelerini ve kalem getiri çubuğunu (gerçek veri) gösterir", async () => {
    mockApi();
    renderWithProviders(<PerformancePage />);

    expect(screen.getByRole("heading", { name: "Performans" })).toBeInTheDocument();

    // Dönem seçici (Tümü varsayılan seçili)
    await waitFor(() => {
      const tumu = screen.getByRole("button", { name: "Tümü", pressed: true });
      expect(tumu).toBeInTheDocument();
    });

    // Kalem bazında getiri bölümü + gerçek veriden getiri (+%43,0 birden çok yerde)
    expect(screen.getByText(/Kalem Bazında Getiri/)).toBeInTheDocument();
    expect(screen.getAllByText(/\+%43,0/).length).toBeGreaterThan(0);
  });
});
