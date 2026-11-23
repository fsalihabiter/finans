import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { FIGURE_KEYS, LessonFigure, priceScale } from "./LessonFigure";

/* ───────────────────────────────────────────────────────────────────────────
 * TD-001 (GD-007) — figür render kapısı · REVIEW-006 ile GENİŞLETİLDİ.
 *
 * Borç şuydu: figürler yalnız GÖZLE denetleniyordu. M4 mutabakatı anahtarın
 * kayıtlı olduğunu doğruluyor ama bileşenin çizip çizmediğine bakmıyor; seed
 * testleri hiç bakmıyor. Sonuç ölçüldü: REVIEW-004 ve REVIEW-005'te figür
 * katmanından çıkan DÖRT bulgunun dördü de otomatik testten kaçtı.
 *
 * ⚠ REVIEW-006'nın düzelttiği şey (RV-065 · RV-070 · RV-081): kapının İLK hâli
 * kuralı "opak dolgu yazı taşımaz" diye YAZIYOR ama yalnız `rect.fig-seg-lead`
 * seçiyordu. App.css'te ÜÇ dolgu alfasızdır — `fig-seg-lead`, `fig-bar-pos`,
 * `fig-bar-neg` — ve diğer ikisinde aynı desenin SEKİZ canlı örneği kapıdan
 * yeşil geçiyordu (`no-guarantee` ≈1,31:1 ve ≈1,03:1 kontrastla). Üç kaçış
 * yolu da kapatıldı:
 *   1. sınıf listesi yerine ETKİN OPAKLIK (opak sınıf **ve** opacity ≥ 0,9),
 *   2. yalnız `rect` değil `circle` de (kayıtta opak `circle` var),
 *   3. çapa noktası yerine yazının KAPLADIĞI ALAN — 623 `<text>`in 509'u
 *      `middle`/`end` çapalı, yani çapa kutunun dışında kalırken harfler
 *      kutuyu kaplayabiliyordu (bir pikselle kapıdan kaçılabiliyordu).
 *
 * ⚠ Dürüst kapsam — bu testin kapsamadığı:
 *   · jsdom düzen hesaplamaz: metin genişliği TAHMİN edilir (karakter sayısı ×
 *     punto × ilerleme katsayısı), gerçek kontrast oranı ÖLÇÜLMEZ.
 *   · NİYET soruları: bir seçeneğin diğerinden vurgulu çizilmesi (RV-054) ve
 *     bir olgunun tek yönde gösterilmesi (RV-063) — kodun hangi tonu hangi
 *     anlamda kullandığını test bilemez. İ6 insan incelemesinde kalır (TD-003).
 *   · GÖRSEL BOZULMA: şekillerin çarpışması, kırpılma, toplanmayan yığın.
 *     Bir koordinatı tuvalle karşılaştırmak jsdom'un dışındadır (TD-004).
 * ─────────────────────────────────────────────────────────────────────────── */

/** Bir SVG özniteliğini sayıya çevirir (yoksa 0 — SVG varsayılanı). */
const num = (el: Element, attr: string) => Number.parseFloat(el.getAttribute(attr) ?? "0") || 0;

/**
 * Kayıttaki figür sayısı — **bilinçli olarak yükseltilen** bir sabit.
 *
 * RV-068: önceki hâl `>= 100` idi ve gerçek sayı 101'di, yani taban bir birim
 * boşluk taşıyordu: bir figür silinebilir ve test yeşil kalabilirdi. "En az N"
 * koruması içerik N'i geçtiği anda korumayı bırakır. Eşitlik iki yönde de
 * kıskaçtır: figür eklendiğinde ya da silindiğinde bu sayı da elle değişmeli.
 */
const BEKLENEN_FIGUR_SAYISI = 110;

/**
 * App.css'te **alfa taşımayan** dolgu sınıfları (`fill: var(--x)`), yani üzerine
 * yazı yazıldığında kontrastı düşüren dolgular. Saydam olanlar (`fig-seg` %32,
 * `fig-bar-muted` %30, `fig-card` %8) bilinçli olarak dışarıda.
 */
const OPAK_DOLGU_SINIFLARI = ["fig-seg-lead", "fig-bar-pos", "fig-bar-neg"] as const;

/**
 * Yazı sınıflarının yaklaşık ölçüleri. jsdom `getBBox` sağlamadığı için metnin
 * kapladığı alan tahmin edilir: genişlik ≈ karakter × punto × ilerleme.
 * Değerler App.css'teki `font-size`lardan; ilerleme katsayısı yaygın
 * sans-serif ortalaması (dar tahmin — yanlış pozitif üretmemek için).
 */
