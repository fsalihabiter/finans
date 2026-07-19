import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { MiniMarkdown } from "./MiniMarkdown";

describe("MiniMarkdown", () => {
  it("başlık, kalın, alıntı, liste ve paragrafı düğümlere çevirir", () => {
    const md = [
      "## Başlık",
      "",
      "Bir **kalın** kelime içeren paragraf.",
      "",
      "> Alıntı satırı",
      "",
      "- ilk madde",
      "- ikinci madde",
    ].join("\n");
    const { container } = render(<MiniMarkdown markdown={md} />);

    expect(screen.getByRole("heading", { level: 3, name: "Başlık" })).toBeInTheDocument();
    expect(container.querySelector("strong")?.textContent).toBe("kalın");
    expect(container.querySelector("blockquote")?.textContent).toBe("Alıntı satırı");
    expect(container.querySelectorAll("li")).toHaveLength(2);
  });

  it("ham HTML enjekte ETMEZ (XSS-güvenli)", () => {
    const { container } = render(<MiniMarkdown markdown={"<img src=x onerror=alert(1)> düz metin"} />);
    expect(container.querySelector("img")).toBeNull();
    expect(container.textContent).toContain("<img src=x onerror=alert(1)> düz metin");
  });

  it("ardışık alıntı satırlarını TEK blockquote'ta birleştirir", () => {
    // REGRESYON (2026-07-20): her `> ` satırı ayrı <blockquote> üretiyordu →
    // çok satırlı alıntı ekranda 4 ayrı kutuya bölünüyordu.
    const { container } = render(
      <MiniMarkdown markdown={["> Birinci satır", "> ikinci satır", "> üçüncü satır"].join("\n")} />,
    );

    const quotes = container.querySelectorAll("blockquote");
    expect(quotes).toHaveLength(1);
    expect(quotes[0].textContent).toBe("Birinci satır ikinci satır üçüncü satır");
  });

  it("boş satır alıntıyı sonlandırır (ayrı alıntılar birleşmez)", () => {
    const { container } = render(
      <MiniMarkdown markdown={["> ilk alıntı", "", "> ayrı alıntı"].join("\n")} />,
    );

    expect(container.querySelectorAll("blockquote")).toHaveLength(2);
  });

  it("sarılmış liste öğesinin devam satırını aynı maddeye ekler", () => {
    // REGRESYON (2026-07-20): uzun madde kaynakta iki satıra sarıldığında
    // ikinci satır ayrı paragraf oluyordu → madde ortadan ikiye bölünüyordu.
    const { container } = render(
      <MiniMarkdown
        markdown={["- Birinci madde uzun", "  ve burada devam ediyor", "- İkinci madde"].join("\n")}
      />,
    );

    const items = container.querySelectorAll("li");
    expect(items).toHaveLength(2);
    expect(items[0].textContent).toBe("Birinci madde uzun ve burada devam ediyor");
    expect(items[1].textContent).toBe("İkinci madde");
    // Devam satırı paragrafa kaçmamalı.
    expect(container.querySelectorAll("p")).toHaveLength(0);
  });
});
