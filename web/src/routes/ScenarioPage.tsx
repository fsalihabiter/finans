import { useState } from "react";
import { Link } from "react-router-dom";
import { formatCurrency, formatPercent } from "@finans/shared";
import { ASSET_META } from "../lib/assetMeta";
import { buildScenarioNarrative } from "../lib/scenarioNarrative";
import { Disclaimer } from "../components/Disclaimer";
import { EmptyState } from "../components/EmptyState";
import { PortfolioSkeleton } from "../components/Skeleton";
import { ScenarioChart } from "../components/ScenarioChart";
import { useHoldings, useScenario } from "../lib/hooks";

/**
 * Senaryo v1 (T5.4) — geçmişe dönük "bu varlığı almasaydım / param nakitte dursaydı"
 * karşılaştırması. Geleceği TAHMİN ETMEZ; birikmiş geçmiş veriyle üç çizgiyi yan yana
 * koyar: gerçek değer · nakitte dursaydı (nominal) · alım gücü eşiği (enflasyon).
 * Al/sat yönlendirmesi YOK — yalnız durum gösterimi + eğitici çerçeve (CLAUDE.md §2).
 */
export function ScenarioPage() {
  const holdings = useHoldings();
  const [selectedId, setSelectedId] = useState("");

  // Nakit pozisyonda "nakitte dursaydı" karşılaştırması totolojik → seçenek dışı.
  const list = (Array.isArray(holdings.data) ? holdings.data : []).filter(
    (h) => h.assetType !== "Cash",
  );
  const activeId = selectedId || list[0]?.id || "";
  const scenario = useScenario(activeId);

  const s = scenario.data;
  const positive = (s?.summary.difference ?? 0) >= 0;

  return (
    <section className="page">
      <header className="page-head">
        <p className="kicker">Geçmişe dönük simülatör</p>
        <h1>Senaryo</h1>
        <p className="page-lead">
          "Bu varlığı almasaydım, param nakitte dursaydı ne olurdu?" — birikmiş geçmiş
          veriyle karşılaştırır. Geleceği öngörmez; öğrenmek için güvenli bir alandır.
        </p>
        <Disclaimer />
      </header>

      {holdings.isLoading && <PortfolioSkeleton />}
      {holdings.isError && (
        <div className="state-error" role="alert">
          <p>Pozisyonlar yüklenemedi. Bağlantını kontrol edip tekrar dene.</p>
          <button type="button" className="btn-primary" onClick={() => holdings.refetch()}>
            Tekrar dene
          </button>
        </div>
      )}

      {holdings.data && list.length === 0 && (
        <EmptyState
          icon="⚖"
          title="Karşılaştırma için önce varlık ekle"
          description="Nakit dışı bir pozisyon eklediğinde 'nakitte dursaydı' karşılaştırmasını burada görebilirsin."
          action={
            <Link to="/varliklar" className="btn-primary lg">
              ＋ Varlıklarım'da ekle
            </Link>
          }
        />
      )}

      {list.length > 0 && (
        <>
          <div className="periods sc-picker" role="group" aria-label="Pozisyon seç">
            {list.map((h) => (
              <button
                key={h.id}
                type="button"
                className={h.id === activeId ? "on" : ""}
                aria-pressed={h.id === activeId}
                onClick={() => setSelectedId(h.id)}
              >
                <span aria-hidden="true">{ASSET_META[h.assetType].icon}</span> {h.name}
              </button>
            ))}
          </div>

          {scenario.isLoading && <PortfolioSkeleton />}
          {scenario.isError && (
            <div className="state-error" role="alert">
              <p>Karşılaştırma yüklenemedi.</p>
              <button type="button" className="btn-primary" onClick={() => scenario.refetch()}>
                Tekrar dene
              </button>
            </div>
          )}

          {s && (
            <div className="card">
              <div className="card-head">
                <h3>{s.name} — nakitte dursaydı?</h3>
                {s.summary.differenceRatio != null && (
                  <span className={`tnum chart-change ${positive ? "up" : "down"}`}>
                    {positive ? "▲" : "▼"} {formatPercent(s.summary.differenceRatio)}
                  </span>
                )}
              </div>

              <div className="sc-summary">
                <div className="sc-stat">
                  <div className="label">Bugünkü değeri</div>
                  <div className="num tnum">{formatCurrency(s.summary.currentValue, s.baseCurrency)}</div>
                </div>
                <div className="sc-stat">
                  <div className="label">Nakitte dursaydı</div>
                  <div className="num tnum">{formatCurrency(s.summary.invested, s.baseCurrency)}</div>
                  <div className="sub">yatırılan tutar (nominal)</div>
                </div>
                <div className="sc-stat">
                  <div className="label">Fark</div>
                  <div className={`num tnum ${positive ? "up" : "down"}`}>
                    {positive ? "+" : ""}
                    {formatCurrency(s.summary.difference, s.baseCurrency)}
                  </div>
                  <div className="sub">değer − yatırılan</div>
                </div>
                {s.summary.inflationAdjustedInvested != null && (
                  <div className="sc-stat">
                    <div className="label">Alım gücü eşiği</div>
                    <div className="num tnum">
                      {formatCurrency(s.summary.inflationAdjustedInvested, s.baseCurrency)}
                    </div>
                    {s.summary.annualInflationRate != null && (
                      <div className="sub">
                        {/* oran gösterimi — işaretsiz (getiri değil, varsayım parametresi) */}
                        yıllık {formatPercent(s.summary.annualInflationRate, 1, true, false)} enflasyonla
                      </div>
                    )}
                  </div>
                )}
              </div>

              {/* Sayıların METİN okuması — deterministik şablon (LLM/tahmin/tavsiye yok). */}
              <p className="sc-narrative">{buildScenarioNarrative(s)}</p>

              {s.points.length >= 2 ? (
                <ScenarioChart points={s.points} currency={s.baseCurrency} />
              ) : (
                <div className="chart-frame">
                  <div className="cf-empty">
                    <div className="cf-ic" aria-hidden="true">🕒</div>
                    <p>Karşılaştırma grafiği için en az iki günlük veri gerekir — bu pozisyon
                      yeni eklenmiş görünüyor. Veri biriktikçe burada görünecek.</p>
                  </div>
                </div>
              )}

              <p className="sc-explain">
                <b>Kesikli çizgi</b>, yatırılan paranın nakitte (yastık altında) kalsaydı bugünkü
                nominal tutarı; <b>noktalı çizgi</b> ise aynı paranın alım gücünü koruması için
                ulaşması gereken seviyedir (yıllık enflasyon varsayımıyla). Değer çizgisi eşiğin
                üzerindeyse varlık alım gücünü korumuş, altındaysa nominal kazansa bile gerçekte
                kaybettirmiş demektir. Grafik geçmişi gösterir; gelecek performansın göstergesi
                değildir.
              </p>
            </div>
          )}
        </>
      )}
    </section>
  );
}
