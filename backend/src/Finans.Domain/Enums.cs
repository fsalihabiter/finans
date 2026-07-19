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

// ── Katmanlı içerik derinliği (15 §2, T6.5) ──────────────────────────────────

/// <summary>
/// Ders bölümünün derinlik katmanı (15 §2.2). Kullanıcının
/// <c>LiteracyLevel</c>'ına KADAR olan katmanlar varsayılan açık gelir; alt
/// seviyedeki kullanıcı üst katmanı "Daha derine in" ile açabilir — tavan
/// kapatılmaz.
/// </summary>
public enum DepthTier
{
    /// <summary>Herkes görür. Kavram nedir, neden önemli (~150 kelime).</summary>
    Core,

    /// <summary>Gelişen+. Nasıl hesaplanır, ne zaman yanıltır (~300 kelime).</summary>
    Context,

    /// <summary>İleri. Formül, kenar durum, TR'ye özgü incelik (~400 kelime).</summary>
    Deep,
}

/// <summary>
/// Kullanıcının dalgalanma karşısındaki tutumu (15 §4.2, T6.6).
/// <b>⚠ KULLANICIYA GÖSTERİLMEZ</b> ve hiçbir dağılım/portföy çıktısı üretmez
/// (15 §1.1 — SPK yerindelik testi taklidi DEĞİL). Yalnızca davranış derslerinin
/// sırasını ve metin varyantını sessizce etkiler. Sızıntısı SC-E4 ile test edilir.
/// </summary>
public enum RiskAttitude
{
    Temkinli,
    Dengeli,
    Atilgan,
}

/// <summary>
/// "Senin portföyünde" bağlam bloğunun veri durumu (15 §3.2, T6.2).
/// Üç durum da birinci sınıf: hiçbiri hata değildir, ders asla kilitlenmez.
/// </summary>
public enum LessonContextState
{
    /// <summary>Kullanıcının gerçek metrikleri kullanıldı.</summary>
    Own,

    /// <summary>
    /// Portföy yok/yetersiz → <b>etiketli demo</b> değerler kullanıldı (onboarding kararı 1c).
    /// Demo sayı kullanıcının pano/özet ekranına ASLA sızmaz.
    /// </summary>
    Demo,

    /// <summary>Kendi verisi var ama fiyatlar bayat — sayı "şu tarihe ait" damgasıyla sunulur.</summary>
    Stale,
}

/// <summary>
/// Ders bölümünün türü (15 §2.3) — derinlik katmanından DİK bir eksendir.
/// </summary>
public enum SectionKind
{
    /// <summary>Anlatım (katmanlı).</summary>
    Explain,

    /// <summary>Jenerik örnek — statik, herkes için aynı, güvenli sayılar.</summary>
    Example,

    /// <summary>Yaygın yanlış anlama / davranışsal tuzak.</summary>
    Trap,

    /// <summary>"Senin portföyünde" — canlı bağlam (T6.2 doldurur).</summary>
    LiveContext,

    /// <summary>Sayılar nereden geldi (şeffaflık, 14 §4-A3).</summary>
    Source,
}

// Not: PriceSource (Manual | <providerKey>) açık uçlu olduğu için enum DEĞİL,
// string olarak saklanır (Source kolonları).
