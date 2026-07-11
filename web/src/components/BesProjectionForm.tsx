import { useMemo, useState } from "react";
import { formatCurrency, formatPercent, formatNumber } from "@finans/shared";
import type { BesProjection } from "@finans/shared";
import { useBesProjection } from "../lib/hooks";
import { CountUpCurrency } from "./CountUp";
import { Sparkline } from "./Sparkline";

const toNumber = (s: string) => Number(s.replace(",", "."));

/** Yıllık getiri öneri çipleri (%) — TR'de yaygın varsayımlar. */
const RATE_PRESETS = [15, 25, 35, 50] as const;

/** Sözleşme kademe preset'i — yıl + etiket + hak ediş yüzdesi. */
interface YearPreset { years: number; label: string; sublabel: string; }

/**
 * BES sözleşme kademelerine göre süre preset'leri üret. Mevcut sözleşmenin başlangıcı varsa,
 * "kalan yıl" mantığıyla 3/6/10 yıl noktalarına ulaşana kadar gereken süreyi hesaplar.
 * Emeklilik (10 yıl + 56 yaş): doğum yılı varsa hedef yıla kalan, yoksa preset gizlenir.
 */
function buildYearPresets(joinedAtUtc: string | null, birthYear: number | null): YearPreset[] {
  const now = new Date();
  const yearsInSystem = joinedAtUtc
    ? Math.max(0, Math.floor((now.getTime() - new Date(joinedAtUtc).getTime()) / (365.25 * 24 * 3600 * 1000)))
    : 0;
  // "X yıl noktasına" ulaşmak için kalan; yeni sözleşme (joined yoksa) için tam X.
  const toMilestone = (m: number) => Math.max(1, m - yearsInSystem);

  const presets: YearPreset[] = [
    { years: toMilestone(3), label: "3. yıl", sublabel: "Kısmen hak ediş %15" },
    { years: toMilestone(6), label: "6. yıl", sublabel: "Kademe %35" },
    { years: toMilestone(10), label: "10. yıl", sublabel: "Kademe %60" },
  ];

  // Emeklilik: 10 yıl tamamlanmış + 56+ yaş. Doğum yılı yoksa preset üretme.
  if (birthYear !== null) {
    const ageNow = now.getFullYear() - birthYear;
    const yearsTo56 = Math.max(0, 56 - ageNow);
    const yearsTo10 = Math.max(0, 10 - yearsInSystem);
    const yearsToRetirement = Math.max(1, Math.max(yearsTo56, yearsTo10));
    presets.push({
      years: yearsToRetirement,
      label: "Emeklilik",
      sublabel: `~${yearsToRetirement} yıl · Tam hak ediş %100`,
    });
  }

  return presets;
}

/**
 * BES eğitici projeksiyon formu + sonuç gösterimi (T-BES.5). Kullanıcı sözleşme kademelerine
 * göre yıl preset'i seçer (3 / 6 / 10 / Emeklilik) — etiketinde **süre sonu hak ediş yüzdesi**
 * — veya "Özel" alanına kendi yılını yazar. Backend deterministik bileşik getiri uygular ve
 * birikim illüstrasyonu döner.
 *
 * <p><b>YATIRIM TAVSİYESİ DEĞİL</b> (CLAUDE.md §2): yalnız "varsayımlarının sonucu" çerçevesi.
 * Gelecek tahmini değil, somut yönlendirme yok. Disclaimer kalıcı + sonuç başında görünür.</p>
 */
