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

    /// <summary>POST /api/holdings/bes — açılış bakiyesiyle yeni BES pozisyonu (T-BES.8, 201).</summary>
    [HttpPost("bes")]
    public async Task<ActionResult<HoldingDto>> CreateBes(
        [FromBody] CreateBesRequest request, CancellationToken ct)
    {
        var created = await holdings.CreateBesAsync(request, ct);
        return Created($"/api/holdings/{created.Id}", created);
    }

    /// <summary>POST /api/holdings/{id}/transactions — mevcut pozisyona alış/satış.</summary>
    [HttpPost("{id:guid}/transactions")]
    public async Task<ActionResult<HoldingDto>> AddTransaction(
        Guid id, [FromBody] TransactionRequest request, CancellationToken ct) =>
        Ok(await holdings.AddTransactionAsync(id, request, ct));

    /// <summary>PUT /api/holdings/{id}/transactions/{txId} — tek işlemi düzenle.</summary>
    [HttpPut("{id:guid}/transactions/{transactionId:guid}")]
    public async Task<ActionResult<HoldingDto>> UpdateTransaction(
        Guid id, Guid transactionId, [FromBody] TransactionRequest request, CancellationToken ct) =>
        Ok(await holdings.UpdateTransactionAsync(id, transactionId, request, ct));

    /// <summary>DELETE /api/holdings/{id}/transactions/{txId} — tek işlemi sil (son işlem silinemez).</summary>
    [HttpDelete("{id:guid}/transactions/{transactionId:guid}")]
    public async Task<ActionResult<HoldingDto>> DeleteTransaction(
        Guid id, Guid transactionId, CancellationToken ct) =>
        Ok(await holdings.DeleteTransactionAsync(id, transactionId, ct));

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

    /// <summary>PUT /api/holdings/{id}/bes — BES sözleşme alanları (başlangıç tarihi → hak ediş).</summary>
    [HttpPut("{id:guid}/bes")]
    public async Task<ActionResult<HoldingDto>> UpdateBes(
        Guid id, [FromBody] UpdateBesRequest request, CancellationToken ct) =>
        Ok(await holdings.UpdateBesAsync(id, request, ct));

    /// <summary>POST /api/holdings/{id}/bes/contributions — düzenli katkıyı tarih aralığından üret (T-BES.6).</summary>
    [HttpPost("{id:guid}/bes/contributions")]
    public async Task<ActionResult<HoldingDto>> GenerateBesContributions(
        Guid id, [FromBody] GenerateBesContributionsRequest request, CancellationToken ct) =>
        Ok(await holdings.GenerateBesContributionsAsync(id, request, ct));

    /// <summary>PUT /api/holdings/{id}/bes/contributions/{cid} — tek BES katkı kaydını düzenle.</summary>
    [HttpPut("{id:guid}/bes/contributions/{contributionId:guid}")]
    public async Task<ActionResult<HoldingDto>> UpdateBesContribution(
        Guid id, Guid contributionId, [FromBody] UpdateBesContributionRequest request, CancellationToken ct) =>
        Ok(await holdings.UpdateBesContributionAsync(id, contributionId, request, ct));

    /// <summary>DELETE /api/holdings/{id}/bes/contributions/{cid} — tek BES katkı kaydını sil.</summary>
    [HttpDelete("{id:guid}/bes/contributions/{contributionId:guid}")]
    public async Task<ActionResult<HoldingDto>> DeleteBesContribution(
        Guid id, Guid contributionId, CancellationToken ct) =>
        Ok(await holdings.DeleteBesContributionAsync(id, contributionId, ct));

    /// <summary>DELETE /api/holdings/{id} — pozisyonu sil (soft-delete, 204).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await holdings.DeleteAsync(id, ct);
        return NoContent();
    }
}
