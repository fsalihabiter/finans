/**
 * Ders figürleri (T6.7) — kavramı tek bakışta gösteren küçük, elle yazılmış SVG'ler.
 *
 * İlkeler:
 * - **Kütüphane yok**: saf SVG; grafik bağımlılığı eklemiyoruz (NFR-9, paket boyutu).
 * - **Statik veri**: sayılar dersin *Örnek* bloğuyla birebir aynı. Kullanıcı verisi
 *   BURAYA GİRMEZ — kişisel bağlam "Senin portföyünde" bloğunun işi (15 §3).
 * - **Tavsiye yok** (CLAUDE.md §2): figürler enstrüman adı taşımaz, sıralama yapmaz;
 *   etiketler "A/B/C yatırımı" gibi soyuttur.
 * - **Tema uyumlu**: renkler CSS değişkenlerinden; koyu/açık temada çalışır.
 * - **Erişilebilir**: her figür `role="img"` + açıklayıcı `aria-label` taşır; içerik
 *   metinde de anlatıldığı için figür kaybolsa ders eksilmez.
 */

/** Grafik alanı ortak sarmalayıcı — responsive (viewBox ile ölçeklenir). */
function Figure({
  label,
  caption,
  height = 150,
  children,
}: {
  label: string;
  caption: string;
  /** Bazı figürler daha uzun; viewBox yüksekliği içeriğe göre ayarlanır. */
  height?: number;
  children: React.ReactNode;
}) {
  return (
    <figure className="lesson-figure">
      <svg viewBox={`0 0 320 ${height}`} role="img" aria-label={label} preserveAspectRatio="xMidYMid meet">
        {children}
      </svg>
      <figcaption>{caption}</figcaption>
    </figure>
  );
}

/** Ders 1 — nominal vs reel: aynı çubuk, enflasyon payı düşülünce ne kalıyor? */
function RealVsNominal() {
  // Örnek bloğuyla aynı: enflasyon %50; A %45 (reel −%3,3), B %50 (0), C %70 (+%13,3).
  const rows = [
    { name: "A", nominal: 45, real: -3.3 },
    { name: "B", nominal: 50, real: 0 },
    { name: "C", nominal: 70, real: 13.3 },
  ];
  // Eksen solda tutulur ki çubuklar genişliğin çoğunu kullansın (ölü boşluk olmasın).
  const AXIS = 78;
  const scale = (v: number) => (v / 70) * 224; // 70 = en uzun nominal → sağ kenara yaklaşır

  return (
    <Figure
      label="Üç yatırımın nominal ve reel getirisi karşılaştırması"
      caption="Açık çubuk nominal getiri, koyu çubuk enflasyondan arındırılmış reel getiri."
    >
      <line x1={AXIS} y1="12" x2={AXIS} y2="142" className="fig-axis" />
      {rows.map((r, i) => {
        const y = 22 + i * 42;
        const realW = scale(Math.abs(r.real));
        return (
          <g key={r.name}>
            <text x="6" y={y + 13} className="fig-label">
              {r.name}
            </text>
            <rect x={AXIS} y={y} width={scale(r.nominal)} height="13" rx="3" className="fig-bar-muted" />
            <rect
              x={r.real >= 0 ? AXIS : AXIS - realW}
              y={y + 15}
              width={Math.max(realW, 2)}
              height="13"
              rx="3"
              className={r.real >= 0 ? "fig-bar-pos" : "fig-bar-neg"}
            />
            <text x={AXIS - 6} y={y + 11} className="fig-value" textAnchor="end">
              %{r.nominal}
            </text>
            <text
              x={AXIS - 6}
              y={y + 26}
              className={r.real >= 0 ? "fig-value pos" : "fig-value neg"}
              textAnchor="end"
            >
              {r.real > 0 ? "+" : ""}
              {String(r.real).replace(".", ",")}%
            </text>
          </g>
        );
      })}
    </Figure>
  );
}

