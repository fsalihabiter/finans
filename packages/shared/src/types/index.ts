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

/** Tek bir BES katkı ödemesi kaydı (T-BES.6). source: "Manual" | "Plan". */
export interface BesContribution {
  ownAmount: number;
  stateAmount: number;
  paidAtUtc: string;
  source: string;
}

/** BES kalemi — devlet katkısı kendi katkısından AYRI (CLAUDE.md §1). */
export interface Bes {
  ownContribution: number;
  stateContribution: number;
  vestingState: VestingState;
  joinedAtUtc: string | null;
  /** Katkı ödeme kayıtları (en yeni üstte) — işlem geçmişi. */
  contributions: BesContribution[];
  /** Bu ayın katkısı henüz girilmedi mi? ("Katkı payını gir" hatırlatması). */
  contributionDue: boolean;
}

/** Bir pozisyonun geçmiş işlemi (detayda gösterilir). */
export interface Transaction {
  id: string;
  type: TransactionType;
  quantity: number;
  unitPrice: number;
  fee: number;
  transactedAtUtc: string;
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
  /** Yalnızca tekil holding (GET /holdings/{id}) yanıtında dolu; listede null. */
  transactions?: Transaction[] | null;
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

// ── Canlı Fiyat & Eğitici Notlar (Faz 2 — 04 §5) ─────────────────────────────

/** Tek enstrümanın güncel fiyatı (GET /api/prices). stale → son bilinen ("yaklaşık"). */
export interface PriceDto {
  kind: "Gold" | "Currency";
  currency: CurrencyCode;
  price: number;
  quoteCurrency: CurrencyCode;
  asOfUtc: string;
  source: string;
  stale: boolean;
}

/** GET /api/prices yanıtı. Fiyatlar kullanıcı-bağımsız (global piyasa). */
export interface PricesResponse {
  refreshedAtUtc: string;
  fromCache: boolean;
  hasStale: boolean;
  failedSources: string[];
  prices: PriceDto[];
}

/** Eğitici not önem düzeyi. */
export type NudgeSeverity = "Info" | "Warning";

/** Kural tabanlı eğitici not (GET /api/portfolio/nudges). Tavsiye DEĞİL — farkındalık. */
export interface Nudge {
  id: string;
  icon: string;
  title: string;
  body: string;
  severity: NudgeSeverity;
}

/** GET /api/portfolio/nudges yanıtı. */
export interface NudgesResponse {
  nudges: Nudge[];
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

/** POST /api/holdings/{id}/bes-contribution — BES'e aylık katkı (kendi + devlet ops.). */
export interface AddBesContributionInput {
  ownAmount: number;
  stateAmount?: number | null;
  /** Ödeme tarihi (verilmezse şimdi). Devlet katkısı oranı bu tarihe göre seçilir (2026 öncesi %30, sonrası %20). */
  paidAtUtc?: string | null;
}

/** PUT /api/holdings/{id}/bes — BES sözleşme alanları (şimdilik başlangıç tarihi; hak edişi yeniden türetir). */
export interface UpdateBesInput {
  joinedAtUtc: string | null;
}

/** POST /api/holdings/{id}/bes/contributions — düzenli katkıyı tarih aralığından üretir (T-BES.6). */
export interface GenerateBesContributionsInput {
  monthlyAmount: number;
  day: number;
  fromUtc: string;
  toUtc: string;
}

// ── Ayarlar (04 §4) ──────────────────────────────────────────────────────────

export interface Settings {
  baseCurrency: CurrencyCode;
}

export interface UpdateSettingsInput {
  baseCurrency: CurrencyCode;
}
