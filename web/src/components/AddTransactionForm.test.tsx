import { afterEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { renderWithProviders } from "../test/renderWithProviders";
import { AddTransactionForm } from "./AddTransactionForm";

afterEach(() => vi.restoreAllMocks());

function mockOk() {
  const fetchMock = vi.fn().mockResolvedValue({
    ok: true,
    status: 200,
    json: async () => ({ id: "h1", quantity: 50 }),
  } as Response);
  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}

describe("AddTransactionForm", () => {
  it("alış işlemini doğru uca POST eder", async () => {
    const fetchMock = mockOk();
    renderWithProviders(<AddTransactionForm holdingId="h1" currency="TRY" unit="gram" />);

    fireEvent.change(screen.getByLabelText(/Miktar/), { target: { value: "10" } });
    fireEvent.change(screen.getByLabelText(/Birim fiyat/), { target: { value: "6500" } });
    fireEvent.click(screen.getByRole("button", { name: "Ekle" }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalled());
    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe("/api/holdings/h1/transactions");
    expect(init.method).toBe("POST");
    expect(JSON.parse(init.body as string)).toMatchObject({
      type: "Buy",
      quantity: 10,
      unitPrice: 6500,
    });
  });

  it("satış seçilince type=Sell gönderir", async () => {
    const fetchMock = mockOk();
    renderWithProviders(<AddTransactionForm holdingId="h1" currency="TRY" unit="gram" />);

    fireEvent.click(screen.getByRole("button", { name: "Satış" }));
    fireEvent.change(screen.getByLabelText(/Miktar/), { target: { value: "5" } });
    fireEvent.change(screen.getByLabelText(/Birim fiyat/), { target: { value: "7000" } });
    fireEvent.click(screen.getByRole("button", { name: "Ekle" }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalled());
    expect(JSON.parse(fetchMock.mock.calls[0][1].body as string)).toMatchObject({
      type: "Sell",
      quantity: 5,
    });
  });

  it("miktar boşken Ekle pasif", () => {
    renderWithProviders(<AddTransactionForm holdingId="h1" currency="TRY" unit="gram" />);
    expect(screen.getByRole("button", { name: "Ekle" })).toBeDisabled();
  });

  it("cash modu: 'Para ekle/çıkar', fiyat alanı yok, unitPrice=1 gönderir", async () => {
    const fetchMock = mockOk();
    renderWithProviders(<AddTransactionForm holdingId="cash1" currency="TRY" unit="TRY" cash />);

    // Etiketler nakit'e göre + birim fiyat alanı yok.
    expect(screen.getByRole("button", { name: "Para ekle" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Para çıkar" })).toBeInTheDocument();
    expect(screen.queryByLabelText(/Birim fiyat/)).not.toBeInTheDocument();

    fireEvent.change(screen.getByLabelText(/Tutar/), { target: { value: "1000" } });
    fireEvent.click(screen.getByRole("button", { name: "Kaydet" }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalled());
    expect(JSON.parse(fetchMock.mock.calls[0][1].body as string)).toMatchObject({
      type: "Buy",
      quantity: 1000,
      unitPrice: 1,
    });
  });
});
