import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { createBrowserRouter, RouterProvider } from "react-router-dom";
import { QueryClientProvider } from "@tanstack/react-query";
// Self-hosted variable fontlar (DESIGN.md §3) — CDN yok (CSP/gizlilik dostu).
import "@fontsource-variable/fraunces";
import "@fontsource-variable/hanken-grotesk";
import App from "./App.tsx";
import { PortfolioPage } from "./routes/PortfolioPage.tsx";
import { AnalysisPage } from "./routes/AnalysisPage.tsx";
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
      { path: "analiz", element: <AnalysisPage /> },
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
