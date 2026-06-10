using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace DjiCloudServer.Controllers;

/// <summary>
/// Autenticación y workspace — DJI Cloud API v1.11.x (doc 15, Access to the Cloud Server).
/// Replica los endpoints del LoginController + WorkspaceController de la demo Java:
///   POST /manage/api/v1/login           → UserDTO con access_token + parámetros MQTT
///   POST /manage/api/v1/token/refresh   → renueva la sesión (valida x-auth-token)
///   GET  /manage/api/v1/workspaces/current → info del workspace activo
///
/// El H5 (index.html) puede usar login para obtener token+MQTT en un solo paso,
/// y Pilot 2 usa token/refresh cuando el token guardado caduca (apiGetToken).
/// </summary>
[ApiController]
public class ManageAuthController : ControllerBase
{
    private readonly DjiCloudOptions _options;
    private readonly ILogger<ManageAuthController> _logger;

    public ManageAuthController(IOptions<DjiCloudOptions> options, ILogger<ManageAuthController> logger)
    {
        _options = options.Value;
        _logger  = logger;
    }

    // POST /manage/api/v1/login
    // Body: { "username": "...", "password": "...", "flag": 1|2 }  (flag: 1=web, 2=pilot)
    [HttpPost("manage/api/v1/login")]
    public IActionResult Login([FromBody] JObject body)
    {
        var username = body["username"]?.ToString() ?? "pilot";
        var flag     = body["flag"]?.Value<int>() ?? 2;

        // Sin almacén de usuarios: cualquier credencial es aceptada (LAN cerrada).
        // El valor está en devolver la estructura UserDTO que Pilot 2/web esperan.
        _logger.LogInformation("[Auth] Login de '{Username}' (flag={Flag})", username, flag);

        return Ok(new
        {
            code    = 0,
            message = "success",
            data    = BuildUserDto(username, flag)
        });
    }

    // POST /manage/api/v1/token/refresh
    // Header: x-auth-token — la demo devuelve 401 si el token no es válido.
    [HttpPost("manage/api/v1/token/refresh")]
    public IActionResult RefreshToken()
    {
        var token = Request.Headers["x-auth-token"].FirstOrDefault();
        if (!SessionAuth.IsValid(token))
        {
            _logger.LogWarning("[Auth] token/refresh con token inválido");
            return Unauthorized(new { code = -1, message = "invalid token", data = new { } });
        }

        return Ok(new
        {
            code    = 0,
            message = "success",
            data    = BuildUserDto("pilot", 2)
        });
    }

    // GET /manage/api/v1/workspaces/current
    [HttpGet("manage/api/v1/workspaces/current")]
    public IActionResult GetCurrentWorkspace()
    {
        return Ok(new
        {
            code    = 0,
            message = "success",
            data    = new
            {
                workspace_id   = _options.WorkspaceId,
                workspace_name = "USBA Sotomayor",
                workspace_desc = "Servidor Local DJI Cloud",
                platform_name  = "DjiCloudServer",
                bind_code      = "DJICLOUD"
            }
        });
    }

    /// <summary>UserDTO con la misma forma que la demo Java (campos snake_case).</summary>
    private object BuildUserDto(string username, int userType)
    {
        var mqttHost = _options.Mqtt.Host;
        if (string.IsNullOrWhiteSpace(mqttHost) || mqttHost is "localhost" or "127.0.0.1")
            mqttHost = !string.IsNullOrWhiteSpace(_options.ServerIp) ? _options.ServerIp : Request.Host.Host;

        return new
        {
            user_id       = "1",
            username,
            user_type     = userType,
            workspace_id  = _options.WorkspaceId,
            mqtt_username = string.IsNullOrEmpty(_options.Mqtt.Username) ? "local_user" : _options.Mqtt.Username,
            mqtt_password = string.IsNullOrEmpty(_options.Mqtt.Password) ? "local_password" : _options.Mqtt.Password,
            mqtt_addr     = $"tcp://{mqttHost}:{_options.Mqtt.Port}",
            access_token  = SessionAuth.Token
        };
    }
}
