import { cssVariables } from "@finans/shared";

/**
 * @finans/shared token'larından üretilen :root CSS değişkenlerini, ilk paint'ten
 * ÖNCE (createRoot'tan önce çağrılır) <style> olarak enjekte eder. Token'lar tek
 * kaynak (DESIGN.md → shared/theme); web ve mobil aynı değerleri kullanır.
 */
export function applyTheme(): void {
  const style = document.createElement("style");
  style.id = "finans-theme-vars";
  style.textContent = cssVariables();
  document.head.appendChild(style);
}
