using Npgsql;

namespace Finans.Infrastructure.Persistence;

/// <summary>
/// Barındırıcıların (Render, Railway, Heroku) verdiği URI biçimli bağlantı adresini
/// (<c>postgresql://user:pass@host:5432/db</c>) Npgsql'in anahtar=değer biçimine çevirir.
/// Zaten anahtar=değer biçimindeyse olduğu gibi döner (yerel dev / compose değişmez).
/// </summary>
public static class PostgresConnectionString
{
    public static string Normalize(string connectionString)
    {
        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return connectionString;

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
        };
        if (userInfo.Length > 1)
            builder.Password = Uri.UnescapeDataString(userInfo[1]);

        // ?sslmode=require gibi sorgu parametreleri (Render dış adresi) korunur.
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length == 2 && kv[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                builder.SslMode = Enum.Parse<SslMode>(kv[1], ignoreCase: true);
        }

        return builder.ConnectionString;
    }
}
