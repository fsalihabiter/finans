import { describe, expect, it } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { CommentaryTabs } from "./CommentaryTabs";
import type { CommentaryCard } from "@finans/shared";

const cards: CommentaryCard[] = [
  { emoji: "⚖️", title: "Yoğunlaşma", body: "Birinci kartın gövde metni burada.", tags: ["a"] },
  { emoji: "📉", title: "Reel Getiri", body: "İkinci kartın gövde metni burada.", detail: "Kavram açıklaması." },
];

describe("CommentaryTabs (T4.5)", () => {
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
});
