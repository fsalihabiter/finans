import type { CurrencyCode } from "@finans/shared";
import { HeroCard } from "../components/HeroCard";
import { CurrencySelector } from "../components/CurrencySelector";
import { usePortfolioSummary, useSettings, useUpdateSettings } from "../lib/hooks";

/**
 * Portföy sayfası (T1.11): özet HeroCard + baz para birimi seçici. Dağılım grafiği
 * (T1.12) ve holdings tablosu (T1.13) sonraki adımlarda eklenir.
 * Sayısal hesap backend'de; burada yalnızca veri bağlama + tr-TR biçimleme.
 */
export function PortfolioPage() {
  const settings = useSettings();
  const summary = usePortfolioSummary();
  const updateSettings = useUpdateSettings();

  const baseCurrency = settings.data?.baseCurrency;
  const onCurrencyChange = (currency: CurrencyCode) =>
    updateSettings.mutate({ baseCurrency: currency });

  return (
    <section>
      <header className="page-head">
        <h1>Portföy</h1>
        {baseCurrency && (
          <CurrencySelector
            value={baseCurrency}
            onChange={onCurrencyChange}
            disabled={updateSettings.isPending}
          />
        )}
      </header>

      {summary.isLoading && <p className="muted">Yükleniyor…</p>}
      {summary.isError && (
        <p className="neg" role="alert">
          Portföy özeti yüklenemedi. Bağlantıyı kontrol edip tekrar deneyin.
        </p>
      )}

      {summary.data && (
        <>
          <HeroCard summary={summary.data} />
          {summary.data.allocation.length === 0 && (
            <p className="muted empty-hint">
              Henüz pozisyonun yok. Bir varlık ekleyerek başla.
            </p>
          )}
        </>
      )}
    </section>
  );
}
