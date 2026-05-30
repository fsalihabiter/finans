namespace Finans.Infrastructure.Pricing;

/// <summary>
/// Fiyat sağlayıcı uç noktaları (appsettings <c>"Pricing"</c>). Faz 2 kaynaklarının
/// <b>ikisi de anahtarsız</b> → repoda sır yok (CLAUDE.md §13). Yalnızca kök adresler;
/// yollar sağlayıcıda sabit.
/// </summary>
public sealed class PricingOptions
{
    public const string SectionName = "Pricing";

    /// <summary>Frankfurter (ECB döviz) kök adresi.</summary>
    public string FrankfurterBaseUrl { get; set; } = "https://api.frankfurter.dev/";

    /// <summary>Truncgil (TR gram altın) kök adresi.</summary>
    public string TruncgilBaseUrl { get; set; } = "https://finans.truncgil.com/";
}
