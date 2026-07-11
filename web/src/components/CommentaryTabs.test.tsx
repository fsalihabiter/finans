import { afterEach, describe, expect, it } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { CommentaryTabs } from "./CommentaryTabs";
import type { CommentaryCard } from "@finans/shared";

const cards: CommentaryCard[] = [
  { emoji: "⚖️", title: "Yoğunlaşma", body: "Birinci kartın gövde metni burada.", tags: ["a"] },
  { emoji: "📉", title: "Reel Getiri", body: "İkinci kartın gövde metni burada.", detail: "Kavram açıklaması." },
];

/** Dar ekranı taklit et: jsdom'da matchMedia yok → tanımlayınca accordion yolu açılır. */
function mockNarrowViewport(matches: boolean) {
  (window as unknown as { matchMedia: unknown }).matchMedia = (query: string) => ({
    matches,
    media: query,
    onchange: null,
    addEventListener: () => undefined,
    removeEventListener: () => undefined,
    addListener: () => undefined,
    removeListener: () => undefined,
    dispatchEvent: () => false,
  });
}

afterEach(() => {
  delete (window as unknown as { matchMedia?: unknown }).matchMedia;
});

describe("CommentaryTabs (T4.5) — dikey ray (geniş ekran)", () => {
  it("her kart için sekme üretir; ilk kart panelde görünür", () => {
    render(<CommentaryTabs cards={cards} source="llm" />);

    expect(screen.getAllByRole("tab")).toHaveLength(2);
    expect(screen.getByRole("tab", { name: /Yoğunlaşma/ })).toHaveAttribute("aria-selected", "true");
    expect(screen.getByText("Birinci kartın gövde metni burada.")).toBeInTheDocument();
    expect(screen.getByText("1 / 2")).toBeInTheDocument();
  });

  it("sekmeye tıklayınca o kart panelde gösterilir", () => {
    render(<CommentaryTabs cards={cards} source="llm" />);

    fireEvent.click(screen.getByRole("tab", { name: /Reel Getiri/ }));

    expect(screen.getByText("İkinci kartın gövde metni burada.")).toBeInTheDocument();
    expect(screen.getByText(/Kavram açıklaması/)).toBeInTheDocument();
    expect(screen.queryByText("Birinci kartın gövde metni burada.")).not.toBeInTheDocument();
  });

  it("önceki/sonraki butonlarıyla gezilir; uçlarda devre dışı kalır", () => {
    render(<CommentaryTabs cards={cards} source="llm" />);

    const next = screen.getByRole("button", { name: /Sonraki/ });
    const prev = screen.getByRole("button", { name: /Önceki/ });
    expect(prev).toBeDisabled();

    fireEvent.click(next);
    expect(screen.getByText("İkinci kartın gövde metni burada.")).toBeInTheDocument();
    expect(next).toBeDisabled();
    expect(prev).toBeEnabled();
  });

  it("boş kart listesinde hiçbir şey çizmez", () => {
    const { container } = render(<CommentaryTabs cards={[]} source="llm" />);
    expect(container.firstChild).toBeNull();
  });

  it("tablist dikey işaretlenir; ↓ tuşu sonraki kartı seçer", () => {
    render(<CommentaryTabs cards={cards} source="llm" />);

    const tablist = screen.getByRole("tablist");
    expect(tablist).toHaveAttribute("aria-orientation", "vertical");

    fireEvent.keyDown(tablist, { key: "ArrowDown" });
    expect(screen.getByText("İkinci kartın gövde metni burada.")).toBeInTheDocument();
  });
});

describe("CommentaryTabs (T4.5) — accordion (dar ekran)", () => {
  it("dar ekranda başlıklar aria-expanded'lı buton olur; ilki açık gelir", () => {
    mockNarrowViewport(true);
    render(<CommentaryTabs cards={cards} source="llm" />);

    expect(screen.queryByRole("tab")).not.toBeInTheDocument();
    const first = screen.getByRole("button", { name: /Yoğunlaşma/ });
    expect(first).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByText("Birinci kartın gövde metni burada.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Reel Getiri/ })).toHaveAttribute("aria-expanded", "false");
  });

  it("başka başlığa tıklayınca o açılır, önceki kapanır", () => {
    mockNarrowViewport(true);
    render(<CommentaryTabs cards={cards} source="llm" />);

    fireEvent.click(screen.getByRole("button", { name: /Reel Getiri/ }));

    expect(screen.getByText("İkinci kartın gövde metni burada.")).toBeInTheDocument();
    expect(screen.queryByText("Birinci kartın gövde metni burada.")).not.toBeInTheDocument();
  });

  it("açık başlığa tıklayınca kapanır (tümü kapalı olabilir)", () => {
    mockNarrowViewport(true);
    render(<CommentaryTabs cards={cards} source="llm" />);

    fireEvent.click(screen.getByRole("button", { name: /Yoğunlaşma/ }));

    expect(screen.queryByText("Birinci kartın gövde metni burada.")).not.toBeInTheDocument();
    expect(screen.queryByText("İkinci kartın gövde metni burada.")).not.toBeInTheDocument();
  });

  it("geniş ekranda (matchMedia eşleşmez) ray görünümü kalır", () => {
    mockNarrowViewport(false);
    render(<CommentaryTabs cards={cards} source="llm" />);
    expect(screen.getAllByRole("tab")).toHaveLength(2);
  });
});