const YAZI_OLCULERI: Record<string, { punto: number; ilerleme: number }> = {
  "fig-big": { punto: 17, ilerleme: 0.56 },
  "fig-label": { punto: 11, ilerleme: 0.54 },
  "fig-value": { punto: 10, ilerleme: 0.53 },
};
const VARSAYILAN_OLCU = { punto: 11, ilerleme: 0.54 };

/** Bir `<text>` elemanının kapladığı tahmini dikdörtgen. */
function yaziAlani(t: Element) {
  const sinif = t.getAttribute("class") ?? "";
  const olcu = YAZI_OLCULERI[sinif.trim().split(/\s+/)[0]] ?? VARSAYILAN_OLCU;
  const metin = (t.textContent ?? "").trim();
  const genislik = metin.length * olcu.punto * olcu.ilerleme;

  const x = num(t, "x");
  const y = num(t, "y"); // SVG'de y taban çizgisidir
  const capa = t.getAttribute("text-anchor") ?? t.getAttribute("textAnchor") ?? "start";
  const x0 = capa === "middle" ? x - genislik / 2 : capa === "end" ? x - genislik : x;

  return {
    x0,
    x1: x0 + genislik,
    // Taban çizgisinin üstünde büyük harf yüksekliği, altında iniş payı.
    y0: y - olcu.punto * 0.72,
    y1: y + olcu.punto * 0.21,
    metin,
    capa,
  };
}

/** Opak bir şeklin sınırlayıcı dikdörtgeni (`rect` ya da `circle`). */
function sekilAlani(el: Element) {
  if (el.tagName.toLowerCase() === "circle") {
    const cx = num(el, "cx");
    const cy = num(el, "cy");
    const r = num(el, "r");
    return { x0: cx - r, x1: cx + r, y0: cy - r, y1: cy + r };
  }
  const x = num(el, "x");
  const y = num(el, "y");
  return { x0: x, x1: x + num(el, "width"), y0: y, y1: y + num(el, "height") };
}

/**
 * Etkin opaklık: elemanın kendi `opacity` niteliği (yoksa 1).
 *
 * RV-081: kapının ilk hâli opaklığı yalnız SINIF ADINDAN çıkarıyordu. Kayıtta
 * `opacity="0.22"` taşıyan ve meşru biçimde okunabilir 14 `fig-bar-*` rect var;
 * ters yönde, saydam tona inmiş bir lead rect yanlış kırmızı üretebilirdi.
 */
const etkinOpaklik = (el: Element) =>
  el.hasAttribute("opacity") ? Number.parseFloat(el.getAttribute("opacity")!) : 1;

