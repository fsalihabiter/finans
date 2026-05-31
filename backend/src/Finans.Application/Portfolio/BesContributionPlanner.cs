namespace Finans.Application.Portfolio;

/// <summary>
/// Düzenli BES katkısı için saf takvim hesabı (T-BES.6): bir tarih aralığında, ayın
/// belirli gününde ödeme tarihlerini üretir. Yan etkisiz, %100 testlenebilir.
/// </summary>
public static class BesContributionPlanner
{
    /// <summary>
    /// [<paramref name="fromUtc"/>.month, <paramref name="toUtc"/>.month] aralığındaki
    /// <b>her ay için</b> <paramref name="dayOfMonth"/> günü ödeme tarihi üretir (artan).
    /// Gün 1–28'e kıskaçlanır (ay sonu taşması olmasın). Ters aralık → boş.
    /// <para>
    /// <b>Önceki davranış değişti (UX düzeltmesi):</b> önceden filtre <c>payDate ∈ [from,to]</c>
    /// idi → kullanıcı sezgisiyle uyumsuz uç-ay kaybı (örn. from=01.05/day=15/to=01.06 → Haziran
    /// 15 &gt; 01.06 olduğu için DÜŞÜYORDU, sessizce 0 kayıt çıkıyordu). Yeni davranış: aralıktaki
    /// her aya bir ödeme — form metniyle ("aralıktaki her ay") tutarlı.
    /// </para>
    /// </summary>
    public static IReadOnlyList<DateTime> MonthlyDates(int dayOfMonth, DateTime fromUtc, DateTime toUtc)
    {
        var dates = new List<DateTime>();
        if (toUtc.Date < fromUtc.Date)
            return dates;

        var day = Math.Clamp(dayOfMonth, 1, 28);
        var cursor = new DateTime(fromUtc.Year, fromUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonth = new DateTime(toUtc.Year, toUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        while (cursor <= lastMonth)
        {
            dates.Add(new DateTime(cursor.Year, cursor.Month, day, 0, 0, 0, DateTimeKind.Utc));
            cursor = cursor.AddMonths(1);
        }

        return dates;
    }
}
