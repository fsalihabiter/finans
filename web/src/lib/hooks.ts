// TanStack Query hook'ları — @finans/shared API istemcisinin üstünde sunucu-durumu
// katmanı (13 §3). Tek veri kaynağı; mutation'lar ilgili query'leri invalidate eder.
// (Mobil geldiğinde bu hook'lar shared'a taşınabilir.)

import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import type {
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
