using Finans.Application.Common;
using Finans.Application.Stocks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Finans.Application.Tests.Stocks;

/// <summary>
/// T4.5 (SC-30) — Fiyat geçmişi servisi: dönem dilimleme, değişim oranı (uçlardan),
/// seyrekleştirmede uçların korunması, cache (ikinci istek kaynağa gitmez), geçersiz
/// dönem → 400, sembol yok → 404. Hesaplar deterministik (NFR-1).
/// </summary>
public class StockHistoryServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 12, 12, 0, 0, TimeSpan.Zero);

    private sealed class FakeProvider(Func<string, IReadOnlyList<StockPricePoint>?> responder) : IStockHistoryProvider
    {
        public int Calls { get; private set; }
        public string Source => "test";
        public Task<IReadOnlyList<StockPricePoint>?> GetDailyHistoryAsync(string symbol, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(responder(symbol));
        }
    }

    private sealed class FakeAppCache : IAppCache
    {
        private readonly Dictionary<string, object> _store = new();
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
            => Task.FromResult(_store.TryGetValue(key, out var v) ? (T?)v : null);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
        { _store[key] = value; return Task.CompletedTask; }
        public async Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<CancellationToken, Task<T>> f, CancellationToken ct = default) where T : class
        { if (_store.TryGetValue(key, out var v)) return (T)v; var c = await f(ct); _store[key] = c!; return c; }
        public Task<T> SingleFlightAsync<T>(string key, Func<CancellationToken, Task<T>> f, CancellationToken ct = default) => f(ct);
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    /// <summary>2020-01-01'den bugüne, her gün +1₺ artan yapay seri (deterministik).</summary>
    private static List<StockPricePoint> LongSeries()
    {
        var start = new DateOnly(2020, 1, 1);
        var end = DateOnly.FromDateTime(Now.UtcDateTime);
        var points = new List<StockPricePoint>();
        var price = 100m;
        for (var d = start; d <= end; d = d.AddDays(1))
            points.Add(new StockPricePoint(d, price++));
        return points;
    }

    private static StockHistoryService Build(FakeProvider provider) =>
        new(provider, new FakeAppCache(), new FixedTime(Now), NullLogger<StockHistoryService>.Instance);

    [Fact]
    public async Task Slices_one_month_window_and_computes_change_from_endpoints()
    {
        var svc = Build(new FakeProvider(_ => LongSeries()));

        var h = await svc.GetHistoryAsync("AAPL", "1m");

        Assert.Equal("1m", h.Range);
        Assert.True(h.Points.Count is >= 28 and <= 32);                    // ~1 aylık pencere
        Assert.Equal(DateOnly.FromDateTime(Now.UtcDateTime), h.Points[^1].Date);
        var expected = (h.Points[^1].Close - h.Points[0].Close) / h.Points[0].Close;
        Assert.Equal(expected, h.ChangeRatio);
        Assert.Equal(new DateOnly(2020, 1, 1), h.FirstTradeDate);          // halka arz bağlamı korunur
    }

    [Fact]
    public async Task Max_range_returns_downsampled_full_series_with_endpoints_kept()
    {
        var full = LongSeries(); // ~2380 gün > 500
        var svc = Build(new FakeProvider(_ => full));

        var h = await svc.GetHistoryAsync("AAPL", "max");

        Assert.Equal(StockHistoryService.MaxPoints, h.Points.Count);       // seyrekleştirildi
        Assert.Equal(full[0].Date, h.Points[0].Date);                      // ilk nokta korunur
        Assert.Equal(full[^1].Date, h.Points[^1].Date);                    // son nokta korunur
        var expected = (full[^1].Close - full[0].Close) / full[0].Close;
        Assert.Equal(expected, h.ChangeRatio);
    }

    [Fact]
    public async Task Defaults_to_one_year_when_range_missing()
    {
        var svc = Build(new FakeProvider(_ => LongSeries()));

        var h = await svc.GetHistoryAsync("AAPL", null);

        Assert.Equal("1y", h.Range);
        Assert.True(h.Points.Count is >= 360 and <= 370);
    }

    [Fact]
    public async Task Invalid_range_throws_validation()
    {
        var svc = Build(new FakeProvider(_ => LongSeries()));

        await Assert.ThrowsAsync<ValidationException>(() => svc.GetHistoryAsync("AAPL", "2h"));
    }

    [Fact]
    public async Task Unknown_symbol_maps_to_not_found()
    {
        var svc = Build(new FakeProvider(_ => null));

        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetHistoryAsync("YOKBU", "1y"));
    }

    [Fact]
    public async Task Second_request_any_range_uses_cached_full_series()
    {
        var provider = new FakeProvider(_ => LongSeries());
        var svc = Build(provider);

        await svc.GetHistoryAsync("AAPL", "1m");
        await svc.GetHistoryAsync("AAPL", "5y"); // farklı dönem — yine tek kaynak çağrısı

        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task New_listing_shorter_than_window_returns_whole_series()
    {
        // 10 günlük yeni halka arz + 1y penceresi → boş grafik yerine eldeki seri.
        var recent = Enumerable.Range(0, 10)
            .Select(i => new StockPricePoint(DateOnly.FromDateTime(Now.UtcDateTime).AddDays(i - 9), 10m + i))
            .ToList();
        var svc = Build(new FakeProvider(_ => recent));

        var h = await svc.GetHistoryAsync("YENI", "5y");

        Assert.Equal(10, h.Points.Count);
    }
}