export function BesProjectionForm({
  holdingId,
  defaultMonthly,
  joinedAtUtc,
  birthYear,
}: {
  holdingId: string;
  /** Mevcut düzenli plan varsa o tutarla ön-doldur; yoksa boş. */
  defaultMonthly: number | null;
  /** BES sözleşme başlangıcı (varsa) — süre preset'leri buna göre türetilir. */
  joinedAtUtc: string | null;
  /** Doğum yılı (varsa) — "Emeklilik" preset'i için. */
  birthYear: number | null;
}) {
  const project = useBesProjection(holdingId);
  const presets = useMemo(() => buildYearPresets(joinedAtUtc, birthYear), [joinedAtUtc, birthYear]);

  const [monthly, setMonthly] = useState(defaultMonthly ? String(defaultMonthly) : "");
  // Başlangıçta ilk preset (3. yıl) seçili — kullanıcının bir anlam yüklü değerle başlaması için.
  const [years, setYears] = useState<number>(presets[0]?.years ?? 5);
  const [ratePct, setRatePct] = useState<string>("25");

  const monthlyNum = toNumber(monthly);
  const rateNum = toNumber(ratePct);
  const valid =
    Number.isFinite(monthlyNum) && monthlyNum >= 0 &&
    Number.isFinite(rateNum) && rateNum >= -99 && rateNum <= 200 &&
    Number.isFinite(years) && years >= 1 && years <= 50;

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!valid) return;
    project.mutate({
      ownMonthly: monthlyNum,
      years,
      annualReturnRatio: rateNum / 100,
    });
  };

  return (
    <section className="bes-proj">
      {/* Kalıcı disclaimer (CLAUDE.md §2): senaryo/farkındalık serbest; tahmin/yönlendirme yasak. */}
      <div className="proj-disclaimer" role="note">
        <b>Varsayımsal senaryo</b> — yatırım tavsiyesi <b>DEĞİLDİR</b>. Bu hesap geleceği tahmin
        etmez; <i>senin verdiğin varsayımlarla</i> aritmetik bir illüstrasyon üretir.
        Vergi, fon yönetim ücreti ve enflasyon dahil edilmez. Gerçek getiri farklı olur.
      </div>

      <form className="tx-form" onSubmit={onSubmit} aria-label="BES eğitici senaryo">
        <div className="tx-row">
          <label>
            Aylık katkı (₺)
            <input
              inputMode="decimal"
              value={monthly}
              onChange={(e) => setMonthly(e.target.value)}
              placeholder={defaultMonthly ? formatNumber(defaultMonthly) : "1.000"}
              required
            />
          </label>
        </div>

        <div className="proj-years">
          <div className="proj-years-h">Süre (sözleşme kademelerine göre)</div>
          <div className="proj-years-chips" role="group" aria-label="Süre preset'leri">
            {presets.map((p) => (
              <button
                key={p.label}
                type="button"
                className={`year-chip ${years === p.years ? "sel" : ""}`}
                onClick={() => setYears(p.years)}
                aria-pressed={years === p.years}
              >
                <span className="yc-main">{p.label} · {p.years} yıl</span>
                <span className="yc-sub">{p.sublabel}</span>
              </button>
            ))}
          </div>
          <label className="proj-years-custom">
            Özel:
            <input
              type="number"
              inputMode="numeric"
              min={1}
              max={50}
              value={years}
              onChange={(e) => {
                const n = Number(e.target.value);
                if (Number.isFinite(n)) setYears(Math.min(50, Math.max(1, Math.floor(n))));
              }}
              aria-label="Özel yıl sayısı"
            />
            yıl
          </label>
        </div>

        <div className="tx-row">
          <label>
            Yıllık getiri varsayımı (%)
            <input
              inputMode="decimal"
              value={ratePct}
              onChange={(e) => setRatePct(e.target.value)}
              placeholder="25"
              required
            />
          </label>
        </div>
        <div className="proj-rate-chips" role="group" aria-label="Yaygın getiri varsayımları">
          {RATE_PRESETS.map((r) => (
            <button
              key={r}
              type="button"
              className={`chip ${Number(ratePct) === r ? "sel" : ""}`}
              onClick={() => setRatePct(String(r))}
            >
              %{r}
            </button>
          ))}
        </div>

        <button type="submit" disabled={!valid || project.isPending} className="btn-primary">
          {project.isPending ? "Hesaplanıyor…" : "Senaryoyu hesapla"}
        </button>
        {project.isError && (
          <p className="neg" role="alert">
            {project.error instanceof Error ? project.error.message : "Hesaplanamadı."}
          </p>
        )}
      </form>

      {project.data && <BesProjectionResultCard data={project.data} years={years} />}
    </section>
  );
}

