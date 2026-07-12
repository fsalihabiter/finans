using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Finans.Domain.Portfolio;

namespace Finans.Infrastructure.Services;

/// <summary>
/// Holding → T5.1 saf servis girdisi indirgemesi (T5.2'de yazıldı; T5.4 Senaryo da kullanır).
/// Kurallar:
/// <list type="bullet">
/// <item><b>Normal pozisyon:</b> işlemler olay; snapshot'lar + (varsa) güncel fiyat
/// (bugüne çapa) fiyat gözlemi. Hiç işlemi olmayan pozisyon (örn. elle girilen nakit)
/// oluşturulma tarihinde tek açılış olayına indirgenir.</item>
/// <item><b>BES:</b> nominal hesap — kendi katkılar ödeme tarihinde miktar olayı
/// (birim fiyat 1), devlet katkısı <b>yatma tarihinde</b> (katkı ayını izleyen ay sonu,
/// BesCalculator) birim fiyata işler, bugünkü fon değeri son gözlem. Böylece serinin
/// son günü özet/detay ekranıyla birebir tutarlıdır (maliyet = kendi katkı; değer = fon).</item>
/// </list>
/// </summary>
internal static class HoldingHistoryInputs
{
    /// <summary>Tek pozisyonu saf servis girdisine indirger.</summary>
    internal static AssetValueHistoryInput ToInput(
        Holding holding, IReadOnlyList<PricePoint>? snapshots, DateOnly today)
    {
        return holding.Asset.Type == AssetType.Bes
            ? ToBesInput(holding, today)
            : ToStandardInput(holding, snapshots, today);
    }

    private static AssetValueHistoryInput ToStandardInput(
        Holding holding, IReadOnlyList<PricePoint>? snapshots, DateOnly today)
    {
        var events = holding.Transactions
            .OrderBy(t => t.TransactedAtUtc)
            .Select(t => new PositionEvent(
                DateOnly.FromDateTime(t.TransactedAtUtc), t.Type, t.Quantity, t.UnitPrice, t.Fee))
            .ToList();

        // İşlemsiz pozisyon (örn. elle girilen nakit): oluşturulma günü tek açılış olayı —
        // özet bu pozisyonu Quantity×AvgCost ile sayar, seri de aynı tabana oturur.
        if (events.Count == 0 && holding.Quantity != 0m)
        {
            events.Add(new PositionEvent(
                DateOnly.FromDateTime(holding.CreatedAtUtc),
                TransactionType.Buy, holding.Quantity, holding.AvgCost));
        }

        var prices = new List<PricePoint>(snapshots?.Count + 1 ?? 1);
        if (snapshots is not null)
            prices.AddRange(snapshots);

        // Güncel fiyat bugüne çapalanır: elle güncellenen fiyatların snapshot'ı yok; otomatik
        // fiyatlananlarda son snapshot'la aynıdır → serinin son günü özetle tutarlı kalır.
        if (holding.CurrentPrice is { } current)
            prices.Add(new PricePoint(today, current));

        return new AssetValueHistoryInput(holding.Asset.Name, holding.Asset.PricingCurrency, events, prices);
    }

    /// <summary>
    /// BES → nominal hesap indirgemesi: miktar = kümülatif kendi katkı (birim fiyat 1);
    /// devlet katkısı yatma tarihinde birim fiyata işler; bugünkü fon değeri son gözlem.
    /// </summary>
    private static AssetValueHistoryInput ToBesInput(Holding holding, DateOnly today)
    {
        // Yatırılmış (bugüne dek ödenmiş) kendi katkılar — miktar olayları.
        var contributions = holding.BesContributions
            .Where(c => DateOnly.FromDateTime(c.PaidAtUtc) <= today)
            .OrderBy(c => c.PaidAtUtc)
            .ToList();

        var events = contributions
            .Where(c => c.OwnAmount > 0m)
            .Select(c => new PositionEvent(
                DateOnly.FromDateTime(c.PaidAtUtc), TransactionType.Buy, c.OwnAmount, UnitPrice: 1m))
            .ToList();

        // Birim fiyat zaman çizgisi: (kümülatif kendi + yatmış devlet) / kümülatif kendi.
        // Değişim noktaları: kendi katkı ödeme günleri + devlet katkısı yatma günleri.
        var changeDates = new SortedSet<DateOnly>();
        foreach (var c in contributions)
        {
            changeDates.Add(DateOnly.FromDateTime(c.PaidAtUtc));
            if (c.StateAmount > 0m)
            {
                var deposit = DateOnly.FromDateTime(BesCalculator.StateDepositDateFor(c.PaidAtUtc));
                if (deposit <= today)
                    changeDates.Add(deposit);
            }
        }

        var prices = new List<PricePoint>(changeDates.Count + 1);
        foreach (var date in changeDates)
        {
            decimal cumOwn = 0m;
            decimal cumState = 0m;
            foreach (var c in contributions)
            {
                if (DateOnly.FromDateTime(c.PaidAtUtc) <= date)
                    cumOwn += c.OwnAmount;
                if (c.StateAmount > 0m &&
                    DateOnly.FromDateTime(BesCalculator.StateDepositDateFor(c.PaidAtUtc)) <= date)
                    cumState += c.StateAmount;
            }

            if (cumOwn > 0m)
                prices.Add(new PricePoint(date, (cumOwn + cumState) / cumOwn));
        }

        // Bugünkü gerçek fon değeri (fon getirisi dahil) — yalnız bugün bilinir, geçmişe yayılmaz
        // (geçmişi gösteriyoruz, uydurmuyoruz — CLAUDE.md §2).
        var totalOwn = contributions.Sum(c => c.OwnAmount);
        if (holding.CurrentPrice is { } fundValue && totalOwn > 0m)
            prices.Add(new PricePoint(today, fundValue / totalOwn));

        return new AssetValueHistoryInput(
            holding.Asset.Name, holding.Asset.PricingCurrency, events, prices);
    }

    /// <summary>Eşit adımlı seyrekleştirme — ilk ve son nokta daima korunur (değişim uçlardan).</summary>
    internal static IReadOnlyList<T> Downsample<T>(IReadOnlyList<T> points, int max)
    {
        if (points.Count <= max)
            return points;

        var result = new List<T>(max);
        var step = (double)(points.Count - 1) / (max - 1);
        for (var i = 0; i < max; i++)
            result.Add(points[(int)Math.Round(i * step)]);
        result[^1] = points[^1];
        return result;
    }
}
