import { afterEach, describe, expect, it, vi } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { renderWithProviders } from "../test/renderWithProviders";
import { HoldingsPage } from "./HoldingsPage";

const summary = {
  baseCurrency: "TRY",
  totalValue: 260000,
  totalCost: 181851,
  netProfit: 78149,
  returnRatio: 0.43,
  realReturnRatio: null,
  allocation: [{ assetType: "Gold", name: "Altın", value: 260000, weight: 1 }],
  asOf: "2026-07-12T00:00:00Z",
};

const holdings = [
  {
    id: "h1", assetType: "Gold", name: "Altın (gram)", symbol: "XAU", currency: "TRY",
    unit: "gram", quantity: 40, avgCost: 4546.275, currentPrice: 6500, totalCost: 181851,
    currentValue: 260000, profit: 78149, returnRatio: 0.43, weight: 1, bes: null,
  },
];

function mockApi(list: unknown[] = holdings) {
  vi.stubGlobal(
    "fetch",
    vi.fn((url: string) => {
      const body: unknown = url.includes("/api/holdings") ? list : summary;
      return Promise.resolve({ ok: true, status: 200, json: async () => body } as Response);
    }),
  );
}

afterEach(() => vi.restoreAllMocks());

const renderPage = () =>
  renderWithProviders(
    <MemoryRouter>
      <HoldingsPage />
    </MemoryRouter>,
  );

describe("HoldingsPage (Varlıklarım — kullanıcı isteği 2026-07-12)", () => {
  it("pozisyon tablosunu ve Varlık Ekle butonunu gösterir", async () => {
    mockApi();
    renderPage();

    expect(screen.getByRole("heading", { name: "Varlıklarım" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Varlık Ekle/ })).toBeInTheDocument();

    await waitFor(() => expect(screen.getByText(/Altın \(gram\)/)).toBeInTheDocument());
    expect(screen.getByText(/1 pozisyon/)).toBeInTheDocument();
  });

  it("boş portföyde ekleme CTA'lı boş durum gösterir", async () => {
    mockApi([]);
    renderPage();

    await waitFor(() =>
      expect(screen.getByText("Portföyün henüz boş")).toBeInTheDocument());
    expect(screen.getByRole("button", { name: /İlk varlığını ekle/ })).toBeInTheDocument();
  });
});
