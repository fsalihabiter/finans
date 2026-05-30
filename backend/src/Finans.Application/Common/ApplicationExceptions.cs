namespace Finans.Application.Common;

/// <summary>
/// Uygulama (use-case) katmanının bilinen hataları. API katmanı bunları
/// sözleşmeli HTTP yanıtlarına eşler (04 §2): NotFound→404, Validation→400,
/// Conflict→409. Beklenmeyenler GlobalExceptionHandler'da 500 (iç detay sızmaz).
/// </summary>
public abstract class AppException(string message) : Exception(message);

/// <summary>Kayıt yok — VEYA mevcut kullanıcıya ait değil (IDOR: varlığı sızdırma, 11 §3).</summary>
public sealed class NotFoundException(string message = "Kayıt bulunamadı.") : AppException(message);

/// <summary>İş kuralı çakışması (örn. aynı varlıkta ikinci aktif pozisyon).</summary>
public sealed class ConflictException(string message) : AppException(message);

/// <summary>Girdi doğrulama hatası — alan bazında ayrıntı taşır (04 §2 details).</summary>
public sealed class ValidationException(string message, IReadOnlyList<ValidationFailure> failures)
    : AppException(message)
{
    public IReadOnlyList<ValidationFailure> Failures { get; } = failures;

    public ValidationException(string field, string issue, string message)
        : this(message, [new ValidationFailure(field, issue)]) { }
}

/// <summary>Tek bir alan doğrulama hatası (field + makine-okur issue kodu).</summary>
public sealed record ValidationFailure(string Field, string Issue);
