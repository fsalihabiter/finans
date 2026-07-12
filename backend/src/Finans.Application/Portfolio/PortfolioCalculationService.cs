using Finans.Domain.Enums;

namespace Finans.Application.Portfolio;

/// <summary>
/// CLAUDE.md § 6 formüllerinin tek deterministik uygulaması. **Saf fonksiyon**:
/// girdi holdings (+ enflasyon); çıktı özet. Yan etki yok, I/O yok → %100 testli
/// (NFR-1). Para hesabı her yerde <see cref="decimal"/>; yuvarlama yalnızca
/// gösterimde (ön yüz), burada tam hassasiyet korunur.
///
/// Faz 1 varsayımı: tüm kalemler baz para birimi cinsinden fiyatlanır
/// (PricingCurrency == baz). Para birimi dönüşümü T1.3'te (CurrencyConversionService)
/// girdiler servise verilmeden önce uygulanır.
/// </summary>
public sealed class PortfolioCalculationService
{
    /// <summary>Tek kalemin toplam maliyeti: miktar × ort. birim maliyet.</summary>
    public static decimal TotalCost(decimal quantity, decimal avgCost) =>
        quantity * avgCost;

    /// <summary>Güncel değer: miktar × güncel fiyat. Fiyat yoksa null.</summary>
    public static decimal? CurrentValue(decimal quantity, decimal? currentPrice) =>
        currentPrice is { } price ? quantity * price : null;

    /// <summary>Net kâr = güncel değer − toplam maliyet (değer yoksa null).</summary>
    public static decimal? Profit(decimal? currentValue, decimal totalCost) =>
        currentValue is { } value ? value - totalCost : null;

    /// <summary>
    /// Kalem getirisi (oran) = (güncel değer − toplam maliyet) / toplam maliyet.
    /// Maliyet sıfırsa veya değer yoksa null (ör. fiyatsız/sıfır-maliyetli kalem).
    /// </summary>
    public static decimal? ReturnRatio(decimal? currentValue, decimal totalCost) =>
        currentValue is { } value && totalCost != 0m
            ? (value - totalCost) / totalCost
            : null;

    /// <summary>
    /// Ağırlıklı ortalama maliyet = Σ(miktarᵢ × fiyatᵢ) / Σ(miktarᵢ).
    /// Toplam miktar sıfırsa null. (Pozisyon Transactions'tan türetilirken kullanılır, T1.5.)
    /// </summary>
    public static decimal? WeightedAverageCost(IEnumerable<(decimal Quantity, decimal UnitPrice)> lots)
    {
        decimal totalQuantity = 0m;
        decimal totalSpend = 0m;
        foreach (var (quantity, unitPrice) in lots)
        {
            totalQuantity += quantity;
            totalSpend += quantity * unitPrice;
        }

        return totalQuantity != 0m ? totalSpend / totalQuantity : null;
    }

    /// <summary>
    /// İşlemlerden pozisyonu türetir (03 §11 — ortalama maliyet yöntemi):
    /// <c>AvgCost = Σ(Buy.Qty×UnitPrice + Buy.Fee) / Σ Buy.Qty</c>,
    /// <c>Quantity = Σ Buy.Qty − Σ Sell.Qty</c>. **Satış ortalamayı bozmaz**,
    /// yalnızca miktarı düşürür (FIFO/LIFO Faz 5). Hiç alış yoksa AvgCost = 0.
    /// </summary>
    public static PositionBasis DerivePosition(IEnumerable<TransactionInput> transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        decimal buyQuantity = 0m;
        decimal buyCost = 0m;   // Σ(Buy.Qty×UnitPrice + Fee) — ortalama maliyetin payı
        decimal sellQuantity = 0m;

        foreach (var tx in transactions)
        {
            if (tx.Type == TransactionType.Buy)
            {
                buyQuantity += tx.Quantity;
                buyCost += tx.Quantity * tx.UnitPrice + tx.Fee;
            }
            else // Sell — yalnızca miktarı etkiler
            {
                sellQuantity += tx.Quantity;
            }
        }

        decimal avgCost = buyQuantity != 0m ? buyCost / buyQuantity : 0m;
        return new PositionBasis(buyQuantity - sellQuantity, avgCost);
    }

