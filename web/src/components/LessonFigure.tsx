import { useId, useState } from "react";

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

// ══ SET 0 — İlk Adımlar figürleri (T6.16) ═══════════════════════════════════
// Çok panelli anlatı ağırlıklı (16 §6.1). Sayılar dersin örnek bloğuyla aynı,
// kurgusal ve etiketli; enstrüman adı YOK, sıralama YOK (CLAUDE.md §2).

/** Ortak panel çerçevesi (çok panelli figürlerin yapı taşı, 16 §8.3). */
function Panel({ x, y, w, h, title }: { x: number; y: number; w: number; h: number; title: string }) {
  return (
    <>
      <rect x={x} y={y} width={w} height={h} rx="8" className="fig-card" />
      <text x={x + w / 2} y={y + 15} className="fig-label" textAnchor="middle">
        {title}
      </text>
    </>
  );
}

/** S0-L1 · Üç eylem: saklamak (sabit) · biriktirmek (yığılır) · yatırmak (belirsiz büyür). */
function ThreeActions() {
  return (
    <Figure
      label="Üç ayrı eylem: saklamak parayı sabit tutar, biriktirmek yığar, yatırmak belirsiz bir büyüme umuduyla çalıştırır"
      caption="Saklamak: rakam sabit. Biriktirmek: yavaş yavaş artar. Yatırmak: belirsiz ama büyüme umutlu."
      height={150}
    >
      <Panel x={6} y={20} w={96} h={116} title="Saklamak" />
      <rect x={38} y={92} width={32} height={30} rx="3" className="fig-bar-muted" />
      <text x={54} y={112} className="fig-value" textAnchor="middle">=</text>

      <Panel x={112} y={20} w={96} h={116} title="Biriktirmek" />
      {[0, 1, 2].map((i) => (
        <rect key={i} x={130} y={112 - i * 14} width={60} height={11} rx="2" className="fig-bar-muted" />
      ))}

      <Panel x={218} y={20} w={96} h={116} title="Yatırmak" />
      <path d="M236 118 L252 84 L268 96 L288 52" className="fig-line-steady" fill="none" />
      <circle cx={288} cy={52} r="3.5" className="fig-dot-volatile" />
      <text x={266} y={130} className="fig-label" textAnchor="middle">?</text>
    </Figure>
  );
}

/** S0-L1 · 10.000 ₺'nin üç yolu, bir yıl sonra (A sabit · B belli · C belirsiz aralık). */
function TenThousandThreePaths() {
  return (
    <Figure
      label="10.000 liranın üç yolu bir yıl sonra: çekmecede 10.000 sabit, vadelide 14.500 belli, ortaklıkta 8.000 ile 13.000 arası belirsiz"
      caption="A çekmece: değişmez. B vadeli: baştan belli. C ortaklık: belirsiz aralık (8–13 bin)."
      height={150}
    >
      <line x1={70} y1="16" x2={70} y2="132" className="fig-axis" />
      {[
        { n: "A", label: "çekmece", bar: 62, val: "10.000", cls: "fig-bar-muted", band: 0 },
        { n: "B", label: "vadeli", bar: 90, val: "14.500", cls: "fig-bar-pos", band: 0 },
        { n: "C", label: "ortaklık", bar: 80, val: "8–13 bin", cls: "fig-bar-muted", band: 44 },
      ].map((r, i) => {
        const y = 26 + i * 36;
        return (
          <g key={r.n}>
            <text x={8} y={y + 13} className="fig-label">{r.n} · {r.label}</text>
            {r.band > 0 ? (
              <rect x={70 + 62} y={y} width={r.band} height="16" rx="3" className="fig-bar-muted" opacity="0.45" />
            ) : null}
            <rect x={70} y={y} width={r.bar} height="16" rx="3" className={r.cls} />
            <text x={70 + r.bar + r.band + 6} y={y + 13} className="fig-value">{r.val}</text>
          </g>
        );
      })}
    </Figure>
  );
}

/** S0-L1 · Yatırımın iki unsuru: sermaye bir kullanıma verilir + getiri belirsizdir. */
function CapitalToUse() {
  return (
    <Figure
      label="Yatırımın iki unsuru: para bir kullanıma verilir ve dönen getiri belirsizdir"
      caption="Para bir işe girer (1. unsur); dönüş bir olasılıktır, garanti değil (2. unsur)."
      height={132}
    >
      <rect x={10} y={54} width={54} height={30} rx="6" className="fig-bar-muted" />
      <text x={37} y={73} className="fig-value" textAnchor="middle">para</text>
      <path d="M68 69 L120 69" className="fig-line-steady" fill="none" markerEnd="" />
      <text x={94} y={60} className="fig-label" textAnchor="middle">1 · kullanıma girer</text>
      <rect x={124} y={48} width={70} height={42} rx="8" className="fig-card" />
      <text x={159} y={73} className="fig-value" textAnchor="middle">bir iş</text>
      <path d="M198 62 L250 40" className="fig-line-steady" fill="none" />
      <path d="M198 76 L250 98" className="fig-line-flat" fill="none" strokeDasharray="4 4" />
      <text x={262} y={44} className="fig-value pos">↑ ?</text>
      <text x={262} y={102} className="fig-value neg">↓ ?</text>
      <text x={236} y={122} className="fig-label" textAnchor="middle">2 · getiri belirsiz</text>
    </Figure>
  );
}

/** S0-L1 · "Parayı çalıştırmak": para → girdi/üretim → gelir → sana dönen pay. */
function MoneyAtWork() {
  const steps = ["para", "un · fırın", "ekmek · satış", "sana pay"];
  return (
    <Figure
      label="Parayı çalıştırmak: para girdiye ve üretim aracına dönüşür, ürün satılır ve gelirden sana pay döner"
      caption="Paran boşta durmaz; bir değer üreten sürecin parçası olur ve getiri o süreçten gelir."
      height={110}
    >
      {steps.map((s, i) => {
        const x = 8 + i * 78;
        return (
          <g key={s}>
            <rect x={x} y={40} width={64} height={34} rx="7" className={i === steps.length - 1 ? "fig-bar-pos" : "fig-card"} />
            <text x={x + 32} y={61} className="fig-value" textAnchor="middle">{s}</text>
            {i < steps.length - 1 && (
              <path d={`M${x + 64} 57 L${x + 78} 57`} className="fig-line-steady" fill="none" />
            )}
          </g>
        );
      })}
    </Figure>
  );
}

/** S0-L1 · "Garanti yok": tek yatırımdan iki olası sonuç (yukarı / aşağı). */
function NoGuarantee() {
  return (
    <Figure
      label="Bir yatırımın sonucu belirsizdir: aynı başlangıçtan hem kazanç hem kayıp ihtimali çıkar"
      caption={'Getiri bir olasılıktır, bir söz değil. "Garantili yüksek getiri" bir çelişkidir.'}
      height={120}
    >
      <rect x={12} y={46} width={58} height={30} rx="6" className="fig-bar-muted" />
      <text x={41} y={65} className="fig-value" textAnchor="middle">yatırım</text>
      <path d="M72 58 L150 26" className="fig-line-steady" fill="none" />
      <path d="M72 62 L150 96" className="fig-line-volatile" fill="none" />
      <rect x={152} y={14} width={64} height={26} rx="6" className="fig-bar-pos" />
      <text x={184} y={31} className="fig-value" textAnchor="middle">kazanç</text>
      <rect x={152} y={84} width={64} height={26} rx="6" className="fig-bar-neg" />
      <text x={184} y={101} className="fig-value" textAnchor="middle">kayıp</text>
      <text x={124} y={66} className="fig-big" textAnchor="middle">?</text>
    </Figure>
  );
}

