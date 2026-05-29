using Finans.Infrastructure;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Veri katmanı (EF Core + Npgsql). Bağlantı dizesi env/User Secrets'tan gelebilir.
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres yapılandırılmamış.");
builder.Services.AddInfrastructure(connectionString);

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Integration testlerinin (WebApplicationFactory) erişebilmesi için açılan
/// kısmi sınıf. Top-level statement'ların ürettiği Program tipini public yapar.
/// </summary>
public partial class Program;
