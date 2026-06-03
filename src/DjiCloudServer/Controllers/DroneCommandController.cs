using DjiCloudServer.Services;
using Microsoft.AspNetCore.Mvc;
using MQTTnet.Protocol;
using System.Text.Json;

namespace DjiCloudServer.Controllers;

/// <summary>
/// Comandos de vuelo de alto nivel: RTH, cancelar RTH, etc.
/// Se publican en el tópico thing/product/{gatewaySn}/services
/// conforme a la DJI Cloud API v1 (método return_home / return_home_cancel).
/// </summary>
[ApiController]
[Route("api/drone-commands")]
public class DroneCommandController(IMqttService mqtt, IAdminDataService admin) : ControllerBase
{
    // ── Regreso a Casa (RTH) ──────────────────────────────────────────────────

    /// <summary>
    /// Ordena al dron asociado al gateway indicado que regrese al Home Point (RTH).
    /// Publica {method:"return_home"} en thing/product/{gatewaySn}/services.
    /// Resultado confirmado escuchando thing/product/{gatewaySn}/services_reply.
    /// </summary>
    [HttpPost("return-home/{gatewaySn}")]
    public async Task<IActionResult> ReturnHome(string gatewaySn)
    {
        var payload = JsonSerializer.Serialize(new
        {
            tid       = Guid.NewGuid().ToString(),
            bid       = Guid.NewGuid().ToString(),
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            gateway   = gatewaySn,
            method    = "return_home"
        });

        await mqtt.PublishAsync(
            $"thing/product/{gatewaySn}/services",
            payload,
            MqttQualityOfServiceLevel.AtLeastOnce);

        admin.AddLog("WARN", "RTH", $"return_home enviado → {gatewaySn}");
        return Ok(new { sent = true, method = "return_home", gatewaySn });
    }

    /// <summary>
    /// Cancela el RTH en curso para el gateway indicado.
    /// Publica {method:"return_home_cancel"} en thing/product/{gatewaySn}/services.
    /// El dron quedará en hover en la posición actual.
    /// </summary>
    [HttpPost("return-home-cancel/{gatewaySn}")]
    public async Task<IActionResult> ReturnHomeCancel(string gatewaySn)
    {
        var payload = JsonSerializer.Serialize(new
        {
            tid       = Guid.NewGuid().ToString(),
            bid       = Guid.NewGuid().ToString(),
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            gateway   = gatewaySn,
            method    = "return_home_cancel"
        });

        await mqtt.PublishAsync(
            $"thing/product/{gatewaySn}/services",
            payload,
            MqttQualityOfServiceLevel.AtLeastOnce);

        admin.AddLog("INFO", "RTH", $"return_home_cancel enviado → {gatewaySn}");
        return Ok(new { sent = true, method = "return_home_cancel", gatewaySn });
    }
}
