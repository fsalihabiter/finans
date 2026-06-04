// API istemci sözleşmesi — fetch wrapper + uç fonksiyonları (framework-bağımsız).
// React Query hook'ları web/mobil tarafında bunun üstüne kurulur.
// Ham sayı gelir; biçimleme `../format` ile yapılır (hesap burada YOK).

import type {
  AddBesContributionInput,
  ApiErrorEnvelope,
  BesProjection,
  BesProjectionInput,
  CommentaryResponse,
  CreateBesInput,
  CreateHoldingInput,
  CurrencyCode,
  GenerateBesContributionsInput,
  HealthResponse,
  Holding,
  NudgesResponse,
  PortfolioSummary,
  PricesResponse,
  Settings,
  TransactionInput,
  UpdateBesContributionInput,
  UpdateBesInput,
  UpdateHoldingInput,
  UpdateSettingsInput,
} from "../types/index";

/** Sözleşmeli hata (04 §2). `code`/`details` zarftan çıkarılır; mesaj TR ve gösterilebilir. */
export class ApiError extends Error {
  readonly status: number;
  readonly code: string;

  constructor(status: number, message: string, code = "UNKNOWN") {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
  }
}

/** Tüm isteklerin tabanı; web/mobil kendi taban URL'ini geçer. */
export interface ApiClientOptions {
  baseUrl: string;
}

async function request<T>(baseUrl: string, path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${baseUrl}${path}`, {
    headers: { "Content-Type": "application/json", ...(init?.headers ?? {}) },
    ...init,
  });

  if (!res.ok) {
    // Sözleşmeli hata zarfını çöz; gövde yoksa/parse edilemezse jenerik mesaj.
    let message = `İstek başarısız: ${res.status}`;
    let code = "UNKNOWN";
    try {
      const body = (await res.json()) as ApiErrorEnvelope;
      if (body?.error?.message) {
        message = body.error.message;
        code = body.error.code ?? code;
      }
    } catch {
      // gövde yok / JSON değil → jenerik mesaj kalır
    }
    throw new ApiError(res.status, message, code);
  }

  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

function withBaseCurrency(path: string, baseCurrency?: CurrencyCode): string {
  return baseCurrency ? `${path}?baseCurrency=${baseCurrency}` : path;
}

export function createApiClient({ baseUrl }: ApiClientOptions) {
  const get = <T>(path: string) => request<T>(baseUrl, path);
  const send = <T>(method: string, path: string, body?: unknown) =>
    request<T>(baseUrl, path, {
      method,
      body: body === undefined ? undefined : JSON.stringify(body),
    });

  return {
    /** GET /api/health (04 §3). */
    getHealth: () => get<HealthResponse>("/api/health"),

    // ── Portföy (04 §4) ──
    getSummary: (baseCurrency?: CurrencyCode) =>
      get<PortfolioSummary>(withBaseCurrency("/api/portfolio/summary", baseCurrency)),
    getHoldings: (baseCurrency?: CurrencyCode) =>
      get<Holding[]>(withBaseCurrency("/api/holdings", baseCurrency)),
    getHolding: (id: string, baseCurrency?: CurrencyCode) =>
      get<Holding>(withBaseCurrency(`/api/holdings/${id}`, baseCurrency)),
    createHolding: (input: CreateHoldingInput) =>
      send<Holding>("POST", "/api/holdings", input),
    createBes: (input: CreateBesInput) =>
      send<Holding>("POST", "/api/holdings/bes", input),
    addTransaction: (id: string, input: TransactionInput) =>
      send<Holding>("POST", `/api/holdings/${id}/transactions`, input),
    updateTransaction: (id: string, transactionId: string, input: TransactionInput) =>
      send<Holding>("PUT", `/api/holdings/${id}/transactions/${transactionId}`, input),
    deleteTransaction: (id: string, transactionId: string) =>
      send<Holding>("DELETE", `/api/holdings/${id}/transactions/${transactionId}`),
    updateHolding: (id: string, input: UpdateHoldingInput) =>
      send<Holding>("PUT", `/api/holdings/${id}`, input),
    addBesContribution: (id: string, input: AddBesContributionInput) =>
      send<Holding>("POST", `/api/holdings/${id}/bes-contribution`, input),
    updateBes: (id: string, input: UpdateBesInput) =>
      send<Holding>("PUT", `/api/holdings/${id}/bes`, input),
    generateBesContributions: (id: string, input: GenerateBesContributionsInput) =>
      send<Holding>("POST", `/api/holdings/${id}/bes/contributions`, input),
    updateBesContribution: (id: string, contributionId: string, input: UpdateBesContributionInput) =>
      send<Holding>("PUT", `/api/holdings/${id}/bes/contributions/${contributionId}`, input),
    deleteBesContribution: (id: string, contributionId: string) =>
      send<Holding>("DELETE", `/api/holdings/${id}/bes/contributions/${contributionId}`),
    projectBes: (id: string, input: BesProjectionInput) =>
      send<BesProjection>("POST", `/api/holdings/${id}/bes/projection`, input),
    deleteHolding: (id: string) => send<void>("DELETE", `/api/holdings/${id}`),

    // ── Canlı fiyat & eğitici notlar (Faz 2 — 04 §5) ──
    /** GET /api/prices — tazeleme turunu tetikler; güncel altın/döviz fiyatları (+ stale). */
    getPrices: () => get<PricesResponse>("/api/prices"),
    /** GET /api/portfolio/nudges — kural tabanlı eğitici notlar (tavsiye değil). */
    getNudges: (baseCurrency?: CurrencyCode) =>
      get<NudgesResponse>(withBaseCurrency("/api/portfolio/nudges", baseCurrency)),
    /** GET /api/portfolio/commentary — LLM eğitici yorum kartları (tavsiye DEĞİL — CLAUDE.md §2). */
    getCommentary: (baseCurrency?: CurrencyCode) =>
      get<CommentaryResponse>(withBaseCurrency("/api/portfolio/commentary", baseCurrency)),

    // ── Ayarlar (04 §4) ──
    getSettings: () => get<Settings>("/api/settings"),
    updateSettings: (input: UpdateSettingsInput) =>
      send<Settings>("PUT", "/api/settings", input),
  };
}

export type ApiClient = ReturnType<typeof createApiClient>;
