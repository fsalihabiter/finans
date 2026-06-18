using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Finans.Api.Auth;
using Finans.Api.ErrorHandling;
using Finans.Api.Observability;
using Finans.Application.Common;
using Finans.Infrastructure;
using Finans.Infrastructure.Caching;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Pricing;
using Finans.Infrastructure.Seed;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Seq;

// Config okunmadan önceki başlatma hataları da loglanabilsin diye bootstrap logger.
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog: yapılandırılmış log. Console her zaman; Seq opsiyonel (Serilog:Seq:ServerUrl verilmişse).
    // Redaksiyon politikası + CorrelationId (middleware'den LogContext) — 12 §3.
    var seqUrl = builder.Configuration["Serilog:Seq:ServerUrl"];
    builder.Services.AddSerilog((services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Destructure.With<SensitiveDataDestructuringPolicy>()
            .WriteTo.Console();
        if (!string.IsNullOrWhiteSpace(seqUrl))
            configuration.WriteTo.Seq(seqUrl,
                apiKey: builder.Configuration["Serilog:Seq:ApiKey"],
                restrictedToMinimumLevel: LogEventLevel.Information);
    });

    // ── OpenTelemetry Metrik (T2.8 / 12 §4) ────────────────────────────────────
    // RED (AspNetCore), bağımlılık (HttpClient), runtime (GC/CPU/Thread), + custom Meter
    // (Finans.Cache hit/miss). Exporter: Prometheus `/metrics` (yığında Prometheus bunu scrape eder).
    // Servis adı, dashboard'larda `service_name` etiketi olarak görünür.
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService(
            serviceName: "finans-api",
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "dev"))
        .WithMetrics(m => m
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(CacheMetrics.MeterName)
            .AddMeter(Finans.Infrastructure.Llm.LlmMetrics.MeterName) // T3.9: LLM çağrı/maliyet metriği
            .AddPrometheusExporter());

    // Enum'lar JSON'da string (allow-list adlarıyla, 04 §1: "Gold"/"TRY"). camelCase varsayılan.
    builder.Services.AddControllers()
        .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddOpenApi();

    // DataAnnotations model hatası → sözleşmeli ApiError zarfı (04 §2), ProblemDetails değil.
    builder.Services.Configure<ApiBehaviorOptions>(options =>
        options.InvalidModelStateResponseFactory = context =>
        {
            var details = context.ModelState
                .Where(kv => kv.Value?.Errors.Count > 0)
                .SelectMany(kv => kv.Value!.Errors.Select(e =>
                    new ApiErrorDetail(ToCamel(kv.Key), e.ErrorMessage)))
                .ToList();

            var envelope = new ApiErrorEnvelope(new ApiError(
                ErrorCodes.Validation, "Girdi doğrulama hatası.", details));
            return new BadRequestObjectResult(envelope);
        });

    // Geçerli kullanıcı (Faz 1: X-User-Id başlığı / dev varsayılanı; Faz 5: JWT) — 11 §3.
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

    // Hata maskeleme: bilinen app hataları (404/400/409) ÖNCE, sonra genel 500 (11 §4, 04 §2).
    builder.Services.AddExceptionHandler<AppExceptionHandler>();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Veri katmanı (EF Core + Npgsql). Bağlantı dizesi env/User Secrets'tan gelebilir.
    var connectionString = builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("ConnectionStrings:Postgres yapılandırılmamış.");
    builder.Services.AddInfrastructure(connectionString,
        pricing => builder.Configuration.GetSection(PricingOptions.SectionName).Bind(pricing),
        builder.Configuration.GetConnectionString("Redis"),
        builder.Configuration);

    // Health: /health (liveness) + /health/ready (DB erişilebilir mi) — 12 §8.
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<FinansDbContext>("database", tags: ["ready"]);

    // CORS allow-list (yalnızca bilinen web origin'leri; * YOK — 11 §5).
    const string corsPolicy = "WebApp";
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options => options.AddPolicy(corsPolicy, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

    // Reverse proxy (Caddy) arkasında gerçek client IP/protokolünü görmek için (rate limit + log).
    // Compose default bridge'inde Caddy IP'si runtime'da bilinmediği için known network'leri açtık;
    // production'da spesifik known proxy/network kısıtlanır (11 §5).
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear(); // ASPDEPR005: KnownNetworks yerine KnownIPNetworks
        options.KnownProxies.Clear();
    });

    // ── Rate limiting (T2.9 — 11 §5, 10 §5) ────────────────────────────────────
    // Bölümlendirme: kullanıcı kimliği varsa onu, yoksa IP. Aynı NAT arkasındaki çoklu
    // kullanıcılar haksız 429 almasın (kimlikli yol daha doğru); henüz JWT yok, X-User-Id proxy.
    // Caddy ön katmandaki kaba (global IP) limit; burada **endpoint başı** ince ayar yapılır.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, ct) =>
        {
            // Sözleşmeli ApiError zarfı (04 §2 — diğer hatalarla simetri). Retry-After header.
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)retry.TotalSeconds).ToString(CultureInfo.InvariantCulture);

            var envelope = new ApiErrorEnvelope(new ApiError(
                "RATE_LIMIT_EXCEEDED",
                "Çok fazla istek. Lütfen biraz bekleyin.",
                Array.Empty<ApiErrorDetail>()));
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsync(
                JsonSerializer.Serialize(envelope, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                }), ct);
        };

        // Global limiter: tüm istekler partition başına (sliding window, daha smooth).
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var key = ResolvePartitionKey(httpContext);
            return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 120,        // dakikada 120 istek (genel kullanıcı tavanı)
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,    // 10sn'lik 6 dilim — kayan pencere yumuşatma
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            });
        });

        // "prices" politikası: fiyat tazeleme dış API çağırır + 10dk cache var → düşük tavan yeter.
        options.AddPolicy("prices", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(ResolvePartitionKey(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));

        // "nudges": eğitici notlar — daha az pahalı ama yine sınırlı.
        options.AddPolicy("nudges", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(ResolvePartitionKey(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));

        // "commentary" (T3.7): LLM çağrısı pahalı (token maliyeti + gecikme). T3.6 cache disiplini
        // şart; rate limit kötü aktöre/yanlış UI döngüsüne karşı son katman.
        options.AddPolicy("commentary", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(ResolvePartitionKey(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
    });

    var app = builder.Build();

    // `dotnet run -- seed`: migration uygula + idempotent seed çalıştır, sonra çık.
    if (args.Contains("seed"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.MigrateAsync();
        await SeedData.SeedAsync(db);
        return;
    }

    // Compose/dev kolaylığı: bayrak açıksa başlangıçta migration (+ ops. seed) uygula.
    // Varsayılan kapalı → testler (WebApplicationFactory) DB'siz koşar. Prod'da kapalı tut.
    if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.MigrateAsync();
        if (app.Configuration.GetValue<bool>("Database:Seed"))
            await SeedData.SeedAsync(db);
    }

    // Reverse proxy başlıkları (X-Forwarded-*) — UseRouting/UseAuthorization'dan ÖNCE gelmeli ki
    // rate limit ve log'lar gerçek client IP'sini görsün (11 §5).
    if (app.Configuration.GetValue<bool>("Security:ForwardedHeaders"))
        app.UseForwardedHeaders();

    // Boru hattı: hata yakalama (en dış) → korelasyon → istek log'u.
    app.UseExceptionHandler();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
        app.MapOpenApi();

    // Container'da API düz HTTP servis eder (TLS reverse proxy'de — 11 §5);
    // bayrakla kapatılabilir. Lokal `dotnet run` (https profili) için varsayılan açık.
    if (app.Configuration.GetValue("Security:UseHttpsRedirection", true))
        app.UseHttpsRedirection();

    app.UseCors(corsPolicy);
    // Rate limit: CORS'tan SONRA (preflight'lar limit'e takılmasın), MapControllers'tan ÖNCE.
    app.UseRateLimiter();
    app.UseAuthorization();

    app.MapControllers();

    // Health endpoint'leri rate limit DIŞINDA — uptime izleme + orchestration probe'ları (11 §5).
    app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false })
        .DisableRateLimiting();
    app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") })
        .DisableRateLimiting();

    // Prometheus scrape endpoint (T2.8). Rate limit dışında: kendi metriklerimizi kendi limitimizle
    // kesemeyiz. Compose'da iç ağda kalır (Caddy `/metrics`'i dışarı vermez — admin-only, 11 §5).
    app.MapPrometheusScrapingEndpoint().DisableRateLimiting();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Uygulama başlatılamadı");
}
finally
{
    Log.CloseAndFlush();
}

