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

/** Değer geçmişi (T5.3): iki günlük mini seri — grafik çizilir. */
const history = {
  baseCurrency: "TRY",
  period: "all",
  points: [
    { date: "2026-05-29", value: 250000, cost: 181851 },
    { date: "2026-05-30", value: 260000, cost: 181851 },
  ],
  changeRatio: 0.04,
  firstDate: "2026-05-29",
  asOf: "2026-05-30T00:00:00Z",
};

function mockApi() {
  vi.stubGlobal(
    "fetch",
    vi.fn((url: string) => {
      let body: unknown = summary;
      if (url.includes("/api/holdings")) body = holdings;
      else if (url.includes("/api/portfolio/history")) body = history;
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

  it("Değer Seyri grafiğini gerçek seriyle çizer (dönem değişimi + tahmin-değil notu)", async () => {
    mockApi();
    const { container } = renderWithProviders(<PerformancePage />);

    // Grafik iki çizgiyle (değer + kesikli yatırılan) çizilir.
    await waitFor(() => expect(container.querySelector(".value-history-chart")).not.toBeNull());
    expect(container.querySelector(".vh-cost-line")).not.toBeNull();

    // Dönem değişim rozeti (+%4,0) ve "geçmiş, tahmin değil" çerçevesi (CLAUDE.md §2).
    expect(screen.getByText(/\+%4,0/)).toBeInTheDocument();
    expect(screen.getByText(/gelecek performansın göstergesi değildir/)).toBeInTheDocument();
  });
});
