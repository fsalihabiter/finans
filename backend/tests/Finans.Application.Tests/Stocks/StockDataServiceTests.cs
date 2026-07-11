using Finans.Application.Common;
using Finans.Application.Stocks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Finans.Application.Tests.Stocks;

/// <summary>
/// T4.2 — Servis akışı (SC-28): sembol doğrulama/normalizasyon, cache (ikinci istek dış
/// kaynağa gitmez — kota koruması), sembol yok → 404, kaynak hatası → 502'ye eşlenecek
/// <see cref="UpstreamException"/>. Sağlayıcı sahte; ağ yok.
/// </summary>
public class StockDataServiceTests
{
    private static StockMetricsDto Dto(string symbol) => new(
        symbol, "Apple Inc.", "NASDAQ", "USD", 201.40m, 0.012m,
        new StockMetricValues(28.4m, 44.1m, 0.0052m, 0.091m),
        new StockSectorContext("above", "high", "low", "positive"),
        new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc), "test");

    private sealed class FakeProvider(Func<string, StockMetricsDto?> responder) : IStockDataProvider
    {
        public int Calls { get; private set; }
        public string? LastSymbol { get; private set; }
        public string Source => "test";
        public Task<StockMetricsDto?> GetMetricsAsync(string symbol, CancellationToken ct = default)
        {
            Calls++;
            LastSymbol = symbol;
            return Task.FromResult(responder(symbol));
        }
    }

    private sealed class ThrowingProvider(Exception ex) : IStockDataProvider
    {
        public string Source => "test";
        public Task<StockMetricsDto?> GetMetricsAsync(string symbol, CancellationToken ct = default)
            => throw ex;
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

    private static StockDataService Build(IStockDataProvider provider) =>
        new(provider, new FakeAppCache(), NullLogger<StockDataService>.Instance);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("AAPL!!")]
    [InlineData("ÇOKUZUNBIRSEMBOLADI")]
    public async Task Invalid_symbol_throws_validation(string symbol)
    {
        var svc = Build(new FakeProvider(Dto));

        await Assert.ThrowsAsync<ValidationException>(() => svc.GetMetricsAsync(symbol));
    }

    [Fact]
    public async Task Normalizes_symbol_before_calling_provider()
    {
        var provider = new FakeProvider(Dto);
        var svc = Build(provider);

        var dto = await svc.GetMetricsAsync("  brk.b ");

        Assert.Equal("BRK.B", provider.LastSymbol);
        Assert.Equal("BRK.B", dto.Symbol);
    }

    [Fact]
    public async Task Second_request_is_served_from_cache_without_provider_call()
    {
        var provider = new FakeProvider(Dto);
        var svc = Build(provider);

        await svc.GetMetricsAsync("AAPL");
        var r2 = await svc.GetMetricsAsync("AAPL");

        Assert.Equal(1, provider.Calls); // kota koruması (60 çağrı/dk — NFR-9)
        Assert.Equal("Apple Inc.", r2.Name);
    }

    [Fact]
    public async Task Unknown_symbol_maps_to_not_found()
    {
        var svc = Build(new FakeProvider(_ => null));

        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetMetricsAsync("YOKBU"));
    }

    [Fact]
    public async Task Transport_error_maps_to_upstream()
    {
        var svc = Build(new ThrowingProvider(new HttpRequestException("boom")));

        await Assert.ThrowsAsync<UpstreamException>(() => svc.GetMetricsAsync("AAPL"));
    }

    [Fact]
    public async Task Provider_upstream_exception_bubbles_with_its_own_message()
    {
        // NotConfigured sağlayıcısının anlamlı mesajı jenerik mesajla ezilmemeli.
        var svc = Build(new ThrowingProvider(new UpstreamException("Hisse veri kaynağı yapılandırılmamış.")));

        var ex = await Assert.ThrowsAsync<UpstreamException>(() => svc.GetMetricsAsync("AAPL"));
        Assert.Contains("yapılandırılmamış", ex.Message);
    }

    [Fact]
    public async Task Failures_are_not_cached()
    {
        // İlk çağrı kaynak hatası; kaynak düzelince ikinci çağrı başarılı olmalı (hata cache'lenmez).
        var calls = 0;
        var provider = new FakeProvider(s =>
        {
            calls++;
            return calls == 1 ? throw new InvalidOperationException("geçici") : Dto(s);
        });
        var svc = Build(provider);

        await Assert.ThrowsAsync<UpstreamException>(() => svc.GetMetricsAsync("AAPL"));
        var ok = await svc.GetMetricsAsync("AAPL");

        Assert.Equal("Apple Inc.", ok.Name);
    }
}
