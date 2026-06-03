using DjiCloudServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MQTTnet.Protocol;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;

namespace DjiCloudServer.Controllers;

[ApiController]
[Route("api/drc")]
public class DrcController(
    IMqttService              mqtt,
    IAdminDataService         admin,
    IOptions<DjiCloudOptions> options) : ControllerBase
{
    /// <summary>
    /// Activa el modo DRC (telemetría alta frecuencia) para el gateway indicado.
    ///
    /// Si serverIp viene informado → se usa directamente.
    /// Si no viene y la máquina tiene exactamente 1 IP → se usa esa.
    /// Si la máquina tiene varias IPs → 409 MULTIPLE_IPS con la lista para que
    /// el frontend muestre el selector al operador.
    /// </summary>
    [HttpPost("enter/{gatewaySn}")]
    public async Task<IActionResult> Enter(
        string gatewaySn,
        [FromQuery] int     osdHz    = 10,
        [FromQuery] string? serverIp = null)
    {
        var mqttOpts = options.Value.Mqtt;
        var osdFreq  = Math.Clamp(osdHz, 1, 30);

        // ── Resolución de IP ──────────────────────────────────────────────────
        string resolvedIp;

        if (!string.IsNullOrWhiteSpace(serverIp))
        {
            // IP explícita enviada por el frontend (después de que el usuario eligió)
            resolvedIp = serverIp.Trim();
        }
        else
        {
            var allIps = GetAllLocalIps();

            if (allIps.Count == 0)
                return BadRequest(new
                {
                    error   = "NO_LOCAL_IP",
                    message = "No se encontró ninguna IP local IPv4 válida en este servidor. " +
                              "Configura ServerIp en appsettings.json."
                });

            if (allIps.Count == 1)
            {
                resolvedIp = allIps[0];
            }
            else
            {
                // Varias interfaces → el operador debe elegir
                return Conflict(new
                {
                    error   = "MULTIPLE_IPS",
                    message = "El servidor tiene varias interfaces de red. Elige la IP alcanzable por el dron.",
                    ips     = allIps
                });
            }
        }

        // ── Payload DJI Cloud API drc_mode_enter ─────────────────────────────
        var payload = JsonSerializer.Serialize(new
        {
            tid       = Guid.NewGuid().ToString(),
            bid       = Guid.NewGuid().ToString(),
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            gateway   = gatewaySn,
            method    = "drc_mode_enter",
            data      = new
            {
                mqtt_broker = new
                {
                    address     = $"{resolvedIp}:{mqttOpts.Port}",
                    username    = mqttOpts.Username ?? "",
                    password    = mqttOpts.Password ?? "",
                    client_id   = $"drc-{gatewaySn[..Math.Min(8, gatewaySn.Length)]}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                    enable_tls  = false,
                    expire_time = 86400
                },
                osd_frequency = osdFreq,
                hsi_frequency = 1
            }
        });

        try
        {
            await mqtt.PublishAsync(
                $"thing/product/{gatewaySn}/services",
                payload,
                MqttQualityOfServiceLevel.AtLeastOnce);
        }
        catch (Exception ex)
        {
            admin.AddLog("WARN", "DRC", $"drc_mode_enter fallo MQTT → {gatewaySn}: {ex.Message}");
            return StatusCode(503, new { error = $"MQTT no disponible: {ex.Message}" });
        }

        admin.SetDrcActive(gatewaySn, true);
        admin.AddLog("INFO", "DRC",
            $"drc_mode_enter → {gatewaySn} | broker={resolvedIp}:{mqttOpts.Port} | osd={osdFreq}Hz");

        return Ok(new
        {
            active        = true,
            gatewaySn,
            brokerAddress = $"{resolvedIp}:{mqttOpts.Port}",
            osdFrequency  = osdFreq
        });
    }

    /// <summary>Sale del modo DRC para el gateway indicado.</summary>
    [HttpPost("exit/{gatewaySn}")]
    public async Task<IActionResult> Exit(string gatewaySn)
    {
        var payload = JsonSerializer.Serialize(new
        {
            tid       = Guid.NewGuid().ToString(),
            bid       = Guid.NewGuid().ToString(),
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            gateway   = gatewaySn,
            method    = "drc_mode_exit"
        });

        try
        {
            await mqtt.PublishAsync(
                $"thing/product/{gatewaySn}/services",
                payload,
                MqttQualityOfServiceLevel.AtLeastOnce);
        }
        catch (Exception ex)
        {
            admin.AddLog("WARN", "DRC", $"drc_mode_exit fallo MQTT → {gatewaySn}: {ex.Message}");
        }

        admin.SetDrcActive(gatewaySn, false);
        admin.AddLog("INFO", "DRC", $"drc_mode_exit → {gatewaySn}");
        return Ok(new { active = false, gatewaySn });
    }

    /// <summary>Devuelve el estado DRC del gateway.</summary>
    [HttpGet("status/{gatewaySn}")]
    public IActionResult Status(string gatewaySn)
        => Ok(new { active = admin.IsDrcActive(gatewaySn), gatewaySn });

    /// <summary>Lista todas las IPs locales IPv4 válidas (diagnóstico).</summary>
    [HttpGet("local-ips")]
    public IActionResult LocalIps()
        => Ok(new { ips = GetAllLocalIps() });

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve todas las IPs IPv4 locales que pueden ser alcanzables
    /// por un dispositivo en la red (excluye loopback y link-local).
    /// </summary>
    private static List<string> GetAllLocalIps()
    {
        var result = new List<string>();
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback) continue;
            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                var ip = ua.Address.ToString();
                if (ip.StartsWith("169.254")) continue;  // link-local / APIPA
                result.Add(ip);
            }
        }
        return result;
    }
}
