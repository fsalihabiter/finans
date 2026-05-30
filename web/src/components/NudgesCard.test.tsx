import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { NudgesCard } from "./NudgesCard";
import type { Nudge } from "@finans/shared";

const nudges: Nudge[] = [
  { id: "concentration", icon: "⚖️", title: "Yoğunlaşma", body: "İki varlıkta %64.", severity: "Warning" },
  { id: "low-cash", icon: "💵", title: "Nakit tamponu", body: "Nakit %0,7.", severity: "Info" },
];

describe("NudgesCard", () => {
  it("notları başlık + gövde ile ve disclaimer'la gösterir", () => {
    render(<NudgesCard nudges={nudges} />);
    expect(screen.getByText("Yoğunlaşma.")).toBeInTheDocument();
    expect(screen.getByText(/İki varlıkta %64/)).toBeInTheDocument();
    expect(screen.getByText("Nakit tamponu.")).toBeInTheDocument();
    // NFR-2: yatırım tavsiyesi değildir çerçevesi her zaman görünür.
    expect(screen.getByText(/yatırım tavsiyesi değildir/i)).toBeInTheDocument();
  });

  it("not yoksa hiçbir şey çizmez", () => {
    const { container } = render(<NudgesCard nudges={[]} />);
    expect(container.firstChild).toBeNull();
  });
});
