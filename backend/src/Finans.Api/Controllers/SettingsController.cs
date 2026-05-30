using Finans.Application.Portfolio;
using Microsoft.AspNetCore.Mvc;

namespace Finans.Api.Controllers;

/// <summary>Kullanıcı ayarları (04 §4). Geçerli kullanıcıya kapsanır (servis katmanı).</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class SettingsController(ISettingsService settings) : ControllerBase
{
    /// <summary>GET /api/settings → { baseCurrency }.</summary>
    [HttpGet]
    public async Task<ActionResult<SettingsDto>> Get(CancellationToken ct) =>
        Ok(await settings.GetAsync(ct));

    /// <summary>PUT /api/settings → baz para birimini günceller.</summary>
    [HttpPut]
    public async Task<ActionResult<SettingsDto>> Update(
        [FromBody] UpdateSettingsRequest request, CancellationToken ct) =>
        Ok(await settings.UpdateAsync(request, ct));
}
