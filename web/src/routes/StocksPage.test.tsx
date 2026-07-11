import { afterEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { renderWithProviders } from "../test/renderWithProviders";
import { StocksPage } from "./StocksPage";

const metricsBody = {
  symbol: "AAPL",
  name: "Apple Inc",
  exchange: "NASDAQ",
  currency: "USD",
  price: 315.32,
  changeRatio: -0.0028,
  metrics: { peRatio: 37.78, pbRatio: 43.49, dividendYield: 0.0034, earningsGrowth: 0.29 },
  sectorContext: { peRatio: "above", pbRatio: "high", dividendYield: "low", earningsGrowth: "positive" },
  asOfUtc: "2026-07-12T00:00:00Z",
  source: "finnhub",
};

const explainBody = {
  cards: [
    {
      emoji: "⚖️",
      title: "Fiyat/Kazanç Oranı Ne Anlatıyor?",
      body: "F/K oranı, hissenin fiyatının şirketin hisse başına yıllık kârının kaç katı olduğunu gösterir; bu hissede yüksek banda düşüyor ve iki yönlü okunmalıdır.",
      detail: "F/K'yı bir dükkânı satın almaya benzetebilirsin: yıllık kazancının kaç katına satılıyorsa o kadar yıl sabır demektir.",
      tags: ["f-k"],
    },
  ],
  source: "llm",
  generatedAtUtc: "2026-07-12T00:00:00Z",
};

function mockApi(metricsStatus = 200) {
  vi.stubGlobal(
    "fetch",
    vi.fn((url: string) => {
      if (url.includes("/metrics")) {
        if (metricsStatus !== 200)
          return Promise.resolve({
            ok: false,
            status: metricsStatus,
            json: async () => ({
              error: { code: "NOT_FOUND", message: "Bu sembol için veri bulunamadı." },
            }),
          } as Response);
        return Promise.resolve({ ok: true, status: 200, json: async () => metricsBody } as Response);
      }
      if (url.includes("/explain"))
        return Promise.resolve({ ok: true, status: 200, json: async () => explainBody } as Response);
      return Promise.reject(new Error(`beklenmeyen istek: ${url}`));
    }),
  );
}

afterEach(() => vi.restoreAllMocks());

describe("StocksPage (T4.4)", () => {
  it("başlangıçta arama + disclaimer + popüler çipler görünür; istek atılmaz", () => {
    const fetchMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);
    renderWithProviders(<StocksPage />);

    expect(screen.getByRole("heading", { name: /Rakamların ne anlama geldiğini/i })).toBeInTheDocument();
    expect(screen.getByText(/Yatırım tavsiyesi değildir/i)).toBeInTheDocument(); // NFR-2
    expect(screen.getByRole("button", { name: "AAPL" })).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled(); // sembol yokken dış çağrı yok
  });

  it("çipe tıklayınca metrikler (tr-TR + bant) ve açıklama kartı gelir", async () => {
    mockApi();
    renderWithProviders(<StocksPage />);

    fireEvent.click(screen.getByRole("button", { name: "AAPL" }));

    // Metrik değerleri tr-TR biçiminde + bant rozetinin Türkçesi.
    expect(await screen.findByText("Apple Inc")).toBeInTheDocument();
    expect(screen.getByText("37,78")).toBeInTheDocument();
    expect(screen.getAllByText("yüksek bant").length).toBeGreaterThan(0);
    expect(screen.getByText("%0,34")).toBeInTheDocument(); // temettü verimi işaretsiz

    // Açıklama kartı (metrikler başarılı olunca tetiklenir).
    expect(
      await screen.findByRole("heading", { name: /Fiyat\/Kazanç Oranı Ne Anlatıyor/i }),
    ).toBeInTheDocument();
    await waitFor(() => expect(screen.getByText(/LLM tarafından üretildi/i)).toBeInTheDocument());
  });

  it("bilinmeyen sembolde sözleşmeli 404 mesajı gösterilir, çökme yok", async () => {
    mockApi(404);
    renderWithProviders(<StocksPage />);

    fireEvent.click(screen.getByRole("button", { name: "TSLA" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Bu sembol için veri bulunamadı.");
    // Disclaimer hâlâ görünür (her durumda — CLAUDE.md §2).
    expect(screen.getByText(/Yatırım tavsiyesi değildir/i)).toBeInTheDocument();
  });
});
