namespace Finans.Integration.Tests;

/// <summary>
/// Repo kökünü bulan <b>tek</b> yardımcı (REVIEW-006 · RV-088).
/// <para>
/// Önceden iki ayrı <c>RepoRoot()</c> vardı ve farklı nirengi dosyaları arıyordu:
/// <c>EducationSeedTests</c> <c>web/src/components/LessonFigure.tsx</c> arayıp
/// bulamazsa açık bir iddia ile patlıyordu, <c>LocalModelTrial</c> ise
/// <c>.agent-method</c> arayıp bulamazsa <b>sessizce</b> çalışma dizinine
/// düşüyordu — yani yolu yanlış çözüp testi anlamsız bir yerde arıyordu.
/// CAPABILITIES.md'nin Reuse → Extend → <b>Extract</b> → New merdiveninde bu
/// "Extract" kalemiydi.
/// </para>
/// <para>
/// Testler <c>bin/</c> altından koşar, bu yüzden yukarı yürünür. Nirengi olarak
/// <c>.git</c> yerine <b>iki</b> dosya birlikte aranır: tek bir dosyanın adı
/// değişirse ya da taşınırsa sessizce yanlış klasöre düşmek yerine hata verilir.
/// </para>
/// </summary>
internal static class RepoPaths
{
    /// <summary>Repo kökünün tam yolu. Bulunamazsa <see cref="DirectoryNotFoundException"/>.</summary>
    public static string Root { get; } = Bul();

    /// <summary>Repo köküne göreli yol birleştirir.</summary>
    public static string At(params string[] parts) =>
        Path.Combine(new[] { Root }.Concat(parts).ToArray());

    private static string Bul()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Nirengi(dir.FullName)) dir = dir.Parent;

        if (dir is null)
            throw new DirectoryNotFoundException(
                "Repo kökü bulunamadı. Beklenen nirengi: `.agent-method/` klasörü ve " +
                "`web/src/components/LessonFigure.tsx` dosyası, aynı klasörde. " +
                $"Arama {AppContext.BaseDirectory} dizininden yukarı doğru yapıldı. " +
                "Testler repo dışına kopyalanıp koşturulduysa (paketlenmiş test çalıştırması) " +
                "bu yol çözülemez — bu testler repo içinden koşar.");

        return dir.FullName;
    }

    private static bool Nirengi(string yol) =>
        Directory.Exists(Path.Combine(yol, ".agent-method"))
        && File.Exists(Path.Combine(yol, "web", "src", "components", "LessonFigure.tsx"));
}
