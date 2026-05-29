using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Finans.Infrastructure.Persistence;

/// <summary>
/// `dotnet ef migrations add` / `database update` için tasarım-zamanı bağlam üretici.
/// Bağlantı dizesini `ConnectionStrings__Postgres` ortam değişkeninden okur;
/// migration ÜRETİMİ gerçek bağlantı gerektirmez, parolasız localhost varsayılanı yeter.
/// Sır repoda tutulmaz (CLAUDE.md §13) — gerçek değer env/User Secrets'tan gelir.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FinansDbContext>
{
    public FinansDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=finans;Username=finans";

        var options = new DbContextOptionsBuilder<FinansDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new FinansDbContext(options);
    }
}
