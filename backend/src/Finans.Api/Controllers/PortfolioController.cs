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
public sealed class PortfolioController(IPortfolioService portfolio, INudgeService nudges) : ControllerBase
{
    /// <summary>GET /api/portfolio/summary — toplam değer/maliyet/getiri/dağılım.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<PortfolioSummaryDto>> GetSummary(
        [FromQuery] CurrencyCode? baseCurrency, CancellationToken ct) =>
        Ok(await portfolio.GetSummaryAsync(baseCurrency, ct));

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
}
