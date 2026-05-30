using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Finans.Api.Controllers;

/// <summary>
/// Portföy özeti (04 §4). Geçerli kullanıcının pozisyonlarını backend'de hesaplar;
/// yatırım tavsiyesi değil — yalnızca durum/dağılım (CLAUDE.md §2).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class PortfolioController(IPortfolioService portfolio) : ControllerBase
{
    /// <summary>GET /api/portfolio/summary — toplam değer/maliyet/getiri/dağılım.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<PortfolioSummaryDto>> GetSummary(
        [FromQuery] CurrencyCode? baseCurrency, CancellationToken ct) =>
        Ok(await portfolio.GetSummaryAsync(baseCurrency, ct));
}
