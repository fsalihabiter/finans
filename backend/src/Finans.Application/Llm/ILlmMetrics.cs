namespace Finans.Application.Llm;

/// <summary>
/// LLM kullanım/maliyet metriği portu (T3.9 — 12 §4, 10 §7). Infrastructure'da
/// <c>System.Diagnostics.Metrics.Meter</c> ile uygulanır → OTel/Prometheus → Grafana bütçe alarmı.
/// Amaç: LLM çağrısı pahalı bir dış bağımlılık; çağrı/token hacmi ve yorum kaynağı dağılımı
/// (cache ne kadar işe yarıyor) görünür olmalı (12 §1 "yeni dış bağımlılıkta metrik").
/// </summary>
public interface ILlmMetrics
{
    /// <summary>
    /// Gerçek bir LLM çağrısı tamamlandığında (iç servis) çağrılır. <paramref name="inputTokens"/>/
    /// <paramref name="outputTokens"/> maliyet metriği; <paramref name="guardBlocked"/> çıktı güvenlik
    /// filtresiyle (T3.5) düşürülen kart sayısı.
    /// </summary>
    void RecordCall(bool success, int inputTokens, int outputTokens, int guardBlocked);

    /// <summary>
    /// İstek başına kullanıcıya ne sunulduğunu kaydeder (dekoratör): <c>"llm"</c> (taze) | <c>"cache"</c>
    /// (aynı portföy, 24s) | <c>"cache_last"</c> (LLM başarısız → son başarılı) | <c>"fallback"</c>
    /// (düz metin). Cache'in çağrıları ne kadar azalttığını gösterir.
    /// </summary>
    void RecordServed(string source);
}

/// <summary>
/// Hiçbir şey kaydetmeyen varsayılan (test/dev güvenli — <c>NullLogger</c> muadili). LLM metriği
/// yapılandırılmamış bağlamlarda servis yine çalışır.
/// </summary>
public sealed class NoopLlmMetrics : ILlmMetrics
{
    public static readonly NoopLlmMetrics Instance = new();
    private NoopLlmMetrics() { }
    public void RecordCall(bool success, int inputTokens, int outputTokens, int guardBlocked) { }
    public void RecordServed(string source) { }
}
