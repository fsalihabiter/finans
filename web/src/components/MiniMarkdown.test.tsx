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
});
