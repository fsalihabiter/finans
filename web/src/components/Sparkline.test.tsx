import { describe, expect, it } from "vitest";
import { render } from "@testing-library/react";
import { Sparkline } from "./Sparkline";

describe("Sparkline", () => {
  it("seri için çizgi + gradyan alan path'i çizer (dekoratif, aria-hidden)", () => {
    const { container } = render(<Sparkline values={[100, 220, 180, 400]} />);
    const svg = container.querySelector("svg.sparkline");
    expect(svg).not.toBeNull();
    expect(svg).toHaveAttribute("aria-hidden", "true");
    expect(container.querySelector("path.spark-line")).not.toBeNull();
    expect(container.querySelector("path.spark-area")).not.toBeNull();
  });

  it("çizgi path'i çizim animasyonu için pathLength=1 taşır", () => {
    const { container } = render(<Sparkline values={[1, 2, 3]} />);
    expect(container.querySelector("path.spark-line")).toHaveAttribute("pathLength", "1");
  });

  it("2'den az noktada hiçbir şey çizmez", () => {
    const { container } = render(<Sparkline values={[42]} />);
    expect(container.firstChild).toBeNull();
  });

  it("sabit seride (min=max) sıfıra bölmeden çizer", () => {
    const { container } = render(<Sparkline values={[50, 50, 50]} />);
    const d = container.querySelector("path.spark-line")?.getAttribute("d") ?? "";
    expect(d).not.toContain("NaN");
  });
});
