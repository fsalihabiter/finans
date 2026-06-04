using Finans.Application.Llm;

namespace Finans.Application.Tests.Llm;

/// <summary>
/// <see cref="ILlmClient"/> sözleşmesinin uçları (T3.1, 07 §5): başarı/başarısızlık ayrımı
/// uygulamanın "LLM kullanılamaz → fallback metin" akışının temel taşı. Sağlayıcı testleri
/// (Anthropic HTTP davranışı) integration tarafına bırakıldı.
/// </summary>
public class LlmContractTests
{
    [Fact]
    public void Ok_marks_success_and_carries_text_and_usage()
    {
        var r = LlmResult.Ok("merhaba", inputTokens: 12, outputTokens: 34);

        Assert.True(r.Success);
        Assert.Equal("merhaba", r.Text);
        Assert.Equal(12, r.InputTokens);
        Assert.Equal(34, r.OutputTokens);
        Assert.Null(r.ErrorReason);
    }

    [Fact]
    public void Fail_marks_failure_with_reason_and_empty_text()
    {
        var r = LlmResult.Fail("llm_not_configured");

        Assert.False(r.Success);
        Assert.Equal(string.Empty, r.Text);
        Assert.Equal(0, r.InputTokens);
        Assert.Equal(0, r.OutputTokens);
        Assert.Equal("llm_not_configured", r.ErrorReason);
    }

    [Fact]
    public void Request_keeps_defaults_temperature_and_max_tokens()
    {
        // Sözleşme defaultları: 0.2 sıcaklık (tutarlı + az halüsinasyon), 1024 token sınırı
        // (NFR-9 maliyet kontrolü için makul yorum kart bütçesi).
        var req = new LlmRequest("sys", "user");

        Assert.Equal(0.2m, req.Temperature);
        Assert.Equal(1024, req.MaxOutputTokens);
        Assert.Null(req.JsonSchema);
    }
}
