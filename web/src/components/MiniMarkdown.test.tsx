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

  // ── T6.8 · Bağlantı ────────────────────────────────────────────────────────

  it("dış bağlantıyı yeni sekmede açar ve rel korumasını koyar", () => {
    const { container } = render(
      <MiniMarkdown markdown={"Kaynak: [TÜİK](https://data.tuik.gov.tr) verisi."} />,
    );

    const a = container.querySelector("a");
    expect(a).not.toBeNull();
    expect(a?.getAttribute("href")).toBe("https://data.tuik.gov.tr");
    expect(a?.getAttribute("target")).toBe("_blank");
    // noopener OLMADAN yeni sekme, açılan sayfaya window.opener erişimi verir.
    expect(a?.getAttribute("rel")).toBe("noopener noreferrer");
    expect(a?.textContent).toContain("TÜİK");
    // Paragrafın kalan metni kaybolmamalı.
    expect(container.textContent).toContain("verisi.");
  });

  it("uygulama içi yolu aynı sekmede açar", () => {
    const { container } = render(<MiniMarkdown markdown={"[Sözlüğe git](/egitim/sozluk)"} />);

    const a = container.querySelector("a");
    expect(a?.getAttribute("href")).toBe("/egitim/sozluk");
    expect(a?.getAttribute("target")).toBeNull();
  });

  it("mailto bağlantısını yeni sekmeye zorlamaz", () => {
    const { container } = render(<MiniMarkdown markdown={"[Yaz](mailto:bilgi@example.org)"} />);

    const a = container.querySelector("a");
    expect(a?.getAttribute("href")).toBe("mailto:bilgi@example.org");
    expect(a?.getAttribute("target")).toBeNull();
  });

  it.each([
    ["javascript şeması", "[Tıkla](javascript:alert(1))"],
    ["data şeması", "[Tıkla](data:text/html;base64,PHNjcmlwdD4=)"],
    ["vbscript şeması", "[Tıkla](vbscript:msgbox)"],
    ["protokole göreli adres", "[Tıkla](//evil.example)"],
  ])("güvensiz hedefi bağlantıya ÇEVİRMEZ — %s", (_ad, markdown) => {
    const { container } = render(<MiniMarkdown markdown={markdown} />);

    // Bağlantı üretilmez; kaynak metin okunur biçimde kalır (içerik kaybolmaz).
    expect(container.querySelector("a")).toBeNull();
    expect(container.textContent).toContain("Tıkla");
  });

  it("kontrol karakteriyle gizlenmiş şemayı yakalar", () => {
    // "java\tscript:" tarayıcıda javascript: olarak çözülür — şema denetimi
    // ham metne bakarsa atlanır.
    const { container } = render(
      <MiniMarkdown markdown={"[Tıkla](java\tscript:alert(1))"} />,
    );

    expect(container.querySelector("a")).toBeNull();
  });

  it("bağlantı metnindeki HTML'i metin olarak bırakır", () => {
    const { container } = render(
      <MiniMarkdown markdown={"[<img src=x onerror=alert(1)>](https://example.org)"} />,
    );

    expect(container.querySelector("img")).toBeNull();
    expect(container.querySelector("a")?.textContent).toContain("<img src=x onerror=alert(1)>");
  });

  // ── T6.8 · Tablo ───────────────────────────────────────────────────────────

  it("boru işaretli tabloyu başlık + gövde olarak çizer", () => {
    const { container } = render(
      <MiniMarkdown
        markdown={[
          "| Kavram | Tanıtım | Tutar |",
          "| --- | :-: | ---: |",
          "| Enflasyon | S0-L4 | 1.000,50 ₺ |",
          "| Reel getiri | S1-L1 | 2.400,00 ₺ |",
        ].join("\n")}
      />,
    );

    const headers = container.querySelectorAll("thead th");
    expect(headers).toHaveLength(3);
    expect(headers[0].textContent).toBe("Kavram");

    const rows = container.querySelectorAll("tbody tr");
    expect(rows).toHaveLength(2);
    expect(rows[0].querySelectorAll("td")[2].textContent).toBe("1.000,50 ₺");

    // Hizalama satırı okunur: `:-:` orta, `---:` sağ (TR para sütunu).
    expect(rows[0].querySelectorAll("td")[1].getAttribute("style")).toContain("center");
    expect(rows[0].querySelectorAll("td")[2].getAttribute("style")).toContain("right");

    // Dar ekranda SAYFA değil tablo kaydırılsın diye kendi kabı var.
    expect(container.querySelector(".md-table-wrap table")).not.toBeNull();
  });

  it("tablo hücrelerinde kalın ve bağlantı işler", () => {
    const { container } = render(
      <MiniMarkdown
        markdown={["| Kurum | Belge |", "| --- | --- |", "| **TÜİK** | [TÜFE](https://example.org) |"].join(
          "\n",
        )}
      />,
    );

    expect(container.querySelector("tbody strong")?.textContent).toBe("TÜİK");
    expect(container.querySelector("tbody a")?.getAttribute("href")).toBe("https://example.org");
  });

  it("eksik ve fazla hücreli satırları başlık sütun sayısına göre hizalar", () => {
    const { container } = render(
      <MiniMarkdown
        markdown={[
          "| A | B | C |",
          "| --- | --- | --- |",
          "| yalnız bir |",
          "| bir | iki | üç | fazladan |",
        ].join("\n")}
      />,
    );

    const rows = container.querySelectorAll("tbody tr");
    expect(rows[0].querySelectorAll("td")).toHaveLength(3);
    expect(rows[0].querySelectorAll("td")[1].textContent).toBe("");
    expect(rows[1].querySelectorAll("td")).toHaveLength(3);
    expect(container.textContent).not.toContain("fazladan");
  });

  it("hizalama satırı olmadan `|` içeren satırı tablo SANMAZ", () => {
    // Geriye dönük davranış: eski içerikte boru işareti düz metin olarak geçebilir.
    const { container } = render(<MiniMarkdown markdown={"Seçenekler | ayraçla yazıldı."} />);

    expect(container.querySelector("table")).toBeNull();
    expect(container.querySelector("p")?.textContent).toBe("Seçenekler | ayraçla yazıldı.");
  });
});
