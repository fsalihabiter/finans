using System.Text.RegularExpressions;
using Finans.Application.Common;

namespace Finans.Application.Stocks;

/// <summary>Sembol normalizasyon/doğrulama — metrik ve geçmiş servisleri ortak kullanır (T4.5).</summary>
public static partial class StockSymbols
{
    [GeneratedRegex("^[A-Z0-9.\\-]{1,12}$")]
    private static partial Regex Pattern();

    /// <summary>Kırp + BÜYÜT; yalnız A-Z, 0-9, nokta, tire (≤12). Geçersizse <see cref="ValidationException"/>.</summary>
    public static string Normalize(string symbol)
    {
        var s = (symbol ?? string.Empty).Trim().ToUpperInvariant();
        if (!Pattern().IsMatch(s))
            throw new ValidationException("symbol", "invalid",
                "Geçersiz sembol. Yalnız harf/rakam/nokta/tire, en çok 12 karakter (örn. AAPL, BRK.B).");
        return s;
    }
}
