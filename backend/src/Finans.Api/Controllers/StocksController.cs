using Finans.Application.Llm;
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
public sealed class StocksController(
    IStockDataService stocks,
    ILlmStockExplainService explain,
    IStockHistoryService history) : ControllerBase
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

    /// <summary>
    /// GET /api/stocks/{symbol}/explain (T4.3 — 07 §8) — metriklerin NE ANLAMA geldiğini
    /// eğitici dille açıklar (commentary kart şeması). Tavsiye/tahmin YOK (CLAUDE.md §2);
    /// LLM erişilemezse fallback kartı (200, çökme yok — NFR-5). Rate limit: LLM pahalı →
    /// "commentary" politikası (10/dk); sembol başına 24 saat cache.
    /// </summary>
    [HttpGet("{symbol}/explain")]
    [EnableRateLimiting("commentary")]
    public async Task<ActionResult<CommentaryResponse>> GetExplain(string symbol, CancellationToken ct)
    {
        var metrics = await stocks.GetMetricsAsync(symbol, ct);
        return Ok(await explain.ExplainAsync(metrics, ct));
    }

    /// <summary>
    /// GET /api/stocks/{symbol}/history?range=1w|1m|3m|1y|5y|max (T4.5) — halka arzdan
    /// bugüne günlük kapanış serisi, dönem dilimli + dönem değişim oranı. Geçmiş gösterimi;
    /// gelecek tahmini DEĞİL (CLAUDE.md §2). Kaynak anahtarsız (Stooq), seri 24s cache'li.
    /// </summary>
    [HttpGet("{symbol}/history")]
    [EnableRateLimiting("stocks")]
    public async Task<ActionResult<StockHistory>> GetHistory(
        string symbol, [FromQuery] string? range, CancellationToken ct) =>
        Ok(await history.GetHistoryAsync(symbol, range, ct));
}
