using DjiCloudServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MQTTnet.Protocol;
using System.Net;
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
    /// Resolución de IP (en orden de prioridad):
    ///   1. serverIp explícita en QueryString → se usa directamente (override manual)
    ///   2. IP del cliente HTTP está en una subred conocida → se selecciona la interfaz local de esa subred
    ///   3. Solo hay 1 IP local válida → se usa
    ///   4. Múltiples IPs y sin coincidencia de subred → 409 MULTIPLE_IPS para que el operador elija
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

        var configuredIp = options.Value.ServerIp?.Trim();

        if (!string.IsNullOrWhiteSpace(serverIp))
        {
            // Override manual — el operador eligió explícitamente esta IP (modal frontend)
            resolvedIp = serverIp.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(configuredIp) && IsLocalIpAddress(configuredIp))
        {
            // IP configurada en appsettings.json → fuente de verdad permanente
            resolvedIp = configuredIp;
            admin.AddLog("INFO", "DRC", $"IP tomada de appsettings.json: {resolvedIp}");
        }
        else
        {
            // ── Resolución automática en tres pasos ──────────────────────────

            // Paso 1: subred del cliente HTTP (falla si el browser está en el mismo PC)
            var clientIp = HttpContext.Connection.RemoteIpAddress;
            if (clientIp is not null && clientIp.IsIPv4MappedToIPv6)
                clientIp = clientIp.MapToIPv4();

            string? auto = null;
            if (clientIp is not null && !IPAddress.IsLoopback(clientIp) &&
                clientIp.AddressFamily == AddressFamily.InterNetwork)
            {
                auto = GetLocalIpOnSubnet(clientIp);
                if (auto is not null)
                    admin.AddLog("INFO", "DRC", $"IP por subred del cliente: {auto}");
            }

            // Paso 2: heurística de adaptador LAN real
            // Busca la interfaz que tiene gateway configurado Y cuyo IP no termina en .1
            // (las IPs .x.x.1 suelen ser adaptadores virtuales que hacen de gateway)
            auto ??= GetPreferredLanIp();
            if (auto is not null)
                admin.AddLog("INFO", "DRC", $"IP seleccionada por heurística LAN: {auto}");

            if (auto is not null)
            {
                resolvedIp = auto;
            }
            else
            {
                // Paso 3: fallback final — pedir confirmación al usuario (solo si la
                // heurística no logró reducir a una única candidata)
                var allIps = GetAllLocalIps();
                if (allIps.Count == 0)
                    return BadRequest(new { error = "NO_LOCAL_IP",
                        message = "No se encontró ninguna IP local IPv4 válida." });

                if (allIps.Count == 1)
                {
                    resolvedIp = allIps[0];
                }
                else
                {
                    return Conflict(new
                    {
                        error     = "MULTIPLE_IPS",
                        message   = "No se pudo determinar la IP automáticamente. Elige la de la LAN del dron.",
                        ips       = allIps
                    });
                }
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
    /// Selecciona automáticamente la IP del adaptador LAN real usando dos heurísticas:
    /// 1. La interfaz tiene un default gateway IPv4 configurado (descarta adaptadores virtuales sin salida).
    /// 2. El último octeto de la IP NO es 1 (las IPs .x.x.1 suelen ser el propio host actuando de gateway
    ///    en adaptadores Hyper-V, VMware, hotspot o VPN).
    /// Devuelve la IP si hay exactamente un candidato válido; null si hay ambigüedad.
    /// </summary>
    private static string? GetPreferredLanIp()
    {
        var candidates = new List<string>();

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback) continue;

            var props = ni.GetIPProperties();

            // Heurística 1: la interfaz debe tener al menos un gateway IPv4 configurado
            bool hasIpv4Gateway = props.GatewayAddresses
                .Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork);
            if (!hasIpv4Gateway) continue;

            foreach (var ua in props.UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                var ip = ua.Address.ToString();
                if (ip.StartsWith("169.254")) continue;

                // Heurística 2: descartar IPs que terminan en .1 (el host es el propio router)
                var lastOctet = ip.Split('.').LastOrDefault();
                if (lastOctet == "1") continue;

                candidates.Add(ip);
            }
        }

        // Solo devolver si la heurística redujo a exactamente un candidato
        return candidates.Count == 1 ? candidates[0] : null;
    }

    /// <summary>
    /// Busca la IP local del servidor que pertenece a la misma subred que <paramref name="clientIp"/>.
    /// Usa la máscara de red de cada interfaz para calcular la dirección de red y compararlas.
    /// </summary>
    private static string? GetLocalIpOnSubnet(IPAddress clientIp)
    {
        var clientBytes = clientIp.GetAddressBytes();

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback) continue;

            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;

                var localBytes = ua.Address.GetAddressBytes();
                var maskBytes  = ua.IPv4Mask?.GetAddressBytes();
                if (maskBytes is null || maskBytes.Length != 4) continue;

                // Comparar las direcciones de red: (IP AND mask) debe coincidir
                var sameSubnet = true;
                for (var i = 0; i < 4; i++)
                {
                    if ((clientBytes[i] & maskBytes[i]) != (localBytes[i] & maskBytes[i]))
                    {
                        sameSubnet = false;
                        break;
                    }
                }

                if (sameSubnet)
                    return ua.Address.ToString();
            }
        }

        return null; // Sin coincidencia de subred
    }

    /// <summary>
    /// Devuelve todas las IPs IPv4 locales válidas (excluye loopback y link-local).
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
                if (ip.StartsWith("169.254")) continue;
                result.Add(ip);
            }
        }
        return result;
    }

    private static bool IsLocalIpAddress(string ip)
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                if (ua.Address.ToString() == ip) return true;
            }
        }
        return false;
    }
}
