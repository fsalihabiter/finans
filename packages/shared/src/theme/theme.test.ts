import { describe, it, expect } from "vitest";
import { tokens, cssVariables } from "./index";

describe("design tokens (DESIGN.md — v2 Gece)", () => {
  it("DESIGN.md renk değerlerini taşır", () => {
    expect(tokens.color.accent).toBe("#8A94DC");
    expect(tokens.color.mint).toBe("#45D5A2");
    expect(tokens.color.bg).toBe("#0B0F1E");
  });
});

describe("cssVariables()", () => {
  const css = cssVariables();

  it(":root bloğu ve çekirdek değişkenleri üretir", () => {
    expect(css).toContain(":root {");
    expect(css).toContain("--bg: #0B0F1E;");
    expect(css).toContain("--accent: #8A94DC;");
  });

  it("camelCase/sayı sınırlarını doğru kebab'lar", () => {
    expect(css).toContain("--panel-2: #1A2240;");
    expect(css).toContain("--accent-soft: #A6AEE8;");
    expect(css).toContain("--muted-2: #6B7699;");
    expect(css).toContain("--line-strong: #42507E;");
  });

  it("kategorik varlık renklerini üretir (donut/rozet tek kaynağı)", () => {
    expect(css).toContain("--gold: #E4C06A;");
    expect(css).toContain("--fx: #A3CE6E;");
    expect(css).toContain("--stock: #4FA3F7;");
    expect(css).toContain("--fund: #38CFC4;");
    expect(css).toContain("--eur: #9BAAF3;");
  });

  it("grup öneklerini uygular (font/radius/space/shadow)", () => {
    expect(css).toContain("--font-display:");
    expect(css).toContain("--radius-card: 22px;");
    expect(css).toContain("--space-screen-x: 20px;");
    expect(css).toContain("--shadow-hero:");
  });
});
