using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Integration.Tests;

/// <summary>
/// Kenar-durum düzeltmeleri uçtan uca (2026-07-12): SC-40 (fiyatsız kalem özete
/// maliyetiyle girer — sahte −%100 yok) + SC-41 (kronolojik aşırı satış yazmada 400).
/// Kendi fixture'ı (taze DB) + göreli önce/sonra doğrulamalar — sınıf içi test
/// sırasından bağımsız kalır.
/// </summary>
public sealed class PricelessAndChronologyApiTests
    : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;

    private static readonly Guid Investor = SeedData.Id("user-1");

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public PricelessAndChronologyApiTests(SqliteWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient Client()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", Investor.ToString());
        return client;
    }

    // ── SC-40: fiyatsız kalem özete maliyetiyle girer ────────────────────────

    [Fact]
    public async Task Priceless_holding_enters_summary_at_cost_not_as_total_loss()
    {
        var client = Client();
        var before = await client.GetFromJsonAsync<PortfolioSummaryDto>("/api/portfolio/summary", Json);

        // Fiyatı hiç girilmemiş fon: 100 adet @ 10 → maliyet 1.000, CurrentPrice null.
        var create = new CreateHoldingRequest(
            AssetType.Fund, "Fiyatsız Fon (SC-40)", Symbol: null, CurrencyCode.TRY, "adet",
            new TransactionRequest(TransactionType.Buy, 100m, 10m));
        var createResp = await client.PostAsJsonAsync("/api/holdings", create, Json);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<HoldingDto>(Json);
        created!.CurrentValue.Should().BeNull(); // kalem satırı "fiyatsız" kalır

        var after = await client.GetFromJsonAsync<PortfolioSummaryDto>("/api/portfolio/summary", Json);

        // Toplam DEĞER de maliyet kadar artar (eskiden 0 sayılıp −1.000 sahte zarardı).
        (after!.TotalCost - before!.TotalCost).Should().Be(1000m);
        (after.TotalValue - before.TotalValue).Should().Be(1000m);
        (after.NetProfit - before.NetProfit).Should().Be(0m);

        // Dağılımda maliyet değeriyle yer alır; ağırlıklar toplamı 1 kalır.
        after.Allocation.Single(a => a.Name == "Fiyatsız Fon (SC-40)").Value.Should().Be(1000m);
        Math.Round(after.Allocation.Sum(a => a.Weight), 6).Should().Be(1m);

        (await client.DeleteAsync($"/api/holdings/{created.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── SC-41: kronolojik aşırı satış → 400, işlem kaydedilmez ───────────────

    [Fact]
    public async Task Sell_dated_before_buy_is_rejected_and_not_persisted()
    {
        var client = Client();

        // 10 Oca 2026'da 10 adet alış.
        var create = new CreateHoldingRequest(
            AssetType.Fund, "Kronoloji Fonu (SC-41)", Symbol: null, CurrencyCode.TRY, "adet",
            new TransactionRequest(TransactionType.Buy, 10m, 100m, 0m,
                new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)));
        var createResp = await client.PostAsJsonAsync("/api/holdings", create, Json);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var holding = await createResp.Content.ReadFromJsonAsync<HoldingDto>(Json);

        // 5 Oca tarihli satış: nihai miktar 5 ≥ 0 ama 5 Oca'da pozisyon −5 olurdu → 400.
        var badSell = await client.PostAsJsonAsync($"/api/holdings/{holding!.Id}/transactions",
            new TransactionRequest(TransactionType.Sell, 5m, 110m, 0m,
                new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)), Json);
        badSell.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using (var doc = JsonDocument.Parse(await badSell.Content.ReadAsStringAsync()))
        {
            var error = doc.RootElement.GetProperty("error");
            error.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
            error.GetProperty("details")[0].GetProperty("field").GetString().Should().Be("quantity");
        }

        // Reddedilen işlem KAYDEDİLMEDİ; alıştan sonraki tarihli geçerli satış kabul edilir.
        var detail = await client.GetFromJsonAsync<HoldingDto>($"/api/holdings/{holding.Id}", Json);
        detail!.Transactions.Should().HaveCount(1);

        var goodSell = await client.PostAsJsonAsync($"/api/holdings/{holding.Id}/transactions",
            new TransactionRequest(TransactionType.Sell, 5m, 110m, 0m,
                new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc)), Json);
        goodSell.StatusCode.Should().Be(HttpStatusCode.OK);
        (await goodSell.Content.ReadFromJsonAsync<HoldingDto>(Json))!.Quantity.Should().Be(5m);
    }

    [Fact]
    public async Task Moving_sell_before_buy_via_update_is_rejected()
    {
        var client = Client();

        // 10 Oca alış + 12 Oca geçerli satış kur.
        var create = new CreateHoldingRequest(
            AssetType.Fund, "Kronoloji Düzenle (SC-41)", Symbol: null, CurrencyCode.TRY, "adet",
            new TransactionRequest(TransactionType.Buy, 10m, 100m, 0m,
                new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)));
        var createResp = await client.PostAsJsonAsync("/api/holdings", create, Json);
        var holding = await createResp.Content.ReadFromJsonAsync<HoldingDto>(Json);

        var sellResp = await client.PostAsJsonAsync($"/api/holdings/{holding!.Id}/transactions",
            new TransactionRequest(TransactionType.Sell, 5m, 110m, 0m,
                new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc)), Json);
        sellResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await client.GetFromJsonAsync<HoldingDto>($"/api/holdings/{holding.Id}", Json);
        var sellTx = detail!.Transactions!.Single(t => t.Type == TransactionType.Sell);

        // Satışı alıştan ÖNCEKİ tarihe taşımaya çalış → 400; tarih değişmemiş kalır.
        var moveResp = await client.PutAsJsonAsync(
            $"/api/holdings/{holding.Id}/transactions/{sellTx.Id}",
            new TransactionRequest(TransactionType.Sell, 5m, 110m, 0m,
                new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)), Json);
        moveResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var unchanged = await client.GetFromJsonAsync<HoldingDto>($"/api/holdings/{holding.Id}", Json);
        unchanged!.Transactions!.Single(t => t.Type == TransactionType.Sell)
            .TransactedAtUtc.Should().Be(new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc));
    }
}
