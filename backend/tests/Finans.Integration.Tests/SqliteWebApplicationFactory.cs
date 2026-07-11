using Finans.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Finans.Integration.Tests;

/// <summary>
/// Integration test fixture'ı (T0.11, 09 §2): gerçek HTTP boru hattı + **Sqlite
/// in-memory** DB (Npgsql yerine; CI'da Postgres gerektirmez). Bağlantı açık
/// tutulur → in-memory şema test boyunca yaşar. Şema `EnsureCreated` ile kurulur
/// (Npgsql migration'ları Sqlite'a uygulanmaz; model sağlayıcı-duyarlı).
/// </summary>
public sealed class SqliteWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public SqliteWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Açılış migrate/seed'i test'te KAPALI: şemayı testler EnsureCreated ile kurar,
        // seed'i fixture çalıştırır. (Aksi halde Development config'i Npgsql migration'ı
        // Sqlite'a uygulamaya çalışıp host'u çökertir.)
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
        builder.UseSetting("Database:Seed", "false");
        // BES plan catch-up arka plan job'ı testlerde KAPALI: testler kendi durumlarını kurar;
        // arka plan tiki testler sırasında DB'ye yazıp deterministik kurgu bozmasın (T-BES.6b ileri).
        builder.UseSetting("Bes:PlanCatchUp:Enabled", "false");
        // LLM anahtarı testlerde ZORLA boş → NoopLlmClient. Development ortamı User Secrets'ı
        // yüklediği için geliştiricinin makinesinde gerçek anahtar sızıyordu → commentary
        // testleri CANLI OpenRouter'a çıkıp yaşayan sonuca göre kırılıyordu (2026-07-11 tespiti).
        // Testler ağsız ve deterministik olmalı (09 §2).
        builder.UseSetting("Llm:ApiKey", "");
        // Aynı gerekçeyle Finnhub anahtarı da testlerde ZORLA boş → NotConfiguredStockDataProvider
        // (geliştirici makinesindeki olası anahtar canlı çağrıya yol açmasın — T4.2).
        builder.UseSetting("Stocks:ApiKey", "");

        builder.ConfigureTestServices(services =>
        {
            // Uygulamanın Npgsql DbContext kayıtlarını (options + provider yapılandırma
            // delegesi) kaldır; aksi halde "tek sağlayıcı" çakışması olur. Sürümden
            // bağımsız olmak için descriptor'ları ada göre tara.
            var toRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<FinansDbContext>)
                    || d.ServiceType == typeof(DbContextOptions)
                    || d.ServiceType == typeof(FinansDbContext)
                    || (d.ServiceType.FullName?.Contains("DbContextOptionsConfiguration") ?? false))
                .ToList();
            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            services.AddDbContext<FinansDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
