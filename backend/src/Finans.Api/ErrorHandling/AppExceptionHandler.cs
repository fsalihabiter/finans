using Finans.Application.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace Finans.Api.ErrorHandling;

/// <summary>
/// Uygulama katmanının bilinen hatalarını (<see cref="AppException"/>) sözleşmeli
/// HTTP yanıtlarına eşler (04 §2): NotFound→404, Validation→400, Conflict→409.
/// <see cref="GlobalExceptionHandler"/>'dan ÖNCE çalışır; eşleşmezse zincir devam
/// eder ve beklenmeyenler 500 olur (iç detay sızmadan, 11 §4).
/// </summary>
public sealed class AppExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, envelope) = exception switch
        {
            ValidationException v => (
                StatusCodes.Status400BadRequest,
                new ApiErrorEnvelope(new ApiError(
                    ErrorCodes.Validation, v.Message,
                    v.Failures.Select(f => new ApiErrorDetail(f.Field, f.Issue)).ToList()))),

            NotFoundException n => (
                StatusCodes.Status404NotFound,
                new ApiErrorEnvelope(new ApiError(ErrorCodes.NotFound, n.Message))),

            ConflictException c => (
                StatusCodes.Status409Conflict,
                new ApiErrorEnvelope(new ApiError(ErrorCodes.Conflict, c.Message))),

            _ => (0, null!),
        };

        if (status == 0)
            return false; // bu handler'a ait değil → zincir devam etsin (GlobalExceptionHandler)

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(envelope, cancellationToken);
        return true;
    }
}
