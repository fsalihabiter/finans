using Finans.Domain.Enums;

namespace Finans.Application.Portfolio;

/// <summary>
/// <see cref="PortfolioCalculationService"/> girdisi: tek bir kalemin (holding)
/// hesaba giren ham değerleri. Saf veri — EF entity'sine bağımlı değil ki servis
/// yan etkisiz ve %100 testlenebilir kalsın (02 §2.2).
/// </summary>
/// <param name="AssetType">Varlık türü (dağılım gruplaması için).</param>
/// <param name="Name">Kullanıcıya görünen ad.</param>
/// <param name="Quantity">Pozisyon miktarı (gram/adet/birim).</param>
/// <param name="AvgCost">PricingCurrency cinsinden ağırlıklı ort. birim maliyet.</param>
/// <param name="CurrentPrice">Güncel birim fiyat; yoksa (örn. fiyatsız nakit) null.</param>
public sealed record HoldingInput(
    AssetType AssetType,
    string Name,
    decimal Quantity,
    decimal AvgCost,
    decimal? CurrentPrice);

/// <summary>
/// Tek bir kalemin türetilmiş metrikleri (04 §4 — GET /holdings kalemi).
/// Hesaplanamayan alanlar null döner (CurrentPrice yoksa veya maliyet sıfırsa).
/// </summary>
public sealed record HoldingResult(
    AssetType AssetType,
    string Name,
    decimal TotalCost,
    decimal? CurrentValue,
    decimal? Profit,
    decimal? ReturnRatio,
    decimal Weight);

/// <summary>
/// Portföy özeti (04 §4 — GET /portfolio/summary). Tüm sayılar burada,
/// deterministik hesaplanır; gösterim/format ön yüzde (CLAUDE.md §3.1).
/// </summary>
public sealed record PortfolioSummary(
    decimal TotalValue,
    decimal TotalCost,
    decimal NetProfit,
    decimal? ReturnRatio,
    decimal? RealReturnRatio,
    IReadOnlyList<AllocationSlice> Allocation);

/// <summary>Dağılım dilimi (donut + legend). Weight = dilim değeri / toplam değer.</summary>
public sealed record AllocationSlice(
    AssetType AssetType,
    string Name,
    decimal Value,
    decimal Weight);
