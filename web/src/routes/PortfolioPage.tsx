import { useState } from "react";
import type { CurrencyCode } from "@finans/shared";
import { HeroCard } from "../components/HeroCard";
import { AllocationDonut } from "../components/AllocationDonut";
import { CurrencySelector } from "../components/CurrencySelector";
import { HoldingsTable } from "../components/HoldingsTable";
import { AddHoldingDialog } from "../components/AddHoldingDialog";
import { useHoldings, usePortfolioSummary, useSettings, useUpdateSettings } from "../lib/hooks";

/**
 * Portföy sayfası (T1.11): özet HeroCard + baz para birimi seçici. Dağılım grafiği
 * (T1.12) ve holdings tablosu (T1.13) sonraki adımlarda eklenir.
 * Sayısal hesap backend'de; burada yalnızca veri bağlama + tr-TR biçimleme.
 */
export function PortfolioPage() {
  const settings = useSettings();
  const summary = usePortfolioSummary();
  const holdings = useHoldings();
  const updateSettings = useUpdateSettings();
  const [addOpen, setAddOpen] = useState(false);

  const baseCurrency = settings.data?.baseCurrency;
  const onCurrencyChange = (currency: CurrencyCode) =>
    updateSettings.mutate({ baseCurrency: currency });

  return (
    <section>
      <header className="page-head">
        <h1>Portföy</h1>
        <div className="page-tools">
          {baseCurrency && (
            <CurrencySelector
              value={baseCurrency}
              onChange={onCurrencyChange}
              disabled={updateSettings.isPending}
            />
          )}
          <button type="button" className="btn-primary" onClick={() => setAddOpen(true)}>
            + Varlık Ekle
          </button>
        </div>
      </header>

      <AddHoldingDialog open={addOpen} onClose={() => setAddOpen(false)} />

      {summary.isLoading && <p className="muted">Yükleniyor…</p>}
      {summary.isError && (
        <p className="neg" role="alert">
          Portföy özeti yüklenemedi. Bağlantıyı kontrol edip tekrar deneyin.
        </p>
      )}

      {summary.data && (
        <>
          <div className="portfolio-grid">
            <HeroCard summary={summary.data} />
            {summary.data.allocation.length > 0 ? (
              <AllocationDonut
                allocation={summary.data.allocation}
                baseCurrency={summary.data.baseCurrency}
              />
            ) : (
              <p className="muted empty-hint">
                Henüz pozisyonun yok. Bir varlık ekleyerek başla.
              </p>
            )}
          </div>

          {holdings.data && holdings.data.length > 0 && (
            <section className="holdings-section">
              <h2>Pozisyonlar</h2>
              <HoldingsTable holdings={holdings.data} baseCurrency={summary.data.baseCurrency} />
            </section>
          )}
        </>
      )}
    </section>
  );
}
