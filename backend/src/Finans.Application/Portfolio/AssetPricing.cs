using Finans.Domain.Enums;

namespace Finans.Application.Portfolio;

/// <summary>
/// Fiyatı TANIM GEREĞİ sabit olan varlıkların birim fiyatı — tek kural, tek yer.
///
/// <para><b>Nakit:</b> kendi para biriminde bir birimin fiyatı her zaman 1'dir
/// (1 ₺ = 1 ₺; USD nakitte 1 $ = 1 $ — baz para birimine çevrim sonra yapılır).
/// Fiyat girilmesini beklemek anlamsız: arayüz bu yüzden nakitte fiyat alanını
/// gizler ("sabit ₺1").</para>
///
/// <para>⚠ Neden gerekli (INC-003, 2026-09-21): API'den oluşturulan her pozisyon
/// <c>CurrentPrice = null</c> başlıyordu; nakitte kullanıcı bunu hiç düzeltemediği
/// için liste satırı "—" gösteriyor, özet ise fiyatsız pozisyonu MALİYETİNDEN
/// değere katıyordu → toplam, görünen satırların toplamına eşit değildi.
/// Kural okuma yolunda uygulandığı için saklanan değerden bağımsız doğrudur.</para>
/// </summary>
public static class AssetPricing
{
    /// <summary>Sabit fiyatlı türde birim fiyat; diğer türlerde null (fiyat dışarıdan gelir).</summary>
    public static decimal? FixedUnitPriceFor(AssetType type) =>
        type == AssetType.Cash ? 1m : null;
}