/** S0-L1 · Değer üretimi (pasta büyür) ↔ sıfır toplamlı oyun (pasta sabit, el değiştirir). */
function ValueVsZeroSum() {
  return (
    <Figure
      label="Yatırımda üretilen değerle pasta büyür ve herkes kazanabilir; şans oyununda pasta sabittir, yalnızca el değiştirir"
      caption="Yatırım: değer üretilir, pasta büyür. Şans oyunu: sıfır toplamlı, biri kazanır biri kaybeder."
      height={150}
    >
      <Panel x={6} y={20} w={148} h={116} title="Yatırım — pasta büyür" />
      <circle cx={54} cy={92} r="20" className="fig-bar-muted" />
      <path d="M96 92 L120 92" className="fig-line-steady" fill="none" />
      <circle cx={130} cy={92} r="30" className="fig-bar-pos" />

      <Panel x={166} y={20} w={148} h={116} title="Şans oyunu — sabit" />
      <circle cx={214} cy={92} r="24" className="fig-bar-muted" />
      <circle cx={286} cy={92} r="24" className="fig-bar-muted" />
      <path d="M240 84 L262 84" className="fig-line-volatile" fill="none" />
      <path d="M262 100 L240 100" className="fig-line-volatile" fill="none" />
      <text x={251} y={128} className="fig-label" textAnchor="middle">el değiştirir</text>
    </Figure>
  );
}

/** S0-L1 · Getiri neden var: güvenli getiri + belirsizliğe karşılık risk primi. */
function RiskPremiumIntro() {
  return (
    <Figure
      label="Riskli bir yolun beklenen getirisi, güvenli getirinin üstüne belirsizliğe katlanmanın karşılığı olan bir risk primi ekler"
      caption="Güvenli getiri (fırsat maliyeti) + risk primi (belirsizliğin karşılığı) = beklenen getiri."
      height={130}
    >
      <line x1={78} y1="14" x2={78} y2="112" className="fig-axis" />
      <text x={8} y={44} className="fig-label">güvenli</text>
      <rect x={78} y={30} width={96} height={20} rx="3" className="fig-bar-muted" />
      <text x={180} y={45} className="fig-value">%45</text>

      <text x={8} y={90} className="fig-label">riskli</text>
      <rect x={78} y={76} width={96} height={20} rx="3" className="fig-bar-muted" />
      <rect x={174} y={76} width={54} height={20} rx="3" className="fig-bar-pos" />
      <text x={234} y={91} className="fig-value pos">+prim</text>
      <text x={126} y={124} className="fig-label" textAnchor="middle">aynı taban + belirsizliğin karşılığı</text>
    </Figure>
  );
}

/** S0-L1 · Aynı varlık, iki davranış: 10 yıl tutmak (değer üretiminden pay) ↔ 2 gün (fiyat farkı). */
function HoldVsFlip() {
  return (
    <Figure
      label="Uzun süre tutan kişi üretilen değerden pay alır; iki gün tutup satan kişi yalnızca fiyat farkından kazanmayı umar"
      caption="Aynı varlık: 10 yıl tutmak üretilen değere ortak olur; 2 gün tutmak fiyat farkına oynar."
      height={140}
    >
      <Panel x={6} y={20} w={148} h={106} title="10 yıl tutmak" />
      <path d="M20 108 L48 96 L76 84 L104 62 L138 40" className="fig-line-steady" fill="none" />
      <text x={80} y={122} className="fig-label" textAnchor="middle">üretilen değere ortak</text>

      <Panel x={166} y={20} w={148} h={106} title="2 gün tutmak" />
      <path d="M180 92 L206 60 L232 96 L258 58 L300 92" className="fig-line-volatile" fill="none" />
      <text x={240} y={122} className="fig-label" textAnchor="middle">fiyat farkına oynar</text>
    </Figure>
  );
}

// ── S0-L2 · Paranın haritası figürleri ──────────────────────────────────────

/** S0-L2 · Üç kova: gelir → zorunlu gider · isteğe bağlı gider · birikim. */
function ThreeBuckets() {
  return (
    <Figure
      label="Gelir üç kovaya dağılır: zorunlu gider, isteğe bağlı gider ve birikim; yatırım hep birikim kovasından başlar"
      caption="Gelir üç kovaya bölünür. Yatırılacak para yalnızca üçüncü kovadan (birikim) çıkar."
      height={150}
    >
      <rect x={110} y={16} width={100} height={22} rx="5" className="fig-bar-muted" />
      <text x={160} y={31} className="fig-value" textAnchor="middle">gelir</text>
      <path d="M140 40 L60 66" className="fig-line-steady" fill="none" />
      <path d="M160 40 L160 66" className="fig-line-steady" fill="none" />
      <path d="M180 40 L260 66" className="fig-line-steady" fill="none" />
      <Panel x={10} y={70} w={96} h={66} title="Zorunlu" />
      <Panel x={112} y={70} w={96} h={66} title="İsteğe bağlı" />
      <rect x={214} y={70} width={96} height={66} rx="8" className="fig-bar-pos" opacity="0.28" />
      <text x={262} y={85} className="fig-label" textAnchor="middle">Birikim</text>
      <text x={262} y={116} className="fig-value pos" textAnchor="middle">→ yatırım</text>
    </Figure>
  );
}

/** S0-L2 · Bir aylık dağılım: gelir çubuğu üç dilime bölünür (18k/9k/3k örnek). */
function MonthlySplit() {
  const segs = [
    { w: 168, cls: "fig-bar-muted", label: "zorunlu 18.000" },
    { w: 84, cls: "fig-bar-muted", label: "isteğe bağlı 9.000" },
    { w: 28, cls: "fig-bar-pos", label: "birikim 3.000" },
  ];
  let x = 20;
  return (
    <Figure
      label="30.000 liralık gelir dilimlere ayrılıyor: 18.000 zorunlu, 9.000 isteğe bağlı, geriye 3.000 birikim kalıyor"
      caption="Örnek: 30.000 ₺ gelirin yalnızca küçük bir dilimi (3.000 ₺) birikime kalıyor."
      height={110}
    >
      {segs.map((s) => {
        const rx = x;
        x += s.w + 2;
        return (
          <g key={s.label}>
            <rect x={rx} y={34} width={s.w} height={28} rx="3" className={s.cls} />
          </g>
        );
      })}
      <text x={20} y={82} className="fig-label">◼ zorunlu 18.000</text>
      <text x={150} y={82} className="fig-label">◼ isteğe bağlı 9.000</text>
      <text x={20} y={98} className="fig-value pos">◼ birikim 3.000 ₺ (%10)</text>
    </Figure>
  );
}

