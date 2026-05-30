using Finans.Application.Portfolio;
using Finans.Domain.Enums;

namespace Finans.Application.Tests.Portfolio;

/// <summary>
/// `NudgeRuleEngine` (T2.5, SC-09): kural tabanlı eğitici notlar — yoğunlaşma, tek varlık
/// ağırlığı, düşük nakit. Boş portföyde not yok; notlar **tavsiye içermez** (CLAUDE.md §2).
/// </summary>
public sealed class NudgeRuleEngineTests
{
    private readonly NudgeRuleEngine _engine = new();

    private static PortfolioSummaryDto Summary(params AllocationDto[] allocation) =>
        new(CurrencyCode.TRY, allocation.Sum(a => a.Value), 0m, 0m, null, null, allocation,
            new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc));

    private static AllocationDto Slice(AssetType type, string name, decimal weight) =>
        new(type, name, weight * 1000m, weight);

    [Fact]
    public void Concentration_fires_when_top_two_weight_high()
    {
        var nudges = _engine.Evaluate(Summary(
            Slice(AssetType.Bes, "BES", 0.40m),
            Slice(AssetType.Gold, "Altın", 0.35m),
            Slice(AssetType.Cash, "Nakit", 0.25m)));

        Assert.Contains(nudges, n => n.Id == "concentration");
    }

    [Fact]
    public void Concentration_does_not_fire_when_diversified()
    {
        var nudges = _engine.Evaluate(Summary(
            Slice(AssetType.Bes, "BES", 0.25m),
            Slice(AssetType.Gold, "Altın", 0.25m),
            Slice(AssetType.Stock, "AAPL", 0.25m),
            Slice(AssetType.Cash, "Nakit", 0.25m)));

        Assert.DoesNotContain(nudges, n => n.Id == "concentration");
    }

    [Fact]
    public void High_single_asset_fires_at_threshold_and_names_the_asset()
    {
        var nudges = _engine.Evaluate(Summary(
            Slice(AssetType.Gold, "Altın", 0.45m),
            Slice(AssetType.Cash, "Nakit", 0.30m),
            Slice(AssetType.Bes, "BES", 0.25m)));

        Assert.Contains(nudges, n => n.Id == "high-single-asset" && n.Body.Contains("Altın"));
    }

    [Fact]
    public void Low_cash_fires_below_threshold_only()
    {
        Assert.Contains(
            _engine.Evaluate(Summary(
                Slice(AssetType.Gold, "Altın", 0.97m),
                Slice(AssetType.Cash, "Nakit", 0.03m))),
            n => n.Id == "low-cash");

        Assert.DoesNotContain(
            _engine.Evaluate(Summary(
                Slice(AssetType.Gold, "Altın", 0.90m),
                Slice(AssetType.Cash, "Nakit", 0.10m))),
            n => n.Id == "low-cash");
    }

    [Fact]
    public void Empty_portfolio_has_no_nudges()
    {
        Assert.Empty(_engine.Evaluate(Summary()));
    }

    [Fact]
    public void Nudge_bodies_contain_no_direct_advice()
    {
        var nudges = _engine.Evaluate(Summary(
            Slice(AssetType.Bes, "BES", 0.45m),
            Slice(AssetType.Gold, "Altın", 0.35m),
            Slice(AssetType.Cash, "Nakit", 0.02m)));

        Assert.NotEmpty(nudges);
        string[] forbidden =
        {
            "satın al", "satış", "alım", "satım", "yükselecek", "düşecek", "tavsiye", "öneri", "almalı", "satmalı",
        };
        foreach (var n in nudges)
        {
            var body = n.Body.ToLowerInvariant();
            Assert.False(forbidden.Any(f => body.Contains(f)),
                $"'{n.Id}' notu somut yönlendirme içermemeli: {n.Body}");
        }
    }
}
