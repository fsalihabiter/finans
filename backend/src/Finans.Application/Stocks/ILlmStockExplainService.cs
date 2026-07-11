using Finans.Application.Llm;

namespace Finans.Application.Stocks;

/// <summary>
/// Hisse metrik açıklama servisi (T4.3 — 07 §8): KODUN çektiği metrikleri (T4.2) LLM'e
/// "bu rakamlar ne anlatıyor" diye açıklatır. Portföy yorumuyla aynı kart şeması, aynı
/// güvenli parse + korkuluklar + dil bekçileri; asla tavsiye/tahmin içermez (CLAUDE.md §2).
/// LLM erişilemezse fallback kartı döner — uygulama çökmez (NFR-5).
/// </summary>
public interface ILlmStockExplainService
{
    Task<CommentaryResponse> ExplainAsync(StockMetricsDto metrics, CancellationToken ct = default);
}
