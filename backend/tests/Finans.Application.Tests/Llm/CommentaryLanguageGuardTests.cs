using Finans.Application.Llm;

namespace Finans.Application.Tests.Llm;

/// <summary>
/// T3.11 — Dil saflığı bekçisi. Canlı gözlemden gelen gerçek sızıntılar test verisi yapıldı:
/// "invested olduğunda", "%5 faiz means 100 TL becomes 105 TL", "yediğini意味します",
/// "%10上がれば", "ownShare/stateShare" alan adı sızıntısı.
/// </summary>
public class CommentaryLanguageGuardTests
{
    // ── Temiz Türkçe geçer (yanlış pozitif disiplini) ──

    [Theory]
    [InlineData("Reel getiri, kazancından enflasyonun yediği kısmı düştükten sonra kalan gerçek satın alma gücü artışıdır.")]
    [InlineData("Portföyünün %84'ü iki kalemde; toplam değerin 641.403 ₺ — bu bir farkındalık, yönlendirme değil.")]
    [InlineData("Bu an itibarıyla on kalemin var; at gözlüğüyle bakma ama bin bir çeşit varlık da gerekmez.")] // Türkçe eşsesliler: an, on, at, bin
    [InlineData("Çeşitlendirme sepetteki yumurta benzetmesiyle anlatılır; ğüşiöç gibi Türkçe karakterler sorun değildir.")]
    public void Clean_turkish_passes(string text)
    {
        Assert.False(CommentaryLanguageGuard.IsForeign("Başlık", text, null, out _));
    }

    // ── Gerçek sızıntılar yakalanır ──

    [Theory]
    [InlineData("Senin portföyünde bu oran %10,6 — yani her 100 TL invested olduğunda yaklaşık 10,6 TL kazanç.")]
    [InlineData("%5 faiz means 100 TL becomes 105 TL. Ancak fiyatlar da artar.")]
    [InlineData("Bu durum fiyat artışlarının kazancının tamamını ve daha fazlasını yediğini gösterir, however dikkatli ol.")]
    public void English_word_leak_is_flagged(string body)
    {
        Assert.True(CommentaryLanguageGuard.IsForeign("Başlık", body, null, out var reason));
        Assert.Equal("english_leak", reason);
    }

    [Theory]
    [InlineData("Fiyat artışlarının kazancını yediğini意味します。")]
    [InlineData("Fiyatlar %10上がれば, bu 105 TL'nin alım gücü azalır.")]
    [InlineData("Портфель iki kalemde yoğunlaşmış.")]
    public void Foreign_script_is_flagged(string body)
    {
        Assert.True(CommentaryLanguageGuard.IsForeign("Başlık", body, null, out var reason));
        Assert.Equal("foreign_script", reason);
    }

    [Fact]
    public void Detail_is_scanned_too()
    {
        Assert.True(CommentaryLanguageGuard.IsForeign(
            "Başlık", "Tamamen temiz ve yeterince uzun bir Türkçe gövde metni.",
            "Kavram: %5 faiz means 100 TL becomes 105 TL gibi düşünebilirsin.", out _));
    }

    [Fact]
    public void Emoji_and_symbols_are_allowed()
    {
        Assert.False(CommentaryLanguageGuard.IsForeign(
            "⚖️ Dağılım", "Portföyün %78'i iki kalemde 📉 — toplam 714.985 ₺ değerinde…", null, out _));
    }

    // ── Alan adı sızıntısı Türkçe'ye çevrilir (kart kurtarılır) ──

    [Fact]
    public void Field_names_are_translated_to_turkish()
    {
        var text = "Yüksek ownShare uzun vadeli tasarruf disiplinini, yüksek stateShare ise dış destek oranını yansıtır.";

        var fixedText = CommentaryLanguageGuard.TranslateFieldNames(text);

        Assert.DoesNotContain("ownShare", fixedText);
        Assert.DoesNotContain("stateShare", fixedText);
        Assert.Contains("kendi katkı payı", fixedText);
        Assert.Contains("devlet katkısı payı", fixedText);
        // Çeviri sonrası dil bekçisinden de geçer (alan adı artık İngilizce sızıntı sayılmaz).
        Assert.False(CommentaryLanguageGuard.IsForeign("Başlık", fixedText, null, out _));
    }

    [Fact]
    public void Field_name_translation_leaves_clean_text_untouched()
    {
        const string text = "Devlet katkısı payın %20,7 — birikimin beşte biri devlet desteğiyle oluşmuş.";
        Assert.Equal(text, CommentaryLanguageGuard.TranslateFieldNames(text));
    }
}
