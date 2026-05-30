import { ComingSoonPage } from "../components/ComingSoonPage";

/**
 * İşlemler — tüm pozisyonların alış/satış hareketlerinin tek listesi. Şu an işlemler
 * pozisyon detayında görülüyor; küresel liste için backend toplu uç gerekir (Faz 2).
 */
export function TransactionsPage() {
  return (
    <ComingSoonPage
      kicker="Tüm hareketler"
      title="İşlemler"
      icon="↕"
      heading="Tüm işlemler tek yerde"
      description="Her pozisyonun alış/satış hareketleri tek akışta, filtrelenebilir biçimde burada listelenecek. Şimdilik işlemleri ilgili varlığın detay sayfasında görebilirsin."
      phase="Faz 2"
    />
  );
}