// Model-state alan adını sözleşmeli camelCase'e çevirir (örn. "Transaction.Quantity" → "transaction.quantity").
static string ToCamel(string key) =>
    string.IsNullOrEmpty(key)
        ? key
        : string.Join('.', key.TrimStart('$', '.').Split('.')
            .Select(seg => seg.Length == 0 ? seg : char.ToLowerInvariant(seg[0]) + seg[1..]));

/// <summary>
/// Rate limit partition anahtarı (T2.9): kimlik biliniyorsa kullanıcı, yoksa IP. Paylaşılan NAT
/// arkasındaki farklı kullanıcılar haksız 429 almasın diye user-bazlı tercih edilir (Faz 5 JWT
/// gelince burası daha güvenli olur — şimdi X-User-Id proxy). 11 §5 + 10 §5.
/// </summary>
static string ResolvePartitionKey(HttpContext ctx)
{
    if (ctx.Request.Headers.TryGetValue(HttpCurrentUser.UserHeader, out var u) &&
        !string.IsNullOrWhiteSpace(u))
        return $"user:{u}";
    return $"ip:{ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}

/// <summary>
/// Integration testlerinin (WebApplicationFactory) erişebilmesi için açılan
/// kısmi sınıf. Top-level statement'ların ürettiği Program tipini public yapar.
/// </summary>
public partial class Program;
