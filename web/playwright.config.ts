import { defineConfig, devices } from "@playwright/test";

// Web E2E iskeleti (T0.11, 13 §3). Sistem Chrome'unu kullanır (channel: "chrome")
// → ayrı tarayıcı indirmesi gerekmez. Vite dev sunucusunu otomatik başlatır.
// Faz 1+'da gerçek akışlar (varlık ekle/sil, dağılım) eklenir.
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: "list",
  use: {
    baseURL: "http://localhost:5199",
    trace: "on-first-retry",
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"], channel: "chrome" },
    },
  ],
  webServer: {
    command: "pnpm dev --port 5199 --strictPort",
    url: "http://localhost:5199",
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
});
