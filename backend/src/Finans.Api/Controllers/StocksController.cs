using Finans.Application.Stocks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Finans.Api.Controllers;

/// <summary>
/// Hisse temel analiz (T4.2 — 04 §7). Piyasa verisi kullanıcıya özgü değildir (per-user
/// kapsam gerekmez); rate limit dış kota (Finnhub 60 çağrı/dk) + kötüye kullanım koruması.
/// <b>Yatırım tavsiyesi DEĞİL</b>: yalnız güncel metrikler + kaba bant etiketi; yorum
/// katmanı (T4.3) ayrı uçta ve aynı "tavsiye değil" korkuluklarıyla gelir (CLAUDE.md §2).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class StocksController(IStockDataService stocks) : ControllerBase
{
    /// <summary>
    /// GET /api/stocks/{symbol}/metrics — fiyat + F/K + PD/DD + temettü verimi + kâr
    /// büyümesi + kaba bağlam etiketleri. 400 geçersiz sembol · 404 veri yok · 502 kaynak
    /// erişilemez/yapılandırılmamış (hepsi sözleşmeli ApiError zarfı, 04 §2).
    /// </summary>
    [HttpGet("{symbol}/metrics")]
    [EnableRateLimiting("stocks")]
    public async Task<ActionResult<StockMetricsDto>> GetMetrics(string symbol, CancellationToken ct) =>
        Ok(await stocks.GetMetricsAsync(symbol, ct));
}
