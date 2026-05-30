using FluentAssertions;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Finans.Domain.Portfolio;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Integration.Tests;

/// <summary>
/// FX cache decorator'ı (T1.15): kur sağlayıcı in-memory cache'lenir → TTL içinde DB
/// değişse bile eski (cache'li) değer döner. Kendi fixture'ı (izole DB + singleton cache).
/// </summary>
public sealed class ProviderCacheTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;

    public ProviderCacheTests(SqliteWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Converter_is_cached_within_ttl()
    {
        // 1) İlk çağrı: seed kuru 48 → 2.000 $ = 96.000 ₺ (ve cache'lenir).
        using (var scope1 = _factory.Services.CreateScope())
        {
            var provider = scope1.ServiceProvider.GetRequiredService<IFxRateProvider>();
            var converter = await provider.GetConverterAsync();
            converter.Convert(2000m, CurrencyCode.USD, CurrencyCode.TRY).Should().Be(96000m);
        }

        // 2) DB'ye daha güncel USD→TRY = 50 ekle (AsOf gelecekte → "en güncel" olurdu).
        using (var scope2 = _factory.Services.CreateScope())
        {
            var db = scope2.ServiceProvider.GetRequiredService<FinansDbContext>();
            db.FxRates.Add(new FxRate
            {
                FromCurrency = CurrencyCode.USD,
                ToCurrency = CurrencyCode.TRY,
                Rate = 50m,
                Source = "Manual",
                AsOfUtc = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAtUtc = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            });
            await db.SaveChangesAsync();
        }

        // 3) TTL içinde tekrar: cache hit → hâlâ 48 (96.000), 50 (100.000) DEĞİL.
        using (var scope3 = _factory.Services.CreateScope())
        {
            var provider = scope3.ServiceProvider.GetRequiredService<IFxRateProvider>();
            var converter = await provider.GetConverterAsync();
            converter.Convert(2000m, CurrencyCode.USD, CurrencyCode.TRY).Should().Be(96000m);
        }
    }
}
