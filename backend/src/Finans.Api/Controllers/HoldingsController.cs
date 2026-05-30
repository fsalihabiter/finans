using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Finans.Api.Controllers;

/// <summary>
/// Pozisyon (holding) CRUD'u (04 §4). Tüm uçlar geçerli kullanıcıya kapsanır
/// (servis katmanı; 11 §3) — başkasının id'si 404 döner (IDOR yok).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class HoldingsController(IHoldingService holdings) : ControllerBase
{
    /// <summary>GET /api/holdings — kullanıcının tüm pozisyonları.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HoldingDto>>> GetAll(
        [FromQuery] CurrencyCode? baseCurrency, CancellationToken ct) =>
        Ok(await holdings.GetAllAsync(baseCurrency, ct));

    /// <summary>GET /api/holdings/{id} — tekil pozisyon (yoksa/başkasınınsa 404).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HoldingDto>> GetById(
        Guid id, [FromQuery] CurrencyCode? baseCurrency, CancellationToken ct) =>
        Ok(await holdings.GetByIdAsync(id, baseCurrency, ct));

    /// <summary>POST /api/holdings — ilk işlemiyle yeni pozisyon (201).</summary>
    [HttpPost]
    public async Task<ActionResult<HoldingDto>> Create(
        [FromBody] CreateHoldingRequest request, CancellationToken ct)
    {
        var created = await holdings.CreateAsync(request, ct);
        return Created($"/api/holdings/{created.Id}", created);
    }

    /// <summary>POST /api/holdings/{id}/transactions — mevcut pozisyona alış/satış.</summary>
    [HttpPost("{id:guid}/transactions")]
    public async Task<ActionResult<HoldingDto>> AddTransaction(
        Guid id, [FromBody] TransactionRequest request, CancellationToken ct) =>
        Ok(await holdings.AddTransactionAsync(id, request, ct));

    /// <summary>PUT /api/holdings/{id} — güncel fiyatı elle güncelle (FR-1.8).</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<HoldingDto>> Update(
        Guid id, [FromBody] UpdateHoldingRequest request, CancellationToken ct) =>
        Ok(await holdings.UpdateAsync(id, request, ct));

    /// <summary>POST /api/holdings/{id}/bes-contribution — BES'e aylık katkı (kendi + devlet).</summary>
    [HttpPost("{id:guid}/bes-contribution")]
    public async Task<ActionResult<HoldingDto>> AddBesContribution(
        Guid id, [FromBody] AddBesContributionRequest request, CancellationToken ct) =>
        Ok(await holdings.AddBesContributionAsync(id, request, ct));

    /// <summary>DELETE /api/holdings/{id} — pozisyonu sil (soft-delete, 204).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await holdings.DeleteAsync(id, ct);
        return NoContent();
    }
}
