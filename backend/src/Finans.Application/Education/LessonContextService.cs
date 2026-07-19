using System.Globalization;
using System.Text.RegularExpressions;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;

namespace Finans.Application.Education;

/// <summary>
/// "Senin portföyünde" bağlam çözümleyicisi (T6.2, 15 §3). Ders metnindeki
/// <c>{{anahtar}}</c> token'larını kullanıcının GERÇEK metrikleriyle değiştirir;
/// portföy yok/yetersizse <b>etiketli demo</b> değerlere düşer (onboarding kararı 1c).
/// </summary>
/// <remarks>
/// <para><b>CLAUDE.md §3.1:</b> tüm sayılar koddan gelir; LLM burada yok.</para>
/// <para><b>Yeni hesap yazılmaz:</b> değerler mevcut <see cref="IPortfolioService"/>
/// özetinden türetilir (tek dış çağrı, ders başına).</para>
/// <para><b>Biçim TR:</b> binlik nokta, ondalık virgül (CLAUDE.md §8).</para>
/// </remarks>
public sealed partial class LessonContextService(IPortfolioService portfolio) : ILessonContextService
{
    /// <summary>Fiyat bu süreden eskiyse bağlam <see cref="LessonContextState.Stale"/> sayılır.</summary>
    private static readonly TimeSpan StaleAfter = TimeSpan.FromHours(24);

    /// <summary>Bağlamın anlamlı olması için gereken asgari kalem sayısı (tek kalemde yoğunlaşma/dağılım anlatılamaz).</summary>
    private const int MinHoldingsForOwnContext = 2;

