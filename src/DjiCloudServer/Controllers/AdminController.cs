using Microsoft.AspNetCore.Mvc;
using DjiCloudServer.Services;
using MQTTnet.Server;
using Microsoft.Extensions.Options;

namespace DjiCloudServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IAdminDataService _adminDataService;
    private readonly MqttServer? _mqttServer;
    private readonly string _serverClientId;

    public AdminController(IAdminDataService adminDataService, MqttServer? mqttServer = null, IOptions<DjiCloudOptions>? options = null)
    {
        _adminDataService = adminDataService;
        _mqttServer = mqttServer;
        _serverClientId = options?.Value?.Mqtt?.ClientId ?? "DjiCloudServer";
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        int clientCount = 0;
        if (_mqttServer != null)
        {
            try
            {
                var clients = await _mqttServer.GetClientsAsync();
                clientCount = clients.Count(c => c.Id != _serverClientId);
            }
            catch
            {
                // Fallback en caso de error al listar clientes
            }
        }
        
        var status = _adminDataService.GetSystemStatus(clientCount);
        return Ok(status);
    }

    [HttpGet("devices")]
    public IActionResult GetDevices()
    {
        var devices = _adminDataService.GetDevices();
        return Ok(devices);
    }

    [HttpGet("gateways")]
    public IActionResult GetGateways()
    {
        var gateways = _adminDataService.GetGateways();
        return Ok(gateways);
    }

    /// <summary>Capacidad de streaming en vivo (cámaras disponibles) del dron.</summary>
    [HttpGet("live-capacity/{sn}")]
    public IActionResult GetLiveCapacity(string sn)
    {
        var cap = _adminDataService.GetLiveCapacity(sn);
        return cap is null ? NotFound(new { error = "Sin datos de live_capacity aún" }) : Ok(cap);
    }

    /// <summary>Códigos HMS (Health Management System) activos del dron.</summary>
    [HttpGet("hms/{sn}")]
    public IActionResult GetHms(string sn)
    {
        var codes = _adminDataService.GetHmsCodes(sn);
        return codes is null ? NotFound(new { error = "Sin datos HMS" }) : Ok(codes);
    }

    /// <summary>Últimas suscripciones MQTT registradas (diagnóstico de topics).</summary>
    [HttpGet("mqtt-subs")]
    public IActionResult GetMqttSubs()
    {
        var logs = _adminDataService.GetLogs();
        var subs = logs.Where(l => l.Source == "MQTT-Sub").ToList();
        return Ok(subs);
    }

    [HttpGet("logs")]
    public IActionResult GetLogs()
    {
        var logs = _adminDataService.GetLogs();
        return Ok(logs);
    }
}