/** S0-L2 · Birikim oranı: gelirin içinde birikim payı (%10 örnek). */
function SavingsRateBar() {
  return (
    <Figure
      label="Birikim oranı gelirin içindeki birikim payıdır: örnekte gelirin yüzde onu kenara ayrılıyor"
      caption="Birikim oranı = birikim ÷ gelir. Mutlak tutar değil, bu oran karşılaştırma sağlar."
      height={96}
    >
      <text x={16} y={50} className="fig-label">gelir</text>
      <rect x={70} y={34} width={230} height={26} rx="4" className="fig-bar-muted" />
      <rect x={70} y={34} width={23} height={26} rx="4" className="fig-bar-pos" />
      <text x={181} y={51} className="fig-value" textAnchor="middle">%90 yaşam · %10 birikim</text>
      <text x={70} y={80} className="fig-value pos">birikim ÷ gelir = %10</text>
    </Figure>
  );
}

/** S0-L2 · Aynı gelir, iki oran: 12 ayda biriken tutar (A %10 → 36k, B %20 → 72k). */
function TwoSavers() {
  return (
    <Figure
      label="Aynı gelirle A yüzde on biriktirince on iki ayda 36.000, B yüzde yirmi biriktirince 72.000 birikir — tam iki katı"
      caption="Aynı gelir, aynı süre: yalnızca birikim oranı farkı biriken tutarı ikiye katlıyor."
      height={140}
    >
      <Panel x={6} y={20} w={148} h={106} title="A · %10" />
      <rect x={30} y={92} width={100} height={22} rx="3" className="fig-bar-muted" />
      <text x={80} y={108} className="fig-value" textAnchor="middle">36.000 ₺</text>

      <Panel x={166} y={20} w={148} h={106} title="B · %20" />
      <rect x={190} y={70} width={100} height={44} rx="3" className="fig-bar-pos" />
      <text x={240} y={97} className="fig-value" textAnchor="middle">72.000 ₺</text>
    </Figure>
  );
}

/** S0-L2 · "Önce harca, kalanı biriktir" → kalan sıfıra yaklaşır. */
function LeftoverTrap() {
  return (
    <Figure
      label="Önce harca kalanı biriktir planında harcama geliri neredeyse tümüyle yer ve birikime kalan sıfıra yaklaşır"
      caption={'Biriktirmeyi "kalan"a bırakmak, onu her ay en zayıf halkaya bağlar.'}
      height={100}
    >
      <text x={16} y={48} className="fig-label">gelir</text>
      <rect x={70} y={32} width={228} height={26} rx="4" className="fig-bar-muted" />
      <rect x={70} y={32} width={222} height={26} rx="4" className="fig-bar-neg" opacity="0.55" />
      <text x={180} y={49} className="fig-value" textAnchor="middle">harcama genişler…</text>
      <text x={70} y={80} className="fig-value neg">kalan ≈ 0</text>
    </Figure>
  );
}

/** S0-L2 · Önce kendine öde: birikim EN BAŞTA ayrılır, kalanla yaşanır. */
function PayYourselfFirst() {
  return (
    <Figure
      label="Önce kendine öde yönteminde birikim gelirin en başında ayrılır ve geriye kalanla yaşanır"
      caption="Sıra tersine döner: önce birikim ayrılır, kalanla yaşanır. Birikim artık öncelik, artık değil."
      height={100}
    >
      <text x={16} y={48} className="fig-label">gelir</text>
      <rect x={70} y={32} width={228} height={26} rx="4" className="fig-bar-muted" />
      <rect x={70} y={32} width={46} height={26} rx="4" className="fig-bar-pos" />
      <text x={93} y={50} className="fig-value" textAnchor="middle">önce</text>
      <text x={207} y={49} className="fig-label" textAnchor="middle">kalanla yaşa</text>
      <text x={70} y={80} className="fig-value pos">birikim = öncelik, artık değil</text>
    </Figure>
  );
}

/** S0-L2 · Yaşam tarzı enflasyonu: zam gelince harcama büyür, birikim yerinde sayar. */
function LifestyleCreep() {
  return (
    <Figure
      label="Zam gelince harcama da onunla büyür ve birikim oranı yerinde sayar; buna yaşam tarzı enflasyonu denir"
      caption="Gelir artınca harcama da büyürse birikim oranı yerinde sayar — yükseltmek bilinçli bir karardır."
      height={130}
    >
      <text x={12} y={44} className="fig-label">önce</text>
      <rect x={64} y={30} width={150} height={22} rx="3" className="fig-bar-muted" />
      <rect x={214} y={30} width={22} height={22} rx="3" className="fig-bar-pos" />

      <text x={12} y={92} className="fig-label">zam sonrası</text>
      <rect x={64} y={78} width={190} height={22} rx="3" className="fig-bar-muted" />
      <rect x={254} y={78} width={22} height={22} rx="3" className="fig-bar-pos" />
      <text x={150} y={120} className="fig-label" textAnchor="middle">harcama büyüdü · birikim aynı kaldı</text>
    </Figure>
  );
}

/** S0-L2 · Oran mı getiri mi: erken dönemde oran kaldıracı tutarı ikiye katlar. */
function RateVsReturnLever() {
  return (
    <Figure
      label="Yolun başında birikim oranını ikiye katlamak biriken tutarı doğrudan ikiye katlar; küçük tutarda getiri farkının etkisi ise çok küçüktür"
      caption="Başlangıçta kaldıraç oran tarafındadır: oranı ikiye katlamak tutarı ikiye katlar; getiri farkı küçük tutarda az fark eder."
      height={150}
    >
      <Panel x={6} y={20} w={148} h={116} title="Oranı 2'ye katla" />
      <rect x={40} y={98} width={40} height={22} rx="3" className="fig-bar-muted" />
      <rect x={40} y={70} width={80} height={22} rx="3" className="fig-bar-pos" />
      <text x={80} y={132} className="fig-value pos" textAnchor="middle">tutar ×2</text>

      <Panel x={166} y={20} w={148} h={116} title="Getiriyi 2'ye katla" />
      <rect x={200} y={98} width={40} height={22} rx="3" className="fig-bar-muted" />
      <rect x={200} y={90} width={46} height={22} rx="3" className="fig-bar-pos" />
      <text x={240} y={132} className="fig-label" textAnchor="middle">küçük tutarda az fark</text>
    </Figure>
  );
}

// ── S0-L3 · Acil durum fonu ve borç figürleri ───────────────────────────────

/** S0-L3 · Şok: düz giden hayat, aniden beklenmedik bir gider (sıçrama). */
function ShockEvent() {
  return (
    <Figure
      label="Hayat düz giderken beklenmedik ve ertelenemez bir gider aniden ortaya çıkar; buna şok denir"
      caption="Şok: ne zaman geleceği belli olmayan, beklemeyen bir gider."
      height={110}
    >
      <path d="M16 78 L120 78 L150 30 L152 78 L300 78" className="fig-line-volatile" fill="none" />
      <circle cx={151} cy={30} r="4" className="fig-dot-volatile" />
      <text x={151} y={20} className="fig-value neg" textAnchor="middle">şok!</text>
      <text x={60} y={95} className="fig-label">olağan akış</text>
    </Figure>
  );
}

