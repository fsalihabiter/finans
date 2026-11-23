using Microsoft.AspNetCore.Hosting;

namespace Finans.Integration.Tests;

/// <summary>
/// Her test host'unun paylaştığı <b>determinizm ve güvenlik</b> ayarları
/// (INC-001 düzeltmesi, REVIEW-006 sonrası).
/// <para>
/// Neden ayrı bir yere çıkarıldı: bu beş ayar önceden yalnız
/// <see cref="SqliteWebApplicationFactory"/> içinde duruyordu. Düz
/// <c>WebApplicationFactory&lt;Program&gt;</c> kullanan dört test (HealthEndpointTests 1 +
/// ObservabilitySecurityTests 3) bunlardan habersizdi ve <b>açılışta çöküyordu</b>:
/// test host'u Development ortamında koşuyor, <c>appsettings.Development.json</c>
/// <c>Database:ApplyMigrationsOnStartup: true</c> diyor, <c>Program</c> da
/// <c>app.Run()</c>'dan ÖNCE <c>MigrateAsync</c> çağırıyor. Yerel PostgreSQL kapalıyken
/// bu patlıyor, <c>Program</c>'ın <c>catch</c> bloğu hatayı <c>Log.Fatal</c>'a yazıp
/// yutuyor, host hiç kurulamıyor ve test "The server has not been started" alıyor.
/// Belirti bir ortam hatası gibi göründüğü için INC-001 iki aydan uzun açık kaldı.
/// </para>
/// <para>
/// Ayarlar tek yerde durduğu için yeni bir fabrika yazan kişi aynı tuzağa düşmez:
/// <see cref="ApplyTestDefaults"/> çağrılır ve biter. CAPABILITIES.md'nin
/// Reuse → Extend → <b>Extract</b> → New merdiveninde bu "Extract" kalemiydi.
/// </para>
/// </summary>
internal static class TestHostDefaults
{
    /// <summary>
    /// Testlerin DB'siz, ağsız ve deterministik koşmasını sağlayan ayarlar.
    /// Veritabanı <i>sağlayıcısını</i> değiştirmez — onu çağıran fabrika seçer.
    /// </summary>
    public static IWebHostBuilder ApplyTestDefaults(this IWebHostBuilder builder)
    {
        // Açılış migrate/seed'i test'te KAPALI. Şemayı kuran taraf testin kendisi
        // (EnsureCreated) ya da fixture; Npgsql migration'ları Sqlite'a uygulanmaz.
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
        builder.UseSetting("Database:Seed", "false");

        // BES plan catch-up arka plan job'ı KAPALI: arka plan tiki testler sırasında
        // DB'ye yazıp deterministik kurguyu bozmasın (T-BES.6b ileri).
        builder.UseSetting("Bes:PlanCatchUp:Enabled", "false");

        // LLM anahtarı ZORLA boş → NoopLlmClient. Development ortamı User Secrets
        // yüklediği için geliştiricinin makinesinde gerçek anahtar sızıyordu ve
        // commentary testleri CANLI OpenRouter'a çıkıyordu (2026-07-11 tespiti).
        builder.UseSetting("Llm:ApiKey", "");

        // Aynı gerekçeyle Finnhub anahtarı da ZORLA boş →
        // NotConfiguredStockDataProvider (T4.2).
        builder.UseSetting("Stocks:ApiKey", "");

        return builder;
    }
}
