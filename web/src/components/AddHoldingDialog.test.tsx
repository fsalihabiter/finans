import { afterEach, describe, expect, it, vi } from "vitest";
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { renderWithProviders } from "../test/renderWithProviders";
import { AddHoldingDialog } from "./AddHoldingDialog";

afterEach(() => vi.restoreAllMocks());

/** URL-duyarlı fetch taklidi: canlı fiyatlar + hisse metrikleri + create uçları. */
function mockFetch(overrides?: { prices?: unknown; stock?: unknown }) {
  const prices = overrides?.prices ?? { refreshedAtUtc: "", fromCache: false, hasStale: false, failedSources: [], prices: [] };
  const fetchMock = vi.fn((url: string) => {
    let body: unknown = { id: "new" };
    if (url.includes("/api/prices")) body = prices;
    else if (url.includes("/api/stocks/")) body = overrides?.stock ?? { name: "Apple Inc", price: 210, currency: "USD" };
    return Promise.resolve({ ok: true, status: 200, json: async () => body } as Response);
  });
  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}

/** create çağrısını (POST) bul — mount'taki GET /api/prices çağrılarını atla. */
const postCall = (fetchMock: ReturnType<typeof mockFetch>, path: string) =>
  fetchMock.mock.calls.find(
    (c) => (c as [string, RequestInit?])[0] === path && (c as [string, RequestInit?])[1]?.method === "POST",
  ) as [string, RequestInit] | undefined;

