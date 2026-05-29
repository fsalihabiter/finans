import { QueryClient } from "@tanstack/react-query";

// Tek sunucu-durumu kaynağı (mobil ile aynı desen — 13 §3, 02 §3).
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 30_000, retry: 1, refetchOnWindowFocus: false },
  },
});
