import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { ComingSoonPage } from "./ComingSoonPage";

describe("ComingSoonPage", () => {
  it("başlık, heading, faz rozeti gösterir", () => {
    render(
      <ComingSoonPage
        kicker="Temel analiz"
        title="Hisse Analizi"
        icon="📊"
        heading="Rakamların anlamı"
        description="açıklama"
        phase="Faz 4"
      />,
    );
    expect(screen.getByRole("heading", { name: "Hisse Analizi" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Rakamların anlamı" })).toBeInTheDocument();
    expect(screen.getByText("Faz 4")).toBeInTheDocument();
  });

  it("withDisclaimer ile yatırım tavsiyesi çerçevesini gösterir (NFR-2)", () => {
    render(
      <ComingSoonPage
        kicker="k"
        title="Senaryo"
        icon="⚖"
        heading="h"
        description="d"
        phase="Faz 5"
        withDisclaimer
      />,
    );
    expect(screen.getByRole("note")).toBeInTheDocument();
    expect(screen.getByText(/Yatırım tavsiyesi değildir/i)).toBeInTheDocument();
  });

  it("withDisclaimer olmadan disclaimer göstermez", () => {
    render(
      <ComingSoonPage kicker="k" title="Eğitim" icon="🎓" heading="h" description="d" phase="Faz 5" />,
    );
    expect(screen.queryByRole("note")).toBeNull();
  });
});