describe("Figür kayıt defteri — render kapısı (TD-001)", () => {
  it(`kayıt defterinde tam ${BEKLENEN_FIGUR_SAYISI} figür var`, () => {
    // Eşitlik: silinme de eklenme de bu sabitin elle değişmesini gerektirir (RV-068/080).
    expect(FIGURE_KEYS).toHaveLength(BEKLENEN_FIGUR_SAYISI);
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
   * RV-039 (REVIEW-004) ve RV-053 + RV-055 (REVIEW-005) **aynı desendi**: tam
   * opak bir dolgunun üzerine yazı yazılması. `fig-value` (gri-mavi, 10px)
   * accent üzerinde ≈1,1:1, mint üzerinde ≈1,31:1, coral üzerinde ≈1,03:1
   * kontrast verir — hiçbiri okunmaz. Desen iki ayrı incelemede üç figürde
   * tekrarladı, hepsi yalnız ekran görüntüsüyle yakalandı; kapı kurulunca
   * `fig-seg-lead`de 7 figür daha, REVIEW-006'da diğer iki opak sınıfta
   * SEKİZ örnek daha çıktı.
   *
   * Kural: **opak dolgu yazı taşımaz.** Vurgu gerekiyorsa yazı kutunun DIŞINA
   * çıkar, dolgu saydam tona iner (`fig-seg` / `fig-bar-muted`), ya da yazı
   * near-white `fig-label`a yükselir.
   */
  it.each(FIGURE_KEYS)("%s: opak dolgunun üzerine yazı yazılmamış", (key) => {
    const { container } = render(<LessonFigure figureKey={key} />);

    const secici = OPAK_DOLGU_SINIFLARI.flatMap((c) => [`rect.${c}`, `circle.${c}`]).join(", ");
    const opaklar = [...container.querySelectorAll(secici)].filter((el) => etkinOpaklik(el) >= 0.9);

    // Dönüşümlü grup içindeki şekil ham koordinatlarla YANLIŞ ölçülür. Sessizce
    // atlamak yerine haber ver: kapının körleşebileceği tek yapı budur.
    for (const el of opaklar) {
      let ata = el.parentElement;
      while (ata && ata.tagName.toLowerCase() !== "svg") {
        expect(ata.hasAttribute("transform"),
          `'${key}': opak şekil dönüşümlü (<g transform>) bir grubun içinde — ham ` +
          "koordinat karşılaştırması bu şekli yanlış ölçer. Dönüşümü şeklin kendi " +
          "koordinatlarına taşı ya da bu kapıyı dönüşüm farkındası yap.").toBe(false);
        ata = ata.parentElement;
      }
    }

    const yazilar = [...container.querySelectorAll("text")];
    const ihlaller: string[] = [];

    for (const el of opaklar) {
      const k = sekilAlani(el);
      if (k.x1 - k.x0 <= 0 || k.y1 - k.y0 <= 0) continue;

      for (const t of yazilar) {
        const y = yaziAlani(t);
        if (!y.metin) continue;

        const ortakW = Math.min(k.x1, y.x1) - Math.max(k.x0, y.x0);
        const ortakH = Math.min(k.y1, y.y1) - Math.max(k.y0, y.y0);
        if (ortakW <= 0 || ortakH <= 0) continue;

        const yaziAlan = (y.x1 - y.x0) * (y.y1 - y.y0);
        const ortusme = yaziAlan > 0 ? (ortakW * ortakH) / yaziAlan : 0;
        // Çapa kutunun içindeyse her hâlükârda ihlal (kanıtlanmış ilk kontrol
        // korunuyor); değilse yazının en az %15'i dolgunun üstüne düşmeli.
        const capaIcerde =
          num(t, "x") >= k.x0 && num(t, "x") <= k.x1 && num(t, "y") >= k.y0 && num(t, "y") <= k.y1;
        if (!capaIcerde && ortusme < 0.15) continue;

        ihlaller.push(
          `"${y.metin}" (${t.getAttribute("class") ?? "?"}, çapa ${y.capa}) ` +
            `→ %${Math.round(ortusme * 100)} ${el.tagName}.${el.getAttribute("class")} ` +
            `[${k.x0},${k.y0} → ${k.x1},${k.y1}] üzerinde`,
        );
      }
    }

    expect(ihlaller,
      `'${key}' opak dolgunun ÜZERİNE yazı yazıyor; bu kontrast okunmuyor ` +
      `(RV-039/053/055/065 deseni). Yazıyı kutunun dışına taşı, dolguyu saydam ` +
      `tona indir ya da yazıyı fig-label yap. İhlaller: ${ihlaller.join(" · ")}`,
    ).toHaveLength(0);
  });
});

/**
 * RV-091 (REVIEW-007) — `ChartFrame` ölçeğinin DEJENERE aralığı.
 *
 * Render kapısı bunu göremez: `priceScale` `Infinity`/`NaN` döndürdüğünde şekil
 * çizilmez ama `svg[role="img"]`, `aria-label` ve `figcaption` yerinde kalır,
 * yani kapı yeşil geçer ve kullanıcı BOŞ bir çerçeve görür. Risk teorik değil —
 * setin bir sonraki dersi (S2-L2) "grafik yatay görünüyor" konusunu işliyor ve
 * düz/dar aralıklı seri çiziyor.
 */
describe("priceScale — grafik figürlerinin dikey ölçeği (RV-091)", () => {
  const plot = { x: 0, y: 10, w: 100, h: 80 };

  it("düz seride (lo === hi) sıfıra BÖLMEZ — değer çerçevenin ortasına oturur", () => {
    const py = priceScale(plot, 100, 100);
    expect(Number.isFinite(py(100)), "dejenere aralık sonlu bir koordinat vermeli").toBe(true);
    expect(py(100)).toBe(50); // y + h/2
  });

  it("normal aralıkta uçları çerçeveye oturtur ve büyük değeri YUKARI koyar", () => {
    const py = priceScale(plot, 90, 110);
    expect(py(90)).toBe(90); // alt kenar (y + h)
    expect(py(110)).toBe(10); // üst kenar (y)
    expect(py(100)).toBe(50); // orta
    expect(py(110)).toBeLessThan(py(90));
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
