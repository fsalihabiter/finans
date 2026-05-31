using System.Text.Json.Serialization;
using Finans.Api.Auth;
using Finans.Api.ErrorHandling;
using Finans.Api.Observability;
using Finans.Application.Common;
using Finans.Infrastructure;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Pricing;
using Finans.Infrastructure.Seed;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Config okunmadan önceki başlatma hataları da loglanabilsin diye bootstrap logger.
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog: yapılandırılmış log (Console; Faz 2'de Seq eklenir). Redaksiyon
    // politikası + CorrelationId (middleware'den LogContext) — 12 §3.
    builder.Services.AddSerilog((services, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Destructure.With<SensitiveDataDestructuringPolicy>()
        .WriteTo.Console());

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
        builder.Configuration.GetConnectionString("Redis"));

    // Health: /health (liveness) + /health/ready (DB erişilebilir mi) — 12 §8.
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<FinansDbContext>("database", tags: ["ready"]);

    // CORS allow-list (yalnızca bilinen web origin'leri; * YOK — 11 §5).
    const string corsPolicy = "WebApp";
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options => options.AddPolicy(corsPolicy, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

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
    app.UseAuthorization();

    app.MapControllers();

    app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

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
/// Integration testlerinin (WebApplicationFactory) erişebilmesi için açılan
/// kısmi sınıf. Top-level statement'ların ürettiği Program tipini public yapar.
/// </summary>
public partial class Program;
