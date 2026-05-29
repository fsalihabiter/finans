// API istemci sözleşmesi — fetch wrapper + uç fonksiyonları.
// TanStack Query hook'ları T1.10'da bunun üstüne eklenir.
// Ham sayı gelir; biçimleme `../format` ile yapılır (hesap burada YOK).

import type { HealthResponse } from "../types/index";

export class ApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
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
    throw new ApiError(res.status, `İstek başarısız: ${res.status}`);
  }
  return (await res.json()) as T;
}

export function createApiClient({ baseUrl }: ApiClientOptions) {
  return {
    /** GET /api/health (04 §3). */
    getHealth: () => request<HealthResponse>(baseUrl, "/api/health"),
  };
}

export type ApiClient = ReturnType<typeof createApiClient>;
