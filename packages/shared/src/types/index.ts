// API DTO tipleri — backend sözleşmesiyle (04-API-CONTRACT.md) birebir.
// Faz 0: yalnızca health. Portföy/eğitim tipleri ilgili fazlarda eklenir.

/** Desteklenen baz para birimleri (CLAUDE.md §3.2). */
export type CurrencyCode = "TRY" | "USD" | "EUR";

/** GET /api/health yanıtı (04 §3). */
export interface HealthResponse {
  status: "ok";
}
