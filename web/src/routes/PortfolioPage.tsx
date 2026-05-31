import { useEffect, useRef } from "react";
import { Link } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import type { CurrencyCode } from "@finans/shared";
import { KpiGrid } from "../components/KpiGrid";
import { AllocationDonut } from "../components/AllocationDonut";
import { PortfolioInsights } from "../components/PortfolioInsights";
import { NudgesCard } from "../components/NudgesCard";
import { PriceTicker } from "../components/PriceTicker";
import { CurrencySelector } from "../components/CurrencySelector";
import { HoldingsTable } from "../components/HoldingsTable";
import { PortfolioSkeleton } from "../components/Skeleton";
import { EmptyState } from "../components/EmptyState";
import { useToast } from "../components/Toast";
import {
  useHoldings,
  useNudges,
  usePortfolioSummary,
  usePrices,
  useSettings,
  useUpdateSettings,
} from "../lib/hooks";
import { useAppShell } from "../lib/appShell";
import { currentGreeting } from "../lib/greeting";

/** Zaman damgasını "14:32" (+ "· yaklaşık" bayatsa) / "—" olarak biçimler. */
function freshness(iso: string, stale = false): string {
  const d = new Date(iso);
  const time = Number.isNaN(d.getTime())
    ? "—"
    : new Intl.DateTimeFormat("tr-TR", { hour: "2-digit", minute: "2-digit" }).format(d);
  return stale ? `${time} · yaklaşık` : time;
}

/**
 * Genel Bakış panosu (13 §4): KPI'lar + dağılım donut + içgörüler + pozisyon tablosu.
 * Tüm sayılar backend'den; burada yalnızca veri bağlama + tr-TR biçimleme (NFR-1/7).
 * Durumlar: yükleniyor → iskelet, hata → tekrar dene, boş → CTA'lı boş durum.
 */
export function PortfolioPage() {
  const settings = useSettings();
  const summary = usePortfolioSummary();
  const holdings = useHoldings();
  const prices = usePrices();
  const nudges = useNudges();
  const updateSettings = useUpdateSettings();
  const { openAddHolding } = useAppShell();
  const { notify } = useToast();
  const qc = useQueryClient();

  // Canlı fiyat tazelendiğinde backend Holding.CurrentPrice'ı yazdı → özet+holdings'i
  // tazele ki pano canlı değeri yansıtsın. Aynı tur için yeniden tetiklemeyi engelle.
  const lastRefresh = useRef<string | null>(null);
  useEffect(() => {
    const refreshed = prices.data?.refreshedAtUtc;
    if (refreshed && refreshed !== lastRefresh.current) {
      lastRefresh.current = refreshed;
      void qc.invalidateQueries({ queryKey: ["summary"] });
      void qc.invalidateQueries({ queryKey: ["holdings"] });
    }
  }, [prices.data?.refreshedAtUtc, qc]);

  const baseCurrency = settings.data?.baseCurrency;
  const onCurrencyChange = (currency: CurrencyCode) =>
    updateSettings.mutate({ baseCurrency: currency });

  const holdingList = Array.isArray(holdings.data) ? holdings.data : [];

  const onRefresh = async () => {
    try {
      const [result] = await Promise.all([prices.refetch(), nudges.refetch()]);
      const stale = result.data?.hasStale ?? false;
      notify(
        stale ? "Fiyatlar güncellendi (bazıları yaklaşık)" : "Fiyatlar güncellendi",
        stale ? "info" : "success",
      );
    } catch {
      notify("Fiyat güncellenemedi. Bağlantını kontrol et.", "error");
    }
  };

  const freshIso = prices.data?.refreshedAtUtc ?? summary.data?.asOf;
  const hasStale = prices.data?.hasStale ?? false;
  const refreshing = prices.isFetching || nudges.isFetching;

  return (
    <section className="page">
      <div className="topbar">
        <div>
          <div className="greet-hi">{currentGreeting()},</div>
          <h1>Genel Bakış</h1>
        </div>
        <PriceTicker prices={prices.data?.prices ?? []} />
        <div className="tools">
          {freshIso && (
            <span className={`freshness${hasStale ? " stale" : ""}`} title="Son fiyat güncellemesi">
              <span className="fresh-dot" aria-hidden="true" /> {freshness(freshIso, hasStale)}
            </span>
          )}
          <button
            type="button"
            className="btn-ghost"
            onClick={onRefresh}
            disabled={refreshing}
          >
            {refreshing ? "Yenileniyor…" : "↻ Yenile"}
          </button>
          {baseCurrency && (
            <CurrencySelector
              value={baseCurrency}
              onChange={onCurrencyChange}
              disabled={updateSettings.isPending}
            />
          )}
        </div>
      </div>

      {summary.isLoading && <PortfolioSkeleton />}

      {summary.isError && (
        <div className="state-error" role="alert">
          <p>Portföy özeti yüklenemedi. Bağlantını kontrol edip tekrar dene.</p>
          <button type="button" className="btn-primary" onClick={() => summary.refetch()}>
            Tekrar dene
          </button>
        </div>
      )}

      {summary.data && (
        <>
          <KpiGrid summary={summary.data} positionCount={holdingList.length} />

          {summary.data.allocation.length > 0 ? (
            <>
              <div className="grid-2">
                <div className="card">
                  <div className="card-head"><h3>Varlık Dağılımı</h3></div>
                  <AllocationDonut allocation={summary.data.allocation} />
                </div>
                <div className="card">
                  <div className="card-head">
                    <h3>Değer Seyri</h3>
                    <Link to="/performans" className="mini link">Detay →</Link>
                  </div>
                  <div className="chart-frame">
                    <div className="cf-empty">
                      <div className="cf-ic" aria-hidden="true">🕒</div>
                      <p>
                        Zaman içindeki değer grafiği, canlı fiyat geçmişi biriktikçe burada
                        görünecek (Faz 2). Şimdilik <b>Performans</b> sekmesinde kalem bazında
                        getiriyi görebilirsin.
                      </p>
                    </div>
                  </div>
                </div>
              </div>

              <PortfolioInsights summary={summary.data} holdings={holdingList} />

              <NudgesCard nudges={nudges.data?.nudges ?? []} />

              <div className="card">
                <div className="card-head">
                  <h3>Varlıklarım</h3>
                  <span className="mini">Detay için satıra tıkla</span>
                </div>
                <HoldingsTable holdings={holdingList} baseCurrency={summary.data.baseCurrency} />
              </div>
            </>
          ) : (
            <EmptyState
              icon="📂"
              title="Portföyün henüz boş"
              description={
                <>
                  İlk varlığını ekle; toplam değer, dağılım ve getiri otomatik
                  hesaplansın. Altın, döviz, hisse, fon, nakit ve BES ekleyebilirsin.
                </>
              }
              action={
                <button type="button" className="btn-primary lg" onClick={openAddHolding}>
                  ＋ İlk varlığını ekle
                </button>
              }
            />
          )}
        </>
      )}
    </section>
  );
}
