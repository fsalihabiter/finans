import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiError, createApiClient } from "./index";

const client = createApiClient({ baseUrl: "http://test" });

function mockFetch(status: number, body: unknown, ok = status < 400) {
  return vi.fn().mockResolvedValue({
    ok,
    status,
    json: async () => body,
  } as Response);
}

afterEach(() => vi.restoreAllMocks());

describe("createApiClient", () => {
  it("özet endpoint'ini baseCurrency ile çağırır", async () => {
    const fetchMock = mockFetch(200, { baseCurrency: "TRY", totalValue: 1 });
    vi.stubGlobal("fetch", fetchMock);

    await client.getSummary("USD");

    expect(fetchMock).toHaveBeenCalledWith(
      "http://test/api/portfolio/summary?baseCurrency=USD",
      expect.anything(),
    );
  });

  it("hata zarfından kod ve TR mesajı çıkarır", async () => {
    vi.stubGlobal(
      "fetch",
      mockFetch(400, { error: { code: "VALIDATION_ERROR", message: "Miktar 0'dan büyük olmalı." } }, false),
    );

    await expect(client.getHoldings()).rejects.toMatchObject({
      status: 400,
      code: "VALIDATION_ERROR",
      message: "Miktar 0'dan büyük olmalı.",
    });
  });

  it("204'te gövdesiz çözer (delete)", async () => {
    vi.stubGlobal("fetch", mockFetch(204, null));
    await expect(client.deleteHolding("abc")).resolves.toBeUndefined();
  });

  it("gövdesi olmayan hatada jenerik mesaj kullanır", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({
      ok: false,
      status: 500,
      json: async () => {
        throw new Error("no body");
      },
    } as unknown as Response));

    await expect(client.getSummary()).rejects.toBeInstanceOf(ApiError);
  });
});
