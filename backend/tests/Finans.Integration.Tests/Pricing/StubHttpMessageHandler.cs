using System.Net;
using System.Text;

namespace Finans.Integration.Tests.Pricing;

/// <summary>
/// Ağ olmadan, isteğe göre hazır yanıt döndüren test handler'ı. Fiyat sağlayıcı
/// ayrıştırma/yönlendirme birim testleri için (dış HTTP yok).
/// </summary>
internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(responder(request));

    /// <summary>JSON gövdeli yanıt üretir.</summary>
    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    /// <summary>İstekten bağımsız hep aynı JSON'ı döndüren handler.</summary>
    public static StubHttpMessageHandler Always(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(_ => Json(json, status));
}

/// <summary>Testlerde sabit "şimdi" döndüren saat (Truncgil çekim anı doğrulaması için).</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
