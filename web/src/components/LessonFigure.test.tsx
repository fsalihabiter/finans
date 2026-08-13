import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { LessonFigure } from "./LessonFigure";

// SC-E23 (15 §6.2, T6.18): enflasyon kaydırıcısı — saf/deterministik, klavye
// erişilebilir, tahmin ÜRETMEZ ("olursa"), varlık adı geçmez; bilinmeyen figür
// anahtarı sessizce düşer (ders metni figürsüz de eksiksizdir).
describe("InflationSlider (enflasyon kaydırıcısı)", () => {
  it("iki kaydırıcı (klavye erişilebilir) ve deterministik alım gücü çıktısı verir", () => {
    render(<LessonFigure figureKey="inflation-slider" />);

    // Native range → role "slider"; klavyeyle sürülebilir (tarayıcı doğal davranışı).
    const sliders = screen.getAllByRole("slider");
    expect(sliders).toHaveLength(2);

    // Başlangıç: %30, 5 yıl → 100 / (1,30)^5 ≈ 27. Grafik aria-label deterministik.
    expect(screen.getByRole("img", { name: /yaklaşık 27 liraya iner/ })).toBeInTheDocument();
  });

  it("aynı girdi aynı çıktı: oran %0 olunca erime durur (100 → 100)", () => {
    render(<LessonFigure figureKey="inflation-slider" />);
    const [rateSlider] = screen.getAllByRole("slider");

    fireEvent.change(rateSlider, { target: { value: "0" } });
    expect(screen.getByRole("img", { name: /100 liradan yaklaşık 100 liraya iner/ })).toBeInTheDocument();
  });

  it("tahmin ÜRETMEZ: 'olursa' der, 'olacak' demez; varlık/enstrüman adı geçmez", () => {
    render(<LessonFigure figureKey="inflation-slider" />);

    expect(screen.getByText(/olursa/)).toBeInTheDocument();
    expect(screen.queryByText(/olacak/)).not.toBeInTheDocument();

    // 15 §3.4 — figür/araç enstrüman adı taşımaz, sıralama yapmaz.
    for (const banned of ["altın", "dolar", "hisse", "bitcoin", "mevduat"]) {
      expect(screen.queryByText(new RegExp(banned, "i"))).not.toBeInTheDocument();
    }
  });

  it("bilinmeyen figür anahtarı sessizce düşer (fallback: içerik metinle eksiksiz)", () => {
    const { container } = render(<LessonFigure figureKey="boyle-bir-figur-yok" />);
    expect(container).toBeEmptyDOMElement();
  });
});
