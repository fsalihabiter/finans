// TanStack Query hook'ları — @finans/shared API istemcisinin üstünde sunucu-durumu
// katmanı (13 §3). Tek veri kaynağı; mutation'lar ilgili query'leri invalidate eder.
// (Mobil geldiğinde bu hook'lar shared'a taşınabilir.)

import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import type {
  AddBesContributionInput,
  BesProjectionInput,
  CreateBesInput,
  CreateHoldingInput,
  CurrencyCode,
  GenerateBesContributionsInput,
  PortfolioHistoryPeriod,
  StockHistoryRange,
  SubmitDiagnosticInput,
  SubmitQuizAttemptInput,
  TransactionInput,
  UpdateBesContributionInput,
  UpdateBesInput,
  UpdateHoldingInput,
  UpdateLessonProgressInput,
  UpdateSettingsInput,
} from "@finans/shared";
import { api } from "./api";

/** Query anahtarları — invalidation için tek yerden. */
export const queryKeys = {
  summary: (baseCurrency?: CurrencyCode) => ["summary", baseCurrency ?? "default"] as const,
  portfolioHistory: (period: string, baseCurrency?: CurrencyCode) =>
    ["portfolio-history", period, baseCurrency ?? "default"] as const,
  scenario: (holdingId: string) => ["scenario", holdingId] as const,
  holdings: (baseCurrency?: CurrencyCode) => ["holdings", baseCurrency ?? "default"] as const,
  holding: (id: string) => ["holding", id] as const,
  settings: ["settings"] as const,
  prices: ["prices"] as const,
  nudges: ["nudges"] as const,
  commentary: ["commentary"] as const,
  stockMetrics: (symbol: string) => ["stock-metrics", symbol] as const,
  stockExplain: (symbol: string) => ["stock-explain", symbol] as const,
  stockHistory: (symbol: string, range: string) => ["stock-history", symbol, range] as const,
  eduTracks: ["edu-tracks"] as const,
  eduTrackLessons: (slug: string) => ["edu-track-lessons", slug] as const,
  eduLesson: (slug: string) => ["edu-lesson", slug] as const,
  eduByConcept: (conceptKey: string) => ["edu-by-concept", conceptKey] as const,
};

export function usePortfolioSummary(baseCurrency?: CurrencyCode) {
  return useQuery({
    queryKey: queryKeys.summary(baseCurrency),
    queryFn: () => api.getSummary(baseCurrency),
  });
}

/**
 * Portföy değer geçmişi (T5.3 — Değer Seyri). Backend seriyi 60s cache'ler (UserId'li);
 * dönem değişimi ucuz (dilimleme). Geçmiş gösterimi — tahmin değil (CLAUDE.md §2).
 */
export function usePortfolioHistory(period: PortfolioHistoryPeriod, baseCurrency?: CurrencyCode) {
  return useQuery({
    queryKey: queryKeys.portfolioHistory(period, baseCurrency),
    queryFn: () => api.getPortfolioHistory(period, baseCurrency),
    staleTime: 60_000,
    retry: 1,
  });
}

/**
 * Senaryo v1 (T5.4): tek pozisyon "nakitte dursaydı" karşılaştırması.
 * Geçmişe dönük — tahmin değil (CLAUDE.md §2).
 */
export function useScenario(holdingId: string) {
  return useQuery({
    queryKey: queryKeys.scenario(holdingId),
    queryFn: () => api.getScenario(holdingId),
    enabled: holdingId.length > 0,
    staleTime: 60_000,
    retry: 1,
  });
}

export function useHoldings(baseCurrency?: CurrencyCode) {
  return useQuery({
    queryKey: queryKeys.holdings(baseCurrency),
    queryFn: () => api.getHoldings(baseCurrency),
  });
}

export function useHolding(id: string) {
  return useQuery({
    queryKey: queryKeys.holding(id),
    queryFn: () => api.getHolding(id),
    enabled: Boolean(id),
  });
}

export function useSettings() {
  return useQuery({
    queryKey: queryKeys.settings,
    queryFn: () => api.getSettings(),
  });
}