/** S0-L3 · Aynı şok, iki kişi: tamponlu (yatırıma dokunmaz) ↔ tamponsuz (satmak zorunda). */
function WithWithoutBuffer() {
  return (
    <Figure
      label="Aynı şok karşısında tamponu olan kişi yatırımına dokunmaz, tamponu olmayan kişi bir varlığı satmak zorunda kalır"
      caption="Tamponlu: şok tampondan karşılanır, yatırım korunur. Tamponsuz: mecburen satış."
      height={150}
    >
      <Panel x={6} y={20} w={148} h={116} title="Tamponlu" />
      <rect x={30} y={92} width={44} height={28} rx="4" className="fig-bar-pos" />
      <text x={52} y={110} className="fig-value" textAnchor="middle">tampon</text>
      <path d="M78 106 L112 106" className="fig-line-steady" fill="none" />
      <text x={128} y={100} className="fig-label" textAnchor="middle">şok</text>
      <text x={90} y={132} className="fig-label" textAnchor="middle">yatırım korunur</text>

      <Panel x={166} y={20} w={148} h={116} title="Tamponsuz" />
      <rect x={196} y={92} width={44} height={28} rx="4" className="fig-bar-muted" />
      <text x={218} y={110} className="fig-value" textAnchor="middle">yatırım</text>
      <path d="M244 106 L280 106" className="fig-line-volatile" fill="none" />
      <text x={252} y={132} className="fig-value neg" textAnchor="middle">mecburen satış</text>
    </Figure>
  );
}

/** S0-L3 · Tamponun üç özelliği: erişilebilir · oynamayan · ayrı. */
function BufferTraits() {
  const traits = ["Erişilebilir", "Oynamayan", "Ayrı"];
  return (
    <Figure
      label="İyi bir tampon üç özellik taşır: erişilebilir, oynamayan ve ayrı"
      caption="Bu üç özellik tamponu bir yatırımdan ayırır: yatırım büyümeyi, tampon hazır olmayı hedefler."
      height={96}
    >
      {traits.map((t, i) => {
        const x = 10 + i * 102;
        return (
          <g key={t}>
            <rect x={x} y={34} width={92} height={34} rx="8" className="fig-bar-pos" opacity="0.22" />
            <text x={x + 46} y={55} className="fig-value" textAnchor="middle">{t}</text>
          </g>
        );
      })}
    </Figure>
  );
}

/** S0-L3 · Getiri peşinde tamponun likiditesi feda edilir (ters yönlü iki ok). */
function LiquidityTradedAway() {
  return (
    <Figure
      label="Tampondan getiri beklemek onu tampon yapan likiditeyi feda eder: getiri artarken erişilebilirlik düşer"
      caption="Getiri peşinde tamponu dalgalı/kilitli yere koymak, şok anında koruma işlevini yitirir."
      height={110}
    >
      <text x={44} y={48} className="fig-label" textAnchor="middle">getiri isteği</text>
      <path d="M20 58 L120 58" className="fig-line-steady" fill="none" />
      <text x={130} y={62} className="fig-value pos">↑</text>

      <text x={44} y={88} className="fig-label" textAnchor="middle">likidite</text>
      <path d="M20 78 L120 78" className="fig-line-volatile" fill="none" />
      <text x={130} y={82} className="fig-value neg">↓</text>
      <text x={220} y={70} className="fig-label" textAnchor="middle">koruma zayıflar</text>
    </Figure>
  );
}

/** S0-L3 · Aylık oran küçük görünür, yıllık maliyet büyür (aylık ↔ yıllık çubuk). */
function MonthlyToYearly() {
  return (
    <Figure
      label="Aylık oran küçük görünür ama yıla yayıldığında bileşiklenerek çok daha büyük bir maliyete döner"
      caption="Aylık küçük oran ≠ yıllık maliyet: faiz faizin de üstüne biner (bileşik)."
      height={110}
    >
      <line x1={78} y1="14" x2={78} y2="94" className="fig-axis" />
      <text x={12} y={40} className="fig-label">aylık</text>
      <rect x={78} y={26} width={22} height={20} rx="3" className="fig-bar-muted" />
      <text x={106} y={41} className="fig-value">%4</text>
      <text x={12} y={80} className="fig-label">yıllık</text>
      <rect x={78} y={66} width={200} height={20} rx="3" className="fig-bar-neg" />
      <text x={284} y={81} className="fig-value neg">≈%60</text>
    </Figure>
  );
}

/** S0-L3 · Aylık %4 → bileşik merdiven → yıllık ≈ %60 (kaba çarpım %48 eksik). */
function DebtCostLadder() {
  const bars = [10, 22, 36, 52, 70, 92, 118, 148, 182, 218];
  return (
    <Figure
      label="Aylık yüzde dört oran her ay bileşiklenerek yükselir ve yılda yaklaşık yüzde altmışa ulaşır; kaba çarpım yüzde kırk sekiz der ve eksik gösterir"
      caption="Her ay oran bir önceki borcun üstüne biner (1,04 on iki kez): yıllık ≈ %60, kaba çarpım %48."
      height={130}
    >
      <line x1={20} y1="104" x2={300} y2="104" className="fig-axis" />
      {bars.map((h, i) => (
        <rect key={i} x={22 + i * 27} y={104 - h * 0.4} width={20} height={h * 0.4} rx="2"
          className={i === bars.length - 1 ? "fig-bar-neg" : "fig-bar-muted"} />
      ))}
      <text x={20} y={122} className="fig-label">1. ay → 12. ay (bileşik)</text>
      <text x={276} y={40} className="fig-value neg" textAnchor="end">≈%60</text>
    </Figure>
  );
}

/** S0-L3 · Bir lira iki işi aynı anda yapamaz: borç azaltmak ↔ yatırım (fırsat maliyeti). */
function OneLiraTwoJobs() {
  return (
    <Figure
      label="Bir lira ya borcu azaltmakta ya yatırımda kullanılabilir ama ikisinde birden değil; birini seçmek diğerinden vazgeçmektir"
      caption="Bir lira, iki işi aynı anda yapamaz. Vazgeçtiğinin değeri: fırsat maliyeti."
      height={130}
    >
      <rect x={132} y={22} width={56} height={26} rx="6" className="fig-bar-muted" />
      <text x={160} y={39} className="fig-value" textAnchor="middle">1 ₺</text>
      <path d="M150 48 L80 84" className="fig-line-steady" fill="none" />
      <path d="M170 48 L240 84" className="fig-line-steady" fill="none" />
      <Panel x={16} y={82} w={128} h={40} title="Borcu azalt" />
      <text x={80} y={116} className="fig-label" textAnchor="middle">kesin kaçınılmış gider</text>
      <Panel x={176} y={82} w={128} h={40} title="Yatırım" />
      <text x={240} y={116} className="fig-label" textAnchor="middle">belirsiz getiri umudu</text>
    </Figure>
  );
}

