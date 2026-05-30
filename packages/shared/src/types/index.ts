// API DTO tipleri — backend sözleşmesiyle (04-API-CONTRACT.md) birebir.
// Sayılar ham (decimal→number); biçimleme `../format` ile. Hesap YOK.

/** Desteklenen baz para birimleri (CLAUDE.md §3.2). */
export type CurrencyCode = "TRY" | "USD" | "EUR";

/** Varlık türleri (Domain.Enums.AssetType ile birebir). */
export type AssetType = "Gold" | "Fx" | "Stock" | "Fund" | "Bes" | "Cash";

/** İşlem türü. */
export type TransactionType = "Buy" | "Sell";

/** BES hak ediş durumu. */
export type VestingState = "NotVested" | "PartiallyVested" | "Vested";

/** GET /api/health yanıtı (04 §3). */
export interface HealthResponse {
  status: "ok";
}

// ── Hata zarfı (04 §2) ───────────────────────────────────────────────────────

export interface ApiErrorDetail {
  field: string;
  issue: string;
}

export interface ApiErrorBody {
  code: string;
  message: string;
  details?: ApiErrorDetail[] | null;
}

export interface ApiErrorEnvelope {
  error: ApiErrorBody;
}

// ── Portföy (04 §4) ──────────────────────────────────────────────────────────

/** BES kalemi — devlet katkısı kendi katkısından AYRI (CLAUDE.md §1). */
export interface Bes {
  ownContribution: number;
  stateContribution: number;
  vestingState: VestingState;
}

/** GET /api/holdings kalemi. Hesaplanamayan alanlar null. */
export interface Holding {
  id: string;
  assetType: AssetType;
  name: string;
  symbol: string | null;
  currency: CurrencyCode;
  unit: string;
  quantity: number;
  avgCost: number;
  currentPrice: number | null;
  totalCost: number;
  currentValue: number | null;
  profit: number | null;
  returnRatio: number | null;
  weight: number;
  bes: Bes | null;
}

/** Dağılım dilimi (donut + legend). */
export interface AllocationSlice {
  assetType: AssetType;
  name: string;
  value: number;
  weight: number;
}

/** GET /api/portfolio/summary. realReturnRatio enflasyon yoksa null. */
export interface PortfolioSummary {
  baseCurrency: CurrencyCode;
  totalValue: number;
  totalCost: number;
  netProfit: number;
  returnRatio: number | null;
  realReturnRatio: number | null;
  allocation: AllocationSlice[];
  asOf: string;
}

/** Bir pozisyona alış/satış (POST .../transactions, POST /holdings içinde). */
export interface TransactionInput {
  type: TransactionType;
  quantity: number;
  unitPrice: number;
  fee?: number;
  date?: string | null;
}

/** POST /api/holdings — ilk işlemiyle yeni pozisyon. */
export interface CreateHoldingInput {
  assetType: AssetType;
  name: string;
  symbol?: string | null;
  currency: CurrencyCode;
  unit: string;
  transaction: TransactionInput;
}

/** PUT /api/holdings/{id} — güncel fiyatı elle güncelle (FR-1.8). */
export interface UpdateHoldingInput {
  currentPrice: number | null;
}

// ── Ayarlar (04 §4) ──────────────────────────────────────────────────────────

export interface Settings {
  baseCurrency: CurrencyCode;
}

export interface UpdateSettingsInput {
  baseCurrency: CurrencyCode;
}