// Otomatik tazeleme: sekme önplandayken 5 dk'da bir + sekmeye dönünce. Backend 10 dk
// cache'lediği için poll'lar çoğunlukla cache-hit (dış API'ye gitmez); arka planda durur
// (refetchIntervalInBackground varsayılan kapalı). "Yenile" butonu açık kontrol olarak kalır.
const LIVE_REFETCH_MS = 5 * 60_000;

/** Canlı altın/döviz fiyatları (T2.6). Sorgu backend'de tazeleme turunu tetikler (cache'li). */
export function usePrices() {
  return useQuery({
    queryKey: queryKeys.prices,
    queryFn: () => api.getPrices(),
    staleTime: 60_000,
    refetchInterval: LIVE_REFETCH_MS,
    refetchOnWindowFocus: true,
  });
}

/** Kural tabanlı eğitici notlar (T2.6). */
export function useNudges() {
  return useQuery({
    queryKey: queryKeys.nudges,
    queryFn: () => api.getNudges(),
    staleTime: 120_000,
    refetchInterval: LIVE_REFETCH_MS,
    refetchOnWindowFocus: true,
  });
}

/**
 * LLM yorum kartları (T3.8 — 07). Pahalı dış çağrı: uzun staleTime + sadece elle tazele.
 * Otomatik refetch yok; kullanıcı butonla yeniler. Disclaimer UI tarafından sabitlenir.
 */
export function useCommentary() {
  return useQuery({
    queryKey: queryKeys.commentary,
    queryFn: () => api.getCommentary(),
    staleTime: 60 * 60_000, // 1 saat — günde 1-2 üretim hedefi (NFR-9)
    gcTime: 24 * 60 * 60_000,
    refetchOnWindowFocus: false,
    refetchOnMount: false,
    retry: 1,
  });
}

/**
 * Hisse metrikleri (T4.4 — 04 §7). Backend 1 saat cache'ler; istemci de aynı ritimde.
 * `symbol` boşken sorgu kapalı (arama yapılmadan istek yok).
 */
export function useStockMetrics(symbol: string) {
  return useQuery({
    queryKey: queryKeys.stockMetrics(symbol),
    queryFn: () => api.getStockMetrics(symbol),
    enabled: symbol.length > 0,
    staleTime: 60 * 60_000,
    refetchOnWindowFocus: false,
    retry: (count, error) =>
      // 404/400/502 sözleşmeli hatalarda tekrar deneme anlamsız; yalnız ağ hatasında 1 kez.
      count < 1 && !(error instanceof Object && "status" in error),
  });
}

/**
 * Hisse metrik açıklaması (T4.4 — 07 §8). LLM pahalı: metrikler BAŞARILI olduktan sonra
 * tetiklenir (geçersiz/bilinmeyen sembolde LLM'e hiç gidilmez); backend sembol başına
 * 24 saat cache'ler. Disclaimer UI tarafından sabitlenir.
 */
export function useStockExplain(symbol: string, enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.stockExplain(symbol),
    queryFn: () => api.getStockExplain(symbol),
    enabled: enabled && symbol.length > 0,
    staleTime: 60 * 60_000,
    gcTime: 24 * 60 * 60_000,
    refetchOnWindowFocus: false,
    refetchOnMount: false,
    retry: 1,
  });
}

/**
 * Hisse fiyat geçmişi (T4.5). Kaynak anahtarsız (Stooq) + backend tüm seriyi 24s cache'ler;
 * dönem değişimi yalnız dilimleme (ucuz). Geçmiş gösterimi — tahmin değil.
 */
export function useStockHistory(symbol: string, range: StockHistoryRange, enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.stockHistory(symbol, range),
    queryFn: () => api.getStockHistory(symbol, range),
    enabled: enabled && symbol.length > 0,
    staleTime: 60 * 60_000,
    refetchOnWindowFocus: false,
    retry: 1,
  });
}

