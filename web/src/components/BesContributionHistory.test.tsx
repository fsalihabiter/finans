import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { BesContributionHistory } from "./BesContributionHistory";
import type { BesContribution } from "@finans/shared";

const items: BesContribution[] = [
  { ownAmount: 1000, stateAmount: 200, paidAtUtc: "2026-03-05T00:00:00Z", source: "Plan" },
  { ownAmount: 1000, stateAmount: 300, paidAtUtc: "2025-12-05T00:00:00Z", source: "Manual" },
];

describe("BesContributionHistory", () => {
  it("katkıları kaynak etiketiyle gösterir", () => {
    render(<BesContributionHistory contributions={items} />);
    expect(screen.getByText("Düzenli")).toBeInTheDocument();
    expect(screen.getByText("Tekil")).toBeInTheDocument();
  });

  it("boşsa bilgi metni gösterir", () => {
    render(<BesContributionHistory contributions={[]} />);
    expect(screen.getByText(/Henüz katkı kaydı yok/)).toBeInTheDocument();
  });
});
