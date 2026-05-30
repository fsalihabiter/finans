using Finans.Application.Portfolio;
using Finans.Domain.Enums;

namespace Finans.Infrastructure.Services;

/// <summary>
/// <see cref="INudgeService"/> uygulaması: portföy özetini (zaten per-user kapsamlı,
/// T1.7) alıp saf <see cref="NudgeRuleEngine"/>'den geçirir. Yeni sayı üretmez; notlar
/// hazır oranlardan deterministik türetilir (CLAUDE.md §3.1).
/// </summary>
public sealed class NudgeService(IPortfolioService portfolio, NudgeRuleEngine engine) : INudgeService
{
    public async Task<IReadOnlyList<Nudge>> GetNudgesAsync(
        CurrencyCode? baseCurrency = null, CancellationToken ct = default)
    {
        var summary = await portfolio.GetSummaryAsync(baseCurrency, ct);
        return engine.Evaluate(summary);
    }
}
