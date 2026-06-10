using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DjiCloudServer.Controllers;

/// <summary>
/// Endpoint para que la H5 obtenga la configuración de red y la licencia de DJI.
/// </summary>
[ApiController]
[Route("api/dji/[controller]")]
[Produces("application/json")]
public class ConfigController : ControllerBase
{
    private readonly DjiCloudOptions _options;

    public ConfigController(IOptions<DjiCloudOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet]
    public IActionResult GetMqttConfig()
    {
        // Si el host es localhost o está vacío, tomamos dinámicamente la IP con la que el mando accedió al servidor.
        // Esto evita tener que hardcodear IPs locales que cambian con el DHCP.
        var mqttHost = _options.Mqtt.Host;
        if (string.IsNullOrWhiteSpace(mqttHost) || mqttHost == "localhost" || mqttHost == "127.0.0.1")
        {
            mqttHost = Request.Host.Host;
        }

        // Token estable por vida del servidor (compartido con /manage/api/v1/login)
        var sessionToken = SessionAuth.Token;

        return Ok(new
        {
            AppId = _options.AppId,
            AppKey = _options.AppKey,
            License = _options.License,
            Mqtt = new
            {
                Host = mqttHost,
                Port = _options.Mqtt.Port,
                Username = string.IsNullOrEmpty(_options.Mqtt.Username) ? "local_user" : _options.Mqtt.Username,
                Password = string.IsNullOrEmpty(_options.Mqtt.Password) ? "local_password" : _options.Mqtt.Password,
                Token = sessionToken
            }
        });
    }
}