/** S0-L3 · Kesin maliyet (tek değer) ↔ belirsiz getiri (olasılık aralığı). */
function CertainVsUncertain() {
  return (
    <Figure
      label="Borcun maliyeti kesin bir sayıdır ama yatırımın getirisi bir olasılık aralığıdır; ikisini doğrudan karşılaştırmak yanıltıcıdır"
      caption="Kesin bir maliyeti belirsiz bir getiriyle doğrudan kıyaslamak elmayla armut gibidir."
      height={140}
    >
      <Panel x={6} y={20} w={148} h={106} title="Borç — KESİN" />
      <rect x={40} y={70} width={80} height={26} rx="3" className="fig-bar-neg" />
      <text x={80} y={88} className="fig-value" textAnchor="middle">%60 · olur</text>

      <Panel x={166} y={20} w={148} h={106} title="Yatırım — BELİRSİZ" />
      <rect x={196} y={62} width={90} height={44} rx="4" className="fig-bar-muted" opacity="0.4" />
      <text x={241} y={78} className="fig-value pos" textAnchor="middle">belki +%40</text>
      <text x={241} y={98} className="fig-value neg" textAnchor="middle">belki −%10</text>
    </Figure>
  );
}

/** S0-L3 · Nakdin iki yüzü: erime (maliyet) ve likidite (değer). */
function TwoCostsOfCash() {
  return (
    <Figure
      label="Nakit tutmanın iki yüzü vardır: enflasyon onu eritir bir maliyettir ama hemen kullanılabilir olması likidite bir değerdir"
      caption="Nakit ne tümüyle kayıp ne tümüyle güvenli: erime bir maliyet, likidite bir değer."
      height={120}
    >
      <rect x={124} y={46} width={72} height={30} rx="7" className="fig-card" />
      <text x={160} y={65} className="fig-value" textAnchor="middle">nakit</text>
      <path d="M122 61 L60 40" className="fig-line-volatile" fill="none" />
      <text x={44} y={34} className="fig-value neg" textAnchor="middle">erime ↓</text>
      <text x={44} y={50} className="fig-label" textAnchor="middle">(maliyet)</text>
      <path d="M198 61 L262 40" className="fig-line-steady" fill="none" />
      <text x={280} y={34} className="fig-value pos" textAnchor="middle">likidite ↑</text>
      <text x={280} y={50} className="fig-label" textAnchor="middle">(değer)</text>
      <text x={160} y={104} className="fig-label" textAnchor="middle">ne kadarı tampon, ne kadarı boşta?</text>
    </Figure>
  );
}

// ── S0-L4 · Bekleyen para neden erir? (enflasyon) figürleri ──────────────────

/** S0-L4 · Aynı sepet, iki tarih: sepet değişmez, fiyatı 1.000 → 1.400 ₺ artar. */
function SameBasketTwoDates() {
  return (
    <Figure
      label="Aynı alışveriş sepeti geçen yıl bin lira iken bu yıl bin dört yüz liraya çıkar; sepet değişmez, fiyatı artar"
      caption="Sepet aynı (aynı ekmek, aynı süt); değişen fiyatı. Bir yılda %40 artmış."
      height={140}
    >
      <Panel x={6} y={20} w={148} h={106} title="Geçen yıl" />
      <rect x={54} y={54} width={52} height={40} rx="6" className="fig-bar-muted" />
      <text x={80} y={78} className="fig-value" textAnchor="middle">🧺</text>
      <text x={80} y={114} className="fig-value" textAnchor="middle">1.000 ₺</text>

      <Panel x={166} y={20} w={148} h={106} title="Bu yıl" />
      <rect x={214} y={54} width={52} height={40} rx="6" className="fig-bar-muted" />
      <text x={240} y={78} className="fig-value" textAnchor="middle">🧺</text>
      <text x={240} y={114} className="fig-value neg" textAnchor="middle">1.400 ₺</text>
    </Figure>
  );
}

/** S0-L4 · Elindeki 1.000 ₺ sepet 1.400 olunca ancak %71'ini alır. */
function BasketPriceUp() {
  return (
    <Figure
      label="Sepet bin dört yüz liraya çıkınca elindeki bin lira sepetin ancak yüzde yetmiş birini alır; para azalmadı ama alabildiği düştü"
      caption="Elindeki 1.000 ₺ değişmedi; ama sepet 1.400 olunca ancak ~%71'ini alır."
      height={100}
    >
      <text x={16} y={40} className="fig-label">geçen yıl</text>
      <rect x={92} y={26} width={200} height={20} rx="3" className="fig-bar-pos" />
      <text x={192} y={41} className="fig-value" textAnchor="middle">tam sepet (%100)</text>
      <text x={16} y={80} className="fig-label">bu yıl</text>
      <rect x={92} y={66} width={200} height={20} rx="3" className="fig-bar-muted" />
      <rect x={92} y={66} width={142} height={20} rx="3" className="fig-bar-neg" opacity="0.5" />
      <text x={163} y={81} className="fig-value" textAnchor="middle">≈ %71</text>
    </Figure>
  );
}

/** S0-L4 · Tutar sabit (rakam), alım gücü düşer (aynı para, daralan sepet). */
function AmountVsPower() {
  return (
    <Figure
      label="Tutar yani cüzdandaki rakam sabit kalır ama alım gücü yani o parayla alınabilen şey enflasyonla düşer"
      caption="Tutar (rakam) sabit; alım gücü (alınabilen) erir. Karıştırılması en yaygın hata."
      height={110}
    >
      <text x={16} y={40} className="fig-label">Tutar</text>
      <rect x={92} y={26} width={140} height={20} rx="3" className="fig-bar-muted" />
      <text x={240} y={41} className="fig-value">1.000 ₺ · sabit</text>
      <text x={16} y={84} className="fig-label">Alım gücü</text>
      <path d="M92 70 L232 92" className="fig-line-volatile" fill="none" />
      <text x={244} y={96} className="fig-value neg">↓ erir</text>
    </Figure>
  );
}

/** S0-L4 · "Param aynı kaldı" tuzağı: rakam sabit, sepet küçülür (görünmez kayıp). */
function StandingStill() {
  return (
    <Figure
      label="Rakam aynı kalsa bile alım gücü düştüyse bir kayıp vardır; enflasyon görünmez biçimde cüzdanın içinde çalışır"
      caption="Rakam sabit ama alınan sepet küçülüyor — görünmez bir kayıp."
      height={120}
    >
      <rect x={30} y={44} width={64} height={34} rx="7" className="fig-card" />
      <text x={62} y={65} className="fig-value" textAnchor="middle">1.000 ₺</text>
      <text x={62} y={98} className="fig-label" textAnchor="middle">rakam sabit</text>
      <path d="M100 61 L150 61" className="fig-line-steady" fill="none" />
      <circle cx={210} cy={61} r={34} className="fig-bar-muted" opacity="0.4" />
      <circle cx={210} cy={61} r={20} className="fig-bar-neg" opacity="0.55" />
      <text x={210} y={110} className="fig-label" textAnchor="middle">alınabilen küçülür</text>
    </Figure>
  );
}

/** S0-L4 · ETKİLEŞİMLİ: enflasyon kaydırıcısı (T6.18). Saf/deterministik, klavye
 *  erişimli, tahmin ÜRETMEZ ("olursa"). Hesap istemcide: 100 / (1+i)^n. */
