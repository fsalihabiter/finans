namespace Finans.Application.Common;

/// <summary>
/// Uygulama cache portu (10 §3-4, T2.7). Infrastructure'da <c>IDistributedCache</c> ile
/// uygulanır → Redis (yapılandırılmışsa) ya da in-memory (yerel dev). Değerler JSON
/// serileştirilir; bu yüzden cache'lenen tipler serileştirilebilir olmalı. Sağlar:
/// <list type="bullet">
///   <item><b>Single-flight (stampede koruması):</b> aynı anahtar için eşzamanlı üretim tektir.</item>
///   <item><b>Hit/miss metriği:</b> her okuma sayılır (T2.8 Prometheus'a bağlanır).</item>
/// </list>
/// </summary>
public interface IAppCache
{
    /// <summary>Cache'te varsa döndürür; yoksa <paramref name="factory"/> ile (tek-uçuş) üretir, yazar, döndürür.</summary>
    Task<T> GetOrCreateAsync<T>(
        string key, TimeSpan ttl, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default)
        where T : class;

    /// <summary>Düşük seviye okuma; yoksa <c>null</c> (hit/miss sayılır).</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;

    /// <summary>Düşük seviye yazma (mutlak TTL).</summary>
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Bir anahtar için <paramref name="factory"/>'yi tek-uçuş çalıştırır (stampede koruması);
    /// sonucu cache'LEMEZ — çağıran kendi yazım/TTL mantığını uygular (örn. değişken TTL).
    /// </summary>
    Task<T> SingleFlightAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default);
}
