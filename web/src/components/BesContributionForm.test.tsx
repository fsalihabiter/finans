import { afterEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { renderWithProviders } from "../test/renderWithProviders";
import { BesContributionForm } from "./BesContributionForm";

afterEach(() => vi.restoreAllMocks());

describe("BesContributionForm", () => {
  it("aylık katkıyı BES ucuna POST eder ve %30 devlet katkısını önizler", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ id: "bes1" }),
    } as Response);
    vi.stubGlobal("fetch", fetchMock);

    renderWithProviders(<BesContributionForm holdingId="bes1" />);

    fireEvent.change(screen.getByLabelText(/Kendi katkın/), { target: { value: "2000" } });
    // %30 önizleme (600,00)
    expect(screen.getByText(/600,00/)).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Katkı ekle" }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalled());
    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe("/api/holdings/bes1/bes-contribution");
    expect(init.method).toBe("POST");
    expect(JSON.parse(init.body as string)).toMatchObject({ ownAmount: 2000 });
  });

  it("tutar boşken buton pasif", () => {
    renderWithProviders(<BesContributionForm holdingId="bes1" />);
    expect(screen.getByRole("button", { name: "Katkı ekle" })).toBeDisabled();
  });
});
