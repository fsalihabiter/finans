import { afterEach, describe, expect, it, vi } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { renderWithProviders } from "../test/renderWithProviders";
import { PortfolioPage } from "./PortfolioPage";

/** PortfolioPage bir route bileşeni (Link + navigasyon içerir) → Router gerekli. */
const renderPage = () =>
  renderWithProviders(
    <MemoryRouter>
      <PortfolioPage />
    </MemoryRouter>,
  );

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

const pricesResponse = {
  refreshedAtUtc: "2026-05-30T00:00:00Z",
  fromCache: false,
  hasStale: false,
  failedSources: [],
  prices: [
    {
      kind: "Gold", currency: "TRY", price: 6500, quoteCurrency: "TRY",
      asOfUtc: "2026-05-30T00:00:00Z", source: "truncgil", stale: false,
    },
  ],
};

/** Fetch'i URL'e göre yanıtlar (settings + holdings + summary + prices + nudges). */
function mockApi(opts?: { historyFails?: boolean }) {
  vi.stubGlobal(
    "fetch",
    vi.fn((url: string) => {
      let body: unknown = summary;
      let ok = true;
      let status = 200;
      if (url.includes("/api/settings")) body = { baseCurrency: "TRY" };
      else if (url.includes("/api/holdings")) body = []; // boş pozisyon listesi
      else if (url.includes("/api/prices")) body = pricesResponse;
      else if (url.includes("/nudges")) body = { nudges: [] };
      else if (url.includes("/api/portfolio/history")) {
        if (opts?.historyFails) {
          // FX yarışı anı: kur commit edilmeden istek DB'ye ulaştı → sözleşmeli 502.
          ok = false;
          status = 502;
          body = { error: { code: "UPSTREAM_ERROR", message: "Kur verisi şu anda hazırlanamadı." } };
        } else {
          body = { baseCurrency: "TRY", period: "1y", points: [], changeRatio: null, firstDate: null, asOf: "2026-05-30T00:00:00Z" };
        }
      }
      return Promise.resolve({ ok, status, json: async () => body } as Response);
    }),
  );
}

afterEach(() => vi.restoreAllMocks());

describe("PortfolioPage", () => {
  it("özeti çeker ve KPI'ları tr-TR biçimiyle gösterir", async () => {
    mockApi();
    renderPage();

    expect(screen.getByRole("heading", { name: "Genel Bakış" })).toBeInTheDocument();

    // Toplam değer + getiri backend'den gelip biçimlenmeli (KPI hero)
    await waitFor(() => expect(screen.getByText(/641\.403,00/)).toBeInTheDocument());
    // Getiri hem hero alt-yazısında hem "Getiri" KPI'sinde görünür.
    expect(screen.getAllByText(/\+%51,6/).length).toBeGreaterThan(0);
  });

  it("baz para birimi seçiciyi kullanıcı tercihiyle gösterir", async () => {
    mockApi();
    renderPage();

    await waitFor(() => {
      const group = screen.getByRole("group", { name: "Baz para birimi" });
      expect(group).toBeInTheDocument();
    });
    // TRY seçili (aria-pressed)
    const tryBtn = screen.getByRole("button", { name: "TRY" });
    expect(tryBtn).toHaveAttribute("aria-pressed", "true");
  });

  // SC-42: hata veri-yokluğu ("en az iki günlük veri gerekir") gibi MASKELENMEZ.
  it("değer seyri isteği hata verirse hata metnini gösterir (veri-yokluğu değil)", async () => {
    mockApi({ historyFails: true });
    renderPage();

    // usePortfolioHistory retry:1 → hata durumu ~1sn retry gecikmesinden sonra oturur.
    await waitFor(
      () => expect(screen.getByText(/Değer seyri yüklenemedi/)).toBeInTheDocument(),
      { timeout: 4000 },
    );
    expect(screen.queryByText(/en az iki günlük veri/)).not.toBeInTheDocument();
  });

  // SC-42: fiyat tazelemesi (refreshedAtUtc) değer serisini de invalidate eder —
  // ilk yüklemedeki FX yarışında hatalı kalan sorgu böylece kendini toparlar.
  it("fiyat tazelemesi değer seyri sorgusunu da yeniden tetikler", async () => {
    mockApi();
    renderPage();

    const fetchMock = vi.mocked(fetch);
    await waitFor(() => {
      const historyCalls = fetchMock.mock.calls.filter(([url]) =>
        String(url).includes("/api/portfolio/history"),
      );
      expect(historyCalls.length).toBeGreaterThanOrEqual(2);
    });
  });
});
