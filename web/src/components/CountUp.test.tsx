import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { CountUpCurrency, CountUpNumber, CountUpPercent } from "./CountUp";

/** jsdom'da matchMedia yok → test yolu: hedef değer ANINDA, tam biçimli görünür. */
describe("CountUp ailesi", () => {
  it("CountUpCurrency parayı tr-TR biçimiyle gösterir", () => {
    render(<span data-testid="v"><CountUpCurrency value={641403} currency="TRY" /></span>);
    expect(screen.getByTestId("v")).toHaveTextContent("641.403,00");
  });

  it("CountUpPercent oranı işaretli yüzdeyle gösterir", () => {
    render(<span data-testid="v"><CountUpPercent value={0.516} /></span>);
    expect(screen.getByTestId("v")).toHaveTextContent("+%51,6");
  });

  it("CountUpNumber tam sayıyı yuvarlayarak gösterir", () => {
    render(<span data-testid="v"><CountUpNumber value={8} /></span>);
    expect(screen.getByTestId("v")).toHaveTextContent("8");
  });
});
