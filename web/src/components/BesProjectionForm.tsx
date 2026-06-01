import { useState } from "react";
import { formatCurrency, formatPercent, formatNumber } from "@finans/shared";
import type { BesProjection } from "@finans/shared";
import { useBesProjection } from "../lib/hooks";

const toNumber = (s: string) => Number(s.replace(",", "."));

/** Ortak süre seçenekleri (yıl) — sık başvurulan eşikler. */
const YEAR_PRESETS = [1, 3, 5, 10, 15, 20, 25, 30] as const;
/** Yıllık getiri öneri çipleri (%) — TR enflasyon/fon piyasasında yaygın varsayımlar. */
const RATE_PRESETS = [15, 25, 35, 50] as const;

/**
 * BES eğitici projeksiyon formu + sonuç gösterimi (T-BES.5). Kullanıcı varsayımları girer
 * (aylık katkı, süre, yıllık getiri); backend deterministik bileşik getiri uygular ve birikim
 * illüstrasyonu döner.
 *
 * <p><b>YATIRIM TAVSİYESİ DEĞİL</b> (CLAUDE.md §2): yalnız "varsayımlarının sonucu" çerçevesi.
 * Gelecek tahmini değil, somut yönlendirme yok. Disclaimer kalıcı + sonuç başında görünür.</p>
 */
export function BesProjectionForm({
  holdingId,
  defaultMonthly,
}: {
  holdingId: string;
  /** Mevcut düzenli plan varsa o tutarla ön-doldur; yoksa boş. */
  defaultMonthly: number | null;
}) {
  const project = useBesProjection(holdingId);
  const [monthly, setMonthly] = useState(defaultMonthly ? String(defaultMonthly) : "");
  const [years, setYears] = useState<number>(10);
  const [ratePct, setRatePct] = useState<string>("25");

  const monthlyNum = toNumber(monthly);
  const rateNum = toNumber(ratePct);
  const valid =
    Number.isFinite(monthlyNum) && monthlyNum >= 0 &&
    Number.isFinite(rateNum) && rateNum >= -99 && rateNum <= 200 &&
    years >= 1 && years <= 50;

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
          <label>
            Süre (yıl)
            <select value={years} onChange={(e) => setYears(Number(e.target.value))}>
              {YEAR_PRESETS.map((y) => (
                <option key={y} value={y}>{y} yıl</option>
              ))}
            </select>
          </label>
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

      {project.data && <BesProjectionResultCard data={project.data} />}
    </section>
  );
}

function BesProjectionResultCard({ data }: { data: BesProjection }) {
  const ownReturnPct = data.totalOwnContribution > 0
    ? data.ownProfit / data.totalOwnContribution
    : 0;
  const stateReturnPct = data.totalStateContribution > 0
    ? data.stateProfit / data.totalStateContribution
    : 0;

  return (
    <div className="proj-result">
      <div className="proj-hero">
        <div className="dh-v tnum">{formatCurrency(data.fundValue, "TRY")}</div>
        <div className="dh-sub">Süre sonu varsayımsal fon değeri</div>
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
