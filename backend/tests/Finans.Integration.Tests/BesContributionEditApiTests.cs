using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Finans.Application.Portfolio;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Integration.Tests;

/// <summary>
/// BES katkı düzenle/sil + düzenli plan bayrağı (T-BES.6/6b, SC-23). **Kendi izole fixture'ı**;
/// göreli delta doğrular (sıra-bağımsız). Maliyet = kendi katkı (cepten).
/// </summary>
public sealed class BesContributionEditApiTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;
    private static readonly Guid Investor = SeedData.Id("user-1");
    private static readonly Guid BesHolding = SeedData.Id("holding-bes");

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter() } };

    public BesContributionEditApiTests(SqliteWebApplicationFactory factory) => _factory = factory;

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
        var c = _factory.CreateClient();
        c.DefaultRequestHeaders.Add("X-User-Id", Investor.ToString());
        return c;
    }

    [Fact]
    public async Task Edit_then_delete_adjust_cumulative_and_cost()
    {
        var client = Client();
        var march = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc);

        // Manuel katkı (own 1.000, 2026 → devlet %20 = 200).
        var add = await client.PostAsJsonAsync($"/api/holdings/{BesHolding}/bes-contribution",
            new AddBesContributionRequest(1000m, null, march), Json);
        var afterAdd = (await add.Content.ReadFromJsonAsync<HoldingDto>(Json))!;
        var rec = afterAdd.Bes!.Contributions.First(c => c.OwnAmount == 1000m && c.StateAmount == 200m);
        var own0 = afterAdd.Bes.OwnContribution;
        var state0 = afterAdd.Bes.StateContribution;
        afterAdd.AvgCost.Should().Be(own0); // maliyet = kendi katkı (cepten)

        // Düzenle: own 2.000 (2026 → state 400). Delta: own +1.000, state +200.
        var edit = await client.PutAsJsonAsync($"/api/holdings/{BesHolding}/bes/contributions/{rec.Id}",
            new UpdateBesContributionRequest(2000m, march), Json);
        edit.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterEdit = (await edit.Content.ReadFromJsonAsync<HoldingDto>(Json))!;
        afterEdit.Bes!.OwnContribution.Should().Be(own0 + 1000m);
        afterEdit.Bes.StateContribution.Should().Be(state0 + 200m);
        afterEdit.AvgCost.Should().Be(afterEdit.Bes.OwnContribution);

        // Sil: eklenen+düzenlenen kayıt kalkar → eklemeden önceki tabana döner.
        var del = await client.DeleteAsync($"/api/holdings/{BesHolding}/bes/contributions/{rec.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterDel = (await del.Content.ReadFromJsonAsync<HoldingDto>(Json))!;
        afterDel.Bes!.OwnContribution.Should().Be(own0 - 1000m);
        afterDel.Bes.Contributions.Should().NotContain(c => c.Id == rec.Id);
    }

    [Fact]
    public async Task Recurring_flag_activates_plan()
    {
        var client = Client();

        // "Bundan sonraki katkılar için kullan" → plan aktif (bugün tarihli → catch-up bu turda üretmez).
        var resp = await client.PostAsJsonAsync($"/api/holdings/{BesHolding}/bes-contribution",
            new AddBesContributionRequest(1500m, null, DateTime.UtcNow, Recurring: true), Json);
        var dto = (await resp.Content.ReadFromJsonAsync<HoldingDto>(Json))!;

        dto.Bes!.PlanActive.Should().BeTrue();
        dto.Bes.MonthlyAmount.Should().Be(1500m);
    }
}
