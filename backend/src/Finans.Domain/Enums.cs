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

// Not: PriceSource (Manual | <providerKey>) açık uçlu olduğu için enum DEĞİL,
// string olarak saklanır (Source kolonları).
