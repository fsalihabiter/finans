namespace Finans.Api.ErrorHandling;

/// <summary>Tüm endpoint'lerin ortak hata gövdesi (04 §2). İstemciye stack trace gitmez.</summary>
public sealed record ApiErrorEnvelope(ApiError Error);

public sealed record ApiError(string Code, string Message, IReadOnlyList<ApiErrorDetail>? Details = null);

public sealed record ApiErrorDetail(string Field, string Issue);

/// <summary>Sözleşmeli hata kodları (04 §2 tablosu).</summary>
public static class ErrorCodes
{
    public const string Validation = "VALIDATION_ERROR"; // 400
    public const string NotFound = "NOT_FOUND"; // 404
    public const string Conflict = "CONFLICT"; // 409
    public const string Upstream = "UPSTREAM_ERROR"; // 502
    public const string Internal = "INTERNAL_ERROR"; // 500
}
