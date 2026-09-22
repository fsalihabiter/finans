using FluentAssertions;
using Finans.Infrastructure.Persistence;
using Npgsql;

namespace Finans.Integration.Tests;

/// <summary>
/// SC-D1: Barındırıcı (Render) bağlantıyı URI olarak verir → API Npgsql biçimine çevirip
/// başlar; yerel anahtar=değer dizesi değişmeden geçer.
/// </summary>
public sealed class PostgresConnectionStringTests
{
    [Fact]
    public void Uri_is_converted_to_npgsql_format()
    {
        var result = PostgresConnectionString.Normalize(
            "postgresql://finans_user:p%40ss:w0rd@dpg-abc123-a:5433/finans_db");

        var b = new NpgsqlConnectionStringBuilder(result);
        b.Host.Should().Be("dpg-abc123-a");
        b.Port.Should().Be(5433);
        b.Database.Should().Be("finans_db");
        b.Username.Should().Be("finans_user");
        b.Password.Should().Be("p@ss:w0rd");
    }

    [Fact]
    public void Uri_without_port_defaults_to_5432_and_keeps_sslmode()
    {
        var result = PostgresConnectionString.Normalize(
            "postgres://u:p@host.render.com/db?sslmode=require");

        var b = new NpgsqlConnectionStringBuilder(result);
        b.Port.Should().Be(5432);
        b.SslMode.Should().Be(SslMode.Require);
    }

    [Fact]
    public void Key_value_string_is_returned_unchanged()
    {
        const string cs = "Host=localhost;Port=5432;Database=finans;Username=finans";

        PostgresConnectionString.Normalize(cs).Should().Be(cs);
    }
}
