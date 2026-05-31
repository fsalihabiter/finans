import { afterEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { renderWithProviders } from "../test/renderWithProviders";
import { AddHoldingDialog } from "./AddHoldingDialog";

afterEach(() => vi.restoreAllMocks());

function mockFetch() {
  const fetchMock = vi.fn().mockResolvedValue({
    ok: true,
    status: 201,
    json: async () => ({ id: "new" }),
  } as Response);
  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}

describe("AddHoldingDialog", () => {
  it("kapalıyken render edilmez", () => {
    const { container } = renderWithProviders(<AddHoldingDialog open={false} onClose={() => {}} />);
    expect(container.firstChild).toBeNull();
  });

  it("formu doldurup POST /api/holdings gönderir ve kapanır", async () => {
    const fetchMock = mockFetch();
    const onClose = vi.fn();

    renderWithProviders(<AddHoldingDialog open onClose={onClose} />);

    fireEvent.change(screen.getByLabelText("Ad"), { target: { value: "Altın (gram)" } });
    fireEvent.change(screen.getByLabelText("Miktar"), { target: { value: "40" } });
    fireEvent.change(screen.getByLabelText(/Alış birim fiyatı/), { target: { value: "4546,275" } });

    fireEvent.click(screen.getByRole("button", { name: "Ekle" }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalled());
    const call = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(call[0]).toBe("/api/holdings");
    expect(call[1].method).toBe("POST");
    expect(JSON.parse(call[1].body as string)).toMatchObject({
      assetType: "Gold",
      name: "Altın (gram)",
      currency: "TRY",
      unit: "gram",
      transaction: { type: "Buy", quantity: 40, unitPrice: 4546.275 },
    });

    await waitFor(() => expect(onClose).toHaveBeenCalled());
  });

  it("eksik zorunlu alanla Ekle butonu pasif + ipucu görünür", () => {
    renderWithProviders(<AddHoldingDialog open onClose={() => {}} />);
    expect(screen.getByRole("button", { name: "Ekle" })).toBeDisabled();
    expect(screen.getByText(/Ad, miktar ve alış fiyatı zorunlu/)).toBeInTheDocument();
  });

  it("tür chip'i seçince birim ön ayarı güncellenir (BES → birim)", () => {
    renderWithProviders(<AddHoldingDialog open onClose={() => {}} />);
    expect(screen.getByLabelText("Birim")).toHaveValue("gram");
    fireEvent.click(screen.getByRole("radio", { name: /BES/ }));
    expect(screen.getByRole("radio", { name: /BES/ })).toHaveAttribute("aria-checked", "true");
  });

  it("BES seçilince açılış bakiyesiyle POST /api/holdings/bes çağrılır", async () => {
    const fetchMock = mockFetch();
    renderWithProviders(<AddHoldingDialog open onClose={() => {}} />);

    fireEvent.click(screen.getByRole("radio", { name: /BES/ }));
    fireEvent.change(screen.getByLabelText(/Plan \/ şirket adı/), { target: { value: "Örnek BES" } });
    fireEvent.change(screen.getByLabelText("BES başlangıç tarihi"), { target: { value: "2024-06-01" } });
    fireEvent.change(screen.getByLabelText(/Güncel fon değeri/), { target: { value: "279378" } });
    fireEvent.change(screen.getByLabelText(/Birikmiş katkı payın/), { target: { value: "120000" } });
    fireEvent.change(screen.getByLabelText(/Birikmiş devlet katkısı/), { target: { value: "28554" } });
    fireEvent.click(screen.getByRole("button", { name: "Ekle" }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalled());
    const call = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(call[0]).toBe("/api/holdings/bes");
    expect(call[1].method).toBe("POST");
    expect(JSON.parse(call[1].body as string)).toMatchObject({
      name: "Örnek BES",
      currentFundValue: 279378,
      openingOwn: 120000,
      openingState: 28554,
      joinedAtUtc: "2024-06-01T00:00:00Z",
    });
  });
});