function InflationSlider() {
  const [rate, setRate] = useState(30); // yıllık enflasyon (%)
  const [years, setYears] = useState(5); // süre (yıl)
  const rateId = useId();
  const yearsId = useId();

  // Bugünkü 100 ₺'nin alım gücü, n yıl sonra (bileşik erime — saf, deterministik).
  const power = (n: number) => 100 / Math.pow(1 + rate / 100, n);
  const endPower = power(years);
  const fmt = (v: number) => v.toLocaleString("tr-TR", { maximumFractionDigits: 0 });

  // Yıl yıl erime çubukları.
  const bars = Array.from({ length: years + 1 }, (_, y) => power(y));
  const bw = Math.min(26, (300 - 20) / bars.length);

  return (
    <figure className="lesson-figure inflation-slider" role="group" aria-label="Enflasyon kaydırıcısı — alım gücü erimesi">
      <div className="infl-controls">
        <label htmlFor={rateId}>
          Yıllık enflasyon: <strong>%{rate}</strong>
        </label>
        <input
          id={rateId}
          type="range"
          min={0}
          max={100}
          step={1}
          value={rate}
          onChange={(e) => setRate(Number(e.target.value))}
          aria-valuetext={`yüzde ${rate}`}
        />
        <label htmlFor={yearsId}>
          Süre: <strong>{years} yıl</strong>
        </label>
        <input
          id={yearsId}
          type="range"
          min={1}
          max={20}
          step={1}
          value={years}
          onChange={(e) => setYears(Number(e.target.value))}
          aria-valuetext={`${years} yıl`}
        />
      </div>

      <p className="infl-readout">
        Bugünkü <strong>100 ₺</strong>, yıllık %{rate} enflasyon {years} yıl sürerse
        yaklaşık <strong>{fmt(endPower)} ₺</strong>'lik alım gücüne iner.
      </p>

      <svg viewBox="0 0 320 96" role="img" aria-label={`${years} yılda alım gücü 100 liradan yaklaşık ${fmt(endPower)} liraya iner`}>
        <line x1="14" y1="82" x2="306" y2="82" className="fig-axis" />
        {bars.map((p, y) => (
          <rect
            key={y}
            x={16 + y * bw}
            y={82 - (p / 100) * 68}
            width={Math.max(bw - 3, 3)}
            height={(p / 100) * 68}
            rx="2"
            className={y === bars.length - 1 ? "fig-bar-neg" : "fig-bar-pos"}
          />
        ))}
        <text x="16" y="94" className="fig-label">bugün → {years}. yıl</text>
      </svg>

      <figcaption>
        Bu bir tahmin değildir — "şu oran <strong>olursa</strong>" senaryosudur; sayılar senin varsayımların.
      </figcaption>
    </figure>
  );
}

/** S0-L4 · Fiyat endeksi (TÜFE): ağırlıklı bir tüketim sepeti. */
function IndexBasket() {
  const items = [
    { name: "Gıda", w: 90 },
    { name: "Konut", w: 70 },
    { name: "Ulaşım", w: 50 },
    { name: "Diğer", w: 40 },
  ];
  return (
    <Figure
      label="Fiyat endeksi TÜFE tipik bir hanenin sepetindeki gıda konut ulaşım gibi kalemleri ağırlıklarıyla ölçer"
      caption="TÜFE, ağırlıklı bir ortalama sepetin fiyat değişimini ölçer — tek bir ürünün değil."
      height={128}
    >
      {items.map((it, i) => {
        const y = 20 + i * 26;
        return (
          <g key={it.name}>
            <text x="12" y={y + 13} className="fig-label">{it.name}</text>
            <rect x="86" y={y} width={it.w * 2.1} height="16" rx="3" className="fig-bar-muted" />
          </g>
        );
      })}
      <text x="12" y="122" className="fig-label">ağırlıklar sepetin katkısını gösterir</text>
    </Figure>
  );
}

/** S0-L4 · Kişisel sepet: kiracı (ortalamanın üstü) ↔ ev sahibi (altı). */
function PersonalBasket() {
  return (
    <Figure
      label="Kirada oturanın kişisel enflasyonu ortalamanın üstünde ev sahibininki altında kalabilir çünkü sepetleri farklıdır"
      caption="Aynı yıl, farklı sepetler: kiracı ortalamanın üstünde, ev sahibi altında hissedebilir."
      height={120}
    >
      <line x1="150" y1="16" x2="150" y2="104" className="fig-axis" />
      <text x="150" y="12" className="fig-value" textAnchor="middle">ortalama (TÜFE)</text>
      <text x="12" y="44" className="fig-label">Kiracı</text>
      <rect x="150" y="30" width="90" height="18" rx="3" className="fig-bar-neg" />
      <text x="246" y="44" className="fig-value neg">üstünde</text>
      <text x="12" y="88" className="fig-label">Ev sahibi</text>
      <rect x="90" y="74" width="60" height="18" rx="3" className="fig-bar-pos" />
      <text x="84" y="88" className="fig-value pos" textAnchor="end">altında</text>
    </Figure>
  );
}

/** S0-L4 · Bileşik erime: 100 → 71 → 51 → 36 (yıllar üst üste binince hızlanır). */
function CompoundedErosion() {
  const vals = [100, 71, 51, 36];
  return (
    <Figure
      label="Yıllık yüzde kırk enflasyonla yüz liranın alım gücü sırayla yetmiş bir elli bir ve otuz altı liraya iner; erime bileşiktir ve hızlanır"
      caption="Yıllık %40 erimeyle: 100 → 71 → 51 → 36. Aynı oran, azalan tabana binerek hızlanır."
      height={130}
    >
      <line x1="20" y1="104" x2="300" y2="104" className="fig-axis" />
      {vals.map((v, i) => (
        <g key={i}>
          <rect x={40 + i * 68} y={104 - v * 0.8} width="44" height={v * 0.8} rx="3"
            className={i === 0 ? "fig-bar-pos" : "fig-bar-muted"} />
          <text x={62 + i * 68} y={104 - v * 0.8 - 6} className="fig-value" textAnchor="middle">{v}</text>
          <text x={62 + i * 68} y="118" className="fig-label" textAnchor="middle">{i}. yıl</text>
        </g>
      ))}
    </Figure>
  );
}

// ── S0-L5 · Nereye yatırılır? — varlık türleri turu figürleri ────────────────
// Sınıflar TANITILIR ama SIRALANMAZ (15 §3.4): eşit boy kartlar, "iyi/kötü" yok.
// Yalnız SINIF adları geçer (mevduat·hisse·altın·döviz·fon); enstrüman/şirket adı yok.

