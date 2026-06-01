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
/// BES kalemi — devlet katkısı kendi katkısından AYRI (CLAUDE.md §1, 03 §A). Tüm toplamlar
/// katkı satırlarından <b>tarihe göre türetilir</b> (T-BES.8): yalnız <b>yatırılmış</b> katkılar
/// (<see cref="OwnContribution"/>/<see cref="StateContribution"/>) maliyet/getiri tabanına girer;
/// henüz yatmamışlar (<see cref="OwnPending"/>/<see cref="StatePending"/>) ayrı gösterilir, toplama
/// dahil edilmez. Devlet katkısı, kendi katkı ayını izleyen ayın sonunda yatmış sayılır.
/// </summary>
public sealed record BesDto(
    /// <summary>Yatırılmış kendi katkı toplamı (maliyet tabanı).</summary>
    decimal OwnContribution,
    /// <summary>Yatırılmış devlet katkısı toplamı.</summary>
    decimal StateContribution,
    /// <summary>Henüz yatmamış (ödeme tarihi gelecekte) kendi katkı toplamı.</summary>
    decimal OwnPending,
    /// <summary>Henüz yatmamış devlet katkısı toplamı (kendi katkı ödendi ama devlet yatma tarihi gelmedi + gelecek).</summary>
    decimal StatePending,
    VestingState VestingState,
    /// <summary>Kademeli hak ediş oranı (0/0.15/0.35/0.60/1.00).</summary>
    decimal VestedRate,
    /// <summary>Hak kazanılan tutar ≈ VestedRate × yatırılmış devlet katkısı (yaklaşık; disclaimer'lı).</summary>
    decimal VestedAmount,
    DateTime? JoinedAtUtc,
    int? BirthYear,
    string? ProviderName,
    IReadOnlyList<BesContributionDto> Contributions,
    bool ContributionDue,
    bool PlanActive,
    decimal? MonthlyAmount,
    int? ContributionDay,
    // ── Fon getirisi (T-BES.10): fon, hem kendi hem devlet birikimi üzerinden büyür.
    // Aynı oran r = fund/(own+state) − 1 her iki katkıya işler; her birinin ayrı kâr/zararı vardır.
    // Fon değeri (Holding.CurrentPrice) ya da (own+state) tabanı yoksa null/0 (gösterimde "—").
    /// <summary>Fon getiri oranı (yalnız yatırılmış kısımda): (fundValue / (own+state)) - 1.</summary>
    decimal? FundReturnRatio = null,
    /// <summary>Kendi katkının güncel değeri ≈ own × (1+r); oran yoksa own.</summary>
    decimal OwnValue = 0m,
    /// <summary>Kendi katkının fon getiri kâr/zararı ≈ own × r; oran yoksa 0.</summary>
    decimal OwnProfit = 0m,
    /// <summary>Devlet katkısının güncel değeri ≈ state × (1+r); oran yoksa state.</summary>
    decimal StateValue = 0m,
    /// <summary>Devlet katkısının fon getiri kâr/zararı ≈ state × r; oran yoksa 0.</summary>
    decimal StateProfit = 0m);

/// <summary>
/// Tek bir BES katkı ödemesi kaydı (T-BES.6). Source: "Opening" | "Manual" | "Plan".
/// <see cref="Status"/> ve <see cref="StateDepositDate"/> tarihten türetilir (saklanmaz).
/// </summary>
public sealed record BesContributionDto(
    Guid Id,
    decimal OwnAmount,
    decimal StateAmount,
    DateTime PaidAtUtc,
    string Source,
    BesContributionStatus Status,
    DateTime StateDepositDate);

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
/// (<paramref name="OwnAmount"/>) + devlet katkısı (verilmezse <paramref name="PaidAtUtc"/>
/// tarihindeki orana göre hesaplanır — 2026 öncesi %30, sonrası %20; geriye dönük değil).
/// Maliyet tabanı (kendi+devlet) ve dolayısıyla getiri buna göre güncellenir.
/// </summary>
public sealed record AddBesContributionRequest(
    decimal OwnAmount,
    decimal? StateAmount = null,
    DateTime? PaidAtUtc = null,
    /// <summary>"Bundan sonraki katkılar için kullan": işaretlenirse düzenli plan kurulur (bu tutar/gün; bitiş yok).</summary>
    bool Recurring = false);

/// <summary>
/// POST /api/holdings/bes — yeni BES pozisyonu kurar (T-BES.8). Mevcut hesabı yansıtmak için
/// <b>açılış bakiyesi</b> alır: güncel fon değeri + bugüne dek birikmiş kendi/devlet katkı toplamı
/// (tek "Opening" katkı kaydı olarak yazılır — geçmiş tek tek girilmez). Düzenli plan opsiyoneldir.
/// </summary>
public sealed record CreateBesRequest(
    string Name,
    string? ProviderName,
    CurrencyCode Currency,
    DateTime JoinedAtUtc,
    int? BirthYear,
    /// <summary>Güncel toplam fon değeri (birikimin piyasa değeri).</summary>
    decimal CurrentFundValue,
    /// <summary>Bugüne dek ödenmiş toplam kendi katkı (açılış maliyeti).</summary>
    decimal OpeningOwn,
    /// <summary>Bugüne dek yatmış toplam devlet katkısı.</summary>
    decimal OpeningState,
    decimal? MonthlyAmount = null,
    int? ContributionDay = null);

/// <summary>
/// PUT /api/holdings/{id}/bes — BES sözleşme/plan alanlarını günceller (T-BES). Tüm alanlar
/// opsiyonel (patch): verilen alan güncellenir. Başlangıç/doğum yılı değişince hak ediş yeniden
/// türetilir. <paramref name="ContributionDay"/> = "ödeme günü" düzenlemesi.
/// </summary>
public sealed record UpdateBesRequest(
    DateTime? JoinedAtUtc = null,
    string? ProviderName = null,
    int? BirthYear = null,
    decimal? MonthlyAmount = null,
    int? ContributionDay = null,
    bool? PlanActive = null);

/// <summary>
/// POST /api/holdings/{id}/bes/contributions — düzenli katkıyı tarih aralığından üretir (T-BES.6):
/// [<paramref name="FromUtc"/>, <paramref name="ToUtc"/>] aralığında her ay <paramref name="Day"/>
/// gününde <paramref name="MonthlyAmount"/> tutarlı kayıt. Zaten kaydı olan ay atlanır (idempotent);
/// ileri tarihli aralık serbest — istenen aylar (gelecek dahil) üretilir (ileriye dönük plan).
/// Devlet katkısı her ayın tarihindeki orana göre (geriye dönük değil).
/// </summary>
public sealed record GenerateBesContributionsRequest(
    decimal MonthlyAmount,
    int Day,
    DateTime FromUtc,
    DateTime ToUtc);

/// <summary>PUT /api/holdings/{id}/bes/contributions/{cid} — tek katkı kaydını düzenle (tutar/tarih).</summary>
public sealed record UpdateBesContributionRequest(
    decimal OwnAmount,
    DateTime PaidAtUtc);
