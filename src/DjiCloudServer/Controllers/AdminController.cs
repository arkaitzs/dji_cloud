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

    /// <summary>
    /// Estado estructurado del dron: modo, batería, posición, GPS, gimbal…
    /// Útil para recuperar la vista del mapa tras reconexión SignalR o recarga de página.
    /// Devuelve 404 si el SN no existe en memoria (dron nunca visto en esta sesión).
    /// </summary>
    [HttpGet("drone-state/{sn}")]
    public IActionResult GetDroneState(string sn)
    {
        var devices = _adminDataService.GetDevices();
        var device  = devices.FirstOrDefault(d =>
            string.Equals(d.Sn, sn, StringComparison.OrdinalIgnoreCase));
        if (device is null)
            return NotFound(new { error = $"Dron '{sn}' no encontrado en memoria" });
        return Ok(device);
    }

    /// <summary>
    /// Todos los drones actualmente online con su estado completo.
    /// El frontend llama a este endpoint al iniciar o al reconectar SignalR
    /// para restaurar marcadores y tarjetas sin esperar el siguiente OSD (≤2s).
    /// </summary>
    [HttpGet("active-drones")]
    public IActionResult GetActiveDrones()
    {
        var devices = _adminDataService.GetDevices()
            .Where(d => d.IsOnline
                     && d.DeviceType != "Mando"
                     && d.DeviceType != "Cliente MQTT"
                     && d.DeviceType != "Dock")
            .ToList();
        return Ok(devices);
    }

    /// <summary>Payload MQTT raw del último OSD recibido (solo para depuración).</summary>
    [HttpGet("drone-raw/{sn}")]
    public IActionResult GetDroneRaw(string sn)
    {
        var payload = _adminDataService.GetLastStatePayload(sn);
        if (payload == null)
            return NotFound(new { error = "Sin payload raw para esta aeronave" });
        return Content(payload, "application/json");
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
