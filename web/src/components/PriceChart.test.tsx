import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, within } from "@testing-library/react";
import { PriceChart } from "./PriceChart";
import type { StockPricePoint } from "@finans/shared";

const points: StockPricePoint[] = [
  { date: "2026-07-01", close: 200 },
  { date: "2026-07-02", close: 210 },
  { date: "2026-07-03", close: 205 },
];

// jsdom PointerEvent bilmez → fireEvent.pointerMove clientX'i düşürür; MouseEvent yeterli taklit.
if (typeof window !== "undefined" && !window.PointerEvent) {
  window.PointerEvent = MouseEvent as unknown as typeof PointerEvent;
}

/** jsdom'da elemanların genişliği 0 → hover eşlemesi için gerçekçi bir rect taklidi. */
function mockRect(el: Element, width = 300) {
  vi.spyOn(el, "getBoundingClientRect").mockReturnValue({
    left: 0, top: 0, right: width, bottom: 240, width, height: 240,
    x: 0, y: 0, toJSON: () => ({}),
  } as DOMRect);
}

describe("PriceChart (T4.5 + hover T5.3)", () => {
  it("çizgi + gradyan alan çizer; iki noktadan az veride hiçbir şey çizmez", () => {
    const { container } = render(<PriceChart points={points} currency="USD" positive />);
    expect(container.querySelector(".spark-line")).not.toBeNull();
    expect(container.querySelector(".spark-area")).not.toBeNull();

    const { container: empty } = render(
      <PriceChart points={points.slice(0, 1)} currency="USD" />,
    );
    expect(empty.firstChild).toBeNull();
  });

  it("imleçle gezinince o günün tarihi + kapanışı tooltip'te görünür", () => {
    const { container } = render(<PriceChart points={points} currency="USD" positive />);

    const area = container.querySelector(".chart-hover-area")!;
    mockRect(area);

    // Orta nokta (02.07: 210 $).
    fireEvent.pointerMove(area, { clientX: 150 });

    const tip = container.querySelector(".ch-tip");
    expect(tip).not.toBeNull();
    expect(within(tip as HTMLElement).getByText("02.07.2026")).toBeInTheDocument();
    expect(within(tip as HTMLElement).getByText(/210/)).toBeInTheDocument();

    fireEvent.pointerLeave(area);
    expect(container.querySelector(".ch-tip")).toBeNull();
  });
});
