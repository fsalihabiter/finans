using Microsoft.AspNetCore.Mvc;

namespace Finans.Api.Controllers;

/// <summary>
/// Servisin ayakta olduğunu doğrulayan basit sağlık ucu (04 §3).
/// Liveness için; bağımlılık (DB/dış API) kontrolü /health/ready'de (T0.12).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    /// <summary>GET /api/health → { "status": "ok" }</summary>
    [HttpGet]
    public IActionResult Get() => Ok(new HealthResponse("ok"));
}

/// <summary>Sağlık yanıtı sözleşmesi (@finans/shared HealthResponse ile birebir).</summary>
public sealed record HealthResponse(string Status);
