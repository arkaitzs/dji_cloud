using Microsoft.AspNetCore.Mvc;
using DjiCloudServer.Services;

namespace DjiCloudServer.Controllers;

/// <summary>
/// Endpoint para obtener e interactuar con el rastro histórico de los drones.
/// </summary>
[ApiController]
[Route("api/dji/[controller]")]
[Produces("application/json")]
public class TrajectoryController : ControllerBase
{
    private readonly ITrajectoryStore _trajectoryStore;

    public TrajectoryController(ITrajectoryStore trajectoryStore)
    {
        _trajectoryStore = trajectoryStore;
    }

    /// <summary>
    /// Devuelve el historial completo de trayectorias agrupadas por SN de dron.
    /// GET /api/dji/trajectory
    /// </summary>
    [HttpGet]
    public IActionResult GetAllTrajectories()
    {
        var data = _trajectoryStore.GetAllTrajectories();
        return Ok(data);
    }

    /// <summary>
    /// Devuelve el historial de trayectoria de un dron específico.
    /// GET /api/dji/trajectory/{sn}
    /// </summary>
    [HttpGet("{sn}")]
    public IActionResult GetTrajectory(string sn)
    {
        var data = _trajectoryStore.GetTrajectory(sn);
        return Ok(data);
    }

    /// <summary>
    /// Limpia el historial de trayectoria de un dron en el servidor.
    /// POST /api/dji/trajectory/clear
    /// </summary>
    [HttpPost("clear")]
    public IActionResult ClearTrajectory([FromBody] ClearRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Sn))
        {
            return BadRequest(new { message = "El SN del dron es obligatorio" });
        }

        _trajectoryStore.ClearTrajectory(request.Sn);
        return Ok(new { message = $"Trayectoria del dron {request.Sn} eliminada del servidor con éxito." });
    }
}

public class ClearRequest
{
    public string Sn { get; set; } = string.Empty;
}
