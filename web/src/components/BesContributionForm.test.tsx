import { afterEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { renderWithProviders } from "../test/renderWithProviders";
import { BesContributionForm } from "./BesContributionForm";

afterEach(() => vi.restoreAllMocks());

describe("BesContributionForm", () => {
  it("devlet katkısını ÖDEME TARİHİNDEKİ orana göre önizler ve POST eder", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ id: "bes1" }),
    } as Response);
    vi.stubGlobal("fetch", fetchMock);

    renderWithProviders(<BesContributionForm holdingId="bes1" />);

    fireEvent.change(screen.getByLabelText(/Katkı Payı/), { target: { value: "2000" } });
    // Varsayılan bugün (2026) → %20 → 400,00
    expect(screen.getByText(/400,00/)).toBeInTheDocument();

    // Geri-tarihli ödeme (2025) → %30 → 600,00 (oran geriye dönük değil). Native date → ISO.
    fireEvent.change(screen.getByLabelText("Ödeme tarihi"), { target: { value: "2025-06-01" } });
    expect(screen.getByText(/600,00/)).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Katkı ekle" }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalled());
    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe("/api/holdings/bes1/bes-contribution");
    expect(init.method).toBe("POST");
    const body = JSON.parse(init.body as string);
    expect(body).toMatchObject({ ownAmount: 2000 });
    expect(body.paidAtUtc).toContain("2025-06-01");
  });

  it("tutar boşken buton pasif", () => {
    renderWithProviders(<BesContributionForm holdingId="bes1" />);
    expect(screen.getByRole("button", { name: "Katkı ekle" })).toBeDisabled();
  });
});
