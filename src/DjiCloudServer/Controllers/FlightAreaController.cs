using DjiCloudServer.Models;
using DjiCloudServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace DjiCloudServer.Controllers;

/// <summary>
/// Endpoints de zonas de vuelo personalizadas (KML).
/// DJI Pilot 2 llama a GET /flight-areas/url para obtener la URL del KML
/// que superpone en el mapa como capa de zonas de vuelo.
/// </summary>
[ApiController]
[Route("map/api/v1/project/{workspaceId}")]
[Produces("application/json")]
public class FlightAreaController(IFlightAreaService flightArea) : ControllerBase
{
    /// <summary>
    /// GET /map/api/v1/project/{workspaceId}/flight-areas/url
    /// El RC llama a este endpoint para obtener la URL del KML activo.
    /// </summary>
    [HttpGet("flight-areas/url")]
    public IActionResult GetFlightAreaUrl(string workspaceId)
    {
        _ = workspaceId; // parámetro de ruta requerido por DJI, no usado en lógica
        var relativePath = flightArea.GetActiveKmlRelativePath();
        if (string.IsNullOrEmpty(relativePath))
            return Ok(DjiApiResponse<object>.Success(new { url = "" }));

        var url = $"{Request.Scheme}://{Request.Host}/{relativePath.TrimStart('/')}";
        return Ok(DjiApiResponse<object>.Success(new { url }));
    }
}
