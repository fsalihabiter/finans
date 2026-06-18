using Finans.Application.Llm;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Finans.Application.Tests.Llm;

/// <summary>
/// T3.3: Orkestrasyon. Stub <see cref="ILlmClient"/> ile mutlu yol + fallback yolları doğrulanır.
/// (Anthropic HTTP davranışı integration tarafında <c>AnthropicLlmClientTests</c>.)
/// </summary>
public class LlmCommentaryServiceTests
{
    private static PortfolioSummaryDto SimpleSummary() => new(
        BaseCurrency: CurrencyCode.TRY,
        TotalValue: 641_403m,
        TotalCost: 422_970m,
        NetProfit: 218_433m,
        ReturnRatio: 0.516m,
        RealReturnRatio: 0.21m,
        Allocation:
        [
            new AllocationDto(AssetType.Gold, "Gram Altın", 260_000m, 0.405m),
            new AllocationDto(AssetType.Bes, "Örnek BES", 280_000m, 0.436m),
            new AllocationDto(AssetType.Fx, "USD", 95_000m, 0.150m),
            new AllocationDto(AssetType.Cash, "TRY", 6_403m, 0.009m),
        ],
        AsOf: new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc));

    private static LlmCommentaryService BuildService(ILlmClient llm) =>
        new(llm, NullLogger<LlmCommentaryService>.Instance, TimeProvider.System);

    private sealed class StubLlmClient(Func<LlmRequest, LlmResult> responder) : ILlmClient
    {
        public LlmRequest? LastRequest { get; private set; }
        public Task<LlmResult> CompleteAsync(LlmRequest request, CancellationToken ct = default)
        {
            LastRequest = request;
            return Task.FromResult(responder(request));
        }
    }

    [Fact]
    public async Task Happy_path_parses_cards_from_llm_json()
    {
        var stub = new StubLlmClient(_ => LlmResult.Ok("""
            {
              "cards": [
                { "emoji": "⚖️", "title": "Dağılımın Yoğun",
                  "body": "Portföyünün yaklaşık %84'ü iki kalemde toplanmış (Altın ve BES). Yoğunlaşma demek bu iki varlık aynı anda değer kaybederse büyük etkilenirsin demektir.",
                  "tags": ["yoğunlaşma"] },
                { "emoji": "📉", "title": "Enflasyon Sonrası",
                  "body": "Nominal getirin %51,6 olsa da enflasyon düşüldüğünde reel getirin %21. Satın alma gücü açısından kazancın bu — büyük bir fark.",
                  "meter": { "value": 0.21, "lowLabel": "Düşük", "highLabel": "Yüksek" } }
              ]
            }
            """, inputTokens: 200, outputTokens: 80));

        var resp = await BuildService(stub).GetCommentaryAsync(SimpleSummary());

        Assert.Equal("llm", resp.Source);
        Assert.Equal(2, resp.Cards.Count);
        Assert.Equal("Dağılımın Yoğun", resp.Cards[0].Title);
        Assert.Equal(0.21m, resp.Cards[1].Meter!.Value);
        Assert.Equal("Düşük", resp.Cards[1].Meter!.LowLabel);
        Assert.Contains("yoğunlaşma", resp.Cards[0].Tags!);
    }

    [Fact]
    public async Task Sends_anonymized_summary_and_uses_static_system_prompt_with_schema()
    {
        var stub = new StubLlmClient(_ => LlmResult.Ok("""{"cards":[{"emoji":"🧭","title":"X","body":"YYYYYYYYYY YYYYYYYYYY YYYYYYYYYY YYYYYYYYYY YYYYYYYYYY YYYYYYYYYY"}]}""", 1, 1));

        await BuildService(stub).GetCommentaryAsync(SimpleSummary());

        var req = stub.LastRequest!;
        // Şema dayatıldı (tool_use için).
        Assert.Equal(CommentaryPrompts.CommentaryJsonSchema, req.JsonSchema);
        // Sistem promptu statik (T3.2) — burada değiştirilemez (cache key tutarlılığı).
        Assert.Equal(CommentaryPrompts.SystemPrompt, req.SystemPrompt);
        // User prompt anonim özet: kullanıcı adı / varlık adı sızmaz.
        Assert.DoesNotContain("Örnek BES", req.UserPrompt);
        Assert.DoesNotContain("Gram Altın", req.UserPrompt);
        // Beklenen alanlar mevcut (camelCase).
        Assert.Contains("\"baseCurrency\"", req.UserPrompt);
        Assert.Contains("\"concentrationTop2\"", req.UserPrompt);
        Assert.Contains("\"allocation\"", req.UserPrompt);
    }

    [Fact]
    public async Task Returns_fallback_card_when_llm_fails()
    {
        var stub = new StubLlmClient(_ => LlmResult.Fail("llm_not_configured"));

        var resp = await BuildService(stub).GetCommentaryAsync(SimpleSummary());

        Assert.Equal("fallback", resp.Source);
        Assert.Single(resp.Cards);
        Assert.Equal("Yorum şu an üretilemedi", resp.Cards[0].Title);
        Assert.Contains("fallback", resp.Cards[0].Tags!);
    }

    [Fact]
    public async Task Returns_fallback_card_when_llm_text_is_not_valid_json()
    {
        var stub = new StubLlmClient(_ => LlmResult.Ok("bu JSON değil sadece düz metin", 50, 10));

        var resp = await BuildService(stub).GetCommentaryAsync(SimpleSummary());

        Assert.Equal("fallback", resp.Source);
    }

    [Fact]
    public async Task Returns_fallback_card_when_llm_returns_zero_cards()
    {
        var stub = new StubLlmClient(_ => LlmResult.Ok("""{"cards":[]}""", 30, 5));

        var resp = await BuildService(stub).GetCommentaryAsync(SimpleSummary());

        Assert.Equal("fallback", resp.Source);
    }

    [Fact]
    public async Task Drops_cards_missing_required_fields_but_keeps_valid_ones()
    {
        var stub = new StubLlmClient(_ => LlmResult.Ok("""
            {
              "cards": [
                { "title": "Eksik emoji", "body": "yeterli uzunlukta gövde olmayabilir bile" },
                { "emoji": "✅", "title": "Geçerli",
                  "body": "Bu kart tüm zorunlu alanlara sahip ve gövdesi de en az asgari uzunluğu doldurabilecek kadar uzun yazılmış." }
              ]
            }
            """, 80, 30));

        var resp = await BuildService(stub).GetCommentaryAsync(SimpleSummary());

        Assert.Equal("llm", resp.Source);
        Assert.Single(resp.Cards);
        Assert.Equal("Geçerli", resp.Cards[0].Title);
    }

    [Fact]
    public async Task Drops_card_with_forbidden_directive_but_keeps_clean_one()
    {
        // T3.5: ikinci kart "geçmelisin" (yönlendirme) → çıktı güvenlik filtresiyle düşer; temiz kart kalır.
        var stub = new StubLlmClient(_ => LlmResult.Ok("""
            {
              "cards": [
                { "emoji": "🧭", "title": "Yoğunlaşma",
                  "body": "Portföyünün büyük kısmı iki varlıkta toplanmış; bu yoğunlaşmayı bilmek faydalı bir farkındalıktır." },
                { "emoji": "🚀", "title": "Aksiyon",
                  "body": "Bence bu noktada altından çıkıp hisseye geçmelisin, fırsat kaçmadan harekete geçmelisin artık." }
              ]
            }
            """, 100, 40));

        var resp = await BuildService(stub).GetCommentaryAsync(SimpleSummary());

        Assert.Equal("llm", resp.Source);
        Assert.Single(resp.Cards);
        Assert.Equal("Yoğunlaşma", resp.Cards[0].Title);
    }

    [Fact]
    public async Task Falls_back_when_every_card_is_blocked_by_output_guard()
    {
        // T3.5: tek kart hem öneri ("almalısın") hem tahmin ("yükselecek") içeriyor → düşer → fallback.
        var stub = new StubLlmClient(_ => LlmResult.Ok("""
            {
              "cards": [
                { "emoji": "🚀", "title": "Al",
                  "body": "Bence şimdi altın almalısın çünkü kısa sürede ciddi biçimde yükselecek, kaçırma sakın." }
              ]
            }
            """, 60, 20));

        var resp = await BuildService(stub).GetCommentaryAsync(SimpleSummary());

        Assert.Equal("fallback", resp.Source);
        Assert.Equal("Yorum şu an üretilemedi", resp.Cards[0].Title);
    }
}
