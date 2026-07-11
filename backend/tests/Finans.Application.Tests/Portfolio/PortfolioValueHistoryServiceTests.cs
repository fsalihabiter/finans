using Finans.Application.Portfolio;
using Finans.Domain.Enums;

namespace Finans.Application.Tests.Portfolio;

/// <summary>
/// PortfolioValueHistoryService birim testleri (SC-32, NFR-1): işlem olayları +
/// fiyat gözlemlerinden günlük değer serisi — fiyat taşıma, pozisyon değişimi,
/// ort. maliyet, kur taşıma/geri-doldurma, uç durumlar.
/// </summary>
public class PortfolioValueHistoryServiceTests
{
    private static readonly DateOnly D0 = new(2026, 7, 1);

    private static DateOnly Day(int offset) => D0.AddDays(offset);

    private static PositionEvent Buy(int day, decimal qty, decimal price, decimal fee = 0m) =>
        new(Day(day), TransactionType.Buy, qty, price, fee);

    private static PositionEvent Sell(int day, decimal qty, decimal price = 0m) =>
        new(Day(day), TransactionType.Sell, qty, price);

    private static AssetValueHistoryInput Asset(
        CurrencyCode currency,
        IReadOnlyList<PositionEvent> events,
        params PricePoint[] prices) =>
        new("Test Varlık", currency, events, prices);

    private static IReadOnlyList<DailyValuePoint> Calc(
        IReadOnlyList<AssetValueHistoryInput> assets,
        int endDay,
        IReadOnlyList<FxRatePoint>? fx = null,
        CurrencyCode baseCurrency = CurrencyCode.TRY) =>
        PortfolioValueHistoryService.Calculate(assets, fx ?? [], baseCurrency, Day(endDay));

    [Fact]
    public void Missing_days_carry_last_known_price_forward()
    {
        // 10 adet @100 (g0); snapshot g0=100, g2=110 → g1 100 taşınır, g3-g4 110 taşınır.
        var series = Calc(
            [Asset(CurrencyCode.TRY, [Buy(0, 10m, 100m)],
                new PricePoint(Day(0), 100m), new PricePoint(Day(2), 110m))],
            endDay: 4);

        Assert.Equal(5, series.Count);
        Assert.Equal([Day(0), Day(1), Day(2), Day(3), Day(4)], series.Select(p => p.Date));
        Assert.Equal([1000m, 1000m, 1100m, 1100m, 1100m], series.Select(p => p.Value));
        Assert.All(series, p => Assert.Equal(1000m, p.Cost)); // maliyet fiyatla değişmez
    }

    [Fact]
    public void Buy_day_without_snapshot_uses_transaction_unit_price()
    {
        // Hiç snapshot yok — alış birim fiyatı gözlem sayılır, seri fiyatsız kalmaz.
        var series = Calc([Asset(CurrencyCode.TRY, [Buy(0, 40m, 4546.275m)])], endDay: 1);

        Assert.Equal(2, series.Count);
        Assert.All(series, p => Assert.Equal(181851m, p.Value));   // 40 × 4.546,275
        Assert.All(series, p => Assert.Equal(181851m, p.Cost));
    }

    [Fact]
    public void Same_day_snapshot_wins_over_transaction_price()
    {
        // Aynı gün hem alış (100) hem snapshot (104) → snapshot (gerçek piyasa) kazanır.
        var series = Calc(
            [Asset(CurrencyCode.TRY, [Buy(0, 10m, 100m)], new PricePoint(Day(0), 104m))],
            endDay: 0);

        Assert.Equal(1040m, series[0].Value);
        Assert.Equal(1000m, series[0].Cost);
    }

    [Fact]
    public void Transaction_days_change_position_and_cost()
    {
        // g0: 10 @100 → g2: +10 @120 (fee 40). Ort. maliyet g2'de (1000+1240)/20=112.
        var series = Calc(
            [Asset(CurrencyCode.TRY, [Buy(0, 10m, 100m), Buy(2, 10m, 120m, fee: 40m)])],
            endDay: 3);

        Assert.Equal([1000m, 1000m, 2400m, 2400m], series.Select(p => p.Value)); // g2+: 20×120
        Assert.Equal([1000m, 1000m, 2240m, 2240m], series.Select(p => p.Cost));  // g2+: 20×112
    }

    [Fact]
    public void Sell_reduces_quantity_but_not_avg_cost()
    {
        // g0: 40 @10 → g2: 10 sat (snapshot g2=12). Kalan 30; ort. maliyet 10 kalır.
        var series = Calc(
            [Asset(CurrencyCode.TRY, [Buy(0, 40m, 10m), Sell(2, 10m)],
                new PricePoint(Day(2), 12m))],
            endDay: 2);

        Assert.Equal(360m, series[2].Value); // 30 × 12
        Assert.Equal(300m, series[2].Cost);  // 30 × 10 — satış ortalamayı bozmaz
    }

