import { describe, it, expect } from "vitest";
import { tokens, cssVariables } from "./index";

describe("design tokens (DESIGN.md)", () => {
  it("DESIGN.md renk değerlerini taşır", () => {
    expect(tokens.color.gold).toBe("#E0B255");
    expect(tokens.color.mint).toBe("#5FC9A0");
    expect(tokens.color.bg).toBe("#14110D");
  });
});

describe("cssVariables()", () => {
  const css = cssVariables();

  it(":root bloğu ve çekirdek değişkenleri üretir", () => {
    expect(css).toContain(":root {");
    expect(css).toContain("--bg: #14110D;");
    expect(css).toContain("--gold: #E0B255;");
  });

  it("camelCase/sayı sınırlarını doğru kebab'lar", () => {
    expect(css).toContain("--panel-2: #241F18;");
    expect(css).toContain("--gold-soft: #CAA05A;");
    expect(css).toContain("--muted-2: #6F6557;");
  });

  it("grup öneklerini uygular (font/radius/space/shadow)", () => {
    expect(css).toContain("--font-display:");
    expect(css).toContain("--radius-card: 22px;");
    expect(css).toContain("--space-screen-x: 20px;");
    expect(css).toContain("--shadow-hero:");
  });
});
