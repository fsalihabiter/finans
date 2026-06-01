using Finans.Application.Pricing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Finans.Api.Controllers;

/// <summary>
/// Canlı fiyatlar (04 §5, T2.4). <c>GET /api/prices</c> tazeleme turunu tetikler
/// (cache'li → dış API en çok TTL'de bir) ve güncel altın/döviz fiyatlarını döner.
/// Bir kaynak çökerse son-bilinen fiyat <c>stale:true</c> ile döner (NFR-5). Bu uç
/// nokta <c>Holding.CurrentPrice</c>'ı da tazeler → ardından çağrılan summary/holdings
/// canlı fiyatı yansıtır. Fiyatlar kullanıcı-bağımsız (global). Yatırım tavsiyesi değil.
/// </summary>
[ApiController]
[Route("api/[controller]")]
// Sıkı rate limit: bu endpoint dış API çağırır (Frankfurter/Truncgil). Cache 10dk olsa da
// upstream koruması için kullanıcı başına dakikada 10 — yeterli (T2.9, 10 §5).
[EnableRateLimiting("prices")]
public sealed class PricesController(IPriceFetchService priceFetch) : ControllerBase
{
    /// <summary>GET /api/prices — güncel altın/döviz fiyatları (+ stale/asOf/source).</summary>
    [HttpGet]
    public async Task<ActionResult<PricesResponse>> Get(CancellationToken ct)
    {
        var result = await priceFetch.RefreshAsync(ct);

        var prices = result.Quotes
            .Select(q => new PriceDto(
                q.Instrument.Kind, q.Instrument.Currency, q.Price,
                q.QuoteCurrency, q.AsOfUtc, q.Source, q.IsStale))
            .ToList();

        return Ok(new PricesResponse(
            result.RefreshedAtUtc, result.FromCache, result.HasStale, result.FailedSources, prices));
    }
}