/** Pozisyon/özet değişimlerinden sonra portföy görünümlerini tazele. */
function useInvalidatePortfolio() {
  const qc = useQueryClient();
  return () => {
    void qc.invalidateQueries({ queryKey: ["summary"] });
    void qc.invalidateQueries({ queryKey: ["holdings"] });
    void qc.invalidateQueries({ queryKey: ["portfolio-history"] });
    void qc.invalidateQueries({ queryKey: ["scenario"] });
  };
}

export function useCreateHolding() {
  const invalidate = useInvalidatePortfolio();
  return useMutation({
    mutationFn: (input: CreateHoldingInput) => api.createHolding(input),
    onSuccess: invalidate,
  });
}

export function useCreateBes() {
  const invalidate = useInvalidatePortfolio();
  return useMutation({
    mutationFn: (input: CreateBesInput) => api.createBes(input),
    onSuccess: invalidate,
  });
}

export function useAddTransaction(id: string) {
  const invalidate = useInvalidatePortfolio();
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: TransactionInput) => api.addTransaction(id, input),
    onSuccess: () => {
      invalidate();
      void qc.invalidateQueries({ queryKey: queryKeys.holding(id) });
    },
  });
}

export function useUpdateTransaction(id: string) {
  const invalidate = useInvalidatePortfolio();
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ transactionId, input }: { transactionId: string; input: TransactionInput }) =>
      api.updateTransaction(id, transactionId, input),
    onSuccess: () => {
      invalidate();
      void qc.invalidateQueries({ queryKey: queryKeys.holding(id) });
    },
  });
}

export function useDeleteTransaction(id: string) {
  const invalidate = useInvalidatePortfolio();
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (transactionId: string) => api.deleteTransaction(id, transactionId),
    onSuccess: () => {
      invalidate();
      void qc.invalidateQueries({ queryKey: queryKeys.holding(id) });
    },
  });
}

export function useUpdateHolding(id: string) {
  const invalidate = useInvalidatePortfolio();
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateHoldingInput) => api.updateHolding(id, input),
    onSuccess: () => {
      invalidate();
      void qc.invalidateQueries({ queryKey: queryKeys.holding(id) });
    },
  });
}

export function useAddBesContribution(id: string) {
  const invalidate = useInvalidatePortfolio();
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: AddBesContributionInput) => api.addBesContribution(id, input),
    onSuccess: () => {
      invalidate();
      void qc.invalidateQueries({ queryKey: queryKeys.holding(id) });
    },
  });
}

export function useUpdateBes(id: string) {
  const invalidate = useInvalidatePortfolio();
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateBesInput) => api.updateBes(id, input),
    onSuccess: () => {
      invalidate();
      void qc.invalidateQueries({ queryKey: queryKeys.holding(id) });
    },
  });
}

export function useGenerateBesContributions(id: string) {
  const invalidate = useInvalidatePortfolio();
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: GenerateBesContributionsInput) => api.generateBesContributions(id, input),
    onSuccess: () => {
      invalidate();
      void qc.invalidateQueries({ queryKey: queryKeys.holding(id) });
    },
  });
}

export function useUpdateBesContribution(id: string) {
  const invalidate = useInvalidatePortfolio();
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ contributionId, input }: { contributionId: string; input: UpdateBesContributionInput }) =>
      api.updateBesContribution(id, contributionId, input),
    onSuccess: () => {
      invalidate();
      void qc.invalidateQueries({ queryKey: queryKeys.holding(id) });
    },
  });
}

/**
 * BES eğitici projeksiyon (T-BES.5) — kullanıcının verdiği varsayımlardan birikim illüstrasyonu.
 * Sonuç pozisyonu DEĞİŞTİRMEZ; bu nedenle invalidate yok (saf hesap). Yatırım tavsiyesi DEĞİL.
 */
export function useBesProjection(id: string) {
  return useMutation({
    mutationFn: (input: BesProjectionInput) => api.projectBes(id, input),
  });
}

