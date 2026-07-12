namespace Finans.Domain.Enums;

// Allow-list enum'ları (03 §2). DB'de varchar olarak saklanır (HasConversion<string>),
// CHECK kısıtıyla sınırlanır. Taşınabilir + okunur.

public enum AssetType
{
    Gold,
    Fx,
    Stock,
    Fund,
    Bes,
    Cash,
    // Gelecek: RealEstate, Crypto
}

public enum TransactionType
{
    Buy,
    Sell,
}

public enum VestingState
{
    NotVested,
    PartiallyVested,
    Vested,
}

/// <summary>
/// Bir BES katkı kaydının (tarihten türetilen) durumu. Saklanmaz; ödeme/devlet-yatma
/// tarihleri ile "bugün" karşılaştırılarak okuma anında hesaplanır (T-BES.8).
/// </summary>
public enum BesContributionStatus
{
    /// <summary>Kendi katkı ödendi ve devlet katkısı da yatırıldı (ödeme ayını izleyen ayın sonu geçti).</summary>
    Deposited,

    /// <summary>Kendi katkı ödendi (tarih geçti) ama devlet katkısı henüz yatmadı (izleyen ay sonu gelmedi).</summary>
    StatePending,

    /// <summary>Ödeme tarihi gelecekte — kendi katkı da henüz ödenmedi (ileriye dönük plan girişi).</summary>
    Future,
}

/// <summary>ISO 4217 alt kümesi (allow-list, 03 §2). @finans/shared CurrencyCode ile birebir.</summary>
public enum CurrencyCode
{
    TRY,
    USD,
    EUR,
}

public enum UserRole
{
    User,
    Admin,
}

public enum AuditAction
{
    Login,
    Logout,
    Create,
    Update,
    Delete,
    AccessDenied,
    Export,
    PasswordChange,
}

public enum AuditResult
{
    Success,
    Denied,
    Failure,
}

// ── Eğitim modülü (03 §C, T5E.1) ─────────────────────────────────────────────

public enum LessonLevel
{
    Beginner,
    Intermediate,
    Advanced,
}

/// <summary>
/// Ders ilerleme durumu. <c>Locked</c> SAKLANMAZ — ön-koşul derslerin
/// tamamlanmışlığından okuma anında TÜRETİLİR (03 §C LessonPrerequisites).
/// </summary>
public enum LessonStatus
{
    NotStarted,
    InProgress,
    Completed,
}

public enum QuizQuestionType
{
    SingleChoice,
    MultipleChoice,
    TrueFalse,
}

// Not: PriceSource (Manual | <providerKey>) açık uçlu olduğu için enum DEĞİL,
// string olarak saklanır (Source kolonları).
