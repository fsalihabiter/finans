using Finans.Domain.Enums;

namespace Finans.Application.Education;

/// <summary>
/// "Senin portföyünde" bağlam anahtarları (15 §3.1, T6.2). Ders içeriği bu anahtarları
/// <c>{{anahtar}}</c> token'ı olarak yazar; <see cref="ILessonContextService"/> bunları
/// <b>deterministik olarak KODDA</b> hesaplanmış değerlerle değiştirir.
/// </summary>
/// <remarks>
/// <b>CLAUDE.md §3.1:</b> buradaki sayıların hiçbirini LLM üretmez. Yeni hesap da
/// yazılmaz — hepsi mevcut <c>PortfolioSummary</c> / <c>PortfolioAnonymizer</c>
/// çıktısından türetilir.
/// </remarks>
public static class ContextKeys
{
    /// <summary>En büyük iki varlığın toplam ağırlığı (yoğunlaşma) — oran.</summary>
    public const string ConcentrationTop2 = "concentration_top2";

    /// <summary>Portföyün nominal getiri oranı.</summary>
    public const string ReturnRatio = "return_ratio";

    /// <summary>Enflasyondan arındırılmış getiri oranı.</summary>
    public const string RealReturn = "real_return";

    /// <summary>Nakit ağırlığı — oran.</summary>
    public const string CashWeight = "cash_weight";

    /// <summary>Hisse senedi ağırlığı — oran.</summary>
    public const string StockWeight = "stock_weight";

    /// <summary>Toplam kalem sayısı.</summary>
    public const string HoldingCount = "holding_count";

    /// <summary>Kaç farklı varlık türüne yayıldığı.</summary>
    public const string AssetClassCount = "asset_class_count";

    /// <summary>Portföy toplam değeri — baz para biriminde.</summary>
    public const string TotalValue = "total_value";

    /// <summary>BES birikiminde devlet katkısının payı — oran (BES yoksa token düşer).</summary>
    public const string BesStateShare = "bes_state_share";
}

/// <summary>
/// Bir bağlam bloğunun çözümlenmiş hâli: metin + hangi veriden geldiği + tazelik damgası.
/// </summary>
/// <param name="Body">Token'ları değerlerle değiştirilmiş markdown.</param>
/// <param name="State">Own / Demo / Stale (15 §3.2).</param>
/// <param name="AsOf">
/// Verinin ait olduğu an. <see cref="LessonContextState.Stale"/> durumunda arayüz bunu
/// "şu tarihe ait" damgası olarak gösterir. Demo'da <c>null</c>.
/// </param>
public sealed record LessonContextResult(string Body, LessonContextState State, DateTime? AsOf);

/// <summary>
/// Ders içeriğindeki <c>{{anahtar}}</c> token'larını kullanıcının gerçek (ya da demo)
/// portföy metrikleriyle değiştirir (T6.2, 15 §3).
/// </summary>
public interface ILessonContextService
{
    /// <summary>
    /// <paramref name="templateBody"/> içindeki token'ları çözümler.
    /// Çözülemeyen token'lar <b>düşürülür</b> (ham <c>{{...}}</c> kullanıcıya asla gösterilmez).
    /// </summary>
    Task<LessonContextResult> ResolveAsync(string templateBody, CancellationToken ct = default);
}
