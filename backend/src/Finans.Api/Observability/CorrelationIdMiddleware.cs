using Serilog.Context;

namespace Finans.Api.Observability;

/// <summary>
/// Her isteğe bir CorrelationId atar (gelen `X-Correlation-ID` varsa kullanır,
/// yoksa üretir), Serilog LogContext'e ekler ve yanıt başlığına yazar — bir
/// isteğin tüm log'ları ilişkilendirilebilsin (12 §3).
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task Invoke(HttpContext context)
    {
        var correlationId =
            context.Request.Headers.TryGetValue(HeaderName, out var incoming) && !string.IsNullOrWhiteSpace(incoming)
                ? incoming.ToString()
                : Guid.CreateVersion7().ToString();

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
