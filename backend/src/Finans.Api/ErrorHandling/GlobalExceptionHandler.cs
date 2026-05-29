using Microsoft.AspNetCore.Diagnostics;

namespace Finans.Api.ErrorHandling;

/// <summary>
/// Beklenmeyen hataları yakalar; istemciye **iç detay/stack trace SIZDIRMADAN**
/// sözleşmeli `INTERNAL_ERROR` döner (11 §4, 04 §2). Tam ayrıntı (stack) yalnızca
/// sunucu log'una yazılır (CorrelationId ile ilişkilendirilir, 12 §3).
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "İşlenmeyen hata: {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var envelope = new ApiErrorEnvelope(
            new ApiError(ErrorCodes.Internal, "Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin."));

        await httpContext.Response.WriteAsJsonAsync(envelope, cancellationToken);
        return true;
    }
}
