using Finans.Api.ErrorHandling;
using Finans.Api.Observability;
using Finans.Infrastructure;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    // Hata maskeleme: istemciye sözleşmeli hata, stack trace sızmaz (11 §4, 04 §2).
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Veri katmanı (EF Core + Npgsql). Bağlantı dizesi env/User Secrets'tan gelebilir.
    var connectionString = builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("ConnectionStrings:Postgres yapılandırılmamış.");
    builder.Services.AddInfrastructure(connectionString);

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

    // Boru hattı: hata yakalama (en dış) → korelasyon → istek log'u.
    app.UseExceptionHandler();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
        app.MapOpenApi();

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

/// <summary>
/// Integration testlerinin (WebApplicationFactory) erişebilmesi için açılan
/// kısmi sınıf. Top-level statement'ların ürettiği Program tipini public yapar.
/// </summary>
public partial class Program;
