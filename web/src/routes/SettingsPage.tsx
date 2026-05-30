import type { CurrencyCode } from "@finans/shared";
import { CurrencySelector } from "../components/CurrencySelector";
import { Disclaimer } from "../components/Disclaimer";
import { useToast } from "../components/Toast";
import { useSettings, useUpdateSettings } from "../lib/hooks";

/**
 * Ayarlar (13 §4). Faz 1'de baz para birimi tercihi (kullanıcıya kapsanır).
 * Bildirimler/veri-dışa-aktarma ileride; disclaimer (NFR-2) her zaman görünür.
 */
export function SettingsPage() {
  const settings = useSettings();
  const update = useUpdateSettings();
  const { notify } = useToast();

  const onCurrencyChange = (currency: CurrencyCode) =>
    update.mutate(
      { baseCurrency: currency },
      { onSuccess: () => notify(`Baz para birimi ${currency} olarak ayarlandı.`, "success") },
    );

  return (
    <section className="page">
      <div className="topbar">
        <div>
          <div className="greet-hi">Tercihler</div>
          <h1>Ayarlar</h1>
        </div>
      </div>

      <div className="setgrp">
        <div className="gt">Para birimi &amp; gösterim</div>
        <div className="setrow">
          <div>
            <div className="sl-n">Baz para birimi</div>
            <div className="sl-d">Tüm toplamlar bu birimde gösterilir; varlıklar güncel kurdan çevrilir.</div>
          </div>
          {settings.data && (
            <CurrencySelector
              value={settings.data.baseCurrency}
              onChange={onCurrencyChange}
              disabled={update.isPending}
            />
          )}
        </div>
      </div>

      <Disclaimer />
    </section>
  );
}
