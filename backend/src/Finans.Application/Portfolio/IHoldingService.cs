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

    /// <summary>Açılış bakiyesiyle yeni BES pozisyonu kurar (Holding+BesDetails+Opening katkı, T-BES.8).</summary>
    Task<HoldingDto> CreateBesAsync(CreateBesRequest request, CancellationToken ct = default);

    Task<HoldingDto> AddTransactionAsync(Guid id, TransactionRequest request, CancellationToken ct = default);

    /// <summary>Tek işlemi düzenler — miktar/ort. maliyet işlemlerden yeniden türetilir (BES'te yok).</summary>
    Task<HoldingDto> UpdateTransactionAsync(Guid id, Guid transactionId, TransactionRequest request, CancellationToken ct = default);

    /// <summary>Tek işlemi siler — miktar/ort. maliyet yeniden türetilir. Son işlemi silmek için pozisyonu silin.</summary>
    Task<HoldingDto> DeleteTransactionAsync(Guid id, Guid transactionId, CancellationToken ct = default);

    Task<HoldingDto> UpdateAsync(Guid id, UpdateHoldingRequest request, CancellationToken ct = default);

    /// <summary>BES pozisyonuna aylık katkı ekler (kendi + devlet). BES değilse 400.</summary>
    Task<HoldingDto> AddBesContributionAsync(Guid id, AddBesContributionRequest request, CancellationToken ct = default);

    /// <summary>BES sözleşme alanlarını günceller (örn. başlangıç tarihi → hak ediş yeniden türer). BES değilse 400.</summary>
    Task<HoldingDto> UpdateBesAsync(Guid id, UpdateBesRequest request, CancellationToken ct = default);

    /// <summary>Düzenli BES katkısını tarih aralığından üretir (kapsanan aylar için kayıt; idempotent). BES değilse 400.</summary>
    Task<HoldingDto> GenerateBesContributionsAsync(Guid id, GenerateBesContributionsRequest request, CancellationToken ct = default);

    /// <summary>Tek BES katkı kaydını düzenler (tutar/tarih → devlet katkısı + maliyet yeniden hesaplanır).</summary>
    Task<HoldingDto> UpdateBesContributionAsync(Guid id, Guid contributionId, UpdateBesContributionRequest request, CancellationToken ct = default);

    /// <summary>Tek BES katkı kaydını siler (kümülatif + maliyet düşülür).</summary>
    Task<HoldingDto> DeleteBesContributionAsync(Guid id, Guid contributionId, CancellationToken ct = default);

    /// <summary>
    /// BES eğitici projeksiyon (T-BES.5): kullanıcının verdiği varsayımlardan birikim illüstrasyonu.
    /// Pozisyonu değiştirmez (saf okuma). BES değilse 400; başkasınınsa 404 (IDOR).
    /// </summary>
    Task<BesProjectionResult> ProjectBesAsync(Guid id, BesProjectionRequest request, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>Portföy özeti use-case'i (04 §4). Geçerli kullanıcıya kapsanır.</summary>
public interface IPortfolioService
{
    Task<PortfolioSummaryDto> GetSummaryAsync(CurrencyCode? baseCurrency = null, CancellationToken ct = default);
}
