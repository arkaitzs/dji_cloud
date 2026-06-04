using DjiCloudServer.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json.Nodes;

namespace DjiCloudServer.Controllers;

[ApiController]
[Route("api/config")]
public class NetworkConfigController(
    IConfiguration      configuration,
    IWebHostEnvironment env,
    IAdminDataService   admin) : ControllerBase
{
    /// <summary>
    /// Devuelve la ServerIp actualmente configurada en appsettings.json
    /// y la lista de IPs IPv4 válidas detectadas en las interfaces del servidor.
    /// </summary>
    [HttpGet("network")]
    public IActionResult GetNetwork()
    {
        var serverIp = configuration["DjiCloud:ServerIp"] ?? "";
        var localIps = GetLocalIps();
        return Ok(new { serverIp, localIps });
    }

    /// <summary>
    /// Actualiza el campo DjiCloud:ServerIp en el archivo appsettings.json en disco.
    /// ASP.NET recargará la configuración automáticamente (reloadOnChange: true).
    /// </summary>
    [HttpPost("network")]
    public IActionResult SetNetwork([FromBody] SetNetworkRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Ip) || !IPAddress.TryParse(req.Ip.Trim(), out _))
            return BadRequest(new { error = "La IP proporcionada no es válida." });

        var ip = req.Ip.Trim();
        var appSettingsPath = Path.Combine(env.ContentRootPath, "appsettings.json");

        if (!System.IO.File.Exists(appSettingsPath))
            return StatusCode(500, new { error = "No se encontró appsettings.json en el directorio raíz." });

        try
        {
            var json = System.IO.File.ReadAllText(appSettingsPath);
            var root = JsonNode.Parse(json)!.AsObject();

            if (root["DjiCloud"] is not JsonObject djiCloud)
                return StatusCode(500, new { error = "No se encontró la sección DjiCloud en appsettings.json." });

            djiCloud["ServerIp"] = ip;

            var writeOptions = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(appSettingsPath, root.ToJsonString(writeOptions));
        }
        catch (Exception ex)
        {
            admin.AddLog("WARN", "Config", $"Error al actualizar ServerIp en appsettings.json: {ex.Message}");
            return StatusCode(500, new { error = $"Error al guardar la configuración: {ex.Message}" });
        }

        admin.AddLog("INFO", "Config", $"ServerIp actualizada a {ip} vía API de configuración");
        return Ok(new { success = true, serverIp = ip });
    }

    private static List<string> GetLocalIps()
    {
        var list = new List<string>();
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback) continue;
            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                var ip = ua.Address.ToString();
                if (ip.StartsWith("169.254")) continue;
                list.Add(ip);
            }
        }
        return list;
    }
}

public record SetNetworkRequest(string Ip);
