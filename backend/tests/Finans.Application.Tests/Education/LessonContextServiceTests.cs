using System.Globalization;
using Finans.Application.Education;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;


namespace Finans.Application.Tests.Education;

/// <summary>
/// "Senin portföyünde" bağlam çözümleyicisi (T6.2, SC-E13, 15 §3).
/// Üç durum (<c>Own</c>/<c>Demo</c>/<c>Stale</c>) + token çözümü + TR biçim +
/// çözülemeyen token'ın satırı düşürmesi. Sayılar KODDA (CLAUDE.md §3.1).
/// </summary>
public sealed class LessonContextServiceTests
{
    private sealed class StubPortfolio(PortfolioSummaryDto? summary, bool throws = false) : IPortfolioService
    {
        public Task<PortfolioSummaryDto> GetSummaryAsync(
            CurrencyCode? baseCurrency = null, CancellationToken ct = default)
        {
            if (throws) throw new InvalidOperationException("fiyat sağlayıcı çöktü");
            return Task.FromResult(summary!);
        }
    }

    private static PortfolioSummaryDto Summary(
        DateTime? asOf = null,
        decimal totalValue = 100_000m,
        decimal? returnRatio = 0.25m,
        decimal? realReturn = -0.05m,
        params AllocationDto[] allocation)
    {
        var slices = allocation.Length > 0
            ? allocation
            :
            [
                new AllocationDto(AssetType.Gold, "A", 50_000m, 0.50m),
                new AllocationDto(AssetType.Stock, "B", 30_000m, 0.30m),
                new AllocationDto(AssetType.Cash, "C", 20_000m, 0.20m),
            ];

        return new PortfolioSummaryDto(
            CurrencyCode.TRY, totalValue, 80_000m, 20_000m, returnRatio, realReturn,
            slices, asOf ?? DateTime.UtcNow);
    }

    /// <summary>
    /// Geçmiş serisi sahtesi (GD-009). <c>points</c> null ise çağrı patlar —
    /// "seri alınamadı" kolunu denemek için.
    /// </summary>
    private sealed class StubHistory(IReadOnlyList<PortfolioHistoryPointDto>? points) : IPortfolioHistoryService
    {
        public int Calls { get; private set; }

        public Task<PortfolioHistoryDto> GetHistoryAsync(
            string? period, CurrencyCode? baseCurrency = null, CancellationToken ct = default)
        {
            Calls++;
            if (points is null) throw new InvalidOperationException("geçmiş servisi çöktü");
            return Task.FromResult(new PortfolioHistoryDto(
                CurrencyCode.TRY, period ?? "all", points, ChangeRatio: 9.99m, // ChangeRatio BİLEREK saçma
                FirstDate: points.Count > 0 ? points[0].Date : null, AsOf: DateTime.UtcNow));
        }
    }

    /// <summary>Bugünden <paramref name="monthsAgo"/> ay önceki gün için bir nokta.</summary>
    private static PortfolioHistoryPointDto Point(int monthsAgo, decimal value, decimal cost) =>
        new(DateOnly.FromDateTime(DateTime.UtcNow.Date).AddMonths(-monthsAgo), value, cost);

    private static LessonContextService NewService(
        PortfolioSummaryDto? summary,
        bool throws = false,
        IReadOnlyList<PortfolioHistoryPointDto>? history = null) =>
        new(new StubPortfolio(summary, throws), new StubHistory(history ?? []));

    [Fact]
    public async Task Resolves_tokens_from_own_portfolio()
    {
        var svc = NewService(Summary());

        var result = await svc.ResolveAsync(
            "Yoğunlaşman {{concentration_top2}}, {{holding_count}} kalem, {{asset_class_count}} tür.");

        Assert.Equal(LessonContextState.Own, result.State);
        // En büyük iki ağırlık: %50 + %30 = %80
        Assert.Equal("Yoğunlaşman %80, 3 kalem, 3 tür.", result.Body);
    }

