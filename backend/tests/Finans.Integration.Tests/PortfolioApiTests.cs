using System.Net;
using System.Net.Http.Headers;
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
/// Portföy API uçtan uca (T1.6/T1.7): Holdings CRUD + summary, geçerli kullanıcıya
/// kapsanma. Senaryolar: SC-01 (özet/getiri doğru), SC-07 (geçersiz girdi→400),
/// SC-13 (IDOR: başkasının id'si→404).
/// </summary>
public sealed class PortfolioApiTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;

    // Seed deterministik Id'leri (Infrastructure/Seed/SeedData.cs).
    private static readonly Guid Investor = SeedData.Id("user-1");
    private static readonly Guid Admin = SeedData.Id("admin-1");
    private static readonly Guid GoldHolding = SeedData.Id("holding-gold");
    private static readonly Guid BesHolding = SeedData.Id("holding-bes");

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public PortfolioApiTests(SqliteWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Belirli kullanıcı kimliğiyle (X-User-Id) istek atan istemci.</summary>
    private HttpClient ClientAs(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", userId.ToString());
        return client;
    }

    // ── SC-01: Özet ve getiri doğru ──────────────────────────────────────────

    [Fact]
    public async Task Summary_matches_seed_totals_and_real_return()
    {
        var client = ClientAs(Investor);

        var summary = await client.GetFromJsonAsync<PortfolioSummaryDto>("/api/portfolio/summary", Json);

        summary.Should().NotBeNull();
        summary!.BaseCurrency.Should().Be(CurrencyCode.TRY);
        summary.TotalCost.Should().Be(422970m);
        summary.TotalValue.Should().Be(641403m);
        summary.NetProfit.Should().Be(218433m);
        Math.Round(summary.ReturnRatio!.Value, 3).Should().Be(0.516m);
        Math.Round(summary.RealReturnRatio!.Value, 4).Should().Be(0.0989m); // enflasyon 0,38
        summary.Allocation.Should().HaveCount(4);
        Math.Round(summary.Allocation.Sum(a => a.Weight), 6).Should().Be(1m);
    }

    [Fact]
    public async Task Holdings_list_returns_gold_with_correct_metrics()
    {
        var client = ClientAs(Investor);

        var holdings = await client.GetFromJsonAsync<List<HoldingDto>>("/api/holdings", Json);

        holdings.Should().NotBeNull().And.HaveCount(4);
        var gold = holdings!.Single(h => h.AssetType == AssetType.Gold);
        gold.Quantity.Should().Be(40m);
        gold.AvgCost.Should().Be(4546.275m);
        gold.TotalCost.Should().Be(181851m);
        gold.CurrentValue.Should().Be(260000m);
        Math.Round(gold.ReturnRatio!.Value, 2).Should().Be(0.43m); // +%43
        Math.Round(gold.Weight, 3).Should().Be(0.405m);

        // BES kalemi devlet katkısını AYRI taşır (03 §A).
        var bes = holdings.Single(h => h.AssetType == AssetType.Bes);
        bes.Bes.Should().NotBeNull();
        bes.Bes!.StateContribution.Should().Be(28554m);
        bes.Bes.OwnContribution.Should().Be(120000m);
    }

    // ── CRUD akışı ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_then_add_transaction_then_update_then_delete()
    {
        var client = ClientAs(Investor);

        // CREATE: yeni fon pozisyonu (ilk işlem Buy 100 @ 10 → ort. maliyet 10)
        var create = new CreateHoldingRequest(
            AssetType.Fund, "Test Fonu", Symbol: null, CurrencyCode.TRY, "adet",
            new TransactionRequest(TransactionType.Buy, 100m, 10m));
        var createResp = await client.PostAsJsonAsync("/api/holdings", create, Json);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<HoldingDto>(Json);
        created!.Quantity.Should().Be(100m);
        created.AvgCost.Should().Be(10m);
        created.CurrentPrice.Should().BeNull(); // fiyat henüz girilmedi

        // ADD TRANSACTION: Buy 100 @ 20 → toplam 200, ağırlıklı ort. 15
        var addResp = await client.PostAsJsonAsync($"/api/holdings/{created.Id}/transactions",
            new TransactionRequest(TransactionType.Buy, 100m, 20m), Json);
        addResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await addResp.Content.ReadFromJsonAsync<HoldingDto>(Json);
        updated!.Quantity.Should().Be(200m);
        updated.AvgCost.Should().Be(15m);

        // UPDATE: güncel fiyat 25 → değer 200 × 25 = 5.000
        var putResp = await client.PutAsJsonAsync($"/api/holdings/{created.Id}",
            new UpdateHoldingRequest(25m), Json);
        putResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var priced = await putResp.Content.ReadFromJsonAsync<HoldingDto>(Json);
        priced!.CurrentPrice.Should().Be(25m);
        priced.CurrentValue.Should().Be(5000m);

        // DELETE: 204, sonra GET 404
        var delResp = await client.DeleteAsync($"/api/holdings/{created.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var afterDelete = await client.GetAsync($"/api/holdings/{created.Id}");
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_second_position_on_same_asset_conflicts()
    {
        var client = ClientAs(Investor);

        // Yatırımcı zaten altın pozisyonuna sahip → ikinci altın pozisyonu 409.
        var create = new CreateHoldingRequest(
            AssetType.Gold, "Altın (gram)", "XAU", CurrencyCode.TRY, "gram",
            new TransactionRequest(TransactionType.Buy, 5m, 6000m));
        var resp = await client.PostAsJsonAsync("/api/holdings", create, Json);

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var code = await ErrorCodeAsync(resp);
        code.Should().Be("CONFLICT");
    }

    // ── SC-07: Geçersiz girdi → 400 ──────────────────────────────────────────

    [Fact]
    public async Task Create_with_non_positive_quantity_returns_400()
    {
        var client = ClientAs(Investor);

        var create = new CreateHoldingRequest(
            AssetType.Fund, "Sıfır Fon", null, CurrencyCode.TRY, "adet",
            new TransactionRequest(TransactionType.Buy, 0m, 10m));
        var resp = await client.PostAsJsonAsync("/api/holdings", create, Json);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var error = doc.RootElement.GetProperty("error");
        error.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        error.GetProperty("details")[0].GetProperty("field").GetString().Should().Be("quantity");
    }

    [Fact]
    public async Task Create_with_empty_name_returns_400_validation()
    {
        var client = ClientAs(Investor);

        var create = new CreateHoldingRequest(
            AssetType.Fund, "", null, CurrencyCode.TRY, "adet",
            new TransactionRequest(TransactionType.Buy, 1m, 10m));
        var resp = await client.PostAsJsonAsync("/api/holdings", create, Json);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(resp)).Should().Be("VALIDATION_ERROR");
    }

    // ── SC-13: IDOR — başkasının kaydı 404 ───────────────────────────────────

    [Fact]
    public async Task Idor_other_users_holding_is_404()
    {
        // Admin'in hiç pozisyonu yok; yatırımcının altın holding id'siyle erişmeye çalışır.
        var admin = ClientAs(Admin);

        var resp = await admin.GetAsync($"/api/holdings/{GoldHolding}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound); // varlığı bile sızdırma
    }

    [Fact]
    public async Task Other_user_sees_empty_list_and_zero_summary()
    {
        var admin = ClientAs(Admin);

        var holdings = await admin.GetFromJsonAsync<List<HoldingDto>>("/api/holdings", Json);
        holdings.Should().BeEmpty();

        var summary = await admin.GetFromJsonAsync<PortfolioSummaryDto>("/api/portfolio/summary", Json);
        summary!.TotalValue.Should().Be(0m);
        summary.TotalCost.Should().Be(0m);
        summary.Allocation.Should().BeEmpty();
    }

    [Fact]
    public async Task Idor_cannot_delete_other_users_holding()
    {
        var admin = ClientAs(Admin);

        var resp = await admin.DeleteAsync($"/api/holdings/{BesHolding}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage resp)
    {
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("error").GetProperty("code").GetString();
    }
}
