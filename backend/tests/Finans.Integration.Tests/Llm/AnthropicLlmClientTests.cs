using System.Net;
using FluentAssertions;
using Finans.Application.Llm;
using Finans.Infrastructure.Llm;
using Finans.Integration.Tests.Pricing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Finans.Integration.Tests.Llm;

/// <summary>
/// <see cref="AnthropicLlmClient"/> HTTP davranışı (T3.1) — stub <see cref="StubHttpMessageHandler"/>
/// ile ağsız: header'lar (api key + sürüm), text yanıt parse, tool-use JSON parse, hata akışı.
/// </summary>
public class AnthropicLlmClientTests
{
    private static AnthropicLlmClient Create(Func<HttpRequestMessage, HttpResponseMessage> responder,
        string apiKey = "test-key")
    {
        var options = Options.Create(new LlmOptions
        {
            Provider = "Anthropic",
            ApiKey = apiKey,
            Model = "claude-test",
            BaseUrl = "https://api.anthropic.test/",
        });
        var http = new HttpClient(new StubHttpMessageHandler(responder))
        {
            BaseAddress = new Uri(options.Value.BaseUrl),
        };
        return new AnthropicLlmClient(http, options, NullLogger<AnthropicLlmClient>.Instance);
    }

    [Fact]
    public async Task Returns_fail_when_api_key_missing()
    {
        var client = Create(_ => throw new InvalidOperationException("Ağa çıkmamalı"), apiKey: "");
        var r = await client.CompleteAsync(new LlmRequest("sys", "user"));

        r.Success.Should().BeFalse();
        r.ErrorReason.Should().Be("llm_not_configured");
    }

    [Fact]
    public async Task Sends_api_key_and_version_headers_and_parses_text_response()
    {
        HttpRequestMessage? captured = null;
        var client = Create(req =>
        {
            captured = req;
            return StubHttpMessageHandler.Json("""
                {
                  "content": [{ "type": "text", "text": "Portföyün %42'si altında." }],
                  "usage": { "input_tokens": 100, "output_tokens": 25 }
                }
                """);
        });

        var r = await client.CompleteAsync(new LlmRequest("sys", "Portföyümü açıkla."));

        r.Success.Should().BeTrue();
        r.Text.Should().Contain("Portföyün");
        r.InputTokens.Should().Be(100);
        r.OutputTokens.Should().Be(25);

        captured.Should().NotBeNull();
        captured!.RequestUri!.AbsolutePath.Should().EndWith("v1/messages");
        captured.Headers.GetValues("x-api-key").Should().ContainSingle().Which.Should().Be("test-key");
        captured.Headers.GetValues("anthropic-version").Should().ContainSingle().Which.Should().Be("2023-06-01");
    }

    [Fact]
    public async Task Parses_tool_use_input_as_structured_json_when_schema_provided()
    {
        var client = Create(_ => StubHttpMessageHandler.Json("""
            {
              "content": [
                { "type": "tool_use", "name": "structured_output",
                  "input": { "cards": [{ "title": "Test", "body": "merhaba" }] } }
              ],
              "usage": { "input_tokens": 50, "output_tokens": 12 }
            }
            """));

        var schema = """{"type":"object","properties":{"cards":{"type":"array"}}}""";
        var r = await client.CompleteAsync(new LlmRequest("sys", "user", JsonSchema: schema));

        r.Success.Should().BeTrue();
        r.Text.Should().Contain("\"cards\"");
        r.Text.Should().Contain("merhaba");
    }

    [Fact]
    public async Task Returns_fail_on_http_error_without_throwing()
    {
        var client = Create(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));

        var r = await client.CompleteAsync(new LlmRequest("sys", "user"));

        r.Success.Should().BeFalse();
        r.ErrorReason.Should().Be("http_502");
    }
}
