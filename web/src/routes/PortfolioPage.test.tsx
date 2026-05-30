import { afterEach, describe, expect, it, vi } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import { renderWithProviders } from "../test/renderWithProviders";
import { PortfolioPage } from "./PortfolioPage";

const summary = {
  baseCurrency: "TRY",
  totalValue: 641403,
  totalCost: 422970,
  netProfit: 218433,
  returnRatio: 0.516,
  realReturnRatio: 0.0989,
  allocation: [{ assetType: "Gold", name: "Altın", value: 260000, weight: 0.405 }],
  asOf: "2026-05-30T00:00:00Z",
};

/** Fetch'i URL'e göre yanıtlar (settings + summary). */
function mockApi() {
  vi.stubGlobal(
    "fetch",
    vi.fn((url: string) => {
      const body = url.includes("/api/settings") ? { baseCurrency: "TRY" } : summary;
      return Promise.resolve({ ok: true, status: 200, json: async () => body } as Response);
    }),
  );
}

afterEach(() => vi.restoreAllMocks());

describe("PortfolioPage", () => {
  it("özeti çeker ve HeroCard'ı tr-TR biçimiyle gösterir", async () => {
    mockApi();
    renderWithProviders(<PortfolioPage />);

    expect(screen.getByRole("heading", { name: "Portföy" })).toBeInTheDocument();

    // Toplam değer + getiri backend'den gelip biçimlenmeli
    await waitFor(() => expect(screen.getByText(/641\.403,00/)).toBeInTheDocument());
    expect(screen.getByText(/\+%51,6/)).toBeInTheDocument();
  });

  it("baz para birimi seçiciyi kullanıcı tercihiyle gösterir", async () => {
    mockApi();
    renderWithProviders(<PortfolioPage />);

    await waitFor(() => {
      const group = screen.getByRole("group", { name: "Baz para birimi" });
      expect(group).toBeInTheDocument();
    });
    // TRY seçili (aria-pressed)
    const tryBtn = screen.getByRole("button", { name: "TRY" });
    expect(tryBtn).toHaveAttribute("aria-pressed", "true");
  });
});
