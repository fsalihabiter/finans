using Finans.Domain.Enums;

namespace Finans.Application.Portfolio;

/// <summary>
/// Pozisyon (holding) use-case'leri. **Her metot geçerli kullanıcıya kapsanır**
/// (11 §3): başkasının kaydı → <see cref="Common.NotFoundException"/> (IDOR yok).
/// Ortalama maliyet/miktar işlemlerden türetilir (03 §11, T1.5).
/// </summary>
public interface IHoldingService
{
    Task<IReadOnlyList<HoldingDto>> GetAllAsync(CurrencyCode? baseCurrency = null, CancellationToken ct = default);

    Task<HoldingDto> GetByIdAsync(Guid id, CurrencyCode? baseCurrency = null, CancellationToken ct = default);

    Task<HoldingDto> CreateAsync(CreateHoldingRequest request, CancellationToken ct = default);

    Task<HoldingDto> AddTransactionAsync(Guid id, TransactionRequest request, CancellationToken ct = default);

    Task<HoldingDto> UpdateAsync(Guid id, UpdateHoldingRequest request, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>Portföy özeti use-case'i (04 §4). Geçerli kullanıcıya kapsanır.</summary>
public interface IPortfolioService
{
    Task<PortfolioSummaryDto> GetSummaryAsync(CurrencyCode? baseCurrency = null, CancellationToken ct = default);
}