/** Ders 1 — alım gücü: aynı para, kaç ekmek? (lira değil mal cinsinden saymak) */
function PurchasingPower() {
  // Örnek bloğuyla aynı: 100.000/14,50 ≈ 6.897 → 140.000/20 = 7.000 ekmek.
  const rows = [
    { when: "Bir yıl önce", money: "100.000 ₺", unit: "14,50 ₺", loaves: 6897, w: 0.985 },
    { when: "Bugün", money: "140.000 ₺", unit: "20,00 ₺", loaves: 7000, w: 1 },
  ];

  return (
    <Figure
      label="Aynı paranın bir yıl arayla kaç ekmek ettiğinin karşılaştırması"
      caption="Lira %40 büyüdü; ekmek cinsinden zenginlik yalnızca %1,5 arttı."
    >
      {rows.map((r, i) => {
        const y = 26 + i * 58;
        return (
          <g key={r.when}>
            <text x="6" y={y} className="fig-label">
              {r.when}
            </text>
            <text x="6" y={y + 15} className="fig-value">
              {r.money} · ekmek {r.unit}
            </text>
            <rect x="150" y={y - 11} width={r.w * 150} height="16" rx="3" className={i === 1 ? "fig-bar-pos" : "fig-bar-muted"} />
            <text x={150 + r.w * 150 + 6} y={y + 2} className="fig-value">
              {r.loaves.toLocaleString("tr-TR")}
            </text>
          </g>
        );
      })}
      <text x="6" y="140" className="fig-value">
        Aynı para, mal cinsinden neredeyse yerinde saydı.
      </text>
    </Figure>
  );
}

/** Ders 1 — çıkarma kısayolunun hatası enflasyonla büyür. */
function SubtractionError() {
  // Örnek bloğuyla aynı üç ortam; hepsinde nominal−enflasyon farkı benzer.
  const rows = [
    { label: "%12 / %10", naive: 2, real: 1.8 },
    { label: "%45 / %35", naive: 10, real: 7.4 },
    { label: "%85 / %75", naive: 10, real: 5.7 },
  ];
  const scale = (v: number) => (v / 10) * 150;

  return (
    <Figure
      label="Çıkarma kısayolu ile gerçek reel getiri arasındaki farkın enflasyonla büyümesi"
      caption="Açık çubuk çıkarma sonucu, koyu çubuk gerçek reel getiri. Enflasyon arttıkça makas açılıyor."
    >
      {rows.map((r, i) => {
        const y = 24 + i * 40;
        return (
          <g key={r.label}>
            <text x="6" y={y + 13} className="fig-label">
              {r.label}
            </text>
            <rect x="96" y={y} width={scale(r.naive)} height="12" rx="3" className="fig-bar-muted" />
            <rect x="96" y={y + 14} width={scale(r.real)} height="12" rx="3" className="fig-bar-pos" />
            <text x={96 + scale(r.naive) + 6} y={y + 10} className="fig-value">
              %{String(r.naive).replace(".", ",")}
            </text>
            <text x={96 + scale(r.real) + 6} y={y + 24} className="fig-value pos">
              %{String(r.real).replace(".", ",")}
            </text>
          </g>
        );
      })}
    </Figure>
  );
}

/** Ders 1 — kişisel sepet: aynı nominal getiri, iki farklı reel sonuç. */
function BasketDifference() {
  const people = [
    { who: "Kiracı", basket: 62, real: -4.3 },
    { who: "Ev sahibi", basket: 44, real: 7.6 },
  ];

  return (
    <Figure
      label="Aynı nominal getirinin farklı kişisel enflasyon sepetlerinde farklı reel sonuç vermesi"
      caption="İkisinin de nominal getirisi %55; farkı yaratan kendi harcama sepetleri."
    >
      <line x1="150" y1="14" x2="150" y2="118" className="fig-axis" />
      <text x="150" y="10" className="fig-value" textAnchor="middle">
        TÜFE %50
      </text>
      {people.map((p, i) => {
        const y = 34 + i * 48;
        return (
          <g key={p.who}>
            <text x="6" y={y} className="fig-label">
              {p.who}
            </text>
            <text x="6" y={y + 15} className="fig-value">
              kendi enflasyonu %{p.basket}
            </text>
            <rect
              x={p.real >= 0 ? 150 : 150 - Math.abs(p.real) * 9}
              y={y - 11}
              width={Math.abs(p.real) * 9}
              height="16"
              rx="3"
              className={p.real >= 0 ? "fig-bar-pos" : "fig-bar-neg"}
            />
            <text
              x={p.real >= 0 ? 150 + p.real * 9 + 6 : 150 - Math.abs(p.real) * 9 - 6}
              y={y + 2}
              className={p.real >= 0 ? "fig-value pos" : "fig-value neg"}
              textAnchor={p.real >= 0 ? "start" : "end"}
            >
              {p.real > 0 ? "+" : ""}
              {String(p.real).replace(".", ",")}%
            </text>
          </g>
        );
      })}
      <text x="6" y="140" className="fig-value">
        Aynı yatırım, aynı yıl — biri kaybetti, diğeri kazandı.
      </text>
    </Figure>
  );
}

