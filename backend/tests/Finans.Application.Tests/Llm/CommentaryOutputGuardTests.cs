using Finans.Application.Llm;

namespace Finans.Application.Tests.Llm;

/// <summary>
/// T3.5 — Çıktı güvenlik filtresi (07 §7, CLAUDE.md §2). İki taraflı koruma kanıtı:
/// (a) meşru eğitim metni KESİLMEZ (yanlış-pozitif yok), (b) yönlendirme/tahmin kalıbı YAKALANIR.
/// </summary>
public class CommentaryOutputGuardTests
{
    // ── TEMİZ: eğitim/farkındalık metni filtreye TAKILMAMALI ──
    [Theory]
    // Kritik yanlış-pozitif tuzakları: "satın alma" / "alım gücü" (al/sat kökü var, yönlendirme yok).
    [InlineData("Satın alma gücün açısından reel getirin %21 — yani enflasyon sonrası kazancın bu kadar.")]
    [InlineData("Paranın alım gücü zamanla değişir; reel getiri bu farkı görmeni sağlar burada.")]
    // Koşul/olasılık → kesin tahmin DEĞİL (07 §7'nin açık örneği).
    [InlineData("Enflasyon yükselirse paranın alım gücü düşebilir; bu yüzden reel getiri önemlidir.")]
    [InlineData("Bu iki varlık aynı anda değer kaybederse portföyünün büyük kısmı birlikte etkilenir.")]
    // Genel çerçeve, yumuşak dil.
    [InlineData("Portföyünün %84'ü iki kalemde toplanmış; bu yoğunlaşmayı bilmek faydalı bir farkındalıktır.")]
    [InlineData("Nakit oranın düşük görünüyor; acil ihtiyaçta bir tampon bulundurmak faydalı olabilir.")]
    public void Clean_educational_text_is_not_flagged(string body)
    {
        var forbidden = CommentaryOutputGuard.IsForbidden("Bilgi", body, null, out var reason);

        Assert.False(forbidden, $"Yanlış-pozitif: '{reason}' → \"{body}\"");
    }

    // ── YASAK: yönlendirme/tahmin kalıbı YAKALANMALI ──
    [Theory]
    // 07 §3'teki "asla yapma" few-shot örnekleri.
    [InlineData("Altından çıkıp hisseye geçmelisin, vakit kaybetme.", "directive")]
    [InlineData("Bu seviyeden BES eklemek mantıklı olur diye düşünüyorum.", "directive")]
    [InlineData("Önümüzdeki ay USD/TRY yükselir, ona göre konumlan.", "forecast")]
    // Doğrudan alım-satım önerisi (ekli + çıplak emir).
    [InlineData("Bence şimdi altın almalısın, gerisi sonra gelir.", "directive")]
    [InlineData("Hemen sat ve dövize geç, beklemenin anlamı yok.", "directive")]
    [InlineData("Tavsiyem nakitte kalıp fırsatı kaçırmamandır.", "directive")]
    // Kesin gelecek tahmini (zaman imi olmadan da).
    [InlineData("Altın fiyatı kısa sürede yükselecek, kaçırma.", "prediction")]
    [InlineData("Bu hisse sana ciddi kazandıracak, izle yeter.", "prediction")]
    // Diyakritiksiz yazım (LLM bazen ASCII üretir) da yakalanmalı.
    [InlineData("Bence altin almalisin, simdi tam zamani.", "directive")]
    public void Directive_or_prediction_text_is_flagged(string body, string expectedReason)
    {
        var forbidden = CommentaryOutputGuard.IsForbidden("Başlık", body, null, out var reason);

        Assert.True(forbidden, $"Yakalanmadı: \"{body}\"");
        Assert.Equal(expectedReason, reason);
    }

    [Fact]
    public void Scans_title_and_tags_too_not_only_body()
    {
        // Yasaklı kalıp başlıkta.
        Assert.True(CommentaryOutputGuard.IsForbidden("Hemen al!", "Gövde tamamen masum bir açıklama metni olabilir burada.", null, out _));
        // Yasaklı kalıp etikette.
        Assert.True(CommentaryOutputGuard.IsForbidden(
            "Başlık", "Gövde tamamen masum bir açıklama metni olabilir burada.", new[] { "yatirim", "almalisin" }, out _));
    }
}
