using System.Diagnostics.Metrics;
using Finans.Application.Llm;

namespace Finans.Infrastructure.Llm;

/// <summary>
/// <see cref="ILlmMetrics"/>'in <c>System.Diagnostics.Metrics</c> (OTel-uyumlu) uygulaması (T3.9 — 12 §4).
/// Program.cs'te <see cref="MeterName"/> OTel'e eklenir → Prometheus <c>/metrics</c> → Grafana/alarm.
/// Etiketler düşük-kardinalite: <c>result</c> (success/fail), <c>direction</c> (input/output),
/// <c>source</c> (llm/cache/cache_last/fallback). Prometheus karşılığı: <c>finans_llm_calls_total</c>,
/// <c>finans_llm_tokens_total</c>, <c>finans_llm_guard_blocked_total</c>, <c>finans_llm_served_total</c>.
/// </summary>
public sealed class LlmMetrics : ILlmMetrics, IDisposable
{
    public const string MeterName = "Finans.Llm";

    private readonly Meter _meter;
    private readonly Counter<long> _calls;
    private readonly Counter<long> _tokens;
    private readonly Counter<long> _guardBlocked;
    private readonly Counter<long> _served;

    public LlmMetrics()
    {
        _meter = new Meter(MeterName);
        _calls = _meter.CreateCounter<long>(
            "finans.llm.calls", unit: "{call}", description: "LLM çağrı sayısı (success/fail).");
        _tokens = _meter.CreateCounter<long>(
            "finans.llm.tokens", unit: "{token}", description: "LLM token kullanımı (input/output) — maliyet.");
        _guardBlocked = _meter.CreateCounter<long>(
            "finans.llm.guard_blocked", unit: "{card}", description: "Çıktı güvenlik filtresiyle (T3.5) düşürülen kart.");
        _served = _meter.CreateCounter<long>(
            "finans.llm.served", unit: "{response}", description: "Yorum kaynağı dağılımı (llm/cache/cache_last/fallback).");
    }

    public void RecordCall(bool success, int inputTokens, int outputTokens, int guardBlocked)
    {
        _calls.Add(1, new KeyValuePair<string, object?>("result", success ? "success" : "fail"));
        if (inputTokens > 0)
            _tokens.Add(inputTokens, new KeyValuePair<string, object?>("direction", "input"));
        if (outputTokens > 0)
            _tokens.Add(outputTokens, new KeyValuePair<string, object?>("direction", "output"));
        if (guardBlocked > 0)
            _guardBlocked.Add(guardBlocked);
    }

    public void RecordServed(string source) =>
        _served.Add(1, new KeyValuePair<string, object?>("source", source));

    public void Dispose() => _meter.Dispose();
}
