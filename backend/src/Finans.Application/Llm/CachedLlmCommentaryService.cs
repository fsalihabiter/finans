using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Finans.Application.Common;
using Finans.Application.Portfolio;
using Microsoft.Extensions.Logging;

namespace Finans.Application.Llm;

/// <summary>
/// <see cref="ILlmCommentaryService"/> üzerine cache + "son başarılı" fallback dekoratörü (T3.6 — 07 §6,
/// 10 §3-4). Mevcut FX/enflasyon/fiyat decorator deseninin (T2.7) LLM karşılığı. İki amaç:
/// <list type="number">
///   <item><b>Maliyet/tetikleme disiplini (NFR-9):</b> yorum portföy <b>değişince</b> ya da <b>günde
///     bir</b> üretilir; her ekran açılışında yeni LLM çağrısı atılmaz. Cache anahtarı anonim portföy
///     özetinin hash'i → portföy değişince anahtar değişir (otomatik tazeleme), değişmezse 24s TTL
///     boyunca cache'ten döner.</item>
///   <item><b>Dayanıklı fallback (07 §5-a):</b> LLM şu an erişilemez/şema bozuksa (inner "fallback"
///     döndürür), <b>son başarılı</b> yorumu (<c>Source="cache"</c>) göster — düz "üretilemedi"
///     kartından (07 §5-b) daha iyi bir kullanıcı deneyimi.</item>
/// </list>
///
/// <para><b>Per-user izolasyon (CLAUDE.md §13):</b> cache anahtarı <see cref="ICurrentUser.UserId"/>
/// içerir; bir kullanıcının yorumu başkasına sızmaz (aynı portföye sahip iki kullanıcı bile ayrı
/// anahtar kullanır).</para>
///
/// <para>Yalnız <b>başarılı</b> (inner <c>Source="llm"</c>) yorum cache'lenir; fallback cache'lenmez
/// (yoksa geçici bir hata 24s boyunca dondurulurdu).</para>
/// </summary>
public sealed class CachedLlmCommentaryService(
    LlmCommentaryService inner,
    IAppCache cache,
    ICurrentUser currentUser,
    ILlmMetrics metrics,
    ILogger<CachedLlmCommentaryService> logger) : ILlmCommentaryService
{
    /// <summary>Aynı portföy için taze yorum TTL'i — "günde bir" (NFR-9).</summary>
    private static readonly TimeSpan FreshTtl = TimeSpan.FromHours(24);

    /// <summary>Son başarılı yorumun saklanma süresi (LLM uzun süre erişilemezse bile elde tutulur).</summary>
    private static readonly TimeSpan LastSuccessTtl = TimeSpan.FromDays(30);

    /// <summary>Hash girdisi için deterministik serileştirme (enum'lar string).</summary>
    private static readonly JsonSerializerOptions HashJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = false,
    };

    public async Task<CommentaryResponse> GetCommentaryAsync(
        PortfolioSummaryDto summary,
        IReadOnlyList<HoldingDto>? holdings = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var userId = currentUser.UserId;
        var hash = HashOf(summary, holdings);
        var key = $"commentary:{userId:N}:{hash}";
        var lastKey = $"commentary-last:{userId:N}";
        var lastHashKey = $"commentary-lasthash:{userId:N}";

        // 1) Aynı portföy + 24s içinde → cache'ten (LLM çağrısı yok).
        var cached = await cache.GetAsync<CommentaryResponse>(key, ct);
        if (cached is not null)
        {
            metrics.RecordServed("cache");
            return cached;
        }

        // 2) Cache miss → tek-uçuş üret (eşzamanlı isteklerde tek LLM çağrısı; stampede koruması).
        return await cache.SingleFlightAsync(key, async innerCt =>
        {
            var again = await cache.GetAsync<CommentaryResponse>(key, innerCt);
            if (again is not null)
            {
                metrics.RecordServed("cache");
                return again;
            }

            // 2a) SABİTLEME (T3.15 — maliyet koruması): tazelik süresi dolmuş olsa bile portföy
            // (kaba çözünürlükte) DEĞİŞMEDİYSE son başarılı yorum LLM'e gidilmeden yeniden
            // sunulur ve tazelik anahtarı yenilenir. "Varlıklarda değişim yoksa yorum sabittir";
            // API maliyeti yalnız anlamlı değişimde doğar.
            var lastHash = await cache.GetAsync<string>(lastHashKey, innerCt);
            if (lastHash == hash)
            {
                var pinned = await cache.GetAsync<CommentaryResponse>(lastKey, innerCt);
                if (pinned is not null)
                {
                    var served = pinned with { Source = "cache" };
                    await cache.SetAsync(key, served, FreshTtl, innerCt);
                    metrics.RecordServed("cache");
                    logger.LogInformation(
                        "Portföy değişmedi — yorum sabitlendi, LLM çağrısı yapılmadı (maliyet koruması).");
                    return served;
                }
            }

            var resp = await inner.GetCommentaryAsync(summary, holdings, innerCt);

            if (resp.Source == "llm")
            {
                // Başarılı: taze anahtara (24s) + "son başarılı"ya ve hash'ine (30g) yaz.
                await cache.SetAsync(key, resp, FreshTtl, innerCt);
                await cache.SetAsync(lastKey, resp, LastSuccessTtl, innerCt);
                await cache.SetAsync(lastHashKey, hash, LastSuccessTtl, innerCt);
                metrics.RecordServed("llm");
                return resp;
            }

            // Başarısız (inner fallback): son başarılı varsa onu göster (07 §5-a).
            var last = await cache.GetAsync<CommentaryResponse>(lastKey, innerCt);
            if (last is not null)
            {
                metrics.RecordServed("cache_last");
                logger.LogInformation("LLM yorumu üretilemedi; son başarılı yorum (cache) gösteriliyor.");
                return last with { Source = "cache" };
            }

            // Son başarılı da yok → düz fallback kartı (07 §5-b).
            metrics.RecordServed("fallback");
            return resp;
        }, ct);
    }

    /// <summary>
    /// Anonim portföy özetinin <b>kaba çözünürlüklü</b> kararlı hash'i (cache anahtarı).
    /// <para>
    /// T3.15 (maliyet koruması): hash girdisi LLM'e giden yükten DAHA kaba yuvarlanır —
    /// oranlar 2 basamak (1 puanlık adım), parasal toplamlar 3 anlamlı basamak (~%0,1-1 adım).
    /// Canlı fiyatların gün içi küçük oynamaları aynı hash'e düşer → yeni LLM üretimi
    /// TETİKLENMEZ. Yalnız anlamlı değişim (işlem/katkı eklemek, ~1 puanlık oran kayması,
    /// kompozisyon değişikliği) yeni anahtar üretir. LLM'e giden metin yükü ise tam
    /// çözünürlükte kalır (yorum hassasiyeti düşmez).
    /// </para>
    /// </summary>
    private static string HashOf(PortfolioSummaryDto summary, IReadOnlyList<HoldingDto>? holdings)
    {
        var anon = PortfolioAnonymizer.Anonymize(summary, holdings);
        var coarse = new
        {
            anon.BaseCurrency,
            TotalValue = RoundSignificant(anon.TotalValue, 3),
            TotalCost = RoundSignificant(anon.TotalCost, 3),
            NetProfit = RoundSignificant(anon.NetProfit, 2),
            ReturnRatio = Round2(anon.ReturnRatio),
            RealReturnRatio = Round2(anon.RealReturnRatio),
            ConcentrationTop2 = Round2(anon.ConcentrationTop2) ?? 0m,
            CashWeight = Round2(anon.CashWeight) ?? 0m,
            anon.HoldingCount,
            Allocation = anon.Allocation
                .Select(a => new { a.Type, Weight = Round2(a.Weight) ?? 0m, ReturnRatio = Round2(a.ReturnRatio), a.ItemCount })
                .ToList(),
            Bes = anon.Bes is null
                ? null
                : new { OwnShare = Round2(anon.Bes.OwnShare) ?? 0m, StateShare = Round2(anon.Bes.StateShare) ?? 0m },
        };
        var json = JsonSerializer.Serialize(coarse, HashJson);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes, 0, 8); // 16 hex karakter — çakışma için fazlasıyla yeterli

        static decimal? Round2(decimal? v) =>
            v is null ? null : Math.Round(v.Value, 2, MidpointRounding.AwayFromZero);

        // 3 anlamlı basamağa yuvarlama: 714.985 → 715.000; 68.350 → 68.000 (2 basamakta).
        static decimal RoundSignificant(decimal v, int digits)
        {
            if (v == 0m) return 0m;
            var exponent = (int)Math.Floor(Math.Log10((double)Math.Abs(v))) + 1 - digits;
            if (exponent <= 0) return Math.Round(v, Math.Min(-exponent, 10), MidpointRounding.AwayFromZero);
            var factor = (decimal)Math.Pow(10, exponent);
            return Math.Round(v / factor, 0, MidpointRounding.AwayFromZero) * factor;
        }
    }
}
