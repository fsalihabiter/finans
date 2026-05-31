using System.Diagnostics.Metrics;

namespace Finans.Infrastructure.Caching;

/// <summary>
/// Cache hit/miss sayacı (12 §4, T2.7). `System.Diagnostics.Metrics` (OTel-uyumlu) → T2.8'de
/// Prometheus exporter bu <see cref="MeterName"/>'i toplar. Etiketler düşük-kardinalite:
/// <c>result</c> (hit/miss) + <c>cache</c> (anahtar öneki, örn. fx/inflation/prices).
/// </summary>
public sealed class CacheMetrics : IDisposable
{
    public const string MeterName = "Finans.Cache";

    private readonly Meter _meter;
    private readonly Counter<long> _requests;

    public CacheMetrics()
    {
        _meter = new Meter(MeterName);
        _requests = _meter.CreateCounter<long>(
            "finans.cache.requests", unit: "{request}", description: "Cache okuma sayısı (hit/miss).");
    }

    public void Record(string key, bool hit) =>
        _requests.Add(
            1,
            new KeyValuePair<string, object?>("result", hit ? "hit" : "miss"),
            new KeyValuePair<string, object?>("cache", Prefix(key)));

    private static string Prefix(string key)
    {
        var i = key.IndexOf(':');
        return i < 0 ? key : key[..i];
    }

    public void Dispose() => _meter.Dispose();
}