/** S0-L5 · Varlık sınıfı haritası: benzer davranan varlıklar eşit kartlarda gruplanır. */
function AssetClassMap() {
  const classes = ["Mevduat", "Hisse", "Altın", "Döviz", "Fon", "BES"];
  return (
    <Figure
      label="Yaygın varlık sınıfları eşit boyutlu kartlar hâlinde bir harita gibi dizilir: mevduat, hisse, altın, döviz, fon ve BES; hiçbiri diğerinden büyük gösterilmez"
      caption="Benzer davranan varlıklar bir sınıf oluşturur. Kartlar eşit — sıralama değil, harita."
      height={150}
    >
      {classes.map((c, i) => {
        const col = i % 3;
        const row = Math.floor(i / 3);
        const x = 8 + col * 103;
        const y = 24 + row * 62;
        return (
          <g key={c}>
            <rect x={x} y={y} width={95} height={48} rx="8" className="fig-card" />
            <text x={x + 47} y={y + 29} className="fig-value" textAnchor="middle">{c}</text>
          </g>
        );
      })}
    </Figure>
  );
}

/** S0-L5 · Mevduat = bankaya borç: para gider, karşılığında anapara+faiz ALACAĞI kalır. */
function DepositLending() {
  return (
    <Figure
      label="Mevduatta para bankaya gider ve karşılığında sana anapara artı faiz alacağı doğar; sen bankanın ortağı değil alacaklısısın"
      caption="Mevduat yatırmak bankaya borç vermektir: sen alacaklısın, ortak değil."
      height={130}
    >
      <rect x={10} y={50} width={68} height={32} rx="7" className="fig-card" />
      <text x={44} y={70} className="fig-value" textAnchor="middle">Sen</text>
      <rect x={242} y={50} width={68} height={32} rx="7" className="fig-card" />
      <text x={276} y={70} className="fig-value" textAnchor="middle">Banka</text>

      <path d="M80 60 L240 60" className="fig-line-steady" fill="none" />
      <text x={160} y={50} className="fig-label" textAnchor="middle">para (borç)</text>

      <path d="M240 74 L80 74" className="fig-line-steady" fill="none" />
      <text x={160} y={92} className="fig-label" textAnchor="middle">anapara + faiz (alacak)</text>
      <text x={160} y={112} className="fig-value pos" textAnchor="middle">sen alacaklısın</text>
    </Figure>
  );
}

/** S0-L5 · Hisse = ortaklık: şirketin bir dilimine sahip olursun, değeri dalgalanır. */
function EquityOwnership() {
  return (
    <Figure
      label="Hisse almak şirkete ortak olmaktır: şirketin bir dilimine sahip olursun, kâr dağıtılırsa pay alırsın ve payının değeri hem yukarı hem aşağı dalgalanabilir"
      caption="Hisse = ortaklık: şirketin bir dilimi senin. Değeri yukarı da aşağı da açık."
      height={140}
    >
      <circle cx={80} cy={72} r={44} className="fig-bar-muted" opacity="0.35" />
      <path d="M80 72 L80 28 A44 44 0 0 1 118 94 Z" className="fig-bar-pos" opacity="0.7" />
      <text x={80} y={128} className="fig-label" textAnchor="middle">şirket · senin payın</text>

      <path d="M148 60 L206 44" className="fig-line-steady" fill="none" />
      <text x={252} y={44} className="fig-value pos" textAnchor="middle">değer ↑ ?</text>
      <path d="M148 84 L206 100" className="fig-line-volatile" fill="none" />
      <text x={252} y={104} className="fig-value neg" textAnchor="middle">değer ↓ ?</text>
      <text x={230} y={128} className="fig-label" textAnchor="middle">dönüş belirsiz</text>
    </Figure>
  );
}

/** S0-L5 · ÇOK PANELLİ · Aynı 10.000 ₺ üç sınıfta üç ayrı şeye dönüşür. */
function SameMoneyThreeForms() {
  return (
    <Figure
      label="Aynı on bin lira üç ayrı sınıfta üç ayrı şeye dönüşür: mevduatta bir alacağa, hissede bir ortaklık payına, altında fiziksel bir metale"
      caption="Aynı 10.000 ₺, üç ayrı şey: alacak (mevduat) · pay (hisse) · metal (altın)."
      height={150}
    >
      <text x={160} y={14} className="fig-value" textAnchor="middle">10.000 ₺</text>

      <Panel x={4} y={26} w={100} h={104} title="Mevduat" />
      <rect x={26} y={62} width={56} height={30} rx="5" className="fig-bar-muted" />
      <text x={54} y={81} className="fig-label" textAnchor="middle">alacak</text>
      <text x={54} y={116} className="fig-label" textAnchor="middle">anapara + faiz</text>

      <Panel x={110} y={26} w={100} h={104} title="Hisse" />
      <circle cx={160} cy={76} r={22} className="fig-bar-muted" opacity="0.4" />
      <path d="M160 76 L160 54 A22 22 0 0 1 179 87 Z" className="fig-bar-pos" opacity="0.7" />
      <text x={160} y={116} className="fig-label" textAnchor="middle">ortaklık payı</text>

      <Panel x={216} y={26} w={100} h={104} title="Altın" />
      <rect x={244} y={60} width={44} height={32} rx="5" className="fig-bar-pos" opacity="0.55" />
      <text x={266} y={81} className="fig-value" textAnchor="middle">Au</text>
      <text x={266} y={116} className="fig-label" textAnchor="middle">fiziksel metal</text>
    </Figure>
  );
}

/** S0-L5 · Altın/döviz = değer saklama: nakit akışı ÜRETMEZ; değeri fiyatından gelir. */
function StoreOfValue() {
  return (
    <Figure
      label="Altın ve döviz değer saklama araçlarıdır: elde tutmak kendiliğinden faiz veya temettü ödemez, bir nakit akışı üretmez; değeri yalnızca başkasının ödediği fiyattan gelir"
      caption="Altın/döviz nakit akışı üretmez (faiz/temettü yok); değeri fiyatından gelir."
      height={130}
    >
      <rect x={116} y={40} width={88} height={36} rx="8" className="fig-card" />
      <text x={160} y={63} className="fig-value" textAnchor="middle">altın · döviz</text>

      <path d="M116 58 L44 58" className="fig-line-flat" fill="none" strokeDasharray="4 4" />
      <text x={44} y={44} className="fig-value neg" textAnchor="middle">faiz ✗</text>
      <text x={44} y={78} className="fig-label" textAnchor="middle">nakit akışı yok</text>

      <path d="M204 58 L276 58" className="fig-line-steady" fill="none" />
      <text x={276} y={44} className="fig-value" textAnchor="middle">değer</text>
      <text x={276} y={78} className="fig-label" textAnchor="middle">= ödenen fiyat</text>
      <text x={160} y={108} className="fig-label" textAnchor="middle">alım gücünü saklamak için tutulur</text>
    </Figure>
  );
}

/** S0-L5 · ÇOK PANELLİ · Fon = sepet: "tek kutu" görünür ama içi çok varlıkla doludur. */
function FundWrapper() {
  const inner = [0, 1, 2, 3, 4, 5];
  return (
    <Figure
      label="Fon görünüşte tek bir kutudur ama gerçekte içi birçok farklı varlıkla dolu bir sepettir; fon payı almak o sepetin tamamından küçük bir dilim almaktır"
      caption="Fon tek bir şey değil, bir sepettir: içinde birçok farklı varlık olabilir."
      height={150}
    >
      <Panel x={6} y={22} w={130} h={110} title="Göründüğü" />
      <rect x={40} y={58} width={62} height={44} rx="6" className="fig-card" />
      <text x={71} y={84} className="fig-value" textAnchor="middle">FON</text>

      <text x={152} y={80} className="fig-value" textAnchor="middle">→</text>

      <Panel x={170} y={22} w={144} h={110} title="Gerçekte (sepet)" />
      {inner.map((i) => {
        const col = i % 3;
        const row = Math.floor(i / 3);
        return (
          <rect key={i} x={182 + col * 42} y={54 + row * 34} width={34} height={26} rx="3"
            className={i % 2 === 0 ? "fig-bar-muted" : "fig-bar-pos"} opacity="0.65" />
        );
      })}
    </Figure>
  );
}

