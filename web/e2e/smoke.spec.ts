import { test, expect } from "@playwright/test";

// Faz 0 smoke: uygulama yükleniyor, route geçişi çalışıyor, tasarım dili uygulanıyor.
test("portföy sayfası yüklenir ve analiz'e geçilir", async ({ page }) => {
  await page.goto("/");

  await expect(page).toHaveTitle(/Finans/);
  await expect(page.getByRole("heading", { name: "Portföy" })).toBeVisible();
  // Paylaşılan tr-TR format util'i DOM'da (backend bağımsız).
  await expect(page.getByText(/641\.403,00/)).toBeVisible();

  await page.getByRole("link", { name: "Analiz" }).click();
  await expect(page.getByRole("heading", { name: "Analiz" })).toBeVisible();
  await expect(page).toHaveURL(/\/analiz$/);
});
