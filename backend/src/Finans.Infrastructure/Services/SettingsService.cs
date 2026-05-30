using Finans.Application.Common;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Finans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Finans.Infrastructure.Services;

/// <summary>
/// <see cref="ISettingsService"/> EF uygulaması. Geçerli kullanıcının kaydını
/// <c>UserId</c> ile çözer (11 §3). Faz 1'de yalnızca <c>BaseCurrency</c>.
/// </summary>
public sealed class SettingsService(FinansDbContext db, ICurrentUser currentUser) : ISettingsService
{
    public async Task<SettingsDto> GetAsync(CancellationToken ct = default)
    {
        var baseCurrency = await db.Users
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => (CurrencyCode?)u.BaseCurrency)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Kullanıcı bulunamadı.");

        return new SettingsDto(baseCurrency);
    }

    public async Task<SettingsDto> UpdateAsync(UpdateSettingsRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct)
            ?? throw new NotFoundException("Kullanıcı bulunamadı.");

        user.BaseCurrency = request.BaseCurrency;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return new SettingsDto(user.BaseCurrency);
    }
}
