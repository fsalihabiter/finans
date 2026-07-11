using Finans.Application.Portfolio;
using Finans.Domain.Enums;

namespace Finans.Application.Llm;

/// <summary>
/// LLM'e gönderilecek <b>anonim</b> portföy özeti (T3.3 + T3.10 derinleştirme — 07 §2 KVKK).
/// PII YOK: kullanıcı kimliği, varlık adı (kullanıcı serbest metni — özel/hassas olabilir),
/// holding id'leri **gönderilmez**. Yalnızca <b>tür-bazlı oranlar</b> ve <b>toplulaştırılmış
/// ölçütler</b> gider.
///
/// <para>
/// <b>T3.10 (2026-07-11):</b> Yorum derinliği için yük zenginleştirildi — maliyet/net kâr,
/// tür-bazlı getiri, nakit ağırlığı, kalem sayısı ve BES kendi/devlet payı eklendi. Hepsi
/// KODDA hesaplanır (CLAUDE.md §3.1); LLM yalnız verilen sayılara atıf yapar.
/// </para>
/// <para>
/// Sayılar üst basamağa yuvarlanır (granüler izleme imkanı azaltılır + LLM gereksiz hassasiyetle
/// uğraşmasın → daha temiz yorum üretir).
/// </para>
/// </summary>
public sealed record AnonymizedPortfolioSummary(
    string BaseCurrency,
    decimal TotalValue,
    decimal TotalCost,
    decimal NetProfit,
    decimal? ReturnRatio,
    decimal? RealReturnRatio,
    decimal ConcentrationTop2,
    decimal CashWeight,
    int HoldingCount,
    IReadOnlyList<AnonymizedAllocationSlice> Allocation,
    AnonymizedBesBreakdown? Bes);

/// <summary>
/// Tür-bazlı dilim (Gold/Fx/Stock/Fund/Bes/Cash) — kullanıcı varlık adı YOK.
/// <see cref="ReturnRatio"/> türün toplulaştırılmış getirisi (Σdeğer−Σmaliyet)/Σmaliyet —
/// holdings verilmişse hesaplanır; <see cref="ItemCount"/> o türdeki kalem sayısı.
/// </summary>
public sealed record AnonymizedAllocationSlice(
    AssetType Type,
    decimal Weight,
    decimal? ReturnRatio = null,
    int ItemCount = 1);

/// <summary>
/// BES kırılımı — eğitici bağlam: birikimin ne kadarı cepten (kendi katkı), ne kadarı devlet
/// katkısından. Oranlar yatırılmış toplamlar üzerinden; tutar gönderilmez (anonimlik).
/// </summary>
public sealed record AnonymizedBesBreakdown(decimal OwnShare, decimal StateShare);

/// <summary>
/// <see cref="PortfolioSummaryDto"/> (+ opsiyonel <see cref="HoldingDto"/> listesi) →
/// <see cref="AnonymizedPortfolioSummary"/> saf dönüşümü. Yan etki yok, deterministik,
/// testlenebilir. Aynı girdi aynı anonim yapıyı verir — T3.6 cache anahtarının hash girdisi.
/// </summary>
public static class PortfolioAnonymizer
{
    /// <summary>
    /// Oranlar 3 basamağa, parasal toplamlar en yakın tam sayıya yuvarlanır.
    /// <paramref name="holdings"/> verilirse tür-bazlı getiri + BES kırılımı da türetilir
    /// (verilmezse bu alanlar boş kalır — geriye uyumlu).
    /// </summary>
    public static AnonymizedPortfolioSummary Anonymize(
        PortfolioSummaryDto summary, IReadOnlyList<HoldingDto>? holdings = null)
    {
        ArgumentNullException.ThrowIfNull(summary);

        // Tür-bazlı getiri/kalem sayısı: holdings'ten (varsa) toplulaştır. Değerler baz para
        // biriminde geldiği için türler arası toplama geçerlidir.
        var perType = holdings?
            .GroupBy(h => h.AssetType)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var cost = g.Sum(h => h.TotalCost);
                    var value = g.Sum(h => h.CurrentValue ?? 0m);
                    var hasValue = g.Any(h => h.CurrentValue is not null);
                    decimal? ret = cost > 0m && hasValue ? Round3((value - cost) / cost) : null;
                    return (Return: ret, Count: g.Count());
                });

        // Aynı türden dilimleri birleştir (servis zaten genellikle gruplu döner ama garanti edelim;
        // kullanıcı aynı türde birden çok holding'e sahipse tek oran çıkar — anonimleştirme tutarlılığı).
        var grouped = summary.Allocation
            .GroupBy(a => a.AssetType)
            .Select(g =>
            {
                var stats = perType is not null && perType.TryGetValue(g.Key, out var s) ? s : default;
                return new AnonymizedAllocationSlice(
                    g.Key,
                    Round3(g.Sum(a => a.Weight)),
                    stats.Return,
                    stats.Count > 0 ? stats.Count : g.Count());
            })
            .OrderByDescending(s => s.Weight)
            .ToList();

        var concentrationTop2 = Round3(grouped.Take(2).Sum(s => s.Weight));
        var cashWeight = grouped.FirstOrDefault(s => s.Type == AssetType.Cash)?.Weight ?? 0m;

        // BES kırılımı: yatırılmış kendi + devlet katkısı toplamlarından pay oranları (tutar sızmaz).
        AnonymizedBesBreakdown? bes = null;
        if (holdings is not null)
        {
            var own = holdings.Where(h => h.Bes is not null).Sum(h => h.Bes!.OwnContribution);
            var state = holdings.Where(h => h.Bes is not null).Sum(h => h.Bes!.StateContribution);
            var total = own + state;
            if (total > 0m)
                bes = new AnonymizedBesBreakdown(Round3(own / total), Round3(state / total));
        }

        return new AnonymizedPortfolioSummary(
            BaseCurrency: summary.BaseCurrency.ToString(),
            TotalValue: Round0(summary.TotalValue),
            TotalCost: Round0(summary.TotalCost),
            NetProfit: Round0(summary.NetProfit),
            ReturnRatio: RoundOpt3(summary.ReturnRatio),
            RealReturnRatio: RoundOpt3(summary.RealReturnRatio),
            ConcentrationTop2: concentrationTop2,
            CashWeight: cashWeight,
            HoldingCount: holdings?.Count ?? summary.Allocation.Count,
            Allocation: grouped,
            Bes: bes);
    }

    private static decimal Round0(decimal value) => Math.Round(value, 0, MidpointRounding.AwayFromZero);
    private static decimal Round3(decimal value) => Math.Round(value, 3, MidpointRounding.AwayFromZero);
    private static decimal? RoundOpt3(decimal? value) => value is null ? null : Round3(value.Value);
}
