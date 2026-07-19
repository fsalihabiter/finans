using Finans.Domain.Common;
using Finans.Domain.Enums;

namespace Finans.Domain.Education;

/// <summary>
/// Ders içi içerik bloğu (03 §C). T6.5 ile katmanlı içeriğin taşıyıcısı oldu:
/// her bölüm bir <see cref="DepthTier"/> (derinlik) ve bir <see cref="SectionKind"/>
/// (tür) taşır — ikisi DİK eksenlerdir (15 §2).
/// </summary>
/// <remarks>
/// GERİYE DÖNÜK UYUM: bölümü olmayan derslerde istemci <c>Lesson.BodyMarkdown</c>
/// alanına düşer (15 §2.1, SC-E2). Bu yüzden mevcut 5 ders kırılmaz ve
/// <c>BodyMarkdown</c> kaldırılmaz.
/// </remarks>
public class LessonSection : Entity
{
    public Guid LessonId { get; set; }
    public int OrderIndex { get; set; }
    public string? Heading { get; set; }
    public string BodyMarkdown { get; set; } = null!;

    /// <summary>Derinlik katmanı — varsayılan <see cref="DepthTier.Core"/> (herkes görür).</summary>
    public DepthTier DepthTier { get; set; } = DepthTier.Core;

    /// <summary>Blok türü — varsayılan <see cref="SectionKind.Explain"/> (anlatım).</summary>
    public SectionKind Kind { get; set; } = SectionKind.Explain;

    public Lesson Lesson { get; set; } = null!;
}
