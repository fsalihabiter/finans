import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, within } from "@testing-library/react";
import { ValueHistoryChart } from "./ValueHistoryChart";
import type { PortfolioHistoryPoint } from "@finans/shared";

// jsdom PointerEvent bilmez → fireEvent.pointerMove clientX'i düşürür; MouseEvent yeterli taklit.
if (typeof window !== "undefined" && !window.PointerEvent) {
  window.PointerEvent = MouseEvent as unknown as typeof PointerEvent;
}

/** jsdom'da elemanların genişliği 0 → hover eşlemesi için gerçekçi bir rect taklidi. */
function mockRect(el: Element, width = 300) {
  vi.spyOn(el, "getBoundingClientRect").mockReturnValue({
    left: 0, top: 0, right: width, bottom: 200, width, height: 200,
    x: 0, y: 0, toJSON: () => ({}),
  } as DOMRect);
}

const points: PortfolioHistoryPoint[] = [
  { date: "2026-07-01", value: 100000, cost: 90000 },
  { date: "2026-07-02", value: 101000, cost: 90000 },
  { date: "2026-07-03", value: 99500, cost: 95000 },
];

describe("ValueHistoryChart (T5.3)", () => {
  it("değer çizgisi + gradyan alan + kesikli yatırılan çizgisini çizer", () => {
    const { container } = render(
      <ValueHistoryChart points={points} currency="TRY" positive />,
    );

    expect(container.querySelector(".spark-line")).not.toBeNull();
    expect(container.querySelector(".spark-area")).not.toBeNull();
    const cost = container.querySelector(".vh-cost-line");
    expect(cost).not.toBeNull();
    expect(cost!.getAttribute("stroke-dasharray")).toBe("5 5"); // referans çizgisi kesikli
  });

  it("uç tarihleri ve iki serinin ortak min/max ölçeğini gösterir", () => {
    render(<ValueHistoryChart points={points} currency="TRY" positive />);

    expect(screen.getByText("01.07.2026")).toBeInTheDocument();
    expect(screen.getByText("03.07.2026")).toBeInTheDocument();
    // Ölçek: max = değer 101.000; min = MALİYET 90.000 (iki serinin ortak aralığı).
    expect(screen.getByText(/101\.000/)).toBeInTheDocument();
    expect(screen.getByText(/90\.000/)).toBeInTheDocument();
    // Lejant iki seriyi adlandırır.
    expect(screen.getByText(/Değer/)).toBeInTheDocument();
    expect(screen.getByText(/Yatırılan/)).toBeInTheDocument();
  });

  it("iki noktadan az veride hiçbir şey çizmez (zarif düşüş sayfada)", () => {
    const { container } = render(
      <ValueHistoryChart points={points.slice(0, 1)} currency="TRY" />,
    );
    expect(container.firstChild).toBeNull();
  });

  it("imleçle gezinince o günün tarihi + iki serinin değeri tooltip'te görünür", () => {
    const { container } = render(
      <ValueHistoryChart points={points} currency="TRY" positive />,
    );

    const area = container.querySelector(".chart-hover-area")!;
    mockRect(area);

    // Sağ uca hareket → son nokta (03.07: değer 99.500, yatırılan 95.000).
    fireEvent.pointerMove(area, { clientX: 300 });

    const tip = container.querySelector(".ch-tip");
    expect(tip).not.toBeNull();
    expect(within(tip as HTMLElement).getByText("03.07.2026")).toBeInTheDocument();
    expect(within(tip as HTMLElement).getByText(/99\.500/)).toBeInTheDocument();
    expect(within(tip as HTMLElement).getByText(/95\.000/)).toBeInTheDocument();
    expect(container.querySelector(".ch-crosshair")).not.toBeNull();

    // Sol uca hareket → ilk nokta; imleç çıkınca tooltip kaybolur.
    fireEvent.pointerMove(area, { clientX: 0 });
    expect(within(container.querySelector(".ch-tip") as HTMLElement).getByText("01.07.2026")).toBeInTheDocument();

    fireEvent.pointerLeave(area);
    expect(container.querySelector(".ch-tip")).toBeNull();
  });
});
