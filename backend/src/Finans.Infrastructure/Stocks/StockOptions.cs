namespace Finans.Infrastructure.Stocks;

/// <summary>
/// Hisse veri kaynağı yapılandırması (T4.2 — Finnhub kararı T4.1). <see cref="ApiKey"/>
/// env/User Secrets/.env'den gelir, repoda YER ALMAZ (11 §6). Ücretsiz katman: 60 çağrı/dk.
/// </summary>
public sealed class StockOptions
{
    public const string SectionName = "Stocks";

    /// <summary>Finnhub API anahtarı. Boşsa endpoint anlamlı 502 döner (yapılandırılmamış).</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://finnhub.io/api/v1/";

    /// <summary>Tek dış çağrı bütçesi (10 §2).</summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>Fiyat geçmişi kaynağı (T4.5 — Yahoo chart API, anahtarsız; halka arzdan bugüne günlük seri).</summary>
    public string HistoryBaseUrl { get; set; } = "https://query1.finance.yahoo.com/";

    /// <summary>Geçmiş CSV'si uzun olabilir (30+ yıl) → metrikten daha geniş bütçe.</summary>
    public int HistoryTimeoutSeconds { get; set; } = 20;
}