    /// <summary>
    /// Kronolojik aşırı satış denetimi (SC-41): işlemler tarihe göre sıralanır (aynı gün
    /// alışlar önce — gün granülü, değer serisinin gün sonu durumuyla uyumlu) ve her adımda
    /// kümülatif miktar ≥ 0 olmalı. İhlal eden ilk işlemin tarihini döner; ihlal yoksa null.
    /// Nihai miktar denetimi tek başına yetmez: alış tarihinden ÖNCEKİ tarihe girilen satış
    /// nihai toplamı bozmadan ara günlerde pozisyonu negatife düşürür → Değer Seyri negatif
    /// değer/maliyet çizer. Yazma yolu bu denetimle böyle diziyi reddeder.
    /// </summary>
    public static DateOnly? FirstOversoldDate(
        IEnumerable<(DateOnly Date, TransactionType Type, decimal Quantity)> transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        decimal quantity = 0m;
        foreach (var tx in transactions
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Type == TransactionType.Buy ? 0 : 1))
        {
            quantity += tx.Type == TransactionType.Buy ? tx.Quantity : -tx.Quantity;
            if (quantity < 0m)
                return tx.Date;
        }

        return null;
    }

    /// <summary>
    /// Reel getiri = (1 + nominal getiri) / (1 + enflasyon) − 1.
    /// Enflasyon verisi yoksa null (04 §4: realReturnRatio nullable).
    /// </summary>
    public static decimal? RealReturn(decimal? nominalReturn, decimal? inflationRate) =>
        nominalReturn is { } nominal && inflationRate is { } inflation && (1m + inflation) != 0m
            ? (1m + nominal) / (1m + inflation) - 1m
            : null;

    /// <summary>
    /// Portföyün tamamını tek geçişte hesaplar: kalem metrikleri, dağılım ve özet.
    /// <paramref name="inflationRate"/> verilirse reel getiri de hesaplanır.
    /// </summary>
    public PortfolioSummary CalculateSummary(
        IReadOnlyList<HoldingInput> holdings,
        decimal? inflationRate = null)
    {
        ArgumentNullException.ThrowIfNull(holdings);

        var results = CalculateHoldings(holdings);

        decimal totalCost = 0m;
        decimal totalValue = 0m;
        foreach (var result in results)
        {
            totalCost += result.TotalCost;
            // Fiyatsız kalem toplam değere MALİYETİYLE girer (SC-40): 0 saymak sahte
            // −%100 zarar üretir; Değer Seyri'nin "hiç fiyat gözlemi yoksa değer = maliyet"
            // kuralıyla aynı ilke (özet = liste = seri, 03 §11.1).
            totalValue += result.CurrentValue ?? result.TotalCost;
        }

        decimal netProfit = totalValue - totalCost;
        decimal? returnRatio = totalCost != 0m ? netProfit / totalCost : null;
        decimal? realReturnRatio = RealReturn(returnRatio, inflationRate);

        // Dağılım kalemin ETKİN değerini kullanır (fiyatsızsa maliyeti, SC-40) —
        // dilim değerleri toplamı TotalValue ile, ağırlıklar CalculateHoldings ile tutarlı.
        var allocation = results
            .Select(r => (Result: r, Value: r.CurrentValue ?? r.TotalCost))
            .Where(x => x.Value > 0m)
            .Select(x => new AllocationSlice(x.Result.AssetType, x.Result.Name, x.Value, x.Result.Weight))
            .ToList();

        return new PortfolioSummary(totalValue, totalCost, netProfit, returnRatio, realReturnRatio, allocation);
    }

    /// <summary>
    /// Her kalemin türetilmiş metriklerini (maliyet, değer, kâr, getiri, ağırlık)
    /// hesaplar. Ağırlık, kalemin ETKİN değerinin (fiyatlıysa güncel değer, fiyatsızsa
    /// maliyet — SC-40) toplam etkin değere oranıdır; toplam CalculateSummary ile tutarlı.
    /// </summary>
    public IReadOnlyList<HoldingResult> CalculateHoldings(IReadOnlyList<HoldingInput> holdings)
    {
        ArgumentNullException.ThrowIfNull(holdings);

        var costs = new decimal[holdings.Count];
        var values = new decimal?[holdings.Count];
        decimal totalValue = 0m;

        for (int i = 0; i < holdings.Count; i++)
        {
            var h = holdings[i];
            costs[i] = TotalCost(h.Quantity, h.AvgCost);
            values[i] = CurrentValue(h.Quantity, h.CurrentPrice);
            totalValue += values[i] ?? costs[i];
        }

        var results = new List<HoldingResult>(holdings.Count);
        for (int i = 0; i < holdings.Count; i++)
        {
            var h = holdings[i];
            decimal? value = values[i];
            decimal weight = totalValue != 0m ? (value ?? costs[i]) / totalValue : 0m;

            results.Add(new HoldingResult(
                h.AssetType,
                h.Name,
                costs[i],
                value,
                Profit(value, costs[i]),
                ReturnRatio(value, costs[i]),
                weight));
        }

        return results;
    }
}
