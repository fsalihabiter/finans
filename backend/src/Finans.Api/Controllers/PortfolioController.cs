using Finans.Application.Llm;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Finans.Api.Controllers;

/// <summary>
/// Portföy özeti (04 §4). Geçerli kullanıcının pozisyonlarını backend'de hesaplar;
/// yatırım tavsiyesi değil — yalnızca durum/dağılım (CLAUDE.md §2).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class PortfolioController(
    IPortfolioService portfolio,
    IPortfolioHistoryService history,
    IScenarioService scenario,
    INudgeService nudges,
    ILlmCommentaryService commentary,
    IHoldingService holdings) : ControllerBase
{
    /// <summary>GET /api/portfolio/summary — toplam değer/maliyet/getiri/dağılım.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<PortfolioSummaryDto>> GetSummary(
        [FromQuery] CurrencyCode? baseCurrency, CancellationToken ct) =>
        Ok(await portfolio.GetSummaryAsync(baseCurrency, ct));

    /// <summary>
    /// GET /api/portfolio/history?period=1m|3m|1y|all (T5.2) — günlük değer + yatırılan
    /// maliyet serisi (Değer Seyri). Geçmişi gösterir; tahmin içermez (CLAUDE.md §2).
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<PortfolioHistoryDto>> GetHistory(
        [FromQuery] string? period, [FromQuery] CurrencyCode? baseCurrency, CancellationToken ct) =>
        Ok(await history.GetHistoryAsync(period, baseCurrency, ct));

    /// <summary>
    /// GET /api/portfolio/scenario/{holdingId} (T5.4) — geçmişe dönük "almasaydım /
    /// nakitte dursaydı" karşılaştırması: değer vs yatırılan vs alım gücü eşiği.
    /// **Tahmin ve tavsiye yok** (CLAUDE.md §2); başkasının pozisyonu → 404 (IDOR yok).
    /// </summary>
    [HttpGet("scenario/{holdingId:guid}")]
    public async Task<ActionResult<ScenarioComparisonDto>> GetScenario(
        Guid holdingId, [FromQuery] CurrencyCode? baseCurrency, CancellationToken ct) =>
        Ok(await scenario.CompareAsync(holdingId, baseCurrency, ct));

    /// <summary>
    /// GET /api/portfolio/nudges — kural tabanlı eğitici notlar (FR-2.4). Durumu açıklar,
    /// çerçeve sunar; **yatırım tavsiyesi değildir** (CLAUDE.md §2, UI disclaimer ile gösterilir).
    /// </summary>
    [HttpGet("nudges")]
    // Orta sıkılıkta rate limit: dakikada 30/kullanıcı (web tarafında 5dk'da bir tazelenir).
    [EnableRateLimiting("nudges")]
    public async Task<ActionResult<NudgesResponse>> GetNudges(
        [FromQuery] CurrencyCode? baseCurrency, CancellationToken ct) =>
        Ok(new NudgesResponse(await nudges.GetNudgesAsync(baseCurrency, ct)));

    /// <summary>
    /// GET /api/portfolio/commentary (T3.7) — KODUN hesapladığı portföy özetini LLM ile eğitici
    /// dille yorumla; PII gönderilmez (07 §2 KVKK — anonim özet). LLM erişilemezse / şema bozulursa
    /// 200 + <see cref="CommentaryResponse"/> ile fallback kart döner — UI çökmez (NFR-5).
    /// **Yatırım tavsiyesi DEĞİL** (CLAUDE.md §2). Rate limit: pahalı dış çağrı, dakikada 10/kullanıcı.
    /// </summary>
    [HttpGet("commentary")]
    [EnableRateLimiting("commentary")]
    public async Task<ActionResult<CommentaryResponse>> GetCommentary(
        [FromQuery] CurrencyCode? baseCurrency, CancellationToken ct)
    {
        var summary = await portfolio.GetSummaryAsync(baseCurrency, ct);
        // T3.10: pozisyon listesi anonim yükü derinleştirir (tür-bazlı getiri + BES payı);
        // ad/id gibi PII yine anonimleştiricide elenir (07 §2 KVKK).
        var positions = await holdings.GetAllAsync(baseCurrency, ct);
        var resp = await commentary.GetCommentaryAsync(summary, positions, ct);
        return Ok(resp);
    }
}