    [Fact]
    public async Task Formats_numbers_in_turkish_convention()
    {
        var svc = NewService(Summary(totalValue: 422_970.50m, returnRatio: 0.315m));

        var result = await svc.ResolveAsync("{{total_value}} · {{return_ratio}}");

        // Binlik nokta, ondalık virgül; yüzde tam sayıya yakınsa ondalıksız (CLAUDE.md §8).
        Assert.Equal("422.970,50 ₺ · %31,5", result.Body);
    }

    [Fact]
    public async Task Falls_back_to_demo_when_portfolio_is_empty()
    {
        var svc = NewService(Summary(totalValue: 0m, allocation: []));

        var result = await svc.ResolveAsync("Yoğunlaşman {{concentration_top2}}.");

        Assert.Equal(LessonContextState.Demo, result.State);
        Assert.Null(result.AsOf);
        Assert.Contains("%", result.Body); // demo değerle çözüldü — ders yine okunabilir
    }

    [Fact]
    public async Task Single_holding_is_not_enough_for_own_context()
    {
        // Tek kalemde "yoğunlaşma/dağılım" anlatmak anlamsız → demo'ya düşer.
        var svc = NewService(Summary(allocation: [new AllocationDto(AssetType.Gold, "A", 100_000m, 1m)]));

        var result = await svc.ResolveAsync("{{concentration_top2}}");

        Assert.Equal(LessonContextState.Demo, result.State);
    }

    [Fact]
    public async Task Marks_context_stale_when_prices_are_old()
    {
        var svc = NewService(Summary(asOf: DateTime.UtcNow.AddDays(-3)));

        var result = await svc.ResolveAsync("{{concentration_top2}}");

        Assert.Equal(LessonContextState.Stale, result.State);
        Assert.True(Math.Abs((result.AsOf!.Value - DateTime.UtcNow.AddDays(-3)).TotalMinutes) < 1);
    }

    [Fact]
    public async Task Drops_line_when_a_token_cannot_be_resolved()
    {
        // Portföyde BES yok → {{bes_state_share}} çözülemez. Ham token gösterilmemeli;
        // uydurma "%0" da yazılmamalı → o satır tamamen düşer.
        var svc = NewService(Summary());

        var result = await svc.ResolveAsync(
            "Yoğunlaşman {{concentration_top2}}.\nBES devlet payın {{bes_state_share}}.");

        Assert.Equal("Yoğunlaşman %80.", result.Body);
        Assert.DoesNotContain("{{", result.Body);
        Assert.DoesNotContain("bes_state_share", result.Body);
    }

    [Fact]
    public async Task Type_weight_tokens_sum_all_slices_of_that_type()
    {
        var svc = NewService(Summary(allocation:
        [
            new AllocationDto(AssetType.Stock, "A", 30_000m, 0.30m),
            new AllocationDto(AssetType.Stock, "B", 25_000m, 0.25m),
            new AllocationDto(AssetType.Gold, "C", 45_000m, 0.45m),
        ]));

        var result = await svc.ResolveAsync("Hisse ağırlığın {{stock_weight}}.");

        Assert.Equal("Hisse ağırlığın %55.", result.Body); // 30 + 25
    }

    [Fact]
    public async Task Falls_back_to_demo_when_portfolio_service_fails()
    {
        // NFR-5: dış çağrı çökerse ders yine okunabilmeli.
        var svc = NewService(null, throws: true);

        var result = await svc.ResolveAsync("{{concentration_top2}}");

        Assert.Equal(LessonContextState.Demo, result.State);
        Assert.NotEmpty(result.Body);
    }

