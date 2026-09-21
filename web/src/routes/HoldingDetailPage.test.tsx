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

  // ── GD-002 (ürün sahibi kararı 2026-09-20): BES iki ayrı fon havuzu ──
  const besHolding = {
    ...base, assetType: "Bes", name: "Örnek BES", currency: "TRY", baseCurrency: "TRY", unit: "birim",
    quantity: 1, avgCost: 100000, currentPrice: 131550, totalCost: 100000,
    currentValue: 131550, profit: 31550, returnRatio: 0.3155,
    bes: {
      ownContribution: 100000, stateContribution: 30000, ownPending: 0, statePending: 0,
      vestingState: "PartiallyVested", vestedRate: 0.35, vestedAmount: 10500,
      joinedAtUtc: "2019-01-01T00:00:00Z", birthYear: 1990, providerName: null,
      contributions: [], contributionDue: false, planActive: false,
      monthlyAmount: null, contributionDay: null,
      fundReturnRatio: 0.1769, ownValue: 120000, ownProfit: 20000, stateValue: 33000, stateProfit: 3000,
      ownFundRate: 0.2, stateFundRate: 0.1, ownFundValue: 120000, stateFundValue: 33000,
      vestedPortfolioValue: 131550,
    },
  };

  it("BES: iki havuz AYRI getiriyle görünür; portföye yalnız hak edilmiş devlet katkısı girer", async () => {
    mockHolding(besHolding);
    renderDetail();

    await waitFor(() => expect(screen.getByRole("heading", { name: /Örnek BES/ })).toBeInTheDocument());

    // Havuzlar kendi oranlarıyla (eski model ikisine AYNI oranı yazıyordu).
    expect(screen.getByText(/%20,0/)).toBeInTheDocument();
    expect(screen.getByText(/%10,0/)).toBeInTheDocument();

    // Portföy değerine giren: 120.000 + 0,35 × 33.000 = 131.550 — gerekçesiyle.
    expect(screen.getByText("Portföy değerine giren")).toBeInTheDocument();
    expect(screen.getAllByText("₺131.550,00").length).toBeGreaterThan(0);
    expect(screen.getByText(/bugün ayrılsan alamayacağın para/)).toBeInTheDocument();
  });

  it("BES: fon değeri formu iki ayrı alan gönderir", async () => {
    const fetchMock = vi.fn((_url: string, init?: RequestInit) =>
      Promise.resolve({
        ok: true, status: 200,
        json: async () => (init?.method === "PUT" ? besHolding : besHolding),
      } as Response));
    vi.stubGlobal("fetch", fetchMock);
    renderDetail();

    await waitFor(() => expect(screen.getByRole("heading", { name: /Örnek BES/ })).toBeInTheDocument());
    screen.getByRole("button", { name: "Fon değerini güncelle" }).click();

    const own = await screen.findByLabelText("Kendi katkı paylarımın fondaki değeri");
    const state = screen.getByLabelText("Devlet katkısının fondaki değeri");
    const { fireEvent } = await import("@testing-library/react");
    fireEvent.change(own, { target: { value: "125000" } });
    fireEvent.change(state, { target: { value: "34000,50" } });
    fireEvent.click(screen.getByRole("button", { name: "Güncelle" }));

    await waitFor(() => {
      const put = fetchMock.mock.calls.find(([, i]) => i?.method === "PUT");
      expect(put).toBeDefined();
      expect(put![0]).toMatch(/\/bes$/);
      expect(JSON.parse(String(put![1]!.body))).toEqual({ ownFundValue: 125000, stateFundValue: 34000.5 });
    });
  });
});
