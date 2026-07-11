using Finans.Domain.Enums;

namespace Finans.Application.Portfolio;

/// <summary>
/// Günlük portföy değer serisi türetimi (T5.1 — Değer Seyri + Senaryo v1 temeli).
/// **Saf fonksiyon**: girdi işlem olayları + fiyat gözlemleri + kur serisi; çıktı
/// ilk işlem gününden <c>endDate</c>'e her gün için (değer, yatırılan maliyet) —
/// baz para biriminde, tam hassasiyet <see cref="decimal"/> (NFR-1).
///
/// Kurallar (SC-32):
/// <list type="bullet">
/// <item>Eksik gün = son bilinen fiyat taşınır; işlem birim fiyatı da gözlemdir
/// (aynı gün snapshot varsa snapshot kazanır).</item>
/// <item>İşlem günü pozisyon değişir; ort. maliyet <see cref="PortfolioCalculationService.DerivePosition"/>
/// ile aynı yöntem (satış ortalamayı bozmaz, fee maliyete dahil) — serinin son günü
/// özet ekranıyla tutarlı kalır.</item>
/// <item>Hiç fiyat gözlemi yoksa değer = maliyet (varlık maliyetiyle taşınır).</item>
/// <item>Kur gün bazlı taşınır; serinin ilk kurundan önceki günler en eski kayıtla
/// geri-doldurulur. Gerekli kur hiç yoksa fırlatır (sessizce yanlış sayı dönmesin).</item>
/// <item><c>endDate</c> sonrası işlem/fiyat/kur yok sayılır; boş girdi → boş seri.</item>
/// </list>
/// </summary>
public sealed class PortfolioValueHistoryService
{
    public static IReadOnlyList<DailyValuePoint> Calculate(
        IReadOnlyList<AssetValueHistoryInput> assets,
        IReadOnlyList<FxRatePoint> fxRates,
        CurrencyCode baseCurrency,
        DateOnly endDate)
    {
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(fxRates);

        // Seri ilk işlem gününde başlar (öncesi boş portföy).
        DateOnly? firstEventDate = null;
        foreach (var asset in assets)
        {
            foreach (var e in asset.Events)
            {
                if (e.Date <= endDate && (firstEventDate is null || e.Date < firstEventDate))
                    firstEventDate = e.Date;
            }
        }

        if (firstEventDate is not { } start)
            return [];

        var states = assets.Select(a => new AssetState(a, endDate)).ToList();
        var fx = new FxTimeline(fxRates, endDate);

        int dayCount = endDate.DayNumber - start.DayNumber + 1;
        var points = new List<DailyValuePoint>(dayCount);

        for (var day = start; day <= endDate; day = day.AddDays(1))
        {
            var converter = fx.ConverterFor(day);
            decimal totalValue = 0m;
            decimal totalCost = 0m;

            foreach (var state in states)
            {
                state.AdvanceTo(day);
                totalValue += ConvertIfAny(converter, state.Value, state.Currency, baseCurrency);
                totalCost += ConvertIfAny(converter, state.Cost, state.Currency, baseCurrency);
            }

            points.Add(new DailyValuePoint(day, totalValue, totalCost));
        }

        return points;
    }

    /// <summary>Sıfır tutar kur gerektirmez (varlık henüz alınmadıysa kur yokluğu seriyi düşürmesin).</summary>
    private static decimal ConvertIfAny(
        CurrencyConverter converter, decimal amount, CurrencyCode from, CurrencyCode to) =>
        amount == 0m ? 0m : converter.Convert(amount, from, to);

    /// <summary>
    /// Tek varlığın gün-gün ilerleyen durumu: olay/fiyat işaretçileri bir daha geri
    /// dönmez (O(gün + olay + fiyat)). Ortalama maliyet DerivePosition ile birebir
    /// aynı birikimle hesaplanır.
    /// </summary>
    private sealed class AssetState
    {
        private readonly List<PositionEvent> _events;      // tarihe göre sıralı (stabil)
        private readonly List<PricePoint> _prices;         // tarihe göre sıralı; aynı gün: snapshot sonda
        private int _eventIndex;
        private int _priceIndex;

