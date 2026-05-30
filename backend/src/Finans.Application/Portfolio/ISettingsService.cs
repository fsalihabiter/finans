using Finans.Domain.Enums;

namespace Finans.Application.Portfolio;

/// <summary>Kullanıcı tercihleri (04 §4 — /api/settings). Geçerli kullanıcıya kapsanır.</summary>
public sealed record SettingsDto(CurrencyCode BaseCurrency);

/// <summary>PUT /api/settings — baz para birimi tercihini günceller (FR-1.4).</summary>
public sealed record UpdateSettingsRequest(CurrencyCode BaseCurrency);

/// <summary>
/// Kullanıcı ayarları use-case'i. Faz 1'de yalnızca baz para birimi (TRY/USD/EUR).
/// **Geçerli kullanıcıya kapsanır** (11 §3) — başka kullanıcının ayarına erişilmez.
/// </summary>
public interface ISettingsService
{
    Task<SettingsDto> GetAsync(CancellationToken ct = default);

    Task<SettingsDto> UpdateAsync(UpdateSettingsRequest request, CancellationToken ct = default);
}
