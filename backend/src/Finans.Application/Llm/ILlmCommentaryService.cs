using Finans.Application.Portfolio;

namespace Finans.Application.Llm;

/// <summary>
/// Portföy yorum servisi (T3.3 — 07): KODUN hazırladığı <see cref="PortfolioSummaryDto"/>'yu
/// anonimleştirir (PII yok — 07 §2 KVKK), <see cref="ILlmClient"/>'a sistem promptu + JSON şema
/// ile gönderir, dönen JSON'u <see cref="CommentaryResponse"/>'a parse eder. LLM erişilemezse veya
/// çıktı şemayı kıramazsa düz metin fallback kartı döner (07 §5 — uygulama çökmez, NFR-5).
///
/// <para>T3.4 tam güvenli parse + ek fallback senaryolarını sıkılaştıracak; T3.6 cache; T3.7 endpoint.</para>
/// </summary>
public interface ILlmCommentaryService
{
    Task<CommentaryResponse> GetCommentaryAsync(
        PortfolioSummaryDto summary, CancellationToken ct = default);
}