/** Ders 1 — dönem seçimi: aynı seri, üç farklı pencere, üç farklı hikâye. */
function WindowSelection() {
  const years = [18, -12, 6, -9, 21];
  const toX = (i: number) => 34 + i * 60;
  const toY = (v: number) => 74 - (v / 21) * 44;

  return (
    <Figure
      label="Aynı yıllık getiri serisinin farklı zaman pencerelerinde farklı görünmesi"
      caption="Üç rakam da doğru; hangi pencereden baktığın hikâyeyi değiştirir."
      height={162}
    >
      <line x1="20" y1="74" x2="300" y2="74" className="fig-axis" />
      {years.map((v, i) => (
        <g key={i}>
          <rect
            x={toX(i) - 13}
            y={v >= 0 ? toY(v) : 74}
            width="26"
            height={Math.max(Math.abs(74 - toY(v)), 2)}
            rx="3"
            className={v >= 0 ? "fig-bar-pos" : "fig-bar-neg"}
          />
          <text x={toX(i)} y={v >= 0 ? toY(v) - 5 : 74 + Math.abs(74 - toY(v)) + 12} className="fig-value" textAnchor="middle">
            {v > 0 ? "+" : ""}
            {v}
          </text>
        </g>
      ))}
      {/* Pencereler */}
      <rect x={toX(4) - 20} y="100" width="40" height="14" rx="4" className="fig-card" />
      <text x={toX(4)} y="110" className="fig-value" textAnchor="middle">
        1 yıl
      </text>
      <rect x={toX(3) - 20} y="120" width="100" height="14" rx="4" className="fig-card" />
      <text x={toX(3) + 30} y="130" className="fig-value" textAnchor="middle">
        2 yıl · +%10
      </text>
      <rect x="14" y="140" width="286" height="12" rx="4" className="fig-card" />
      <text x="157" y="149" className="fig-value" textAnchor="middle">
        5 yıl · toplam +%21 (yılda ≈ +%3,9)
      </text>
    </Figure>
  );
}

/** Ders 2 — yoğunlaşma: aynı kalem sayısı, çok farklı ağırlık dağılımı. */
function Concentration() {
  // Örnek bloğuyla aynı: %70/10/8/7/5 ve %25/22/20/18/15.
  const a = [70, 10, 8, 7, 5];
  const b = [25, 22, 20, 18, 15];

  const bar = (weights: number[], y: number) => {
    let x = 10;
    return weights.map((w, i) => {
      const width = (w / 100) * 300;
      const el = (
        <rect
          key={i}
          x={x}
          y={y}
          width={width - 2}
          height="26"
          rx="3"
          className={i === 0 ? "fig-seg-lead" : "fig-seg"}
        />
      );
      x += width;
      return el;
    });
  };

  return (
    <Figure
      label="İki portföyün ağırlık dağılımı: biri tek kalemde yoğunlaşmış, diğeri dengeli"
      caption="İkisinin de beş kalemi var; farkı kalem sayısı değil ağırlık dağılımı yaratıyor."
    >
      <text x="10" y="22" className="fig-label">
        Yoğunlaşmış
      </text>
      {bar(a, 28)}
      <text x="10" y="88" className="fig-label">
        Dengeli
      </text>
      {bar(b, 94)}
      <text x="312" y="22" className="fig-value" textAnchor="end">
        en büyük %70
      </text>
      <text x="312" y="88" className="fig-value" textAnchor="end">
        en büyük %25
      </text>
    </Figure>
  );
}

/** Ders 2 — aynı sektörde on kalem: sayıca çok, davranışça bir. */
function SameSector() {
  const cols = Array.from({ length: 10 }, (_, i) => i);
  return (
    <Figure
      label="Aynı sektördeki on kalemin tek bir haberle birlikte düşmesi"
      caption="Kalem sayısı on; ama hepsi aynı sebeple hareket ettiği için tek bir bahis gibi davranır."
    >
      <text x="6" y="20" className="fig-label">
        Sektör haberi ↓
      </text>
      {cols.map((i) => (
        <g key={i}>
          <rect x={14 + i * 30} y="34" width="20" height="34" rx="3" className="fig-seg" />
          <rect x={14 + i * 30} y="78" width="20" height="30" rx="3" className="fig-bar-neg" />
          <text x={24 + i * 30} y="124" className="fig-value" textAnchor="middle">
            ↓
          </text>
        </g>
      ))}
      <text x="6" y="142" className="fig-value">
        Sayıca 10 · davranışça 1 — gerçek çeşitlilik yok.
      </text>
    </Figure>
  );
}