    /// <summary>
    /// TR sayı biçimi (binlik nokta, ondalık virgül — CLAUDE.md §8), <b>açıkça</b> tanımlı.
    /// </summary>
    /// <remarks>
    /// <c>CultureInfo.GetCultureInfo("tr-TR")</c> KULLANILMAZ: üretim imajı
    /// <i>globalization-invariant</i> modda çalışıyor ve orada kültür arama
    /// <c>CultureNotFoundException</c> fırlatır (canlıda 500 → 2026-07-19'da yakalandı).
    /// Biçimi elle tanımlamak hem ortamdan bağımsız hem de niyeti açık kılar.
    /// </remarks>
    private static readonly NumberFormatInfo Tr = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
        NumberGroupSizes = [3],
        NumberNegativePattern = 1, // "-%5" değil "-5" → yüzde işareti biz ekliyoruz
    };

    [GeneratedRegex(@"\{\{\s*(?<key>[a-z0-9_]+)\s*\}\}", RegexOptions.IgnoreCase)]
    private static partial Regex TokenRegex();

    public async Task<LessonContextResult> ResolveAsync(string templateBody, CancellationToken ct = default)
    {
        var (values, state, asOf) = await BuildValuesAsync(ct);

        // Çözülemeyen token ham hâliyle ASLA gösterilmez — içeren satır tamamen düşer.
        // (Örn. BES'i olmayan kullanıcıda {{bes_state_share}} cümlesi anlamsız kalırdı.)
        var lines = templateBody.Split('\n');
        var kept = new List<string>(lines.Length);

        foreach (var line in lines)
        {
            var unresolved = false;
            var rendered = TokenRegex().Replace(line, m =>
            {
                var key = m.Groups["key"].Value.ToLowerInvariant();
                if (values.TryGetValue(key, out var v))
                    return v;

                unresolved = true;
                return string.Empty;
            });

            if (!unresolved)
                kept.Add(rendered);
        }

        // Blok tamamen boşaldıysa (her satırda çözülemeyen token vardı) boş string döner;
        // çağıran taraf bu bloğu hiç göstermez.
        var body = string.Join('\n', kept).Trim();
        return new LessonContextResult(body, state, asOf);
    }

    private async Task<(Dictionary<string, string> Values, LessonContextState State, DateTime? AsOf)>
        BuildValuesAsync(CancellationToken ct)
    {
        PortfolioSummaryDto? summary = null;
        try
        {
            summary = await portfolio.GetSummaryAsync(ct: ct);
        }
        catch
        {
            // Portföy özeti alınamazsa ders yine de okunabilmeli (NFR-5) → demo'ya düş.
        }

        // NOT: Allocation dilimleri KALEM başınadır (tür başına değil) — dolayısıyla
        // dilim sayısı = kalem sayısı, tür sayısı ise Distinct(AssetType) ile bulunur.
        var holdingCount = summary?.Allocation.Count ?? 0;
        if (summary is null || holdingCount < MinHoldingsForOwnContext || summary.TotalValue <= 0)
            return (DemoValues(), LessonContextState.Demo, null);

        var state = DateTime.UtcNow - summary.AsOf > StaleAfter
            ? LessonContextState.Stale
            : LessonContextState.Own;

        return (OwnValues(summary), state, summary.AsOf);
    }

    private static Dictionary<string, string> OwnValues(PortfolioSummaryDto s)
    {
        // Ağırlıklar hesaplayıcıdan hazır gelir (özet = liste = seri tutarlılığı, SC-34).
        // Varlık ADI kullanılmaz — kullanıcı serbest metni, hassas olabilir (11 §4).
        var topTwo = s.Allocation
            .Select(a => a.Weight)
            .OrderByDescending(w => w)
            .Take(2)
            .Sum();

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [ContextKeys.ConcentrationTop2] = Percent(topTwo),
            [ContextKeys.HoldingCount] = s.Allocation.Count.ToString(Tr),
            [ContextKeys.AssetClassCount] = s.Allocation.Select(a => a.AssetType).Distinct().Count().ToString(Tr),
            [ContextKeys.TotalValue] = Money(s.TotalValue, s.BaseCurrency),
        };

        if (s.ReturnRatio is { } r) values[ContextKeys.ReturnRatio] = Percent(r);
        if (s.RealReturnRatio is { } rr) values[ContextKeys.RealReturn] = Percent(rr);

        AddTypeWeight(values, ContextKeys.CashWeight, s, AssetType.Cash);
        AddTypeWeight(values, ContextKeys.StockWeight, s, AssetType.Stock);

        return values;
    }

    private static void AddTypeWeight(
        Dictionary<string, string> values, string key, PortfolioSummaryDto s, AssetType type)
    {
        // Tür portföyde hiç yoksa token çözülmez → o cümle düşer.
        // (Uydurma "%0" yazmayız; olmayan şey hakkında cümle kurmak yanıltıcı olur.)
        var slices = s.Allocation.Where(a => a.AssetType == type).ToList();
        if (slices.Count > 0)
            values[key] = Percent(slices.Sum(a => a.Weight));
    }

    /// <summary>
    /// Portföyü olmayan kullanıcı için <b>örnek</b> portföy değerleri (15 §3.2 · karar 1c).
    /// Gerçekçi ve eğitici olacak şekilde seçildi; arayüz bunları belirgin bir
    /// "örnek portföy" rozetiyle gösterir ve kendi panosuna asla taşımaz.
    /// </summary>
    private static Dictionary<string, string> DemoValues() => new(StringComparer.OrdinalIgnoreCase)
    {
        [ContextKeys.ConcentrationTop2] = Percent(0.62m),
        [ContextKeys.ReturnRatio] = Percent(0.31m),
        [ContextKeys.RealReturn] = Percent(-0.04m),
        [ContextKeys.CashWeight] = Percent(0.08m),
        [ContextKeys.StockWeight] = Percent(0.24m),
        [ContextKeys.HoldingCount] = "6",
        [ContextKeys.AssetClassCount] = "4",
        [ContextKeys.TotalValue] = Money(250_000m, CurrencyCode.TRY),
        [ContextKeys.BesStateShare] = Percent(0.23m),
    };

    /// <summary>Oran → TR yüzde ("%61,5"); tam sayıya yakınsa ondalık gösterilmez ("%62").</summary>
    private static string Percent(decimal ratio)
    {
        var pct = Math.Round(ratio * 100m, 1, MidpointRounding.AwayFromZero);
        return Math.Abs(pct - Math.Round(pct)) < 0.05m
            ? $"%{Math.Round(pct).ToString("0", Tr)}"
            : $"%{pct.ToString("0.0", Tr)}";
    }

    /// <summary>Tutar → TR para biçimi ("250.000 ₺"); kuruş anlamlıysa gösterilir.</summary>
    private static string Money(decimal amount, CurrencyCode currency)
    {
        var symbol = currency switch
        {
            CurrencyCode.TRY => "₺",
            CurrencyCode.USD => "$",
            CurrencyCode.EUR => "€",
            _ => currency.ToString(),
        };

        var text = amount == Math.Round(amount)
            ? amount.ToString("#,##0", Tr)
            : amount.ToString("#,##0.00", Tr);

        return $"{text} {symbol}";
    }
}