describe("AddHoldingDialog", () => {
  it("kapalıyken render edilmez", () => {
    const { container } = renderWithProviders(<AddHoldingDialog open={false} onClose={() => {}} />);
    expect(container.firstChild).toBeNull();
  });

  it("altın: ad ön-dolu; formu doldurup POST /api/holdings gönderir (XAU/gram/TRY otomatik)", async () => {
    const fetchMock = mockFetch();
    const onClose = vi.fn();

    renderWithProviders(<AddHoldingDialog open onClose={onClose} />);

    // Ad tür varsayılanıyla gelir; gereksiz alanlar (sembol/birim/pb) sorulmaz.
    expect(screen.getByLabelText("Ad")).toHaveValue("Altın (gram)");
    expect(screen.queryByLabelText("Birim")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Para birimi")).not.toBeInTheDocument();

    fireEvent.change(screen.getByLabelText("Miktar (gram)"), { target: { value: "40" } });
    fireEvent.change(screen.getByLabelText(/Alış birim fiyatı/), { target: { value: "4546,275" } });
    fireEvent.click(screen.getByRole("button", { name: "Ekle" }));

    await waitFor(() => expect(postCall(fetchMock, "/api/holdings")).toBeTruthy());
    const call = postCall(fetchMock, "/api/holdings")!;
    expect(JSON.parse(call[1].body as string)).toMatchObject({
      assetType: "Gold",
      name: "Altın (gram)",
      symbol: "XAU",
      currency: "TRY",
      unit: "gram",
      transaction: { type: "Buy", quantity: 40, unitPrice: 4546.275 },
    });
    await waitFor(() => expect(onClose).toHaveBeenCalled());
  });

  it("canlı altın fiyatı alış fiyatına otomatik gelir (düzenlenebilir ipucuyla)", async () => {
    mockFetch({
      prices: {
        refreshedAtUtc: "", fromCache: false, hasStale: false, failedSources: [],
        prices: [{ kind: "Gold", currency: "TRY", price: 6225.55, quoteCurrency: "TRY", asOfUtc: "", source: "truncgil", stale: false }],
      },
    });
    renderWithProviders(<AddHoldingDialog open onClose={() => {}} />);

    await waitFor(() =>
      expect(screen.getByLabelText(/Alış birim fiyatı/)).toHaveValue("6225,55"));
    expect(screen.getByText(/Güncel fiyat otomatik geldi/)).toBeInTheDocument();
  });

  it("nakit: yalnız ad + tutar sorulur; fiyat 1 ve birim TRY otomatik gider", async () => {
    const fetchMock = mockFetch();
    renderWithProviders(<AddHoldingDialog open onClose={() => {}} />);

    fireEvent.click(screen.getByRole("radio", { name: /Nakit/ }));
    expect(screen.getByLabelText("Ad")).toHaveValue("Nakit (TL)");
    expect(screen.queryByLabelText(/Alış birim fiyatı/)).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/Sembol/)).not.toBeInTheDocument();

    fireEvent.change(screen.getByLabelText("Tutar (₺)"), { target: { value: "15.000".replace(".", "") } });
    fireEvent.click(screen.getByRole("button", { name: "Ekle" }));

    await waitFor(() => expect(postCall(fetchMock, "/api/holdings")).toBeTruthy());
    const body = JSON.parse(postCall(fetchMock, "/api/holdings")![1].body as string);
    expect(body).toMatchObject({
      assetType: "Cash",
      name: "Nakit (TL)",
      currency: "TRY",
      unit: "TRY",
      transaction: { type: "Buy", quantity: 15000, unitPrice: 1 },
    });
  });

  it("hisse: sembol terk edilince ad + güncel fiyat otomatik getirilir", async () => {
    const fetchMock = mockFetch();
    renderWithProviders(<AddHoldingDialog open onClose={() => {}} />);

    fireEvent.click(screen.getByRole("radio", { name: /Hisse/ }));
    fireEvent.change(screen.getByLabelText("Sembol"), { target: { value: "aapl" } });
    fireEvent.blur(screen.getByLabelText("Sembol"));

    await waitFor(() => expect(screen.getByLabelText("Ad")).toHaveValue("Apple Inc"));
    expect(screen.getByLabelText(/Alış birim fiyatı/)).toHaveValue("210");

    fireEvent.change(screen.getByLabelText("Adet"), { target: { value: "12" } });
    fireEvent.click(screen.getByRole("button", { name: "Ekle" }));

    await waitFor(() => expect(postCall(fetchMock, "/api/holdings")).toBeTruthy());
    const body = JSON.parse(postCall(fetchMock, "/api/holdings")![1].body as string);
    expect(body).toMatchObject({
      assetType: "Stock",
      name: "Apple Inc",
      symbol: "AAPL",
      currency: "USD",
      unit: "adet",
      transaction: { type: "Buy", quantity: 12, unitPrice: 210 },
    });
  });

  it("hisse: TL seçilirse BIST hissesi TRY para birimiyle gönderilir + fiyat etiketi ₺ olur", async () => {
    const fetchMock = mockFetch();
    renderWithProviders(<AddHoldingDialog open onClose={() => {}} />);

    fireEvent.click(screen.getByRole("radio", { name: /Hisse/ }));
    // Varsayılan USD; kullanıcı TL'ye geçer (Türk hissesi).
    fireEvent.click(screen.getByRole("radio", { name: /🇹🇷 TL/ }));
    expect(screen.getByLabelText(/Alış birim fiyatı \(₺\)/)).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText("Sembol"), { target: { value: "THYAO" } });
    fireEvent.change(screen.getByLabelText("Ad"), { target: { value: "Türk Hava Yolları" } });
    fireEvent.change(screen.getByLabelText("Adet"), { target: { value: "10" } });
    fireEvent.change(screen.getByLabelText(/Alış birim fiyatı/), { target: { value: "352,25" } });
    fireEvent.click(screen.getByRole("button", { name: "Ekle" }));

    await waitFor(() => expect(postCall(fetchMock, "/api/holdings")).toBeTruthy());
    const body = JSON.parse(postCall(fetchMock, "/api/holdings")![1].body as string);
    expect(body).toMatchObject({
      assetType: "Stock",
      symbol: "THYAO",
      currency: "TRY",
      unit: "adet",
      transaction: { type: "Buy", quantity: 10, unitPrice: 352.25 },
    });
  });

  it("hisse: elle TL seçilince sembol otomatiği (USD) bunu EZMEZ", async () => {
    const fetchMock = mockFetch();
    renderWithProviders(<AddHoldingDialog open onClose={() => {}} />);

    fireEvent.click(screen.getByRole("radio", { name: /Hisse/ }));
    fireEvent.click(screen.getByRole("radio", { name: /🇹🇷 TL/ }));
    // Sembol terk → Finnhub USD döner ama kullanıcı seçimi korunur.
    fireEvent.change(screen.getByLabelText("Sembol"), { target: { value: "aapl" } });
    fireEvent.blur(screen.getByLabelText("Sembol"));
    await waitFor(() => expect(screen.getByLabelText("Ad")).toHaveValue("Apple Inc"));

    fireEvent.change(screen.getByLabelText("Adet"), { target: { value: "5" } });
    fireEvent.click(screen.getByRole("button", { name: "Ekle" }));

    await waitFor(() => expect(postCall(fetchMock, "/api/holdings")).toBeTruthy());
    const body = JSON.parse(postCall(fetchMock, "/api/holdings")![1].body as string);
    expect(body).toMatchObject({ assetType: "Stock", currency: "TRY" });
  });

  it("döviz: USD/EUR seçimi ad ve birimi otomatik ayarlar", () => {
    mockFetch();
    renderWithProviders(<AddHoldingDialog open onClose={() => {}} />);

    fireEvent.click(screen.getByRole("radio", { name: /Döviz/ }));
    expect(screen.getByLabelText("Ad")).toHaveValue("ABD Doları");
    expect(screen.getByLabelText("Miktar (USD)")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("radio", { name: /Euro/ }));
    expect(screen.getByLabelText("Ad")).toHaveValue("Euro");
    expect(screen.getByLabelText("Miktar (EUR)")).toBeInTheDocument();
  });

  it("eksik zorunlu alanla Ekle butonu pasif + türe uygun ipucu görünür", () => {
    mockFetch();
    renderWithProviders(<AddHoldingDialog open onClose={() => {}} />);
    expect(screen.getByRole("button", { name: "Ekle" })).toBeDisabled();
    expect(screen.getByText(/miktar ve alış fiyatı zorunlu/i)).toBeInTheDocument();

    fireEvent.click(screen.getByRole("radio", { name: /Nakit/ }));
    expect(screen.getByText(/Tutar 0'dan büyük olmalı/)).toBeInTheDocument();
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

    await waitFor(() => expect(postCall(fetchMock, "/api/holdings/bes")).toBeTruthy());
    const call = postCall(fetchMock, "/api/holdings/bes")!;
    expect(JSON.parse(call[1].body as string)).toMatchObject({
      name: "Örnek BES",
      currentFundValue: 279378,
      openingOwn: 120000,
      openingState: 28554,
      joinedAtUtc: "2024-06-01T00:00:00Z",
    });
  });
});
