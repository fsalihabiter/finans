import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // /api/* istekleri geliştirmede .NET backend'e yönlendirilir. Hedef VITE_API_TARGET
    // ile değiştirilebilir:
    //   • Varsayılan (VS / dotnet run http profili): http://localhost:5298
    //   • Compose üzerinden (Caddy + TLS, T2.9):      VITE_API_TARGET=https://localhost
    //   • Doğrudan API container'a:                   VITE_API_TARGET=http://localhost:8080 (port açıksa)
    // `secure: false` Caddy'nin internal CA'sını (yerelde güvenilmeyen) kabul eder — sadece dev,
    // production'da Vite yok.
    proxy: {
      "/api": {
        target: process.env.VITE_API_TARGET ?? "http://localhost:5298",
        changeOrigin: true,
        secure: false,
      },
    },
  },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./src/test/setup.ts"],
    css: false,
    // Yalnızca birim/komponent testleri (src). Playwright e2e ayrı koşar (`pnpm e2e`);
    // aksi halde vitest `e2e/*.spec.ts`'i toplayıp Playwright test()'iyle çakışır.
    include: ["src/**/*.{test,spec}.{ts,tsx}"],
  },
});
