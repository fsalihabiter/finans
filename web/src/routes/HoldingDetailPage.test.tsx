import { afterEach, describe, expect, it, vi } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { renderWithProviders } from "../test/renderWithProviders";
import { HoldingDetailPage } from "./HoldingDetailPage";

const base = {
  id: "x", symbol: null as string | null, currency: "TRY", baseCurrency: "TRY", unit: "TRY",
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

  // Kullanıcı bildirimi 2026-09-20: USD kalemde 0,0603 × 721,63 = 43,50 $ beklenirken
  // 2.089,22 $ görünüyordu. Sebep: toplulaştırmalar BAZ para biriminde (TRY) gelir ama
  // sayfa hepsini varlığın birimiyle ("$") etiketliyordu. Çapraz kurda iki alan ayrılır.
  it("çapraz kur: birim alanlar varlığın biriminde, toplamlar baz para biriminde etiketlenir", async () => {
    mockHolding({
      ...base, assetType: "Stock", name: "NASDAQ 100 Endeks Fonu", symbol: "QQQ",
      currency: "USD", baseCurrency: "TRY", unit: "adet",
      quantity: 0.0603, avgCost: 721.63, currentPrice: 722.09,
      // 0,0603 × 721,63 × 48 (kur) — backend baz para biriminde döner.
      totalCost: 2087.89, currentValue: 2089.22, profit: 1.33, returnRatio: 0.001,
    });
    renderDetail();

    await waitFor(() => expect(screen.getByRole("heading", { name: /NASDAQ/ })).toBeInTheDocument());

    // Birim alanlar: varlığın kendi birimi ($).
    expect(screen.getByText("$721,63")).toBeInTheDocument();
    expect(screen.getByText("$722,09")).toBeInTheDocument();

    // Toplulaştırma: baz para birimi (₺) — "$2.087,89" ASLA görünmemeli.
    expect(screen.getByText(/₺2\.087,89/)).toBeInTheDocument();
    expect(screen.queryByText(/\$2\.087,89/)).not.toBeInTheDocument();
    expect(screen.queryByText(/\$2\.089,22/)).not.toBeInTheDocument();
  });

  // Kullanıcı bildirimi 2026-09-20: geri linki anasayfaya (Genel Bakış) götürüyordu;
  // detaya YALNIZ /varliklar listesinden gelinir, dönüş de oraya olmalı.
  it("geri linki varlık listesine döner (anasayfaya değil)", async () => {
    mockHolding({ ...base, assetType: "Gold", name: "Altın" });
    renderDetail();

    await waitFor(() => expect(screen.getByRole("heading", { name: /Altın/ })).toBeInTheDocument());
    expect(screen.getByRole("link", { name: /Varlıklarım/ })).toHaveAttribute("href", "/varliklar");
  });
});
