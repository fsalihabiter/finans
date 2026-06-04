using Finans.Application.Portfolio;
using Finans.Domain.Enums;

namespace Finans.Application.Llm;

/// <summary>
/// LLM'e gönderilecek <b>anonim</b> portföy özeti (T3.3 — 07 §2 KVKK çerçevesi). PII YOK:
/// kullanıcı kimliği, varlık adı (kullanıcı serbest metni — özel/hassas olabilir), holding id'leri
/// **gönderilmez**. Yalnızca <b>tür-bazlı oranlar</b> ve <b>toplulaştırılmış ölçütler</b> gider.
///
/// <para>
/// Sayılar üst basamağa yuvarlanır (granüler izleme imkanı azaltılır + LLM gereksiz hassasiyetle
/// uğraşmasın → daha temiz yorum üretir).
/// </para>
/// </summary>
public sealed record AnonymizedPortfolioSummary(
    string BaseCurrency,
    decimal TotalValue,
    decimal? ReturnRatio,
    decimal? RealReturnRatio,
    IReadOnlyList<AnonymizedAllocationSlice> Allocation,
    decimal ConcentrationTop2);

/// <summary>Tür-bazlı dilim (Gold/Fx/Stock/Fund/Bes/Cash) — kullanıcı varlık adı YOK.</summary>
public sealed record AnonymizedAllocationSlice(AssetType Type, decimal Weight);

/// <summary>
/// <see cref="PortfolioSummaryDto"/> → <see cref="AnonymizedPortfolioSummary"/> saf dönüşümü.
/// Yan etki yok, deterministik, testlenebilir. Aynı kullanıcı kapsamlı özet aynı anonim yapıyı verir
/// — bu T3.6'da cache anahtarı için hash girdisi olacak.
/// </summary>
public static class PortfolioAnonymizer
{
    /// <summary>Oranlar 3 basamağa, toplam değer en yakın tam sayıya yuvarlanır.</summary>
    public static AnonymizedPortfolioSummary Anonymize(PortfolioSummaryDto summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        // Aynı türden dilimleri birleştir (servis zaten genellikle gruplu döner ama garanti edelim;
        // kullanıcı aynı türde birden çok holding'e sahipse tek oran çıkar — anonimleştirme tutarlılığı).
        var grouped = summary.Allocation
            .GroupBy(a => a.AssetType)
            .Select(g => new AnonymizedAllocationSlice(
                g.Key,
                Round3(g.Sum(a => a.Weight))))
            .OrderByDescending(s => s.Weight)
            .ToList();

        var concentrationTop2 = Round3(grouped.Take(2).Sum(s => s.Weight));

        return new AnonymizedPortfolioSummary(
            BaseCurrency: summary.BaseCurrency.ToString(),
            TotalValue: Math.Round(summary.TotalValue, 0, MidpointRounding.AwayFromZero),
            ReturnRatio: RoundOpt3(summary.ReturnRatio),
            RealReturnRatio: RoundOpt3(summary.RealReturnRatio),
            Allocation: grouped,
            ConcentrationTop2: concentrationTop2);
    }

    private static decimal Round3(decimal value) => Math.Round(value, 3, MidpointRounding.AwayFromZero);
    private static decimal? RoundOpt3(decimal? value) => value is null ? null : Round3(value.Value);
}