    [Fact]
    public async Task Formatting_does_not_depend_on_ambient_culture()
    {
        // REGRESYON (2026-07-19): biçimlendirme önce CultureInfo.GetCultureInfo("tr-TR")
        // kullanıyordu; üretim imajı globalization-invariant modda çalıştığı için canlıda
        // CultureNotFoundException → 500 verdi. Biçim artık açık NumberFormatInfo ile
        // tanımlı; ortam kültürü ne olursa olsun TR çıktı üretmeli.
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var svc = NewService(Summary(totalValue: 422_970.50m, returnRatio: 0.315m));

            var result = await svc.ResolveAsync("{{total_value}} · {{return_ratio}}");

            Assert.Equal("422.970,50 ₺ · %31,5", result.Body);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public async Task Resolution_is_deterministic()
    {
        // SC-E8: aynı portföy → aynı çıktı (LLM yok, saf hesap).
        var summary = Summary();
        var a = await NewService(summary).ResolveAsync("{{concentration_top2}} {{total_value}}");
        var b = await NewService(summary).ResolveAsync("{{concentration_top2}} {{total_value}}");

        Assert.Equal(b.Body, a.Body);
        Assert.Equal(b.State, a.State);
    }

    /* ───────────────────────────────────────────────────────────────────────
     * GD-009 — {{return_ratio_6m_ago}} (S2-L2 · 14. aşama)
     *
     * Ders "aynı portföy, iki ölçüm tarihi, iki farklı getiri" diyor. Sayının
     * ÖZETLE AYNI FORMÜLDEN gelmesi bu dersin tezinin kendisi: fark ölçüm
     * tarihinden doğmalı, hesaptan değil.
     * ⚠ Geçmiş uç noktasının `ChangeRatio` alanı KULLANILMAZ — katkılarla
     * kirlidir (INC-005). Sahte servis oraya bilerek saçma bir sayı (9,99)
     * koyuyor: biri onu kullanmaya kalkarsa test bağırır.
     * ─────────────────────────────────────────────────────────────────────── */

    [Fact]
    public async Task Earlier_return_uses_the_same_formula_as_the_summary_not_change_ratio()
    {
        // 6 ay önce: değer 110, maliyet 100 → %10. ChangeRatio (9,99) kullanılırsa
        // çıktı %999 olurdu; bugünkü oran ise özetten geliyor (%25).
        var svc = NewService(Summary(returnRatio: 0.25m), history:
        [
            Point(12, 100m, 100m),
            Point(6, 110m, 100m),
            Point(0, 125m, 100m),
        ]);

        var result = await svc.ResolveAsync("Bugün {{return_ratio}} · altı ay önce {{return_ratio_6m_ago}}");

        Assert.Equal(LessonContextState.Own, result.State);
        Assert.Equal("Bugün %25 · altı ay önce %10", result.Body);
    }

    [Fact]
    public async Task Earlier_return_is_omitted_when_the_series_is_younger_than_the_lookback()
    {
        // Portföy iki aylık → altı ay önceki ölçüm YOK. Uydurmak yerine satır düşer.
        var svc = NewService(Summary(), history: [Point(2, 110m, 100m), Point(0, 120m, 100m)]);

        var result = await svc.ResolveAsync(
            "Altı ay önce {{return_ratio_6m_ago}} idi.\nToplam {{total_value}}.");

        Assert.Equal("Toplam 100.000 ₺.", result.Body);
    }

    [Fact]
    public async Task Earlier_return_is_omitted_when_the_history_call_fails()
    {
        // NFR-5: geçmiş servisi çökse bile ders okunabilir kalır.
        var svc = new LessonContextService(new StubPortfolio(Summary()), new StubHistory(null));

        var result = await svc.ResolveAsync(
            "Altı ay önce {{return_ratio_6m_ago}} idi.\nToplam {{total_value}}.");

        Assert.Equal("Toplam 100.000 ₺.", result.Body);
    }

    [Fact]
    public async Task History_is_not_fetched_for_lessons_that_do_not_ask_for_it()
    {
        // "Ders başına tek dış çağrı" ilkesi: ikinci çağrının istisnası ÖLÇÜLÜ.
        var stub = new StubHistory([Point(6, 110m, 100m)]);
        var svc = new LessonContextService(new StubPortfolio(Summary()), stub);

        await svc.ResolveAsync("Yoğunlaşman {{concentration_top2}}, toplam {{total_value}}.");

        Assert.Equal(0, stub.Calls);
    }
}
