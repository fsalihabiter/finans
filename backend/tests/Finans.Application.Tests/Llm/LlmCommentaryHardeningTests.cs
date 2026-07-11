using Finans.Application.Llm;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Finans.Application.Tests.Llm;

/// <summary>
/// T3.4 — Güvenli parse hardening: LLM çıktısının şemayı tam tutmadığı vakaları kapatır.
/// Cards üst sınırı, body min/max, meter clamp, tags filtreleme, ek alanları yutma. T3.5 çıktı
/// güvenlik filtresi (yasaklı yönlendirme kalıbı) bunun üstüne gelir; T3.6 cache.
/// T3.10 derinleştirme: sınırlar büyüdü (6 kart, body 120-600) + opsiyonel detail alanı.
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

    // T3.10: MinBody 120 — geçerli fixture gövdesi 150 char.
    private static string ValidBody() => new('a', 150);

    private static string ValidCardJson(int count) =>
        "{\"cards\":[" +
        string.Join(",", Enumerable.Range(1, count).Select(i =>
            $"{{\"emoji\":\"✅\",\"title\":\"Kart {i}\",\"body\":\"" +
            ValidBody() + $"\"}}")) +
        "]}";

    [Fact]
    public async Task Caps_cards_at_max_even_when_llm_returns_more()
    {
        // 9 kart yolla → ilk 6 alınır (07 §4 maxItems, T3.10).
        var svc = BuildService(ValidCardJson(9));

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Equal(6, resp.Cards.Count);
    }

    [Fact]
    public async Task Truncates_overlong_body_keeping_card()
    {
        var longBody = new string('x', 700); // > 600 → kırpılmalı, kart kalmalı (T3.10)
        var json = "{\"cards\":[{\"emoji\":\"✅\",\"title\":\"OK\",\"body\":\"" + longBody + "\"}]}";
        var svc = BuildService(json);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Single(resp.Cards);
        Assert.Equal(600, resp.Cards[0].Body.Length);
    }

    [Fact]
    public async Task Truncation_cuts_at_sentence_boundary_not_mid_word()
    {
        // T3.10 canlı gözlem: 600'ü aşan gövde kelime ortasından kesiliyordu ("…eksikliği ri").
        // Cümleli metinde kesim son cümle sonunda biter.
        var sentence = "Bu tam bir cümledir ve anlamlı biter. "; // 39 char
        var longBody = string.Concat(Enumerable.Repeat(sentence, 20)).Trim(); // ~780 char
        var json = "{\"cards\":[{\"emoji\":\"✅\",\"title\":\"OK\",\"body\":\"" + longBody + "\"}]}";
        var svc = BuildService(json);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Single(resp.Cards);
        Assert.True(resp.Cards[0].Body.Length <= 600);
        Assert.EndsWith("biter.", resp.Cards[0].Body); // cümle sınırında, kelime ortasında değil
    }

    [Fact]
    public async Task Drops_card_with_body_too_short()
    {
        // 120 char minimum altında → kart düşer (tek cümlelik yüzeysel yorum istemiyoruz — T3.10).
        var json = "{\"cards\":[" +
            "{\"emoji\":\"❌\",\"title\":\"Kısa\",\"body\":\"" + new string('k', 80) + "\"}," +
            "{\"emoji\":\"✅\",\"title\":\"Uzun\",\"body\":\"" + ValidBody() + "\"}" +
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
        var json = "{\"cards\":[{\"emoji\":\"✅\",\"title\":\"" + longTitle + "\",\"body\":\"" + ValidBody() + "\"}]}";
        var svc = BuildService(json);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Single(resp.Cards);
        Assert.Equal(48, resp.Cards[0].Title.Length);
    }

    [Fact]
    public async Task Keeps_detail_and_truncates_overlong_detail()
    {
        // T3.10: detail opsiyonel eğitim paragrafı — 500 üstü kırpılır, kart kalır.
        var json = "{\"cards\":[{\"emoji\":\"✅\",\"title\":\"Detay\",\"body\":\"" + ValidBody() + "\"," +
            "\"detail\":\"" + new string('d', 620) + "\"}]}";
        var svc = BuildService(json);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Single(resp.Cards);
        Assert.NotNull(resp.Cards[0].Detail);
        Assert.Equal(500, resp.Cards[0].Detail!.Length);
    }

    [Fact]
    public async Task Nulls_detail_when_it_contains_finance_numbers()
    {
        // Kural 8: detail'de FİNANSAL sayı olamaz. Canlı gözlem: model detail'de girdiyle
        // tutarsız örnek yüzdeler uydurdu (%67/%33) → deterministik süzgeç detail'i atar.
        var json = "{\"cards\":[{\"emoji\":\"🏦\",\"title\":\"BES\",\"body\":\"" + ValidBody() + "\"," +
            "\"detail\":\"Bir bahçenin yarısını sen suluyorsan toplam ürünün payı senin emeğin %67, komşu katkısı %33 olur; benzer çalışır.\"}]}";
        var svc = BuildService(json);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Single(resp.Cards);
        Assert.Null(resp.Cards[0].Detail);
    }

    [Fact]
    public async Task Keeps_detail_with_innocent_numbers()
    {
        // T3.12 yumuşatma: benzetmedeki masum sayılar ("10 kilo elma") kavram bloğunu ÖLDÜRMEZ —
        // önceki katı her-rakam kuralı kullanıcının sevdiği kavram açıklamalarını siliyordu.
        var json = "{\"cards\":[{\"emoji\":\"📉\",\"title\":\"Reel Getiri\",\"body\":\"" + ValidBody() + "\"," +
            "\"detail\":\"Mağazada aynı parayla önce 10 kilo elma alabiliyordun; şimdi 8 kilo alabiliyorsan gerçek zenginlikten kaybettin demektir.\"}]}";
        var svc = BuildService(json);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Single(resp.Cards);
        Assert.NotNull(resp.Cards[0].Detail);
        Assert.Contains("10 kilo", resp.Cards[0].Detail);
    }

    [Fact]
    public async Task Nulls_detail_when_too_short_but_keeps_card()
    {
        // Gürültü seviyesinde kısa detail → null; kart body ile yaşamaya devam eder.
        var json = "{\"cards\":[{\"emoji\":\"✅\",\"title\":\"Detay\",\"body\":\"" + ValidBody() + "\"," +
            "\"detail\":\"kısa\"}]}";
        var svc = BuildService(json);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Single(resp.Cards);
        Assert.Null(resp.Cards[0].Detail);
    }

    [Fact]
    public async Task Drops_card_with_foreign_language_leak_but_keeps_clean_one()
    {
        // T3.11: canlı gözlem — ücretsiz model Türkçe'ye İngilizce/Japonca karıştırabiliyor.
        var json = "{\"cards\":[" +
            "{\"emoji\":\"❌\",\"title\":\"Sızıntı\",\"body\":\"Senin portföyünde bu oran yüzde on — yani her yüz lira invested olduğunda yaklaşık on lira nominal kazanç elde etmiş olursun; bu oran pozitif olduğu için portföy nominal olarak büyümüş.\"}," +
            "{\"emoji\":\"✅\",\"title\":\"Temiz\",\"body\":\"" + ValidBody() + "\"}" +
            "]}";
        var svc = BuildService(json);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Single(resp.Cards);
        Assert.Equal("Temiz", resp.Cards[0].Title);
    }

    [Fact]
    public async Task Translates_leaked_field_names_instead_of_dropping_card()
    {
        // T3.11: alan adı sızıntısı düzeltilebilir bir kusur — kart düşürülmez, Türkçeleştirilir.
        var body = "Senin BES hesabında kendi payın yüksek; yüksek ownShare uzun vadeli tasarruf disiplinini, yüksek stateShare ise dış destek oranını yansıtır. Bu yapı uzun vadeli birikimi teşvik etmek için tasarlanmıştır.";
        var json = "{\"cards\":[{\"emoji\":\"🏦\",\"title\":\"BES Katkı Dağılımı\",\"body\":\"" + body + "\"}]}";
        var svc = BuildService(json);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Single(resp.Cards);
        Assert.DoesNotContain("ownShare", resp.Cards[0].Body);
        Assert.Contains("kendi katkı payı", resp.Cards[0].Body);
    }

    [Fact]
    public async Task Drops_card_when_detail_contains_forbidden_directive()
    {
        // T3.10: güvenlik filtresi detail'i de tarar — gövde temiz olsa bile yasaklı detail kartı düşürür.
        var json = "{\"cards\":[" +
            "{\"emoji\":\"❌\",\"title\":\"Sızma\",\"body\":\"" + ValidBody() + "\"," +
            "\"detail\":\"Bu durumda altın satmalısın ve hisseye geçmelisin, en mantıklısı bu olur bence.\"}," +
            "{\"emoji\":\"✅\",\"title\":\"Temiz\",\"body\":\"" + ValidBody() + "\"}" +
            "]}";
        var svc = BuildService(json);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Single(resp.Cards);
        Assert.Equal("Temiz", resp.Cards[0].Title);
    }

    [Fact]
    public async Task Clamps_meter_value_into_unit_interval()
    {
        var json = "{\"cards\":[{\"emoji\":\"✅\",\"title\":\"Meter\",\"body\":\"" + ValidBody() + "\"," +
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
        var json = "{\"cards\":[{\"emoji\":\"✅\",\"title\":\"Meter\",\"body\":\"" + ValidBody() + "\"," +
            "\"meter\":{\"value\":0.5,\"lowLabel\":\"\",\"highLabel\":\"\"}}]}";
        var svc = BuildService(json);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Null(resp.Cards[0].Meter);
    }

    [Fact]
    public async Task Filters_non_string_tags_and_caps_tag_count()
    {
        var json = "{\"cards\":[{\"emoji\":\"✅\",\"title\":\"Tags\",\"body\":\"" + ValidBody() + "\"," +
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
        var json = "{\"meta\":{\"v\":1},\"cards\":[{\"emoji\":\"✅\",\"title\":\"FW\",\"body\":\"" + ValidBody() + "\"," +
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

    // ── T3.12: kalite düşünce yeniden üretim (kart kaybını telafi) ──

    private sealed class SequenceLlmClient(params string[] responses) : ILlmClient
    {
        public int Calls { get; private set; }
        public Task<LlmResult> CompleteAsync(LlmRequest r, CancellationToken ct = default) =>
            Task.FromResult(LlmResult.Ok(responses[Math.Min(Calls++, responses.Length - 1)], 1, 1));
    }

    [Fact]
    public async Task Retries_once_when_guard_drops_cards_and_uses_best_attempt()
    {
        // 1. deneme: 1 temiz + 1 yasaklı (bekçi düşürür) → kusurlu; 2. deneme: 5 temiz → o kullanılır.
        var flawed = "{\"cards\":[" +
            "{\"emoji\":\"✅\",\"title\":\"Temiz\",\"body\":\"" + ValidBody() + "\"}," +
            "{\"emoji\":\"❌\",\"title\":\"Sızıntı\",\"body\":\"Senin portföyünde bu oran yüzde on — yani her yüz lira invested olduğunda yaklaşık on lira kazanç elde etmiş olursun; oran pozitif olduğu için portföy büyümüş.\"}" +
            "]}";
        var client = new SequenceLlmClient(flawed, ValidCardJson(5));
        var svc = new LlmCommentaryService(client, NullLogger<LlmCommentaryService>.Instance, TimeProvider.System);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Equal(2, client.Calls);          // yeniden üretildi
        Assert.Equal(5, resp.Cards.Count);      // en iyi deneme kullanıldı — kart kaybı yok
        Assert.Equal("llm", resp.Source);
    }

    [Fact]
    public async Task Does_not_retry_when_first_attempt_is_clean()
    {
        var client = new SequenceLlmClient(ValidCardJson(4));
        var svc = new LlmCommentaryService(client, NullLogger<LlmCommentaryService>.Instance, TimeProvider.System);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Equal(1, client.Calls);          // temiz turda ikinci çağrı YOK (maliyet disiplini)
        Assert.Equal(4, resp.Cards.Count);
    }

    [Fact]
    public async Task Keeps_first_attempt_when_retry_is_worse()
    {
        // 1. deneme: 4 temiz + 1 yasaklı (bekçi 1 düşürdü → 4 kart, kusurlu tur);
        // 2. deneme: 2 kart → daha kötü. En iyi (ilk) deneme kullanılır.
        var flawed = "{\"cards\":[" +
            string.Join(",", Enumerable.Range(1, 4).Select(i =>
                $"{{\"emoji\":\"✅\",\"title\":\"Kart {i}\",\"body\":\"" + ValidBody() + "\"}")) +
            ",{\"emoji\":\"❌\",\"title\":\"Sızıntı\",\"body\":\"Bu noktada hiç düşünmeden altın almalısın çünkü kısa sürede ciddi biçimde yükselecek; bu tarihi fırsatı sakın kaçırma, herkes alırken sen de almalısın.\"}" +
            "]}";
        var client = new SequenceLlmClient(flawed, ValidCardJson(2));
        var svc = new LlmCommentaryService(client, NullLogger<LlmCommentaryService>.Instance, TimeProvider.System);

        var resp = await svc.GetCommentaryAsync(Summary());

        Assert.Equal(2, client.Calls);
        Assert.Equal(4, resp.Cards.Count);      // ilk denemenin 4 temiz kartı korunur
    }
}
