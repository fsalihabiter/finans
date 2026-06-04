namespace Finans.Application.Llm;

/// <summary>
/// LLM yorum çıktısı (07 §4). Üst katman (Web) bu DTO'yu render eder; <see cref="Source"/> "llm" /
/// "fallback" / "cache" → UI'da kullanıcıya "şu an üretilemedi" mesajı için ipucu. Yatırım tavsiyesi
/// DEĞİL disclaimer'ı bu yanıtın üzerine UI tarafından sabitlenir (T3.8).
/// </summary>
public sealed record CommentaryResponse(
    IReadOnlyList<CommentaryCard> Cards,
    string Source,
    DateTime GeneratedAtUtc);

/// <summary>Tek bir yorum kartı. Şema 07 §4 ile uyumlu; servis çıktıyı bu tipe normalize eder.</summary>
public sealed record CommentaryCard(
    string Emoji,
    string Title,
    string Body,
    CommentaryMeter? Meter = null,
    IReadOnlyList<string>? Tags = null);

/// <summary>Opsiyonel gösterge (0..1) — örn. yoğunlaşma seviyesi.</summary>
public sealed record CommentaryMeter(decimal Value, string LowLabel, string HighLabel);