    [Fact]
    public void Foreign_currency_converts_with_daily_rate_carry_forward()
    {
        // 1 adet USD varlık @100 USD; kur g0: 40, g2: 42 → TRY değer g0-g1 4.000, g2+ 4.200.
        var series = Calc(
            [Asset(CurrencyCode.USD, [Buy(0, 1m, 100m)])],
            endDay: 3,
            fx:
            [
                new FxRatePoint(Day(0), CurrencyCode.USD, CurrencyCode.TRY, 40m),
                new FxRatePoint(Day(2), CurrencyCode.USD, CurrencyCode.TRY, 42m),
            ]);

        Assert.Equal([4000m, 4000m, 4200m, 4200m], series.Select(p => p.Value));
        Assert.Equal([4000m, 4000m, 4200m, 4200m], series.Select(p => p.Cost)); // maliyet de gün kurundan
    }

    [Fact]
    public void Days_before_first_rate_are_backfilled_with_earliest()
    {
        // Portföy kur geçmişinden yaşlı: ilk kur g2'de → g0-g1 en eski kurla (42) doldurulur.
        var series = Calc(
            [Asset(CurrencyCode.USD, [Buy(0, 1m, 100m)])],
            endDay: 2,
            fx: [new FxRatePoint(Day(2), CurrencyCode.USD, CurrencyCode.TRY, 42m)]);

        Assert.All(series, p => Assert.Equal(4200m, p.Value));
    }

    [Fact]
    public void Missing_required_rate_throws_instead_of_silently_wrong_number()
    {
        // USD varlık var ama USD→TRY kuru hiç yok → sessiz yanlış sayı yerine fırlatır.
        Assert.Throws<InvalidOperationException>(() =>
            Calc([Asset(CurrencyCode.USD, [Buy(0, 1m, 100m)])], endDay: 0));
    }

    [Fact]
    public void Multi_asset_values_are_summed_in_base_currency()
    {
        // TRY varlık (1.000) + USD varlık (100 USD × 40 = 4.000) → toplam 5.000.
        var series = Calc(
            [
                Asset(CurrencyCode.TRY, [Buy(0, 10m, 100m)]),
                Asset(CurrencyCode.USD, [Buy(1, 1m, 100m)]), // g1'de alınır — g0'da katkısı 0
            ],
            endDay: 1,
            fx: [new FxRatePoint(Day(0), CurrencyCode.USD, CurrencyCode.TRY, 40m)]);

        Assert.Equal(1000m, series[0].Value);
        Assert.Equal(5000m, series[1].Value);
    }

    [Fact]
    public void Events_and_prices_after_end_date_are_ignored()
    {
        var series = Calc(
            [Asset(CurrencyCode.TRY, [Buy(0, 10m, 100m), Buy(5, 10m, 200m)],
                new PricePoint(Day(4), 999m))],
            endDay: 2);

        Assert.Equal(3, series.Count);
        Assert.All(series, p => Assert.Equal(1000m, p.Value)); // g5 alışı ve g4 fiyatı yok sayıldı
    }

    [Fact]
    public void Empty_inputs_yield_empty_series()
    {
        Assert.Empty(Calc([], endDay: 5));
        Assert.Empty(Calc([Asset(CurrencyCode.TRY, [])], endDay: 5)); // varlık var, işlem yok
    }

    [Fact]
    public void Series_starts_at_first_transaction_across_assets()
    {
        // İlk işlem g1'de (ikinci varlıkta) → seri g1'de başlar, g0 yok.
        var series = Calc(
            [
                Asset(CurrencyCode.TRY, [Buy(3, 1m, 50m)]),
                Asset(CurrencyCode.TRY, [Buy(1, 1m, 100m)]),
            ],
            endDay: 3);

        Assert.Equal(3, series.Count);
        Assert.Equal(Day(1), series[0].Date);
        Assert.Equal([100m, 100m, 150m], series.Select(p => p.Value));
    }

    [Fact]
    public void Full_precision_is_preserved_no_rounding()
    {
        // 3 adet @ 33,333333 → 99,999999 (yuvarlama YOK — gösterim ön yüzün işi).
        var series = Calc([Asset(CurrencyCode.TRY, [Buy(0, 3m, 33.333333m)])], endDay: 0);

        Assert.Equal(99.999999m, series[0].Value);
    }
}
