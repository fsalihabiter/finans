import { afterEach, describe, expect, it, vi } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { renderWithProviders } from "../test/renderWithProviders";
import { HoldingDetailPage } from "./HoldingDetailPage";

const base = {
  id: "x", symbol: null as string | null, currency: "TRY", unit: "TRY",
  quantity: 6025, avgCost: 1, currentPrice: 1, totalCost: 6025, currentValue: 6025,
  profit: 0, returnRatio: 0, weight: 0.003, bes: null, transactions: [],
};

function mockHolding(body: unknown) {
  vi.stubGlobal(
    "fetch",
    vi.fn(() => Promise.resolve({ ok: true, status: 200, json: async () => body } as Response)),
  );
}

function renderDetail() {
  return renderWithProviders(
    <MemoryRouter initialEntries={["/holdings/x"]}>
      <Routes>
        <Route path="/holdings/:id" element={<HoldingDetailPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

afterEach(() => vi.restoreAllMocks());

describe("HoldingDetailPage — fiyat güncelleme görünürlüğü", () => {
  it("nakit: 'Fiyatı güncelle' yok, sabit-fiyat notu var", async () => {
    mockHolding({ ...base, assetType: "Cash", name: "Nakit (TL)" });
    renderDetail();

    await waitFor(() => expect(screen.getByRole("heading", { name: /Nakit/ })).toBeInTheDocument());
    expect(screen.queryByText("Fiyatı güncelle")).not.toBeInTheDocument();
    expect(screen.getByText(/Nakit fiyatı sabittir/)).toBeInTheDocument();
  });

  it("altın (canlı): 'Fiyatı güncelle' yok, canlı-kaynak notu var", async () => {
    mockHolding({
      ...base, assetType: "Gold", name: "Altın (gram)", symbol: "XAU", unit: "gram",
      quantity: 40, currentPrice: 6687.67,
    });
    renderDetail();

    await waitFor(() => expect(screen.getByRole("heading", { name: /Altın/ })).toBeInTheDocument());
    expect(screen.queryByText("Fiyatı güncelle")).not.toBeInTheDocument();
    expect(screen.getByText(/canlı kaynaktan otomatik/)).toBeInTheDocument();
  });

  it("hisse: elle 'Fiyatı güncelle' görünür (canlı sağlayıcı yok)", async () => {
    mockHolding({
      ...base, assetType: "Stock", name: "Apple Inc.", symbol: "AAPL", currency: "USD",
      unit: "adet", quantity: 12, avgCost: 175, currentPrice: 210,
    });
    renderDetail();

    await waitFor(() => expect(screen.getByRole("heading", { name: /Apple/ })).toBeInTheDocument());
    expect(screen.getByText("Fiyatı güncelle")).toBeInTheDocument();
  });
});