/** Ders 2 — birlikte hareket vs bağımsız hareket: hangi çift dalgayı yumuşatır? */
function CorrelationPaths() {
  // Örnek bloğuyla aynı: birlikte hareket eden çift vs farklı hareket eden çift.
  const together = [19, -14.5, 19, -11.5];
  const mixed = [7, -2, 7, -1.5];
  const toX = (i: number) => 60 + i * 72;
  const toY = (v: number) => 60 - (v / 20) * 34;
  const path = (vals: number[]) => vals.map((v, i) => `${i === 0 ? "M" : "L"}${toX(i)},${toY(v)}`).join(" ");

  return (
    <Figure
      label="Birlikte hareket eden ve farklı hareket eden varlık çiftlerinin portföy dalgalanmasına etkisi"
      caption="Üstteki çift birlikte hareket ediyor — dalga aynen sürüyor. Alttaki çift birbirini yumuşatıyor."
      height={168}
    >
      <line x1="20" y1="60" x2="300" y2="60" className="fig-axis" />
      <text x="6" y="18" className="fig-label">
        Birlikte hareket eden çift
      </text>
      <path d={path(together)} className="fig-line-volatile" fill="none" />
      {together.map((v, i) => (
        <circle key={i} cx={toX(i)} cy={toY(v)} r="3" className="fig-dot-volatile" />
      ))}

      <g transform="translate(0, 90)">
        <line x1="20" y1="60" x2="300" y2="60" className="fig-axis" />
        <text x="6" y="18" className="fig-label">
          Farklı hareket eden çift
        </text>
        <path d={path(mixed)} className="fig-line-steady" fill="none" />
        {mixed.map((v, i) => (
          <circle key={i} cx={toX(i)} cy={toY(v)} r="3" className="fig-dot-steady" />
        ))}
      </g>
    </Figure>
  );
}

/** Ders 2 — ağırlık kayması: hiçbir işlem yapmadan yoğunlaşmak. */
function ConcentrationDrift() {
  // Örnek bloğuyla aynı: %20×5 → %45, %21, %21, %6,5, %6,5.
  const before = [20, 20, 20, 20, 20];
  const after = [45, 21, 21, 6.5, 6.5];

  const bar = (weights: number[], y: number) => {
    let x = 10;
    return weights.map((w, i) => {
      const width = (w / 100) * 300;
      const el = (
        <rect key={i} x={x} y={y} width={width - 2} height="26" rx="3" className={i === 0 ? "fig-seg-lead" : "fig-seg"} />
      );
      x += width;
      return el;
    });
  };

  return (
    <Figure
      label="Üç yıl boyunca işlem yapılmadan ağırlıkların kendiliğinden yoğunlaşması"
      caption="Hiçbir karar alınmadı; iyi giden kalem büyüdü ve portföy kendiliğinden yoğunlaştı."
    >
      <text x="10" y="22" className="fig-label">
        Başlangıç · eşit
      </text>
      {bar(before, 28)}
      <text x="10" y="88" className="fig-label">
        Üç yıl sonra · işlem yok
      </text>
      {bar(after, 94)}
      <text x="312" y="22" className="fig-value" textAnchor="end">
        en büyük %20
      </text>
      <text x="312" y="88" className="fig-value" textAnchor="end">
        en büyük %45
      </text>
    </Figure>
  );
}

/** Ders 3 — aynı F/K, farklı hikâye: oran tek başına yetmez. */
function RatioContext() {
  return (
    <Figure
      label="Aynı F/K oranına sahip iki şirketin farklı durumları"
      caption="Aynı rakam, iki ayrı hikâye. Oran soruyu başlatır; cevabı vermez."
    >
      <g>
        <rect x="12" y="24" width="140" height="100" rx="10" className="fig-card" />
        <text x="82" y="52" className="fig-big" textAnchor="middle">
          F/K 8
        </text>
        <text x="82" y="76" className="fig-label" textAnchor="middle">
          Birinci şirket
        </text>
        <text x="82" y="96" className="fig-value" textAnchor="middle">
          kâr istikrarlı
        </text>
        <text x="82" y="112" className="fig-value" textAnchor="middle">
          borç düşük
        </text>
      </g>
      <g>
        <rect x="168" y="24" width="140" height="100" rx="10" className="fig-card" />
        <text x="238" y="52" className="fig-big" textAnchor="middle">
          F/K 8
        </text>
        <text x="238" y="76" className="fig-label" textAnchor="middle">
          İkinci şirket
        </text>
        <text x="238" y="96" className="fig-value neg" textAnchor="middle">
          tek seferlik gelir
        </text>
        <text x="238" y="112" className="fig-value neg" textAnchor="middle">
          onsuz F/K ≈ 20
        </text>
      </g>
    </Figure>
  );
}

