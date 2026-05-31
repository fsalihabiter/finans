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
  CreateHoldingInput,
  CurrencyCode,
  TransactionInput,
  UpdateHoldingInput,
  UpdateSettingsInput,
} from "@finans/shared";
import { api } from "./api";

/** Query anahtarları — invalidation için tek yerden. */
export const queryKeys = {
  summary: (baseCurrency?: CurrencyCode) => ["summary", baseCurrency ?? "default"] as const,
  holdings: (baseCurrency?: CurrencyCode) => ["holdings", baseCurrency ?? "default"] as const,
  holding: (id: string) => ["holding", id] as const,
  settings: ["settings"] as const,
  prices: ["prices"] as const,
  nudges: ["nudges"] as const,
};

export function usePortfolioSummary(baseCurrency?: CurrencyCode) {
  return useQuery({
    queryKey: queryKeys.summary(baseCurrency),
    queryFn: () => api.getSummary(baseCurrency),
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

/** Pozisyon/özet değişimlerinden sonra portföy görünümlerini tazele. */
function useInvalidatePortfolio() {
  const qc = useQueryClient();
  return () => {
    void qc.invalidateQueries({ queryKey: ["summary"] });
    void qc.invalidateQueries({ queryKey: ["holdings"] });
  };
}

export function useCreateHolding() {
  const invalidate = useInvalidatePortfolio();
  return useMutation({
    mutationFn: (input: CreateHoldingInput) => api.createHolding(input),
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