        private decimal _buyQuantity;
        private decimal _buyCost;   // Σ(Buy.Qty×UnitPrice + Fee)
        private decimal _sellQuantity;
        private decimal? _lastPrice;

        public CurrencyCode Currency { get; }

        public AssetState(AssetValueHistoryInput asset, DateOnly endDate)
        {
            Currency = asset.PricingCurrency;

            _events = asset.Events
                .Where(e => e.Date <= endDate)
                .OrderBy(e => e.Date)
                .ToList();

            // Fiyat gözlemleri: işlem birim fiyatları (öncelik 0) + snapshot'lar (öncelik 1).
            // Aynı gün ikisi de varsa sıralamada snapshot SONA gelir → son uygulanan kazanır.
            _prices = asset.Events
                .Where(e => e.Date <= endDate && e.UnitPrice > 0m)
                .Select(e => (e.Date, Price: e.UnitPrice, Priority: 0))
                .Concat(asset.Prices
                    .Where(p => p.Date <= endDate)
                    .Select(p => (p.Date, p.Price, Priority: 1)))
                .OrderBy(p => p.Date)
                .ThenBy(p => p.Priority)
                .Select(p => new PricePoint(p.Date, p.Price))
                .ToList();
        }

        public decimal Quantity => _buyQuantity - _sellQuantity;

        /// <summary>Yatırılan maliyet = miktar × ort. maliyet (satış ortalamayı bozmaz).</summary>
        public decimal Cost =>
            _buyQuantity != 0m ? Quantity * (_buyCost / _buyQuantity) : 0m;

        /// <summary>Güncel değer; hiç fiyat gözlemi yoksa maliyetle taşınır.</summary>
        public decimal Value =>
            _lastPrice is { } price ? Quantity * price : Cost;

        public void AdvanceTo(DateOnly day)
        {
            while (_eventIndex < _events.Count && _events[_eventIndex].Date <= day)
            {
                var e = _events[_eventIndex++];
                if (e.Type == TransactionType.Buy)
                {
                    _buyQuantity += e.Quantity;
                    _buyCost += e.Quantity * e.UnitPrice + e.Fee;
                }
                else // Sell — yalnızca miktarı düşürür (ortalama maliyet yöntemi)
                {
                    _sellQuantity += e.Quantity;
                }
            }

            while (_priceIndex < _prices.Count && _prices[_priceIndex].Date <= day)
                _lastPrice = _prices[_priceIndex++].Price;
        }
    }

    /// <summary>
    /// Gün bazlı kur zaman çizgisi: her (From,To) çifti için en güncel ≤ gün tırnağı.
    /// Serinin ilk kurundan önceki günler en eski kayıtla geri-doldurulur (portföy
    /// kur geçmişinden yaşlıysa seri düşmesin — bilinen en yakın kur kullanılır).
    /// Converter yalnızca kur değişen günlerde yeniden kurulur.
    /// </summary>
    private sealed class FxTimeline
    {
        private readonly List<FxRatePoint> _points;   // tarihe göre sıralı
        private readonly Dictionary<(CurrencyCode From, CurrencyCode To), decimal> _current = new();
        private int _index;
        private CurrencyConverter? _converter;

        public FxTimeline(IReadOnlyList<FxRatePoint> fxRates, DateOnly endDate)
        {
            _points = fxRates
                .Where(p => p.Date <= endDate)
                .OrderBy(p => p.Date)
                .ToList();

            // Geri-doldurma: her çiftin EN ESKİ tırnağı başlangıç değeri olur.
            foreach (var p in _points)
                _current.TryAdd((p.From, p.To), p.Rate);
        }

        public CurrencyConverter ConverterFor(DateOnly day)
        {
            bool changed = _converter is null;
            while (_index < _points.Count && _points[_index].Date <= day)
            {
                var p = _points[_index++];
                var key = (p.From, p.To);
                if (!changed && (!_current.TryGetValue(key, out var existing) || existing != p.Rate))
                    changed = true;
                _current[key] = p.Rate;
            }

            if (changed)
            {
                _converter = new CurrencyConverter(
                    _current.Select(kv => new FxQuote(kv.Key.From, kv.Key.To, kv.Value)));
            }

            return _converter!;
        }
    }
}
