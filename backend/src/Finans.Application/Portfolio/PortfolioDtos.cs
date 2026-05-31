using System.ComponentModel.DataAnnotations;
using Finans.Domain.Enums;

namespace Finans.Application.Portfolio;

// ── Yanıt DTO'ları (04 §4) ───────────────────────────────────────────────────

/// <summary>
/// Tek bir pozisyonun API gösterimi (04 §4 — GET /holdings kalemi). Birim alanlar
/// (avgCost/currentPrice) varlığın kendi para biriminde; toplulaştırmalar
/// (totalCost/currentValue/profit/weight) baz para biriminde (weight tutarlılığı).
/// Hesaplanamayan alanlar null.
/// </summary>
public sealed record HoldingDto(
    Guid Id,
    AssetType AssetType,
    string Name,
    string? Symbol,
    CurrencyCode Currency,
    string Unit,
    decimal Quantity,
    decimal AvgCost,
    decimal? CurrentPrice,
    decimal TotalCost,
    decimal? CurrentValue,
    decimal? Profit,
    decimal? ReturnRatio,
    decimal Weight,
    BesDto? Bes,
    IReadOnlyList<TransactionDto>? Transactions = null);

/// <summary>
/// BES kalemi — devlet katkısı kendi katkısından AYRI (CLAUDE.md §1, 03 §A).
/// BES nominal hesaptır: maliyet = kendi + devlet katkısı; "alış/satış" modeline
/// uymaz, aylık katkı ile büyür (JoinedAtUtc = sözleşme başlangıcı).
/// </summary>
public sealed record BesDto(
    decimal OwnContribution,
    decimal StateContribution,
    VestingState VestingState,
    DateTime? JoinedAtUtc);

/// <summary>Bir pozisyonun geçmiş işlemi (detayda gösterilir, 04 §4).</summary>
public sealed record TransactionDto(
    Guid Id,
    TransactionType Type,
    decimal Quantity,
    decimal UnitPrice,
    decimal Fee,
    DateTime TransactedAtUtc);

/// <summary>Portföy özeti (04 §4 — GET /portfolio/summary). Tüm sayılar backend hesabı.</summary>
public sealed record PortfolioSummaryDto(
    CurrencyCode BaseCurrency,
    decimal TotalValue,
    decimal TotalCost,
    decimal NetProfit,
    decimal? ReturnRatio,
    decimal? RealReturnRatio,
    IReadOnlyList<AllocationDto> Allocation,
    DateTime AsOf);

/// <summary>Dağılım dilimi (donut + legend).</summary>
public sealed record AllocationDto(
    AssetType AssetType,
    string Name,
    decimal Value,
    decimal Weight);

// ── İstek DTO'ları (DataAnnotations: temel doğrulama; iş kuralları serviste) ──

/// <summary>
/// POST /api/holdings — ilk işlemiyle birlikte yeni pozisyon (04 §4).
/// Not: Record primary-constructor parametrelerinde doğrulama attribute'ları
/// **doğrudan parametreye** konur (`[property:]` hedefi MVC validasyonunu çökertir).
/// </summary>
public sealed record CreateHoldingRequest(
    AssetType AssetType,
    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(120, MinimumLength = 1, ErrorMessage = "Ad 1-120 karakter olmalı.")]
    string Name,
    [StringLength(20, ErrorMessage = "Sembol en fazla 20 karakter.")]
    string? Symbol,
    CurrencyCode Currency,
    [Required(ErrorMessage = "Birim zorunludur.")]
    [StringLength(20, MinimumLength = 1, ErrorMessage = "Birim 1-20 karakter olmalı.")]
    string Unit,
    TransactionRequest Transaction);

/// <summary>Bir pozisyona alış/satış ekler (04 §4). Miktar/fiyat işareti serviste denetlenir.</summary>
public sealed record TransactionRequest(
    TransactionType Type,
    decimal Quantity,
    decimal UnitPrice,
    decimal Fee = 0m,
    DateTime? Date = null);

/// <summary>PUT /api/holdings/{id} — Faz 1'de güncel fiyatı elle güncelle (FR-1.8).</summary>
public sealed record UpdateHoldingRequest(
    decimal? CurrentPrice);

/// <summary>
/// POST /api/holdings/{id}/bes-contribution — BES'e aylık katkı ekler. Kendi katkı
/// (<paramref name="OwnAmount"/>) + devlet katkısı (verilmezse %30 hesaplanır, TR kuralı).
/// Maliyet tabanı (kendi+devlet) ve dolayısıyla getiri buna göre güncellenir.
/// </summary>
public sealed record AddBesContributionRequest(
    decimal OwnAmount,
    decimal? StateAmount = null);

/// <summary>
/// PUT /api/holdings/{id}/bes — BES sözleşme alanlarını günceller (T-BES). Şimdilik
/// başlangıç tarihi (<paramref name="JoinedAtUtc"/>); değişince hak ediş yeniden türetilir.
/// </summary>
public sealed record UpdateBesRequest(
    DateTime? JoinedAtUtc);
