using System.Text.Json;
using Finans.Application.Llm;

namespace Finans.Application.Tests.Llm;

/// <summary>
/// Statik portföy yorum sözleşmeleri (T3.2 — 07 §3,§4): KESİN KURALLAR kaybolmasın,
/// JSON şeması parse edilebilir kalsın. Bunlar **regresyon kapısı** — sistem promptu
/// gevşetilirse veya şema bozulursa testler kırılır.
/// </summary>
public class CommentaryPromptsTests
{
    [Fact]
    public void SystemPrompt_includes_the_strict_rules_that_prevent_advice()
    {
        var p = CommentaryPrompts.SystemPrompt;

        // "Tavsiye değil" çerçevesi: kimlik + 3 yasak (yönlendirme, tahmin, yeni rakam).
        Assert.Contains("EĞİTMEN", p);
        Assert.Contains("danışman DEĞİL", p);
        Assert.Contains("YÖNLENDİRME YAPMA", p);
        Assert.Contains("TAHMİN ETME", p);
        Assert.Contains("Yeni yüzde/oran/tutar üretme", p);
    }

    [Fact]
    public void SystemPrompt_demands_turkish_and_structured_only_output()
    {
        var p = CommentaryPrompts.SystemPrompt;

        Assert.Contains("Türkçe", p);
        Assert.Contains("structured_output", p); // tool çağrısı adı — şema ile aynı (T3.3'te kullanılır)
    }

    [Fact]
    public void SystemPrompt_shows_at_least_one_correct_and_one_forbidden_example()
    {
        var p = CommentaryPrompts.SystemPrompt;

        Assert.Contains("DOĞRU", p);
        Assert.Contains("YANLIŞ", p);
        // Yasaklı kalıplara somut örnek (savunma derinliği, 07 §7'nin prompt seviyesindeki ayağı).
        Assert.Contains("YASAK", p);
    }

    [Fact]
    public void CommentaryJsonSchema_is_valid_json_with_cards_array()
    {
        using var doc = JsonDocument.Parse(CommentaryPrompts.CommentaryJsonSchema);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.Equal("object", root.GetProperty("type").GetString());

        var cards = root.GetProperty("properties").GetProperty("cards");
        Assert.Equal("array", cards.GetProperty("type").GetString());

        var item = cards.GetProperty("items");
        var required = item.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToHashSet();
        Assert.Contains("emoji", required);
        Assert.Contains("title", required);
        Assert.Contains("body", required);
    }

    [Fact]
    public void CommentaryJsonSchema_bounds_body_length_and_card_count()
    {
        using var doc = JsonDocument.Parse(CommentaryPrompts.CommentaryJsonSchema);
        var cards = doc.RootElement.GetProperty("properties").GetProperty("cards");

        // T3.12 ek (kullanıcı beklentisi): kart sayısı üretimden üretime değişmesin — TAM 6.
        Assert.Equal(6, cards.GetProperty("minItems").GetInt32());
        Assert.Equal(6, cards.GetProperty("maxItems").GetInt32());

        // T3.10 derinlik kapısı: gövde tek cümlelik yüzeyselliğe İZİN VERMEZ (min ≥100),
        // makale boyuna da kaçamaz (max ≤800).
        var body = cards.GetProperty("items").GetProperty("properties").GetProperty("body");
        Assert.InRange(body.GetProperty("minLength").GetInt32(), 100, 200);
        Assert.InRange(body.GetProperty("maxLength").GetInt32(), 400, 800);
    }

    [Fact]
    public void SystemPrompt_demands_depth_structure_and_term_definitions()
    {
        var p = CommentaryPrompts.SystemPrompt;

        // T3.10 regresyon kapısı: derinlik yapısı (tanım + senin portföyünde + çerçeve) ve
        // terimlerin ilk kullanımda tanımlanması prompttan silinmesin.
        Assert.Contains("3-6 cümle", p);
        Assert.Contains("İLK kullanımda", p);
        Assert.Contains("detail", p);
    }

    [Fact]
    public void SystemPrompt_forbids_foreign_language_and_field_name_leaks()
    {
        var p = CommentaryPrompts.SystemPrompt;

        // T3.11 regresyon kapısı: dil saflığı kuralı prompttan silinmesin (kuşak-1;
        // kuşak-2 deterministik bekçi CommentaryLanguageGuard'da).
        Assert.Contains("TAMAMEN TÜRKÇE", p);
        Assert.Contains("ownShare", p); // alan adlarını geçirme talimatı örnekleriyle
        Assert.Contains("AYNEN GEÇİRME", p);
    }

    [Fact]
    public void CommentaryJsonSchema_requires_detail_with_bounds()
    {
        using var doc = JsonDocument.Parse(CommentaryPrompts.CommentaryJsonSchema);
        var item = doc.RootElement.GetProperty("properties").GetProperty("cards").GetProperty("items");

        var detail = item.GetProperty("properties").GetProperty("detail");
        Assert.Equal("string", detail.GetProperty("type").GetString());
        Assert.True(detail.GetProperty("maxLength").GetInt32() <= 600);

        // T3.13 (kullanıcı beklentisi): kavram bloğu HER kartta — şemada zorunlu.
        // (Parse yine detail'siz kartı düşürmez; eksik kavram retry sebebi olur.)
        var required = item.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToHashSet();
        Assert.Contains("detail", required);
    }
}
