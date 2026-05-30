import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // /api/* istekleri geliştirmede .NET backend'e (http profili) yönlendirilir.
    proxy: {
      "/api": {
        target: "http://localhost:5298",
        changeOrigin: true,
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
