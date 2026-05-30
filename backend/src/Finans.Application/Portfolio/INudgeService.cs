using Finans.Domain.Enums;

namespace Finans.Application.Portfolio;

/// <summary>
/// Geçerli kullanıcının portföyünden kural tabanlı eğitici notları üretir (T2.5).
/// Özeti per-user hesaplar (IDOR yok, 11 §3) ve <see cref="NudgeRuleEngine"/>'den geçirir.
/// </summary>
public interface INudgeService
{
    Task<IReadOnlyList<Nudge>> GetNudgesAsync(CurrencyCode? baseCurrency = null, CancellationToken ct = default);
}
