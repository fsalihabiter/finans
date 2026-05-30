namespace Finans.Application.Common;

/// <summary>
/// Geçerli isteğin kullanıcısını sağlar (11 §3 — per-user izolasyonun tek kaynağı).
/// Faz 1'de kimlik yok → implementasyon istek başlığı/config'ten çözer; Faz 5'te
/// aynı arayüz JWT claim'lerinden beslenir (controller/servisler değişmez).
/// </summary>
public interface ICurrentUser
{
    /// <summary>Geçerli kullanıcının kimliği. Çözümlenemezse implementasyon fırlatır.</summary>
    Guid UserId { get; }
}