export function useDeleteBesContribution(id: string) {
  const invalidate = useInvalidatePortfolio();
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (contributionId: string) => api.deleteBesContribution(id, contributionId),
    onSuccess: () => {
      invalidate();
      void qc.invalidateQueries({ queryKey: queryKeys.holding(id) });
    },
  });
}

export function useDeleteHolding() {
  const invalidate = useInvalidatePortfolio();
  return useMutation({
    mutationFn: (id: string) => api.deleteHolding(id),
    onSuccess: invalidate,
  });
}

export function useUpdateSettings() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateSettingsInput) => api.updateSettings(input),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: queryKeys.settings });
      // baz para birimi değişince tüm parasal görünümler yeniden hesaplanmalı
      void qc.invalidateQueries({ queryKey: ["summary"] });
      void qc.invalidateQueries({ queryKey: ["holdings"] });
    },
  });
}

// ── Eğitim (04 §7.5) ──
// ── Tanılama testi (T6.6, 15 §4) ─────────────────────────────────────────────

/** Kullanıcının okuryazarlık profili — onboarding gösterilecek mi? */
export function useLiteracyProfile() {
  return useQuery({
    queryKey: ["edu-profile"],
    queryFn: () => api.getLiteracyProfile(),
    staleTime: 5 * 60_000,
  });
}

/** 8 tanılama sorusu (cevap anahtarı gelmez). */
export function useDiagnosticQuestions(enabled: boolean) {
  return useQuery({
    queryKey: ["edu-diagnostic"],
    queryFn: () => api.getDiagnostic(),
    staleTime: Infinity, // soru bankası statik
    enabled,
  });
}

export function useSubmitDiagnostic() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: SubmitDiagnosticInput) => api.submitDiagnostic(input),
    onSuccess: () => {
      // Profil değişti → ders derinliği (T6.7) ve onboarding kararı tazelenmeli.
      void qc.invalidateQueries({ queryKey: ["edu-profile"] });
      void qc.invalidateQueries({ queryKey: ["edu-lesson"] });
    },
  });
}

export function useEducationTracks() {
  return useQuery({
    queryKey: queryKeys.eduTracks,
    queryFn: () => api.getEducationTracks(),
    staleTime: 5 * 60_000, // içerik statik-benzeri; sık tazeleme gereksiz
  });
}

export function useTrackLessons(slug: string) {
  return useQuery({
    queryKey: queryKeys.eduTrackLessons(slug),
    queryFn: () => api.getTrackLessons(slug),
    enabled: slug.length > 0,
  });
}

export function useLesson(slug: string) {
  return useQuery({
    queryKey: queryKeys.eduLesson(slug),
    queryFn: () => api.getLesson(slug),
    enabled: slug.length > 0,
  });
}

export function useLessonsByConcept(conceptKey: string) {
  return useQuery({
    queryKey: queryKeys.eduByConcept(conceptKey),
    queryFn: () => api.getLessonsByConcept(conceptKey),
    enabled: conceptKey.length > 0,
  });
}

export function useUpdateLessonProgress(lessonId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateLessonProgressInput) => api.updateLessonProgress(lessonId, input),
    onSuccess: () => {
      // Ders listesi (durum/kilit) + ders detayı yeniden çekilsin.
      void qc.invalidateQueries({ queryKey: ["edu-track-lessons"] });
      void qc.invalidateQueries({ queryKey: ["edu-lesson"] });
      void qc.invalidateQueries({ queryKey: ["edu-by-concept"] });
    },
  });
}

export function useSubmitQuizAttempt(quizId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: SubmitQuizAttemptInput) => api.submitQuizAttempt(quizId, input),
    onSuccess: () => {
      // Testi GEÇMEK dersi tamamlar (öğrenme kapısı, backend) → ders durumu ve
      // sonraki dersin kilidi değişmiş olabilir; liste + detay tazelenmeli.
      void qc.invalidateQueries({ queryKey: ["edu-track-lessons"] });
      void qc.invalidateQueries({ queryKey: ["edu-lesson"] });
      void qc.invalidateQueries({ queryKey: ["edu-by-concept"] });
    },
  });
}
