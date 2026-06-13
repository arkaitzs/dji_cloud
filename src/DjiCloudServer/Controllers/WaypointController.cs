using DjiCloudServer.Services;
using Microsoft.AspNetCore.Mvc;
using MQTTnet;
using MQTTnet.Server;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DjiCloudServer.Controllers;

[ApiController]
[Route("api/waypoints")]
public class WaypointController(
    IWebHostEnvironment env,
    MqttServer mqttServer,
    IAdminDataService adminData) : ControllerBase
{
    // ─── Guardar KMZ en disco y devolver URL accesible ───────────────────────

    [HttpPost("save")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> SaveKmz(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Archivo vacío" });

        if (!file.FileName.EndsWith(".kmz", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Solo se aceptan archivos .kmz" });

        var dir = Path.Combine(env.WebRootPath, "missions");
        Directory.CreateDirectory(dir);

        var fileName = $"mission_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.kmz";
        var dest     = Path.Combine(dir, fileName);

        await using var stream = System.IO.File.Create(dest);
        await file.CopyToAsync(stream);
        stream.Close();

        // MD5 fingerprint para DJI
        var bytes       = await System.IO.File.ReadAllBytesAsync(dest);
        var fingerprint = Convert.ToHexString(MD5.HashData(bytes)).ToLowerInvariant();

        // La URL se construye a partir de la petición actual
        var baseUrl  = $"{Request.Scheme}://{Request.Host}";
        var fileUrl  = $"{baseUrl}/missions/{fileName}";

        return Ok(new
        {
            fileName,
            fileUrl,
            fingerprint,
            size = bytes.Length
        });
    }

    // ─── Enviar misión al dron vía MQTT (DJI Cloud API flighttask_create) ────

    [HttpPost("send/{sn}")]
    public async Task<IActionResult> SendMission(string sn, [FromBody] SendMissionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FileUrl))
            return BadRequest(new { error = "Se requiere fileUrl" });

        var bid       = Guid.NewGuid().ToString();
        var flightId  = Guid.NewGuid().ToString();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Payload DJI Cloud API v1 — flighttask_create
        var payload = new
        {
            bid,
            timestamp,
            method = "flighttask_create",
            data   = new
            {
                flight_id    = flightId,
                execute_time = 0,
                task_type    = 0,   // 0 = inmediato
                wayline_file = new
                {
                    url         = req.FileUrl,
                    fingerprint = req.Fingerprint ?? "",
                    size        = req.Size
                }
            }
        };

        var json    = JsonSerializer.Serialize(payload);

        // #3.1: services es un tópico de GATEWAY (doc 27). Si el cliente pasó el SN
        // de la aeronave, resolver su mando/gateway pareado — un comando publicado
        // bajo el SN de la aeronave no lo escucha nadie.
        var gatewaySn = sn;
        if (!adminData.IsGateway(sn)) // si es una aeronave, resolver su mando/gateway
            gatewaySn = adminData.GetGatewayForAircraft(sn) ?? sn;

        var topic   = $"thing/product/{gatewaySn}/services";

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(json))
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await mqttServer.InjectApplicationMessage(
            new InjectedMqttApplicationMessage(message) { SenderClientId = "CloudServer" });

        return Ok(new
        {
            sent    = true,
            topic,
            bid,
            flightId
        });
    }
}

public record SendMissionRequest(string FileUrl, string? Fingerprint, long Size);
