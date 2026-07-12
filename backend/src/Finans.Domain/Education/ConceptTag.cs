using Finans.Domain.Common;

namespace Finans.Domain.Education;

/// <summary>
/// Portföy kavramı etiketi (03 §C) — Analiz/Hisse kartlarından derse derin bağlantı
/// (örn. "Yoğunlaşma" kartı → `diversification` → "Çeşitlendirme" dersi). Sözlük
/// (T6.3) bu etiketlerin aranabilir indeksidir.
/// </summary>
public class ConceptTag : Entity
{
    /// <summary>Makine anahtarı (örn. "diversification", "real-return", "pe-ratio").</summary>
    public string Key { get; set; } = null!;

    /// <summary>Kullanıcıya görünen ad (TR — örn. "Çeşitlendirme").</summary>
    public string Label { get; set; } = null!;

    public ICollection<LessonConceptTag> Lessons { get; set; } = new List<LessonConceptTag>();
}
