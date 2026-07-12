using Finans.Application.Common;

namespace Finans.Infrastructure.Services;

/// <summary>
/// Kullanıcı bazlı portföy cache damgası: Değer Seyri (T5.2) ve Senaryo (T5.4) cache
/// anahtarlarına girer. Pozisyon/işlem değiştiren her yazma damgayı tazeler → eski
/// anahtarlar bir daha okunmaz ve seri **anında** güncel hesaplanır (60s TTL'yi
/// beklemeden). Damga yoksa "0" varsayılır; eski girdiler kendi TTL'leriyle düşer.
/// </summary>
internal static class PortfolioCacheStamp
{
    private static string Key(Guid userId) => $"portfolio:stamp:{userId}";

    internal static async Task<string> GetAsync(IAppCache cache, Guid userId, CancellationToken ct) =>
        await cache.GetAsync<string>(Key(userId), ct) ?? "0";

    /// <summary>Yazma sonrası çağrılır — kullanıcının seri cache'lerini geçersiz kılar.</summary>
    internal static Task BumpAsync(IAppCache cache, Guid userId, CancellationToken ct) =>
        cache.SetAsync(Key(userId), Guid.NewGuid().ToString("N"), TimeSpan.FromDays(7), ct);
}