/** S0-L5 · ÇOK PANELLİ · Ortaklık↔alacaklılık ekseni: iki uç, bir ROL ekseni (kalite değil). */
function OwnershipLendingAxis() {
  return (
    <Figure
      label="Bir eksenin bir ucunda alacaklılık yani mevduat ve tahvil, diğer ucunda ortaklık yani hisse yer alır; bu bir iyi kötü ekseni değil bir rol eksenidir"
      caption="Alacaklı mısın, ortak mı? Bir rol ekseni — biri diğerinden üstün değil."
      height={140}
    >
      <line x1={20} y1="66" x2={300} y2="66" className="fig-axis" />
      <text x={20} y={26} className="fig-label">Alacaklılık</text>
      <text x={300} y={26} className="fig-label" textAnchor="end">Ortaklık</text>

      <circle cx={20} cy={66} r="5" className="fig-dot-volatile" />
      <circle cx={300} cy={66} r="5" className="fig-dot-volatile" />

      <Panel x={6} y={80} w={140} h={52} title="Borç verirsin" />
      <text x={76} y={116} className="fig-label" textAnchor="middle">mevduat · tahvil</text>
      <text x={76} y={44} className="fig-label" textAnchor="middle">belli ödeme · öncelik</text>

      <Panel x={174} y={80} w={140} h={52} title="Sahip olursun" />
      <text x={244} y={116} className="fig-label" textAnchor="middle">hisse</text>
      <text x={244} y={44} className="fig-label" textAnchor="middle">belirsiz · üst sınır yok</text>
    </Figure>
  );
}

/** S0-L5 · Likidite ekseni: hızlı↔yavaş nakde dönme. Bir KALİTE sırası DEĞİL (caption'da). */
function LiquidityAxis() {
  const items = [
    { label: "nakit · mevduat", x: 48 },
    { label: "büyük hisse", x: 160 },
    { label: "gayrimenkul", x: 280 },
  ];
  return (
    <Figure
      label="Likidite ekseninde nakit ve mevduat hızlı, büyük hisseler orta, gayrimenkul yavaş nakde döner; bu bir kalite sıralaması değil yalnızca hız eksenidir"
      caption="Ne kadar hızlı nakde döner? Bir hız ekseni — kalite/iyi-kötü sırası DEĞİL."
      height={120}
    >
      <line x1={20} y1="60" x2={300} y2="60" className="fig-axis" />
      <text x={20} y={92} className="fig-label">← yavaş</text>
      <text x={300} y={92} className="fig-label" textAnchor="end">hızlı →</text>
      {items.map((it) => (
        <g key={it.label}>
          <circle cx={it.x} cy={60} r="5" className="fig-dot-volatile" />
          <text x={it.x} y={42} className="fig-label" textAnchor="middle">{it.label}</text>
        </g>
      ))}
    </Figure>
  );
}

/** S0-L5 · Alış-satış makası: aynı anda alış 102 / satış 98 → girip çıkmak ≈ %3,9. */
function BidAskSpread() {
  return (
    <Figure
      label="Aynı varlığın aynı anda alış fiyatı yüz iki lira satış fiyatı doksan sekiz liradır; alıp hemen satarsan dört lira yaklaşık yüzde üç virgül dokuz kaybedersin, bu makastır"
      caption="Aynı an, iki fiyat: alış 102 ₺ / satış 98 ₺. Girip çıkmak ≈ 4 ₺ (%3,9) makas."
      height={120}
    >
      <text x={16} y={44} className="fig-label">alış</text>
      <rect x={64} y={30} width={210} height={20} rx="3" className="fig-bar-muted" />
      <text x={284} y={45} className="fig-value" textAnchor="end">102 ₺</text>

      <text x={16} y={84} className="fig-label">satış</text>
      <rect x={64} y={70} width={190} height={20} rx="3" className="fig-bar-pos" opacity="0.6" />
      <text x={284} y={85} className="fig-value" textAnchor="end">98 ₺</text>

      <rect x={254} y={30} width={20} height={60} className="fig-bar-neg" opacity="0.4" />
      <text x={160} y={110} className="fig-value neg" textAnchor="middle">makas ≈ 4 ₺ (%3,9)</text>
    </Figure>
  );
}

/** Anahtar → figür kayıt defteri. Bilinmeyen anahtar `null` (içerik bozulmaz). */
const FIGURES: Record<string, () => React.JSX.Element> = {
  // Set 0 — İlk Adımlar (T6.16)
  "asset-class-map": AssetClassMap,
  "deposit-lending": DepositLending,
  "equity-ownership": EquityOwnership,
  "same-money-three-forms": SameMoneyThreeForms,
  "store-of-value": StoreOfValue,
  "fund-wrapper": FundWrapper,
  "ownership-lending-axis": OwnershipLendingAxis,
  "liquidity-axis": LiquidityAxis,
  "bid-ask-spread": BidAskSpread,
  "three-actions": ThreeActions,
  "hold-vs-flip": HoldVsFlip,
  "same-basket-two-dates": SameBasketTwoDates,
  "basket-price-up": BasketPriceUp,
  "amount-vs-power": AmountVsPower,
  "standing-still": StandingStill,
  "inflation-slider": InflationSlider,
  "index-basket": IndexBasket,
  "personal-basket": PersonalBasket,
  "compounded-erosion": CompoundedErosion,
  "shock-event": ShockEvent,
  "with-without-buffer": WithWithoutBuffer,
  "buffer-traits": BufferTraits,
  "liquidity-traded-away": LiquidityTradedAway,
  "monthly-to-yearly": MonthlyToYearly,
  "debt-cost-ladder": DebtCostLadder,
  "one-lira-two-jobs": OneLiraTwoJobs,
  "certain-vs-uncertain": CertainVsUncertain,
  "two-costs-of-cash": TwoCostsOfCash,
  "three-buckets": ThreeBuckets,
  "monthly-split": MonthlySplit,
  "savings-rate-bar": SavingsRateBar,
  "two-savers": TwoSavers,
  "leftover-trap": LeftoverTrap,
  "pay-yourself-first": PayYourselfFirst,
  "lifestyle-creep": LifestyleCreep,
  "rate-vs-return-lever": RateVsReturnLever,
  "ten-thousand-three-paths": TenThousandThreePaths,
  "capital-to-use": CapitalToUse,
  "money-at-work": MoneyAtWork,
  "no-guarantee": NoGuarantee,
  "value-vs-zero-sum": ValueVsZeroSum,
  "risk-premium-intro": RiskPremiumIntro,
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
