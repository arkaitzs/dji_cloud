using Microsoft.AspNetCore.Mvc;
using DjiCloudServer.Models;

namespace DjiCloudServer.Controllers;

/// <summary>
/// Endpoint de verificación del estado del servidor.
/// GET /api/health
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>Devuelve el estado del servidor DJI Cloud API.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(DjiApiResponse<HealthStatusDto>), StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        _logger.LogInformation("Health check solicitado");
        return Ok(DjiApiResponse<HealthStatusDto>.Success(new HealthStatusDto
        {
            Status    = "OK",
            Version   = "1.11.3",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }));
    }
}

public record HealthStatusDto
{
    public string Status  { get; init; } = "OK";
    public string Version { get; init; } = string.Empty;
    public long Timestamp { get; init; }
}
