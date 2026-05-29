using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Finans.Integration.Tests;

/// <summary>
/// SC: "Servis ayakta mı?" — GET /api/health 200 ve { status: "ok" } döner (T0.7).
/// WebApplicationFactory ile uçtan uca (gerçek HTTP boru hattı) doğrulama (09 §2-3).
/// </summary>
public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Get_health_returns_ok_status()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthDto>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("ok");
    }

    private sealed record HealthDto(string Status);
}
