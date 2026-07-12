import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { createBrowserRouter, RouterProvider } from "react-router-dom";
import { QueryClientProvider } from "@tanstack/react-query";
// Self-hosted variable fontlar (DESIGN.md §3) — CDN yok (CSP/gizlilik dostu).
import "@fontsource-variable/space-grotesk";
import "@fontsource-variable/inter";
import App from "./App.tsx";
import { PortfolioPage } from "./routes/PortfolioPage.tsx";
import { HoldingDetailPage } from "./routes/HoldingDetailPage.tsx";
import { AnalysisPage } from "./routes/AnalysisPage.tsx";
import { SettingsPage } from "./routes/SettingsPage.tsx";
import { TransactionsPage } from "./routes/TransactionsPage.tsx";
import { PerformancePage } from "./routes/PerformancePage.tsx";
import { ScenarioPage } from "./routes/ScenarioPage.tsx";
import { HoldingsPage } from "./routes/HoldingsPage.tsx";
import { StocksPage } from "./routes/StocksPage.tsx";
import { EducationPage } from "./routes/EducationPage.tsx";
import { queryClient } from "./lib/queryClient.ts";
import { applyTheme } from "./lib/applyTheme.ts";
import "./index.css";

// Tasarım token'larını paint'ten önce uygula (DESIGN.md → @finans/shared/theme).
applyTheme();

const router = createBrowserRouter([
  {
    path: "/",
    element: <App />,
    children: [
      { index: true, element: <PortfolioPage /> },
      { path: "varliklar", element: <HoldingsPage /> },
      { path: "holdings/:id", element: <HoldingDetailPage /> },
      { path: "islemler", element: <TransactionsPage /> },
      { path: "performans", element: <PerformancePage /> },
      { path: "analiz", element: <AnalysisPage /> },
      { path: "senaryo", element: <ScenarioPage /> },
      { path: "hisse", element: <StocksPage /> },
      { path: "egitim", element: <EducationPage /> },
      { path: "ayarlar", element: <SettingsPage /> },
    ],
  },
]);

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  </StrictMode>,
);
