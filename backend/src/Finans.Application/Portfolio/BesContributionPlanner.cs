namespace Finans.Application.Portfolio;

/// <summary>
/// Düzenli BES katkısı için saf takvim hesabı (T-BES.6): bir tarih aralığında, ayın
/// belirli gününde ödeme tarihlerini üretir. Yan etkisiz, %100 testlenebilir.
/// </summary>
public static class BesContributionPlanner
{
    /// <summary>
    /// <paramref name="fromUtc"/>–<paramref name="toUtc"/> aralığında her ay
    /// <paramref name="dayOfMonth"/> gününe denk gelen ödeme tarihleri (artan).
    /// Gün 1–28'e kıskaçlanır (ay sonu taşması olmasın). Aralık dışı/ters ise boş.
    /// </summary>
    public static IReadOnlyList<DateTime> MonthlyDates(int dayOfMonth, DateTime fromUtc, DateTime toUtc)
    {
        var dates = new List<DateTime>();
        if (toUtc < fromUtc)
            return dates;

        var day = Math.Clamp(dayOfMonth, 1, 28);
        var from = fromUtc.Date;
        var to = toUtc.Date;

        // İlk ayın 1'inden başla; her ayın `day`'ini aralık içindeyse ekle.
        var cursor = new DateTime(from.Year, from.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonth = new DateTime(to.Year, to.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        while (cursor <= lastMonth)
        {
            var payDate = new DateTime(cursor.Year, cursor.Month, day, 0, 0, 0, DateTimeKind.Utc);
            if (payDate >= from && payDate <= to)
                dates.Add(payDate);
            cursor = cursor.AddMonths(1);
        }

        return dates;
    }
}
