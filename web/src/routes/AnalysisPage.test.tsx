import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import { AnalysisPage } from "./AnalysisPage";
import { renderWithProviders } from "../test/renderWithProviders";

// SC-W2 (NFR-2, CLAUDE.md §2): Analiz sayfasında "yatırım tavsiyesi değildir" çerçevesi HER ZAMAN.
describe("AnalysisPage (T3.8)", () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });
  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it("disclaimer'ı her durumda gösterir (yükleme dahil)", () => {
    vi.stubGlobal("fetch", vi.fn().mockReturnValue(new Promise(() => {}))); // hiç çözülmez

    renderWithProviders(<AnalysisPage />);

    expect(screen.getByText(/Yatırım tavsiyesi değildir/i)).toBeInTheDocument();
    expect(screen.getByRole("note")).toBeInTheDocument();
  });

  it("LLM yanıt geldiğinde kart başlığı ve gövdesi görünür", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        status: 200,
        json: async () => ({
          cards: [
            {
              emoji: "⚖️",
              title: "Dağılımın Yoğun",
              body: "Portföyünün yaklaşık %84'ü iki kalemde toplanmış (Altın ve BES). Yoğunlaşma demek bu iki varlık aynı anda değer kaybettiğinde büyük etkilenirsin demektir.",
              tags: ["yoğunlaşma"],
            },
          ],
          source: "llm",
          generatedAtUtc: "2026-06-05T00:00:00Z",
        }),
      } as Response),
    );

    renderWithProviders(<AnalysisPage />);

    expect(await screen.findByRole("heading", { name: "Dağılımın Yoğun" })).toBeInTheDocument();
    await waitFor(() =>
      expect(screen.getByText(/LLM tarafından üretildi/i)).toBeInTheDocument(),
    );
  });

  it("fallback yanıtta 'Yorum şu an üretilemedi' kartı + uyarı yazısı", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        status: 200,
        json: async () => ({
          cards: [
            {
              emoji: "💬",
              title: "Yorum şu an üretilemedi",
              body: "Eğitici yorum servisine şu an ulaşılamıyor. Portföy sayıların doğru ve güncel — bu kart yenilenebilir, biraz sonra tekrar deneyebilirsin.",
              tags: ["fallback"],
            },
          ],
          source: "fallback",
          generatedAtUtc: "2026-06-05T00:00:00Z",
        }),
      } as Response),
    );

    renderWithProviders(<AnalysisPage />);

    expect(
      await screen.findByRole("heading", { name: "Yorum şu an üretilemedi" }),
    ).toBeInTheDocument();
    expect(screen.getByText(/Yorum şu an üretilemedi — sayıların etkilenmedi/i)).toBeInTheDocument();
  });
});
