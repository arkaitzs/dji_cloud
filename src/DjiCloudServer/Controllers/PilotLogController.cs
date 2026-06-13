using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace DjiCloudServer.Controllers;

/// <summary>
/// Recepción de logs de diagnóstico desde el webview del mando (DJI Pilot 2).
///
/// La página H5 (index.html) captura todo su log de consola JSBridge (verificación
/// de licencia, resultados de platformLoadComponent, callbacks, estados de conexión)
/// y lo sube aquí con el botón "Exportar diagnóstico". Permite correlacionar lo que
/// pasa EN EL MANDO con lo que vemos en el servidor.
///
/// Los logs nativos de Pilot 2 (platformGetLogPath) van cifrados y requieren
/// PlogDecoder.jar de DJI — el endpoint /native los acepta igualmente por si el
/// webview puede leer el fichero.
/// </summary>
[ApiController]
[Route("api/dji/pilot-log")]
public class PilotLogController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PilotLogController> _logger;

    public PilotLogController(IWebHostEnvironment env, ILogger<PilotLogController> logger)
    {
        _env    = env;
        _logger = logger;
    }

    private string LogsDir => Path.Combine(_env.WebRootPath, "pilot_logs");

    // POST /api/dji/pilot-log/console — log de consola + snapshot de estado JSBridge (JSON)
    [HttpPost("console")]
    public async Task<IActionResult> UploadConsoleLog([FromBody] JObject body)
    {
        Directory.CreateDirectory(LogsDir);
        var ts       = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"console_{ts}.json";
        var path     = Path.Combine(LogsDir, fileName);

        await System.IO.File.WriteAllTextAsync(path, body.ToString(Newtonsoft.Json.Formatting.Indented));

        // Volcar el snapshot clave al log del servidor para verlo de inmediato
        var snapshot = body["bridge_state"]?.ToString(Newtonsoft.Json.Formatting.None) ?? "{}";
        var lines    = (body["console"] as JArray)?.Count ?? 0;
        _logger.LogInformation(
            "[PilotLog] Diagnóstico recibido del mando: {Lines} líneas de consola → {File} | bridge_state={Snapshot}",
            lines, fileName, snapshot);

        return Ok(new { code = 0, message = "success", data = new { file = fileName, url = $"/pilot_logs/{fileName}" } });
    }

    // POST /api/dji/pilot-log/native?name=xxx — log nativo de Pilot (binario, cifrado)
    [HttpPost("native")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadNativeLog([FromQuery] string name = "pilot.log")
    {
        Directory.CreateDirectory(LogsDir);
        var safeName = Path.GetFileName(name);
        var ts       = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"native_{ts}_{safeName}";
        var path     = Path.Combine(LogsDir, fileName);

        await using (var fs = System.IO.File.Create(path))
        {
            await Request.Body.CopyToAsync(fs);
        }

        var size = new FileInfo(path).Length;
        _logger.LogInformation("[PilotLog] Log nativo de Pilot recibido: {File} ({Size} bytes, cifrado — usar PlogDecoder.jar)",
            fileName, size);

        return Ok(new { code = 0, message = "success", data = new { file = fileName, size } });
    }

    // GET /api/dji/pilot-log/list — listado de diagnósticos guardados
    [HttpGet("list")]
    public IActionResult ListLogs()
    {
        if (!Directory.Exists(LogsDir)) return Ok(new { code = 0, message = "success", data = Array.Empty<object>() });

        var files = Directory.GetFiles(LogsDir)
            .Select(f => new FileInfo(f))
            .OrderByDescending(fi => fi.LastWriteTimeUtc)
            .Select(fi => new
            {
                name = fi.Name,
                url  = $"/pilot_logs/{fi.Name}",
                size = fi.Length,
                date = fi.LastWriteTimeUtc.ToString("O")
            });

        return Ok(new { code = 0, message = "success", data = files });
    }
}
