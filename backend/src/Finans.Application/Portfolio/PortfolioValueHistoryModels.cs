using Finans.Domain.Enums;

namespace Finans.Application.Portfolio;

/// <summary>
/// <see cref="PortfolioValueHistoryService"/> girdisi: tek varlığın işlem olayları +
/// fiyat gözlemleri (03 §A — PriceSnapshots + Transactions). Saf veri, EF'e bağımsız
/// (02 §2.2). BES gibi işlem-dışı kalemler orkestrasyonda bu modele indirgenir
/// (katkı = miktar olayı; birim fiyat = 1 veya fon değeri gözlemi).
/// </summary>
/// <param name="Name">Teşhis/kırılım için görünen ad.</param>
/// <param name="PricingCurrency">Olay ve fiyatların para birimi.</param>
/// <param name="Events">Alış/satış olayları (sırasız verilebilir; servis sıralar).</param>
/// <param name="Prices">Fiyat gözlemleri — PriceSnapshots (PricingCurrency cinsinden).</param>
public sealed record AssetValueHistoryInput(
    string Name,
    CurrencyCode PricingCurrency,
    IReadOnlyList<PositionEvent> Events,
    IReadOnlyList<PricePoint> Prices);

/// <summary>
/// Pozisyonu değiştiren olay (Transaction'ın saf izdüşümü). <see cref="UnitPrice"/>
/// pozitifse aynı zamanda o günün fiyat gözlemi sayılır (alış günü snapshot olmasa
/// bile seri fiyatsız kalmaz).
/// </summary>
public sealed record PositionEvent(
    DateOnly Date,
    TransactionType Type,
    decimal Quantity,
    decimal UnitPrice,
    decimal Fee = 0m);

/// <summary>Tek fiyat gözlemi (PriceSnapshot'ın saf izdüşümü).</summary>
public sealed record PricePoint(DateOnly Date, decimal Price);

/// <summary>Gün bazlı kur tırnağı: 1 From = Rate × To (FxRate'in saf izdüşümü).</summary>
public sealed record FxRatePoint(DateOnly Date, CurrencyCode From, CurrencyCode To, decimal Rate);

/// <summary>
/// Serinin bir günü (baz para biriminde, tam hassasiyet — yuvarlama gösterimde).
/// <see cref="Cost"/> = o gün itibarıyla yatırılan para (miktar × ort. maliyet) —
/// grafikte "değer" ile "yatırılan" çizgilerinin karşılaştırması için.
/// </summary>
public sealed record DailyValuePoint(DateOnly Date, decimal Value, decimal Cost);
