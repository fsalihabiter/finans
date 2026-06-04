using Finans.Application.Llm;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Finans.Application.Tests.Llm;

/// <summary>
/// T3.4 — Güvenli parse hardening: LLM çıktısının şemayı tam tutmadığı vakaları kapatır.
/// Cards üst sınırı, body min/max, meter clamp, tags filtreleme, ek alanları yutma. T3.5 çıktı
/// güvenlik filtresi (yasaklı yönlendirme kalıbı) bunun üstüne gelecek; T3.6 cache.
/// </summary>
public class LlmCommentaryHardeningTests
{
    private static PortfolioSummaryDto Summary() => new(
        BaseCurrency: CurrencyCode.TRY,
        TotalValue: 1_000_000m,
        TotalCost: 800_000m,
        NetProfit: 200_000m,
        ReturnRatio: 0.25m,
        RealReturnRatio: 0.1m,
        Allocation: [new AllocationDto(AssetType.Gold, "G", 1_000_000m, 1m)],
        AsOf: new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc));

    private static LlmCommentaryService BuildService(string llmJson) =>
        new(new StubLlmClient(_ => LlmResult.Ok(llmJson, 1, 1)),
            NullLogger<LlmCommentaryService>.Instance, TimeProvider.System);

    private sealed class StubLlmClient(Func<LlmRequest, LlmResult> responder) : ILlmClient
    {
        public Task<LlmResult> CompleteAsync(LlmRequest r, CancellationToken ct = default) =>
            Task.FromResult(responder(r));
    }

    private static string ValidCardJson(int count) =>
        "{\"cards\":[" +
        string.Join(",", Enumerable.Range(1, count).Select(i =>
            $"{{\"emoji\":\"✅\",\"title\":\"Kart {i}\",\"body\":\"" +
            new string('a', 80) + $"\"}}")) +
        "]}";

    [Fact]
    public async Task Caps_cards_at_max_even_when_llm_returns_more()
    {
        // 8 kart yolla → ilk 5 alınır (07 §4 maxItems).
        var svc = BuildService(ValidCardJson(8));

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Equal(5, resp.Cards.Count);
    }

    [Fact]
    public async Task Truncates_overlong_body_keeping_card()
    {
        var longBody = new string('x', 400); // > 220 → kırpılmalı, kart kalmalı
        var json = "{\"cards\":[{\"emoji\":\"✅\",\"title\":\"OK\",\"body\":\"" + longBody + "\"}]}";
        var svc = BuildService(json);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Single(resp.Cards);
        Assert.Equal(220, resp.Cards[0].Body.Length);
    }

    [Fact]
    public async Task Drops_card_with_body_too_short()
    {
        // 60 char minimum altında → kart düşer.
        var json = "{\"cards\":[" +
            "{\"emoji\":\"❌\",\"title\":\"Kısa\",\"body\":\"çok kısa\"}," +
            "{\"emoji\":\"✅\",\"title\":\"Uzun\",\"body\":\"" + new string('a', 80) + "\"}" +
            "]}";
        var svc = BuildService(json);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Single(resp.Cards);
        Assert.Equal("Uzun", resp.Cards[0].Title);
    }

    [Fact]
    public async Task Truncates_overlong_title()
    {
        var longTitle = new string('T', 100);
        var json = "{\"cards\":[{\"emoji\":\"✅\",\"title\":\"" + longTitle + "\",\"body\":\"" + new string('a', 80) + "\"}]}";
        var svc = BuildService(json);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Single(resp.Cards);
        Assert.Equal(40, resp.Cards[0].Title.Length);
    }

    [Fact]
    public async Task Clamps_meter_value_into_unit_interval()
    {
        var json = "{\"cards\":[{\"emoji\":\"✅\",\"title\":\"Meter\",\"body\":\"" + new string('a', 80) + "\"," +
            "\"meter\":{\"value\":2.5,\"lowLabel\":\"Az\",\"highLabel\":\"Çok\"}}]}";
        var svc = BuildService(json);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.NotNull(resp.Cards[0].Meter);
        Assert.Equal(1m, resp.Cards[0].Meter!.Value);
    }

    [Fact]
    public async Task Drops_meter_when_labels_all_empty()
    {
        // Anlamsız meter UI'da gri çubuk olmasın.
        var json = "{\"cards\":[{\"emoji\":\"✅\",\"title\":\"Meter\",\"body\":\"" + new string('a', 80) + "\"," +
            "\"meter\":{\"value\":0.5,\"lowLabel\":\"\",\"highLabel\":\"\"}}]}";
        var svc = BuildService(json);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Null(resp.Cards[0].Meter);
    }

    [Fact]
    public async Task Filters_non_string_tags_and_caps_tag_count()
    {
        var json = "{\"cards\":[{\"emoji\":\"✅\",\"title\":\"Tags\",\"body\":\"" + new string('a', 80) + "\"," +
            "\"tags\":[\"ok\",42,null,\"yine\",\"3\",\"4\",\"5\",\"6\"]}]}";
        var svc = BuildService(json);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.NotNull(resp.Cards[0].Tags);
        Assert.Equal(4, resp.Cards[0].Tags!.Count); // ≤ MaxTags
        Assert.All(resp.Cards[0].Tags!, s => Assert.False(string.IsNullOrEmpty(s)));
    }

    [Fact]
    public async Task Ignores_unknown_extra_fields_at_root_and_card_level()
    {
        // Forward compat: LLM ek alan üretse de istemci yutar.
        var json = "{\"meta\":{\"v\":1},\"cards\":[{\"emoji\":\"✅\",\"title\":\"FW\",\"body\":\"" + new string('a', 80) + "\"," +
            "\"futureField\":\"x\"}]}";
        var svc = BuildService(json);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Single(resp.Cards);
        Assert.Equal("FW", resp.Cards[0].Title);
    }

    [Fact]
    public async Task Falls_back_when_cards_root_missing()
    {
        var svc = BuildService("{\"something\":\"else\"}");

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Equal("fallback", resp.Source);
    }
}
