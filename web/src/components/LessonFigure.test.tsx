import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { FIGURE_KEYS, LessonFigure } from "./LessonFigure";

/* ───────────────────────────────────────────────────────────────────────────
 * TD-001 (GD-007) — figür render kapısı.
 *
 * Borç şuydu: figürler yalnız GÖZLE denetleniyordu. M4 mutabakatı anahtarın
 * kayıtlı olduğunu doğruluyor ama bileşenin çizip çizmediğine bakmıyor; seed
 * testleri hiç bakmıyor. Sonuç ölçüldü: REVIEW-004 ve REVIEW-005'te figür
 * katmanından çıkan DÖRT bulgunun dördü de otomatik testten kaçtı.
 *
 * ⚠ Dürüst kapsam: o dört bulgunun yalnız İKİSİ makineye bağlanabilir
 * (ikisi de aynı desen: opak dolgunun üzerine yazı). Kalan ikisi — bir
 * seçeneğin diğerinden vurgulu çizilmesi (RV-054) ve boşluğun tek yönde
 * gösterilmesi (RV-063) — NİYET sorusudur: kodun hangi tonu hangi anlamda
 * kullandığını test bilemez. Onlar İ6 insan incelemesinde kalır ve bu test
 * onları kapsadığını iddia etmez.
 * ─────────────────────────────────────────────────────────────────────────── */

/** Bir SVG özniteliğini sayıya çevirir (yoksa 0 — SVG varsayılanı). */
const num = (el: Element, attr: string) => Number.parseFloat(el.getAttribute(attr) ?? "0") || 0;

describe("Figür kayıt defteri — render kapısı (TD-001)", () => {
  it("kayıtta en az 100 figür var (liste küçülürse haber ver)", () => {
    expect(FIGURE_KEYS.length).toBeGreaterThanOrEqual(100);
  });

  it.each(FIGURE_KEYS)("%s: çiziliyor, etiketli ve boş değil", (key) => {
    const { container } = render(<LessonFigure figureKey={key} />);

    const svg = container.querySelector('svg[role="img"]');
    expect(svg, `'${key}' bir svg[role="img"] çizmeli — bileşen patlarsa ya da ` +
      "boş dönerse ders görselsiz kalır ve hiçbir test bunu görmez").not.toBeNull();

    // Erişilebilirlik sözleşmesi: etiket ekran okuyucunun gördüğü TEK içeriktir.
    const label = svg!.getAttribute("aria-label") ?? "";
    expect(label.length, `'${key}' anlamlı bir aria-label taşımalı`).toBeGreaterThan(10);

    // Gerçekten bir şey çizilmiş mi? (yalnız <defs>/<style> yetmez)
    const shapes = svg!.querySelectorAll("rect, circle, line, path, polyline, polygon, text");
    expect(shapes.length, `'${key}' hiçbir şekil çizmiyor — boş çerçeve`).toBeGreaterThan(0);

    // Altyazı: figürün ne söylediğini metin olarak da taşır.
    const caption = container.querySelector("figcaption");
    expect(caption?.textContent?.trim().length ?? 0,
      `'${key}' bir figcaption taşımalı`).toBeGreaterThan(10);
  });

  /**
   * RV-039 (REVIEW-004) ve RV-053 + RV-055 (REVIEW-005) **aynı desendi**:
   * `fig-seg-lead` tam opak aksan dolgudur (`fill: var(--accent)`); üzerine
   * yazılan `fig-value` (gri-mavi, 10px) ≈1,1:1, `fig-label` (near-white, 11px)
   * ≈2:1 kontrast verir — ikisi de okunmaz. Desen iki ayrı incelemede, üç ayrı
   * figürde tekrarladı ve her seferinde yalnız ekran görüntüsüyle yakalandı.
   *
   * Kural: **opak dolgu yazı taşımaz.** Vurgu gerekiyorsa yazı kutunun DIŞINA
   * çıkar ya da dolgu saydam tona (`fig-seg`) iner — RV-039'da verilen çözüm bu.
   */
  it.each(FIGURE_KEYS)("%s: opak dolgunun (fig-seg-lead) üzerine yazı yazılmamış", (key) => {
    const { container } = render(<LessonFigure figureKey={key} />);

    const opaque = [...container.querySelectorAll("rect.fig-seg-lead")];
    if (opaque.length === 0) return;

    const texts = [...container.querySelectorAll("text")];
    const ihlaller: string[] = [];

    for (const rect of opaque) {
      const x = num(rect, "x");
      const y = num(rect, "y");
      const w = num(rect, "width");
      const h = num(rect, "height");
      if (w === 0 || h === 0) continue;

      for (const t of texts) {
        const tx = num(t, "x");
        const ty = num(t, "y");
        if (tx >= x && tx <= x + w && ty >= y && ty <= y + h) {
          ihlaller.push(`"${t.textContent}" → (${tx},${ty}) kutunun içinde [${x},${y} ${w}×${h}]`);
        }
      }
    }

    expect(ihlaller,
      `'${key}' opak aksan dolgunun ÜZERİNE yazı yazıyor; bu kontrast okunmuyor ` +
      `(RV-039/053/055 deseni). Yazıyı kutunun dışına taşı ya da dolguyu fig-seg yap. ` +
      `İhlaller: ${ihlaller.join(" · ")}`).toHaveLength(0);
  });
});

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
