import { describe, it, expect, beforeEach } from "vitest";
import { applyTheme } from "./applyTheme";

describe("applyTheme", () => {
  beforeEach(() => {
    document.getElementById("finans-theme-vars")?.remove();
  });

  it("token CSS değişkenlerini <style> olarak head'e enjekte eder", () => {
    applyTheme();
    const style = document.getElementById("finans-theme-vars");
    expect(style).not.toBeNull();
    expect(style!.textContent).toContain("--gold: #E0B255;");
    expect(style!.textContent).toContain("--font-display:");
  });
});
