namespace Finans.Domain.Common;

/// <summary>
/// Tüm entity'ler için temel: zaman-sıralı UUIDv7 PK (indeks lokalitesi iyi,
/// tahmin edilemez → IDOR yüzeyi küçük, 03 §1). EF DB'den materialize ederken
/// Id'yi okunan değerle ezer; yeni nesnelerde v7 üretilir.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
}

/// <summary>Soft-delete edilebilen kullanıcı-içeriği tabloları (03 §1).</summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
}
