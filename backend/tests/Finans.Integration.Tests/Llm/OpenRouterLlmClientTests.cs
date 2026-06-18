using System.Net;
using System.Text.Json;
using FluentAssertions;
using Finans.Application.Llm;
using Finans.Infrastructure.Llm;
using Finans.Integration.Tests.Pricing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Finans.Integration.Tests.Llm;

/// <summary>
/// <see cref="OpenRouterLlmClient"/> HTTP davranışı (OpenAI-uyumlu Chat Completions): Bearer auth +
/// OpenRouter meta header'ları, JSON şema verilince <c>response_format=json_object</c> ve şemanın
/// sistem promptuna eklenmesi, <c>choices[0].message.content</c> parse, hata akışı.
/// </summary>
public class OpenRouterLlmClientTests
{
    private static OpenRouterLlmClient Create(
        Func<HttpRequestMessage, HttpResponseMessage> responder, string apiKey = "test-key")
    {
        var options = Options.Create(new LlmOptions
        {
            Provider = "OpenRouter",
            ApiKey = apiKey,
            Model = "meta-llama/llama-3.3-70b-instruct:free",
            BaseUrl = "https://openrouter.test/api/",
            OpenRouterAppUrl = "https://localhost",
            OpenRouterAppName = "Finans",
        });
        var http = new HttpClient(new StubHttpMessageHandler(responder))
        {
            BaseAddress = new Uri(options.Value.BaseUrl),
        };
        return new OpenRouterLlmClient(http, options, NullLogger<OpenRouterLlmClient>.Instance);
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
    public async Task Sends_bearer_token_and_openrouter_meta_headers_and_parses_text()
    {
        HttpRequestMessage? captured = null;
        var client = Create(req =>
        {
            captured = req;
            return StubHttpMessageHandler.Json("""
                {
                  "choices": [{ "message": { "role": "assistant", "content": "Portföyün %42'si altında." } }],
                  "usage": { "prompt_tokens": 100, "completion_tokens": 25 }
                }
                """);
        });

        var r = await client.CompleteAsync(new LlmRequest("sys", "Portföyümü açıkla."));

        r.Success.Should().BeTrue();
        r.Text.Should().Contain("Portföyün");
        r.InputTokens.Should().Be(100);
        r.OutputTokens.Should().Be(25);

        captured.Should().NotBeNull();
        captured!.RequestUri!.AbsolutePath.Should().EndWith("v1/chat/completions");
        captured.Headers.Authorization!.Scheme.Should().Be("Bearer");
        captured.Headers.Authorization.Parameter.Should().Be("test-key");
        captured.Headers.GetValues("HTTP-Referer").Should().ContainSingle().Which.Should().Be("https://localhost");
        captured.Headers.GetValues("X-Title").Should().ContainSingle().Which.Should().Be("Finans");
    }

    [Fact]
    public async Task Forces_json_object_response_format_and_embeds_schema_into_system_prompt()
    {
        // İçeriği responder İÇİNDE oku: çağrı dönünce OpenRouterLlmClient `using var http` ile
        // request'i (ve Content'ini) dispose eder → sonradan okumak ObjectDisposedException verir.
        string? bodyJson = null;
        var client = Create(req =>
        {
            bodyJson = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return StubHttpMessageHandler.Json("""
                {
                  "choices": [{ "message": { "content": "{\"cards\":[{\"emoji\":\"✅\",\"title\":\"OK\",\"body\":\"x\"}]}" } }],
                  "usage": { "prompt_tokens": 50, "completion_tokens": 12 }
                }
                """);
        });
        var schema = """{"type":"object","properties":{"cards":{"type":"array"}}}""";

        await client.CompleteAsync(new LlmRequest("sys-only", "user", JsonSchema: schema));

        using var doc = JsonDocument.Parse(bodyJson!);
        var root = doc.RootElement;

        // response_format: json_object dayatıldı.
        root.GetProperty("response_format").GetProperty("type").GetString().Should().Be("json_object");
        // Şema sistem promptuna eklendi (görüleceği yer: messages[0].content).
        var sysMessage = root.GetProperty("messages")[0].GetProperty("content").GetString();
        sysMessage.Should().Contain("sys-only");
        sysMessage.Should().Contain("JSON şeması:");
        sysMessage.Should().Contain("\"cards\"");
    }

    [Fact]
    public async Task Returns_fail_on_http_error_without_throwing()
    {
        var client = Create(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var r = await client.CompleteAsync(new LlmRequest("sys", "user"));

        r.Success.Should().BeFalse();
        r.ErrorReason.Should().Be("http_503");
    }

    /// <summary>
    /// Regresyon kapısı: OpenRouter free "reasoning" modelleri (Laguna, Nemotron Super, DeepSeek-R…)
    /// gizli düşünme tokens'ı harcayıp content'i yarım bırakıyordu — biz çağrıda <c>reasoning.exclude=true</c>
    /// ve <c>enabled=false</c> ikilisini gönderiyoruz, böylece destekleyen modellerde reasoning kapanır,
    /// desteklemeyenler alanı yutar (geniş uyum). Bu test alanın gerçekten body'ye yazıldığını korur.
    /// </summary>
    [Fact]
    public async Task Sends_reasoning_exclude_and_disabled_to_neutralize_reasoning_models()
    {
        // İçeriği responder İÇİNDE oku (yukarıdaki testle aynı dispose nedeni).
        string? bodyJson = null;
        var client = Create(req =>
        {
            bodyJson = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return StubHttpMessageHandler.Json("""
                { "choices": [{ "message": { "content": "ok" } }] }
                """);
        });

        await client.CompleteAsync(new LlmRequest("sys", "user"));

        using var doc = JsonDocument.Parse(bodyJson!);
        var reasoning = doc.RootElement.GetProperty("reasoning");
        reasoning.GetProperty("exclude").GetBoolean().Should().BeTrue();
        reasoning.GetProperty("enabled").GetBoolean().Should().BeFalse();
    }
}
