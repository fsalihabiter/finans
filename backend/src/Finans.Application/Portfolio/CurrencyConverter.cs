using Finans.Domain.Enums;

namespace Finans.Application.Portfolio;

/// <summary>Yön+kur tırnağı: 1 <see cref="From"/> = <see cref="Rate"/> × <see cref="To"/>.</summary>
public sealed record FxQuote(CurrencyCode From, CurrencyCode To, decimal Rate);

/// <summary>
/// Para birimi dönüşümünün deterministik, saf çekirdeği (CLAUDE.md §3.2, NFR-1):
/// <c>tutar × kur(varlık_pb → baz_pb)</c>. Değişmez bir kur anlık görüntüsü alır;
/// I/O yapmaz → %100 testlenebilir. Kur yükleme/cache <see cref="IFxRateProvider"/>
/// üzerinden Infrastructure'da (kurlar repository'den/cache'ten gelir, 02 §2.2).
///
/// <para>Hassasiyet: ters yön <b>çarpma değil bölme</b> ile uygulanır (1 USD = 48 TRY
/// ise TRY→USD = tutar / 48, hatasız 2.000). Yani 1/Rate önceden saklanıp çarpılmaz —
/// finans uygulamasında 1999,99… kabul edilemez (NFR-1). Çapraz kur de adım adım
/// çarp/böl uygulanır.</para>
///
/// Dönüşüm sırası: aynı pb → birim (1) · doğrudan kur (×) · ters kur (÷) ·
/// çapraz kur (bir pivot pb üzerinden, örn. EUR→USD = EUR→TRY sonra TRY→USD).
/// </summary>
public sealed class CurrencyConverter
{
    // Yalnızca verilen yönlü tırnaklar (ters yön türetilmez; bölmeyle anında uygulanır).
    private readonly Dictionary<(CurrencyCode From, CurrencyCode To), decimal> _quotes = new();

    public CurrencyConverter(IEnumerable<FxQuote> quotes)
    {
        ArgumentNullException.ThrowIfNull(quotes);
        foreach (var q in quotes)
        {
            if (q.From == q.To || q.Rate <= 0m)
                continue; // anlamsız/güvensiz tırnağı sessizce atla

            _quotes[(q.From, q.To)] = q.Rate; // aynı çift tekrar gelirse son değer kazanır
        }
    }

    /// <summary>
    /// <paramref name="amount"/> tutarını <paramref name="from"/>'dan
    /// <paramref name="to"/>'ya çevirir. Kur bulunamazsa
    /// <see cref="InvalidOperationException"/> fırlatır (sessizce yanlış sayı dönmesin).
    /// </summary>
    public decimal Convert(decimal amount, CurrencyCode from, CurrencyCode to) =>
        TryConvert(amount, from, to, out var result)
            ? result
            : throw new InvalidOperationException($"Kur bulunamadı: {from} → {to}.");

    /// <summary>Kur biliniyorsa çevirir; bilinmiyorsa <c>false</c> döner (çökme yok).</summary>
    public bool TryConvert(decimal amount, CurrencyCode from, CurrencyCode to, out decimal result)
    {
        if (from == to)
        {
            result = amount;
            return true;
        }

        // Tek adım (doğrudan ×Rate veya ters ÷Rate) — bölme tam hassasiyeti korur.
        if (TryHop(amount, from, to, out result))
            return true;

        // Çapraz kur: pivot pb üzerinden iki adım, her biri ayrı çarp/böl.
        foreach (var pivot in Enum.GetValues<CurrencyCode>())
        {
            if (pivot == from || pivot == to)
                continue;

            if (TryHop(amount, from, pivot, out var atPivot) &&
                TryHop(atPivot, pivot, to, out result))
                return true;
        }

        result = 0m;
        return false;
    }

    /// <summary><c>1 from = ? to</c> efektif kuru. Bulunamazsa fırlatır.</summary>
    public decimal RateFor(CurrencyCode from, CurrencyCode to) =>
        Convert(1m, from, to);

    /// <summary>Tek adımlık dönüşüm: doğrudan tırnak ×Rate, ters tırnak ÷Rate.</summary>
    private bool TryHop(decimal amount, CurrencyCode from, CurrencyCode to, out decimal result)
    {
        if (_quotes.TryGetValue((from, to), out var direct))
        {
            result = amount * direct;
            return true;
        }

        if (_quotes.TryGetValue((to, from), out var inverse))
        {
            result = amount / inverse; // ters yön: bölme (1/Rate çarpmaktan daha hassas)
            return true;
        }

        result = 0m;
        return false;
    }
}

/// <summary>
/// Geçerli kur anlık görüntüsünü (her pb çifti için en güncel tırnak) sağlar.
/// Implementasyon Infrastructure'da (EF + cache). Saf hesap katmanı yalnızca
/// <see cref="CurrencyConverter"/>'ı kullanır (02 §2.2).
/// </summary>
public interface IFxRateProvider
{
    Task<CurrencyConverter> GetConverterAsync(CancellationToken ct = default);
}
