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
  // Kullanıcı bildirimi 2026-09-21 (ilk bildirimin ASIL isteği): 0,0603 adet $721,63'ten
  // alındı ($43,51), fiyat $740,84'e güncellendi → detay "$44,67 · +%2,7" göstermeli.
  // İlk düzeltme etiketi ₺'ye çevirmişti (doğru etiket, yanlış birim) — kullanıcı DOLAR istiyor.
  it("çapraz kur: detay varlığın KENDİ biriminde ($), ₺ karşılığı ikincil satırda", async () => {
    mockHolding({
      ...base, assetType: "Stock", name: "NASDAQ 100 Endeks Fonu", symbol: "QQQ",
      currency: "USD", baseCurrency: "TRY", unit: "adet",
      quantity: 0.0603, avgCost: 721.63, currentPrice: 740.84,
      // Baz birim (TRY) alanlar — listede kullanılır.
      totalCost: 2123.45, currentValue: 2179.98, profit: 56.53, returnRatio: 0.02662,
      // Varlığın kendi birimi (USD) — backend hesaplar (D-001).
      totalCostNative: 43.514289, currentValueNative: 44.672652, profitNative: 1.158363,
    });
    renderDetail();

    await waitFor(() => expect(screen.getByRole("heading", { name: /NASDAQ/ })).toBeInTheDocument());

    // Birim alanlar ve toplamlar aynı birimde ($).
    expect(screen.getByText("$721,63")).toBeInTheDocument();
    expect(screen.getByText("$740,84")).toBeInTheDocument();
    expect(screen.getByText("$43,51")).toBeInTheDocument();             // toplam maliyet
    await waitFor(() => expect(screen.getByText("$44,67")).toBeInTheDocument()); // değer (count-up)
    expect(screen.getByText(/\+%2,7/)).toBeInTheDocument();

    // ₺ karşılığı ikincil satırda, KENDİ etiketiyle.
    expect(screen.getByTestId("hero-base-equivalent")).toHaveTextContent("≈ ₺2.179,98");

    // Etiket karışıklığı ASLA: TRY tutar "$" ile görünmez.
    expect(screen.queryByText(/\$2\.179,98/)).not.toBeInTheDocument();
    expect(screen.queryByText(/\$2\.123,45/)).not.toBeInTheDocument();
  });

  // REVIEW-002 · RV-005: fiyatı HENÜZ girilmemiş yabancı kalem (hisse/fon fiyatsız başlar).
  // Değer null, maliyet native USD — birim yine $ olmalı. Eskiden ₺'ye düşüyordu.
  it("çapraz kur, fiyatsız: maliyet varlığın biriminde kalır ($, ₺ değil)", async () => {
    mockHolding({
      ...base, assetType: "Stock", name: "Yeni Hisse", symbol: "QQQ",
      currency: "USD", baseCurrency: "TRY", unit: "adet",
      quantity: 0.0603, avgCost: 721.63, currentPrice: null,
      totalCost: 2088.69, currentValue: null, profit: null, returnRatio: null,
      totalCostNative: 43.514289, currentValueNative: null, profitNative: null,
    });
    renderDetail();

    await waitFor(() => expect(screen.getByRole("heading", { name: /Yeni Hisse/ })).toBeInTheDocument());
    expect(screen.getByText("$43,51")).toBeInTheDocument();
    expect(screen.queryByText("₺43,51")).not.toBeInTheDocument();
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
