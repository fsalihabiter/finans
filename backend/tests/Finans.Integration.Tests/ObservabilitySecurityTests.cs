using System.Net;
using System.Text.Json;
using FluentAssertions;
using Finans.Api.ErrorHandling;
using Finans.Api.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Core;
using Serilog.Events;

namespace Finans.Integration.Tests;

/// <summary>
/// T0.12/T0.13 kapıları: liveness health, CorrelationId, hata maskeleme (stack
/// sızmaz — 11 §4), log redaksiyonu (12 §3).
/// </summary>
public sealed class ObservabilitySecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ObservabilitySecurityTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Liveness_health_returns_ok_without_db()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Response_carries_generated_correlation_id()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");
        response.Headers.Should().ContainKey(CorrelationIdMiddleware.HeaderName);
    }

    [Fact]
    public async Task Provided_correlation_id_is_echoed_back()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "test-correlation-123");

        var response = await client.SendAsync(request);

        response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Should().Contain("test-correlation-123");
    }

    [Fact]
    public async Task Exception_handler_masks_internal_details()
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var secret = "GİZLİ stack trace detayı: SECRET-DB-CONNSTR";

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException(secret), default);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        body.Should().Contain(ErrorCodes.Internal);
        body.Should().NotContain(secret); // iç detay/stack istemciye sızmaz
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be(ErrorCodes.Internal);
    }

    [Fact]
    public void Redaction_masks_sensitive_properties_only()
    {
        var policy = new SensitiveDataDestructuringPolicy();
        var sample = new { DisplayName = "Yatırımcı", PasswordHash = "argon2-hash", Email = "x@y.z" };

        var handled = policy.TryDestructure(sample, new StubFactory(), out var result);

        handled.Should().BeTrue();
        var structure = (StructureValue)result!;
        var props = structure.Properties.ToDictionary(p => p.Name, p => p.Value.ToString());
        props["PasswordHash"].Should().Contain("***");
        props["Email"].Should().Contain("***");
        props["DisplayName"].Should().Contain("Yatırımcı"); // hassas değil → korunur
    }

    [Fact]
    public void Redaction_ignores_objects_without_sensitive_fields()
    {
        var policy = new SensitiveDataDestructuringPolicy();
        var handled = policy.TryDestructure(new { Amount = 100, Name = "Altın" }, new StubFactory(), out _);
        handled.Should().BeFalse(); // hassas alan yoksa varsayılana bırakır
    }

    private sealed class StubFactory : ILogEventPropertyValueFactory
    {
        public LogEventPropertyValue CreatePropertyValue(object? value, bool destructureObjects) =>
            new ScalarValue(value);
    }
}