/** Ders 4 — aynı ortalama, farklı yolculuk: oynaklık ortalamada görünmez. */
function VolatilityPaths() {
  // Örnek bloğuyla aynı: A istikrarlı ~%15, B çok oynak — ikisi de ort. %15.
  const a = [14, 16, 15, 14, 16];
  const b = [70, -35, 60, -20, 35];
  const toY = (v: number) => 78 - (v / 70) * 58; // 0 çizgisi y=78
  const toX = (i: number) => 30 + i * 66;
  const path = (vals: number[]) => vals.map((v, i) => `${i === 0 ? "M" : "L"}${toX(i)},${toY(v)}`).join(" ");

  return (
    <Figure
      label="Aynı ortalama getiriye sahip iki yatırımın yıllık dalgalanması"
      caption="İkisinin de beş yıllık ortalaması aynı; yaşanan yolculuk tamamen farklı."
    >
      <line x1="18" y1="78" x2="308" y2="78" className="fig-axis" />
      <path d={path(b)} className="fig-line-volatile" fill="none" />
      <path d={path(a)} className="fig-line-steady" fill="none" />
      {a.map((v, i) => (
        <circle key={`a${i}`} cx={toX(i)} cy={toY(v)} r="3" className="fig-dot-steady" />
      ))}
      {b.map((v, i) => (
        <circle key={`b${i}`} cx={toX(i)} cy={toY(v)} r="3" className="fig-dot-volatile" />
      ))}
      <text x="18" y="128" className="fig-value">
        A: dar bantta
      </text>
      <text x="308" y="128" className="fig-value neg" textAnchor="end">
        B: sert iniş-çıkış
      </text>
      <text x="18" y="18" className="fig-label">
        yıllık getiri
      </text>
    </Figure>
  );
}

/** Ders 5 — bileşik eğri: artış her yıl büyüyor (doğrusal değil). */
function CompoundCurve() {
  // Örnek: 100.000 ₺, yılda %20 → 120 / 144 / 172,8 / 207,4 (bin ₺).
  const vals = [100, 120, 144, 172.8, 207.4];
  const toX = (i: number) => 26 + i * 68;
  const toY = (v: number) => 132 - ((v - 100) / 110) * 108;
  const curve = vals.map((v, i) => `${i === 0 ? "M" : "L"}${toX(i)},${toY(v)}`).join(" ");
  const linear = [100, 120, 140, 160, 180].map((v, i) => `${i === 0 ? "M" : "L"}${toX(i)},${toY(v)}`).join(" ");

  return (
    <Figure
      label="Bileşik büyüme eğrisi ile doğrusal artışın karşılaştırması"
      caption="Kesikli çizgi her yıl aynı tutarı ekleseydi; dolu çizgi kazancın da kazanması."
    >
      <path d={linear} className="fig-line-flat" fill="none" strokeDasharray="4 4" />
      <path d={curve} className="fig-line-steady" fill="none" />
      {vals.map((v, i) => (
        <g key={i}>
          <circle cx={toX(i)} cy={toY(v)} r="3.5" className="fig-dot-steady" />
          <text x={toX(i)} y={toY(v) - 10} className="fig-value" textAnchor="middle">
            {String(v).replace(".", ",")}
          </text>
        </g>
      ))}
      <text x="26" y="146" className="fig-label">
        bin ₺ · yıllar
      </text>
    </Figure>
  );
}

/** Anahtar → figür kayıt defteri. Bilinmeyen anahtar `null` (içerik bozulmaz). */
const FIGURES: Record<string, () => React.JSX.Element> = {
  "real-vs-nominal": RealVsNominal,
  "purchasing-power": PurchasingPower,
  "subtraction-error": SubtractionError,
  "basket-difference": BasketDifference,
  "window-selection": WindowSelection,
  "same-sector": SameSector,
  "correlation-paths": CorrelationPaths,
  "concentration-drift": ConcentrationDrift,
  concentration: Concentration,
  "ratio-context": RatioContext,
  "volatility-paths": VolatilityPaths,
  "compound-curve": CompoundCurve,
};

export function LessonFigure({ figureKey }: { figureKey: string | null }) {
  if (!figureKey) return null;
  const Component = FIGURES[figureKey];
  // Bilinmeyen anahtar sessizce yok sayılır — ders metni kendi başına eksiksizdir.
  return Component ? <Component /> : null;
}
