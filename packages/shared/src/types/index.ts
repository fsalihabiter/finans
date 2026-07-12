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

/** Bir BES katkı kaydının tarihten türetilen durumu (T-BES.8). */
export type BesContributionStatus = "Deposited" | "StatePending" | "Future";

/** Tek bir BES katkı ödemesi kaydı (T-BES.6). source: "Opening" | "Manual" | "Plan". */
export interface BesContribution {
  id: string;
  ownAmount: number;
  stateAmount: number;
  paidAtUtc: string;
  source: string;
  /** Tarihten türetilen durum: yatırıldı / devlet bekliyor / gelecek. */
  status: BesContributionStatus;
  /** Devlet katkısının yatma tarihi (ödeme ayını izleyen ayın sonu). */
  stateDepositDate: string;
}

/**
 * BES kalemi — devlet katkısı kendi katkısından AYRI (CLAUDE.md §1). Toplamlar katkı
 * satırlarından TARİHE göre türetilir: yatırılmış (ownContribution/stateContribution) tabana
 * girer; bekleyenler (ownPending/statePending) ayrı, toplama dahil edilmez.
 */
export interface Bes {
  /** Yatırılmış kendi katkı toplamı (maliyet tabanı). */
  ownContribution: number;
  /** Yatırılmış devlet katkısı toplamı. */
  stateContribution: number;
  /** Henüz ödenmemiş (gelecek tarihli) kendi katkı toplamı. */
  ownPending: number;
  /** Henüz yatmamış devlet katkısı toplamı. */
  statePending: number;
  vestingState: VestingState;
  /** Kademeli hak ediş oranı (0 / 0.15 / 0.35 / 0.60 / 1.00). */
  vestedRate: number;
  /** Hak kazanılan tutar ≈ vestedRate × yatırılmış devlet katkısı (yaklaşık). */
  vestedAmount: number;
  joinedAtUtc: string | null;
  /** Doğum yılı (opsiyonel; %100 hak ediş için 56 yaş kontrolü). */
  birthYear: number | null;
  /** Plan/şirket adı. */
  providerName: string | null;
  /** Katkı ödeme kayıtları (en yeni üstte) — işlem geçmişi. */
  contributions: BesContribution[];
  /** Bu ayın katkısı henüz girilmedi mi? ("Katkı payını gir" hatırlatması). */
  contributionDue: boolean;
  /** Düzenli katkı planı aktif mi? */
  planActive: boolean;
  /** Aktif plan aylık tutarı (varsa). */
  monthlyAmount: number | null;
  /** Ödeme günü (1–28). */
  contributionDay: number | null;
  // ── Fon getirisi (T-BES.10): fon hem own hem state birikimi üzerinde işler;
  //    aynı oran r = fund/(own+state) − 1 her iki katkıya yansır. Fon değeri yoksa
  //    veya taban 0 ise oran null; değerler tabana eşit (kâr/zarar 0).
  /** Fon getiri oranı; (own+state)>0 ve fonValue varsa, aksi halde null. */
  fundReturnRatio: number | null;
  /** Kendi katkının güncel değeri ≈ own × (1+r). */
  ownValue: number;
  /** Kendi katkının fon getiri kâr/zararı ≈ own × r. */
  ownProfit: number;
  /** Devlet katkısının güncel değeri ≈ state × (1+r). */
  stateValue: number;
  /** Devlet katkısının fon getiri kâr/zararı ≈ state × r. */
  stateProfit: number;
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

/** LLM yorum kart göstergesi (opsiyonel; 0..1). */
export interface CommentaryMeter {
  value: number;
  lowLabel: string;
  highLabel: string;
}

/** LLM yorum kartı (GET /api/portfolio/commentary). Tavsiye DEĞİL — eğitici çerçeve. */
export interface CommentaryCard {
  emoji: string;
  title: string;
  body: string;
  meter?: CommentaryMeter | null;
  tags?: string[] | null;
  /** Kavramı sıfırdan öğreten opsiyonel ek paragraf (T3.10) — rakamsız, tavsiyesiz. */
  detail?: string | null;
}

/** GET /api/portfolio/commentary yanıtı. `source`: "llm" | "fallback" | "cache". */
export interface CommentaryResponse {
  cards: CommentaryCard[];
  source: string;
  generatedAtUtc: string;
}

// ── Hisse temel analiz (Faz 4 — 04 §7) ──

/** Dört çekirdek metrik; kaynağın vermediği alan null ("veri yok"). Oranlar 0-1 ondalık. */
export interface StockMetricValues {
  peRatio?: number | null;
  pbRatio?: number | null;
  dividendYield?: number | null;
  earningsGrowth?: number | null;
}

/** Kaba bant etiketleri (KODDA türetilir; tavsiye değil): "low"/"moderate"/"above"/"high"/… */
export interface StockSectorContext {
  peRatio?: string | null;
  pbRatio?: string | null;
  dividendYield?: string | null;
  earningsGrowth?: string | null;
}

/** Portföy değer geçmişi dönem anahtarları (T5.2). */
export type PortfolioHistoryPeriod = "1m" | "3m" | "1y" | "all";

/** Serinin bir günü: portföy değeri + o güne dek yatırılan maliyet (baz pb; tarih ISO). */
export interface PortfolioHistoryPoint {
  date: string;
  value: number;
  cost: number;
}

/** GET /api/portfolio/history yanıtı (T5.2) — geçmiş gösterimi, tahmin DEĞİL. */
export interface PortfolioHistory {
  baseCurrency: CurrencyCode;
  period: string;
  points: PortfolioHistoryPoint[];
  /** Dönem başı → sonu değer değişim oranı (0,12 = %12). */
  changeRatio?: number | null;
  /** TÜM serinin ilk günü (dönemden bağımsız) — "veri şu tarihten beri". */
  firstDate?: string | null;
  asOf: string;
}

/** Senaryo serisinin bir günü (T5.4). inflationAdjustedCost null = enflasyon verisi yok. */
export interface ScenarioPoint {
  date: string;
  value: number;
  cost: number;
  inflationAdjustedCost?: number | null;
}

/** Senaryo özeti — sayılar koddan; yorum/tavsiye YOK (çerçeve UI'da). */
export interface ScenarioSummary {
  currentValue: number;
  invested: number;
  difference: number;
  differenceRatio?: number | null;
  inflationAdjustedInvested?: number | null;
  annualInflationRate?: number | null;
}

/** GET /api/portfolio/scenario/{holdingId} yanıtı (T5.4) — geçmişe dönük, tahmin DEĞİL. */
export interface ScenarioComparison {
  holdingId: string;
  name: string;
  assetType: AssetType;
  baseCurrency: CurrencyCode;
  points: ScenarioPoint[];
  summary: ScenarioSummary;
  firstDate?: string | null;
  asOf: string;
}

/** Fiyat geçmişi dönem anahtarları (T4.5). */
export type StockHistoryRange = "1w" | "1m" | "3m" | "1y" | "5y" | "max";

/** Tek günlük kapanış noktası (tarih ISO "yyyy-MM-dd"). */
export interface StockPricePoint {
  date: string;
  close: number;
}

/** GET /api/stocks/{symbol}/history yanıtı (T4.5) — geçmiş gösterimi, tahmin DEĞİL. */
export interface StockHistory {
  symbol: string;
  range: string;
  points: StockPricePoint[];
  /** Dönem başı → sonu değişim oranı (0,12 = %12). */
  changeRatio?: number | null;
  /** Serinin ilk kaydı — "piyasaya girişten beri" bağlamı. */
  firstTradeDate: string;
  source: string;
}

/** GET /api/stocks/{symbol}/metrics yanıtı (T4.2). */
export interface StockMetrics {
  symbol: string;
  name: string;
  exchange?: string | null;
  currency: string;
  price?: number | null;
  /** Günlük değişim oranı (0,012 = %1,2). */
  changeRatio?: number | null;
  metrics: StockMetricValues;
  sectorContext: StockSectorContext;
  asOfUtc: string;
  source: string;
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
  /** "Bundan sonraki katkılar için kullan": işaretlenirse düzenli plan kurulur (bu tutar/gün; bitiş yok). */
  recurring?: boolean;
}

/** PUT /api/holdings/{id}/bes/contributions/{cid} — tek katkı kaydını düzenle. */
export interface UpdateBesContributionInput {
  ownAmount: number;
  paidAtUtc: string;
}

/** POST /api/holdings/{id}/bes/projection — eğitici varsayımsal projeksiyon girdileri (T-BES.5). */
export interface BesProjectionInput {
  /** Varsayılan aylık katkı (TRY). */
  ownMonthly: number;
  /** Süre (yıl, 1-50). */
  years: number;
  /** Yıllık nominal getiri varsayımı (örn. 0.20 = %20). */
  annualReturnRatio: number;
}

/** BES projeksiyon sonucu — varsayımsal birikim illüstrasyonu (T-BES.5). */
export interface BesProjection {
  totalOwnContribution: number;
  totalStateContribution: number;
  fundValue: number;
  ownValue: number;
  stateValue: number;
  ownProfit: number;
  stateProfit: number;
  annualReturnRatio: number;
  yearly: BesProjectionYear[];
  /** Süre sonunda hak ediş oranı (0/0.15/0.35/0.60/1.00) — sözleşme kademeleri (3/6/10/+56). */
  vestedRateAtEnd: number;
  /** Süre sonunda hak kazanılan devlet katkısı ≈ rate × stateValue. */
  vestedStateAmountAtEnd: number;
}

/** Her yıl sonu birikim/değer (büyüme eğrisi için). */
export interface BesProjectionYear {
  year: number;
  ownContribution: number;
  stateContribution: number;
  fundValue: number;
  ownValue: number;
  stateValue: number;
  ownProfit: number;
  stateProfit: number;
}

/** POST /api/holdings/bes — açılış bakiyesiyle yeni BES pozisyonu (T-BES.8). */
export interface CreateBesInput {
  name: string;
  providerName?: string | null;
  currency: CurrencyCode;
  joinedAtUtc: string;
  birthYear?: number | null;
  /** Güncel toplam fon değeri. */
  currentFundValue: number;
  /** Bugüne dek ödenmiş toplam kendi katkı (açılış maliyeti). */
  openingOwn: number;
  /** Bugüne dek yatmış toplam devlet katkısı. */
  openingState: number;
  monthlyAmount?: number | null;
  contributionDay?: number | null;
}

/** PUT /api/holdings/{id}/bes — BES sözleşme/plan alanları (patch). Verilen alan güncellenir. */
export interface UpdateBesInput {
  joinedAtUtc?: string | null;
  providerName?: string | null;
  birthYear?: number | null;
  monthlyAmount?: number | null;
  contributionDay?: number | null;
  planActive?: boolean | null;
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
