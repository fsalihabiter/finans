import { afterEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { renderWithProviders } from "../test/renderWithProviders";
import { AddHoldingDialog } from "./AddHoldingDialog";

afterEach(() => vi.restoreAllMocks());

describe("AddHoldingDialog", () => {
  it("kapalıyken render edilmez", () => {
    const { container } = renderWithProviders(<AddHoldingDialog open={false} onClose={() => {}} />);
    expect(container.firstChild).toBeNull();
  });

  it("formu doldurup POST /api/holdings gönderir ve kapanır", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 201,
      json: async () => ({ id: "new" }),
    } as Response);
    vi.stubGlobal("fetch", fetchMock);
    const onClose = vi.fn();

    renderWithProviders(<AddHoldingDialog open onClose={onClose} />);

    fireEvent.change(screen.getByLabelText("Ad"), { target: { value: "Altın (gram)" } });
    fireEvent.change(screen.getByLabelText("Miktar"), { target: { value: "40" } });
    fireEvent.change(screen.getByLabelText(/Alış birim fiyatı/), { target: { value: "4546,275" } });

    fireEvent.click(screen.getByRole("button", { name: "Ekle" }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalled());
    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe("/api/holdings");
    expect(init.method).toBe("POST");
    const body = JSON.parse(init.body as string);
    expect(body).toMatchObject({
      assetType: "Gold",
      name: "Altın (gram)",
      currency: "TRY",
      unit: "gram",
      transaction: { type: "Buy", quantity: 40, unitPrice: 4546.275 },
    });

    await waitFor(() => expect(onClose).toHaveBeenCalled());
  });

  it("eksik zorunlu alanla Ekle butonu pasif", () => {
    renderWithProviders(<AddHoldingDialog open onClose={() => {}} />);
    expect(screen.getByRole("button", { name: "Ekle" })).toBeDisabled();
  });
});
