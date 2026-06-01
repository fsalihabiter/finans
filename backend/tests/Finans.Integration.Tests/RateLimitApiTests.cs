using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Finans.Application.Common;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Integration.Tests;

/// <summary>
/// T2.9: ASP.NET Core RateLimiter kapısı (11 §5, 10 §5). İki kanıt:
/// (a) sıkı endpoint <c>/api/prices</c> dakikada 10'da kesilir + ApiError zarfıyla 429 döner;
/// (b) <c>/health</c> rate limit DIŞINDADIR (uptime probe'ları kesilmemeli).
///
/// <para>Partition izolasyonu: her test rastgele <c>X-User-Id</c> kullanır → testler birbirinin
/// limit sayacını paylaşmaz (paylaşılan WebApplicationFactory'de bile temiz).</para>
/// </summary>
public sealed class RateLimitApiTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public RateLimitApiTests(SqliteWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient ClientAsRandomUser()
    {
        var client = _factory.CreateClient();
        // Her test/her bağlantı için yeni user-id → partition izolasyonu (counter taze).
        client.DefaultRequestHeaders.Add("X-User-Id", Guid.NewGuid().ToString());
        return client;
    }

    [Fact]
    public async Task Prices_returns_429_with_api_error_after_policy_limit()
    {
        var client = ClientAsRandomUser();

        // İlk 10 istek geçmeli (FixedWindow PermitLimit=10).
        for (var i = 0; i < 10; i++)
        {
            var ok = await client.GetAsync("/api/prices");
            ok.StatusCode.Should().Be(HttpStatusCode.OK, $"istek #{i + 1} cap altında olmalı");
        }

        // 11. istek 429 + sözleşmeli ApiError zarfı.
        var blocked = await client.GetAsync("/api/prices");
        blocked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var envelope = await blocked.Content.ReadFromJsonAsync<ApiErrorEnvelope>(Json);
        envelope!.Error.Code.Should().Be("RATE_LIMIT_EXCEEDED");
        envelope.Error.Message.Should().Contain("Çok fazla istek");
    }

    [Fact]
    public async Task Health_endpoint_is_not_rate_limited()
    {
        var client = ClientAsRandomUser();

        // 150 istek (global limit 120/dk üstü) — hiçbiri 429 olmamalı (health bypass'lı).
        for (var i = 0; i < 150; i++)
        {
            var resp = await client.GetAsync("/health");
            resp.StatusCode.Should().Be(HttpStatusCode.OK,
                $"health istek #{i + 1} rate limit'e takılmamalı");
        }
    }
}
