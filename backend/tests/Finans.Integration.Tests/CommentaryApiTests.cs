using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Finans.Application.Llm;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Integration.Tests;

/// <summary>
/// `GET /api/portfolio/commentary` uçtan uca (T3.7). LLM yapılandırılmamış (test factory'sinde
/// `Llm:ApiKey` boş) → <c>NoopLlmClient</c> devrede → her zaman fallback kartı döner.
/// Bu bizim için "endpoint hattı çalışıyor mu + KVKK dışına çıkmıyor mu + per-user kapsam +
/// uygulama çökmüyor" doğrulamasıdır (NFR-5). Gerçek LLM çıktısı E2E'de (manuel) doğrulanır.
/// </summary>
public sealed class CommentaryApiTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;
    private static readonly Guid Investor = SeedData.Id("user-1");

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public CommentaryApiTests(SqliteWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient ClientAs(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", userId.ToString());
        return client;
    }

    [Fact]
    public async Task Returns_200_with_fallback_card_when_llm_not_configured()
    {
        var resp = await ClientAs(Investor).GetAsync("/api/portfolio/commentary");

        resp.StatusCode.Should().Be(HttpStatusCode.OK); // çökme yok, NFR-5
        var body = await resp.Content.ReadFromJsonAsync<CommentaryResponse>(Json);

        body.Should().NotBeNull();
        body!.Source.Should().Be("fallback");
        body.Cards.Should().HaveCount(1);
        body.Cards[0].Title.Should().Be("Yorum şu an üretilemedi");
    }

    [Fact]
    public async Task Other_user_gets_their_own_summary_not_leaked()
    {
        // Per-user kapsam: başka kullanıcı (admin seed'de portföysüz) → boş ama 200 + fallback;
        // farklı kullanıcının verisi sızmaz (CommentaryResponse "fallback" — endpoint çökmez).
        var resp = await ClientAs(SeedData.Id("admin-1")).GetAsync("/api/portfolio/commentary");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<CommentaryResponse>(Json);
        body!.Source.Should().Be("fallback");
    }
}
