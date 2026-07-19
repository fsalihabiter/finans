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
  children,
}: {
  label: string;
  caption: string;
  children: React.ReactNode;
}) {
  return (
    <figure className="lesson-figure">
      <svg viewBox="0 0 320 150" role="img" aria-label={label} preserveAspectRatio="xMidYMid meet">
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
