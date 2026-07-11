using Finans.Application.Common;
using Finans.Application.Llm;
using Finans.Application.Pricing;
using Finans.Application.Portfolio;
using Finans.Infrastructure.Caching;
using Finans.Infrastructure.Llm;
using Finans.Infrastructure.Persistence;
using Finans.Application.Stocks;
using Finans.Infrastructure.Pricing;
using Finans.Infrastructure.Services;
using Finans.Infrastructure.Stocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Finans.Infrastructure;

/// <summary>Infrastructure katmanının DI kaydı (02 §2.1).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        Action<PricingOptions>? configurePricing = null,
        string? redisConnectionString = null,
        IConfiguration? configuration = null)
    {
        services.AddDbContext<FinansDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Dağıtık cache (T2.7, 10 §3-4): Redis (yapılandırılmışsa) ya da in-memory (yerel dev —
        // Redis kurulu değil). IAppCache single-flight (stampede koruması) + hit/miss metriği
        // sağlar; FX/enflasyon/fiyat decorator'ları bunu kullanır.
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
            services.AddStackExchangeRedisCache(o => o.Configuration = redisConnectionString);
        else
            services.AddDistributedMemoryCache();
        services.AddSingleton<CacheMetrics>();
        services.AddSingleton<IAppCache, DistributedAppCache>();

        // Kur/enflasyon sağlayıcılar DbContext'e bağlı → scoped; cache decorator'ı (IAppCache)
        // ile sarılır. IAppCache singleton, decorator scoped.
        services.AddScoped<EfFxRateProvider>();
        services.AddScoped<IFxRateProvider, CachedFxRateProvider>();
        services.AddScoped<EfInflationRateProvider>();
        services.AddScoped<IInflationRateProvider, CachedInflationRateProvider>();
        services.AddSingleton<PortfolioCalculationService>();

        // Use-case servisleri (DbContext + ICurrentUser'a bağlı) → scoped.
        services.AddScoped<BesPlanCatchUpRunner>();
        services.AddScoped<IHoldingService, HoldingService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<ISettingsService, SettingsService>();

        // Arka plan job (T-BES.6b ileri): aktif BES planlarını periyodik ilerletir. Konfig
        // `Bes:PlanCatchUp:Enabled` (varsayılan true; test fixture'ı false yapar).
        if (configuration is not null)
            services.Configure<BesPlanCatchUpOptions>(configuration.GetSection(BesPlanCatchUpOptions.SectionName));
        services.AddHostedService<BesPlanCatchUpHostedService>();

        // Eğitici notlar (T2.5): saf kural motoru (singleton) + per-user servis (scoped).
        services.AddSingleton<NudgeRuleEngine>();
        services.AddScoped<INudgeService, NudgeService>();

        // Fiyat sağlayıcılar (Faz 2, T2.1): anahtarsız dış kaynaklar → typed HttpClient.
        // Frankfurter = döviz (ECB), Truncgil = gram altın. Üst katman (T2.2) IEnumerable
        // ile çözüp CanQuote'a göre yönlendirir; cache/fallback orada.
        var pricing = new PricingOptions();
        configurePricing?.Invoke(pricing);

        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient<FrankfurterPriceProvider>(c =>
        {
            c.BaseAddress = new Uri(pricing.FrankfurterBaseUrl);
            c.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddHttpClient<TruncgilGoldPriceProvider>(c =>
        {
            c.BaseAddress = new Uri(pricing.TruncgilBaseUrl);
            c.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddTransient<IPriceProvider>(sp => sp.GetRequiredService<FrankfurterPriceProvider>());
        services.AddTransient<IPriceProvider>(sp => sp.GetRequiredService<TruncgilGoldPriceProvider>());

        // Orkestrasyon (T2.2): sağlayıcıları yönlendir + cache + snapshot/fxrate/CurrentPrice yaz.
        services.AddScoped<IPriceFetchService, PriceFetchService>();

        // LLM (T3.1, 07 §2): API anahtarı yapılandırılmışsa Anthropic; aksi halde Noop (dev/test
        // güvenli varsayılan — uygulama çökmez, fallback metin döner). Anahtar 11 §6 (env/User Secrets).
        var llm = new LlmOptions();
        configuration?.GetSection(LlmOptions.SectionName).Bind(llm);
        if (configuration is not null)
            services.Configure<LlmOptions>(configuration.GetSection(LlmOptions.SectionName));
        if (!string.IsNullOrWhiteSpace(llm.ApiKey) && llm.Provider.Equals("Anthropic", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<ILlmClient, AnthropicLlmClient>(c =>
            {
                c.BaseAddress = new Uri(llm.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(Math.Max(1, llm.TimeoutSeconds));
            });
        }
        else if (!string.IsNullOrWhiteSpace(llm.ApiKey) && llm.Provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase))
        {
            // OpenRouter dev-friendly: BaseUrl varsayılan farklı; kullanıcı appsettings'te belirtmediyse override.
            var baseUrl = llm.BaseUrl.Contains("anthropic.com", StringComparison.OrdinalIgnoreCase)
                ? "https://openrouter.ai/api/"
                : llm.BaseUrl;
            services.AddHttpClient<ILlmClient, OpenRouterLlmClient>(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                c.Timeout = TimeSpan.FromSeconds(Math.Max(1, llm.TimeoutSeconds));
            });
        }
        else
        {
            services.AddSingleton<ILlmClient, NoopLlmClient>();
        }

        // Hisse verisi (T4.2 — Finnhub, karar T4.1). Anahtar varsa typed HttpClient (token
        // başlıkta → log'a sızmaz); yoksa NotConfigured sağlayıcı → anlamlı 502 (NFR-5).
        // Servis: sembol doğrulama + 1 saat ortak cache + tek-uçuş (60 çağrı/dk kota koruması).
        var stocks = new StockOptions();
        configuration?.GetSection(StockOptions.SectionName).Bind(stocks);
        if (!string.IsNullOrWhiteSpace(stocks.ApiKey))
        {
            services.AddHttpClient<IStockDataProvider, FinnhubStockDataProvider>(c =>
            {
                c.BaseAddress = new Uri(stocks.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(Math.Max(1, stocks.TimeoutSeconds));
                c.DefaultRequestHeaders.Add("X-Finnhub-Token", stocks.ApiKey);
            });
        }
        else
        {
            services.AddSingleton<IStockDataProvider, NotConfiguredStockDataProvider>();
        }
        services.AddScoped<IStockDataService, StockDataService>();
        // T4.3: metrik açıklama — ILlmClient soyutlaması üstünde; sembol bazlı 24s cache
        // + son-başarılı fallback servisin içinde (IAppCache). LLM yoksa Noop → fallback kartı.
        services.AddScoped<ILlmStockExplainService, LlmStockExplainService>();

        // T4.5: fiyat geçmişi — Yahoo chart API (ANAHTARSIZ; halka arzdan bugüne günlük seri).
        // User-Agent şart (botsuz istemci reddedilir); uzun seri (40+ yıl ≈ 1-2 MB JSON) için
        // geniş zaman aşımı; seri 24s cache'lenir.
        services.AddHttpClient<IStockHistoryProvider, YahooStockHistoryProvider>(c =>
        {
            c.BaseAddress = new Uri(stocks.HistoryBaseUrl);
            c.Timeout = TimeSpan.FromSeconds(Math.Max(1, stocks.HistoryTimeoutSeconds));
            c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; Nirengi/1.0)");
        });
        services.AddScoped<IStockHistoryService, StockHistoryService>();

        // T3.9: LLM kullanım/maliyet metriği (Meter "Finans.Llm" → OTel/Prometheus). Singleton.
        services.AddSingleton<ILlmMetrics, LlmMetrics>();

        // T3.3: portföy yorum orkestrasyonu (Application'da, ILlmClient soyutlamasının üstünde).
        // LLM yapılandırılmamışsa NoopLlmClient → her çağrı fallback metin kartı döner (07 §5).
        // T3.6: cache + "son başarılı" fallback dekoratörü (FX/enflasyon/fiyat decorator deseni, T2.7):
        // iç servis üretir, dış servis IAppCache ile portföy-hash'i bazlı cache'ler + son başarılıyı saklar.
        services.AddScoped<LlmCommentaryService>();
        services.AddScoped<ILlmCommentaryService>(sp => new CachedLlmCommentaryService(
            sp.GetRequiredService<LlmCommentaryService>(),
            sp.GetRequiredService<IAppCache>(),
            sp.GetRequiredService<ICurrentUser>(),
            sp.GetRequiredService<ILlmMetrics>(),
            sp.GetRequiredService<ILogger<CachedLlmCommentaryService>>()));

        return services;
    }
}