function BesProjectionResultCard({ data, years }: { data: BesProjection; years: number }) {
  const ownReturnPct = data.totalOwnContribution > 0
    ? data.ownProfit / data.totalOwnContribution
    : 0;
  const stateReturnPct = data.totalStateContribution > 0
    ? data.stateProfit / data.totalStateContribution
    : 0;

  return (
    <div className="proj-result">
      <div className="proj-hero">
        <div className="dh-v tnum"><CountUpCurrency value={data.fundValue} currency="TRY" /></div>
        <div className="dh-sub">{years}. yıl sonu varsayımsal fon değeri</div>
        {/* Yıllık fon değeri serisi — key: yeni hesapta çizim animasyonu baştan oynasın. */}
        {data.yearly.length >= 2 && (
          <Sparkline
            key={`${data.yearly.length}-${data.fundValue}`}
            values={data.yearly.map((y) => y.fundValue)}
          />
        )}
      </div>

      <div className="proj-grid">
        <div className="proj-card">
          <div className="proj-card-h">Kendi katkı</div>
          <div className="drow">
            <span className="dk">Yatırılan toplam</span>
            <span className="dv tnum">{formatCurrency(data.totalOwnContribution, "TRY")}</span>
          </div>
          <div className="drow">
            <span className="dk">Süre sonu değeri</span>
            <span className="dv tnum">{formatCurrency(data.ownValue, "TRY")}</span>
          </div>
          <div className="drow">
            <span className="dk">Kâr/Zarar</span>
            <span className={`dv tnum ${data.ownProfit >= 0 ? "up" : "down"}`}>
              {data.ownProfit >= 0 ? "+" : ""}{formatCurrency(data.ownProfit, "TRY")}
              {" · "}{formatPercent(ownReturnPct)}
            </span>
          </div>
        </div>
        <div className="proj-card">
          <div className="proj-card-h">Devlet katkısı</div>
          <div className="drow">
            <span className="dk">Yatırılan toplam</span>
            <span className="dv tnum up">{formatCurrency(data.totalStateContribution, "TRY")}</span>
          </div>
          <div className="drow">
            <span className="dk">Süre sonu değeri</span>
            <span className="dv tnum">{formatCurrency(data.stateValue, "TRY")}</span>
          </div>
          <div className="drow">
            <span className="dk">Kâr/Zarar</span>
            <span className={`dv tnum ${data.stateProfit >= 0 ? "up" : "down"}`}>
              {data.stateProfit >= 0 ? "+" : ""}{formatCurrency(data.stateProfit, "TRY")}
              {" · "}{formatPercent(stateReturnPct)}
            </span>
          </div>
        </div>
      </div>

      {/* Süre sonu hak ediş kartı — sözleşme kademesi + hak kazanılan devlet katkısı (T-BES.5). */}
      <div className="proj-vesting">
        <div className="proj-vesting-h">Süre sonu hak ediş</div>
        <div className="drow">
          <span className="dk">Hak ediş oranı</span>
          <span className="dv">
            <b>{formatPercent(data.vestedRateAtEnd)}</b>
            <span className="muted">{" · "}sözleşme kademesi</span>
          </span>
        </div>
        <div className="drow">
          <span className="dk">Hak kazanılan devlet katkısı</span>
          <span className="dv tnum up">{formatCurrency(data.vestedStateAmountAtEnd, "TRY")}</span>
        </div>
        <p className="note-muted">
          Hak ediş <b>devlet katkısının</b> sana kalan kısmı. Kademeler:{" "}
          <b>&lt;3 yıl %0</b> · <b>3-6 yıl %15</b> · <b>6-10 yıl %35</b> · <b>10 yıl+ %60</b> ·{" "}
          <b>10 yıl + 56 yaş %100</b>. Kendi katkı payın <b>her zaman senindir</b>.
        </p>
      </div>

      {data.yearly.length > 0 && (
        <div className="history-scroll">
          <table className="holdings-table fit proj-series">
            <thead>
              <tr>
                <th scope="col">Yıl</th>
                <th scope="col" className="num">Kendi katkı</th>
                <th scope="col" className="num">Devlet</th>
                <th scope="col" className="num">Fon değeri</th>
                <th scope="col" className="num">Toplam K/Z</th>
              </tr>
            </thead>
            <tbody>
              {data.yearly.map((y) => {
                const totalProfit = y.ownProfit + y.stateProfit;
                return (
                  <tr key={y.year}>
                    <td>{y.year}. yıl</td>
                    <td className="num">{formatCurrency(y.ownContribution, "TRY")}</td>
                    <td className="num up">{formatCurrency(y.stateContribution, "TRY")}</td>
                    <td className="num">{formatCurrency(y.fundValue, "TRY")}</td>
                    <td className={`num ${totalProfit >= 0 ? "up" : "down"}`}>
                      {totalProfit >= 0 ? "+" : ""}{formatCurrency(totalProfit, "TRY")}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <p className="note-muted">
        Yıllık {formatPercent(data.annualReturnRatio)} sabit getiri varsayımıyla hesaplanmıştır.
        Gerçek getiri her ay farklıdır; düşüşler de olur. Devlet katkısının ~1 ay gecikmeli
        yatması bu illüstrasyonda göz ardı edilmiştir. <b>Yatırım tavsiyesi değildir.</b>
      </p>
    </div>
  );
}
