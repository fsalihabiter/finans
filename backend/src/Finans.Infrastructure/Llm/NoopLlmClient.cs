using Finans.Application.Llm;

namespace Finans.Infrastructure.Llm;

/// <summary>
/// API anahtarı yapılandırılmamışken (yerel dev / testler / kalkık API) varsayılan sağlayıcı.
/// <b>Hiçbir dış çağrı yapmaz</b>; başarısız sonuç döner. Üst katman (07 §5 fallback) bunu
/// "LLM kullanılamaz" olarak ele alıp düz metin kartı gösterir — uygulama çökmez (NFR-5).
/// Faz 3 testlerinde ve yerel geliştirmede varsayılan budur; API anahtarı gelince DI sağlayıcı
/// otomatik olarak <see cref="AnthropicLlmClient"/>'a geçer.
/// </summary>
public sealed class NoopLlmClient : ILlmClient
{
    public Task<LlmResult> CompleteAsync(LlmRequest request, CancellationToken ct = default) =>
        Task.FromResult(LlmResult.Fail("llm_not_configured"));
}
