using DjiCloudServer.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DjiCloudServer.Controllers;

/// <summary>
/// Wayline Management — DJI Cloud API v1.11.x (docs 44-50).
/// Rutas spec /wayline/api/v1/workspaces/{wid}/... que DJI Pilot 2 usa para
/// listar, descargar, subir y gestionar ficheros de misión (.kmz WPML).
///
/// Almacenamiento: wwwroot/waylines/{id}.kmz + índice waylines_index.json.
/// Los ficheros llegan vía el endpoint S3-compatible (/api/media/mock-s3) y
/// se registran aquí con el upload-callback.
/// </summary>
[ApiController]
public class DjiWaylineController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DjiWaylineController> _logger;
    private static readonly object _indexLock = new();

    public DjiWaylineController(IWebHostEnvironment env, ILogger<DjiWaylineController> logger)
    {
        _env    = env;
        _logger = logger;
    }

    private string WaylinesDir   => Path.Combine(_env.WebRootPath, "waylines");
    private string IndexFilePath => Path.Combine(WaylinesDir, "waylines_index.json");

    // ── POST /api/web-map/workspaces/{wid}/waylines/publish ─────────────────────
    // Publica en la BIBLIOTECA DE WAYLINES un .kmz generado por el planificador de
    // map.html (multipart: file=.kmz, name, start_lat, start_lng). Así el mando lo
    // ve en su lista de misiones (GET .../waylines) y puede descargarlo y volarlo.
    // (El .kmz lo genera el cliente; aquí solo se guarda y se registra en el índice.)
    [HttpPost("api/web-map/workspaces/{workspaceId}/waylines/publish")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> PublishWayline(
        string workspaceId,
        IFormFile file,
        [FromForm] string? name,
        [FromForm] double? start_lat,
        [FromForm] double? start_lng,
        [FromForm] string? drone_model_key,
        [FromForm] string? payload_model_key)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, error = "Falta el fichero .kmz." });

        var id       = Guid.NewGuid().ToString();
        var fileName = $"{id}.kmz";
        Directory.CreateDirectory(WaylinesDir);
        await using (var fs = System.IO.File.Create(Path.Combine(WaylinesDir, fileName)))
            await file.CopyToAsync(fs);

        var entry = new JObject
        {
            ["id"]                 = id,
            ["name"]               = string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(file.FileName) : name,
            ["file_name"]          = fileName,
            ["object_key"]         = fileName,
            ["drone_model_key"]    = drone_model_key ?? "0-77-1",   // M3T por defecto
            ["payload_model_keys"] = new JArray(payload_model_key ?? "1-67-0"),
            ["template_types"]     = new JArray(0),
            ["action_type"]        = 0,
            ["favorited"]          = false,
            ["user_name"]          = "Web",
            ["sign"]               = "",
            ["size"]               = file.Length,
            ["update_time"]        = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        if (start_lat.HasValue && start_lng.HasValue)
            entry["start_wayline_point"] = new JObject
            {
                ["start_latitude"]  = start_lat.Value,
                ["start_lontitude"] = start_lng.Value   // (typo del spec DJI: "lontitude")
            };

        lock (_indexLock)
        {
            var index = LoadIndex();
            index.Add(entry);
            SaveIndex(index);
        }

        _logger.LogInformation("[Wayline] Misión publicada en biblioteca desde web: '{Name}' ({Bytes} bytes) — el mando la verá al refrescar.",
            entry["name"], file.Length);

        return Ok(new { success = true, id, name = entry["name"]?.ToString(), size = file.Length });
    }

    // ── GET /wayline/api/v1/workspaces/{wid}/waylines (doc 44) ──────────────────
    // Lista paginada de waylines. Pilot 2 la pide al abrir la biblioteca de misiones.
    [HttpGet("wayline/api/v1/workspaces/{workspaceId}/waylines")]
    public IActionResult GetWaylines(
        string workspaceId,
        [FromQuery] int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 10,
        [FromQuery] bool? favorited = null,
        [FromQuery(Name = "order_by")] string? orderBy = null)
    {
        var all = LoadIndex();

        if (favorited == true)
            all = all.Where(w => w["favorited"]?.Value<bool>() == true).ToList();

        // order_by: "<columna> asc|desc" — soportamos update_time y name
        if (!string.IsNullOrEmpty(orderBy))
        {
            var desc = orderBy.EndsWith("desc", StringComparison.OrdinalIgnoreCase);
            if (orderBy.StartsWith("name", StringComparison.OrdinalIgnoreCase))
                all = desc ? all.OrderByDescending(w => w["name"]?.ToString()).ToList()
                           : all.OrderBy(w => w["name"]?.ToString()).ToList();
            else
                all = desc ? all.OrderByDescending(w => w["update_time"]?.Value<long>() ?? 0).ToList()
                           : all.OrderBy(w => w["update_time"]?.Value<long>() ?? 0).ToList();
        }

        var total = all.Count;
        var items = all.Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize);

        _logger.LogInformation("[Wayline] GET waylines workspace={WorkspaceId} → {Total} misiones", workspaceId, total);
        return Ok(new
        {
            code    = 0,
            message = "success",
            data    = new
            {
                list       = items,
                pagination = new { page, page_size = pageSize, total }
            }
        });
    }

    // ── GET /wayline/api/v1/workspaces/{wid}/waylines/{id}/url (doc 46) ─────────
    // Descarga del .kmz. CRÍTICO: la demo Java NO devuelve JSON — hace un REDIRECT 302
    // a la dirección del fichero (rsp.sendRedirect). Pilot 2 sigue el redirect y descarga
    // el .kmz directamente. Si devolvemos JSON, Pilot no encuentra el fichero y muestra
    // "archivo de ruta eliminado". Por eso aquí devolvemos Redirect, no Ok(json).
    [HttpGet("wayline/api/v1/workspaces/{workspaceId}/waylines/{waylineId}/url")]
    public IActionResult GetWaylineUrl(string workspaceId, string waylineId)
    {
        var entry = LoadIndex().FirstOrDefault(w => w["id"]?.ToString() == waylineId);
        if (entry == null)
            return NotFound(new { code = -1, message = "wayline not found" });

        var fileName = entry["file_name"]?.ToString() ?? $"{waylineId}.kmz";
        var filePath = Path.Combine(WaylinesDir, fileName);
        if (!System.IO.File.Exists(filePath))
            return NotFound(new { code = -1, message = "wayline file missing" });

        var url = $"{Request.Scheme}://{Request.Host}/waylines/{fileName}";
        _logger.LogInformation("[Wayline] Descarga id={Id} → redirect 302 a {Url}", waylineId, url);
        return Redirect(url);   // 302 → Pilot sigue el redirect y baja el .kmz
    }

    // ── GET /wayline/api/v1/workspaces/{wid}/waylines/duplicate-names (doc 47) ──
    [HttpGet("wayline/api/v1/workspaces/{workspaceId}/waylines/duplicate-names")]
    public IActionResult GetDuplicateNames(string workspaceId, [FromQuery] string[] name)
    {
        var existing = LoadIndex()
            .Select(w => w["name"]?.ToString())
            .Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var duplicated = name.Where(n => existing.Contains(n)).ToArray();
        return Ok(new { code = 0, message = "success", data = duplicated });
    }

    // ── POST /wayline/api/v1/workspaces/{wid}/upload-callback (doc 48) ──────────
    // Pilot 2 confirma la subida de un .kmz (que llegó por el endpoint S3).
    // Registramos la misión en el índice y movemos el fichero a waylines/.
    [HttpPost("wayline/api/v1/workspaces/{workspaceId}/upload-callback")]
    public IActionResult UploadCallback(string workspaceId, [FromBody] JObject body)
    {
        var objectKey = body["object_key"]?.ToString() ?? "";
        var nameField = body["name"]?.ToString() ?? Path.GetFileNameWithoutExtension(objectKey);
        var metadata  = body["metadata"] as JObject;

        if (string.IsNullOrEmpty(objectKey))
            return Ok(new { code = -1, message = "object_key required", data = new { } });

        var id       = Guid.NewGuid().ToString();
        var fileName = $"{id}.kmz";

        // Mover el fichero subido vía mock-s3 (wwwroot/media/{object_key}) a waylines/
        Directory.CreateDirectory(WaylinesDir);
        var mediaRoot  = Path.GetFullPath(Path.Combine(_env.WebRootPath, "media"));
        var sourcePath = Path.GetFullPath(Path.Combine(mediaRoot, objectKey.Replace('\\', '/')));
        if (sourcePath.StartsWith(mediaRoot, StringComparison.OrdinalIgnoreCase)
            && System.IO.File.Exists(sourcePath))
        {
            System.IO.File.Copy(sourcePath, Path.Combine(WaylinesDir, fileName), overwrite: true);
        }
        else
        {
            _logger.LogWarning("[Wayline] upload-callback sin fichero en media: object_key={ObjectKey}", objectKey);
        }

        var entry = new JObject
        {
            ["id"]                 = id,
            ["name"]               = nameField,
            ["file_name"]          = fileName,
            ["object_key"]         = objectKey,
            ["drone_model_key"]    = metadata?["drone_model_key"]?.ToString() ?? "",
            ["payload_model_keys"] = metadata?["payload_model_keys"] as JArray ?? new JArray(),
            ["template_types"]     = metadata?["template_types"] as JArray ?? new JArray(0),
            ["action_type"]        = 0,
            ["favorited"]          = false,
            ["user_name"]          = "pilot",
            ["update_time"]        = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        lock (_indexLock)
        {
            var index = LoadIndex();
            index.Add(entry);
            SaveIndex(index);
        }

        _logger.LogInformation("[Wayline] Misión registrada: id={Id} nombre='{Name}'", id, nameField);
        return Ok(new { code = 0, message = "success", data = new { } });
    }

    // ── POST/DELETE /wayline/api/v1/workspaces/{wid}/favorites (docs 49/50) ─────
    [HttpPost("wayline/api/v1/workspaces/{workspaceId}/favorites")]
    public IActionResult BatchFavorite(string workspaceId, [FromBody] JObject body) =>
        SetFavorited(body, true);

    [HttpDelete("wayline/api/v1/workspaces/{workspaceId}/favorites")]
    public IActionResult BatchUnfavorite(string workspaceId, [FromBody] JObject body) =>
        SetFavorited(body, false);

    // ── DELETE /wayline/api/v1/workspaces/{wid}/waylines/{id} ───────────────────
    [HttpDelete("wayline/api/v1/workspaces/{workspaceId}/waylines/{waylineId}")]
    public IActionResult DeleteWayline(string workspaceId, string waylineId)
    {
        lock (_indexLock)
        {
            var index = LoadIndex();
            var entry = index.FirstOrDefault(w => w["id"]?.ToString() == waylineId);
            if (entry != null)
            {
                var fileName = entry["file_name"]?.ToString();
                if (!string.IsNullOrEmpty(fileName))
                {
                    var filePath = Path.Combine(WaylinesDir, fileName);
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                }
                index.Remove(entry);
                SaveIndex(index);
                _logger.LogInformation("[Wayline] Misión eliminada: id={Id}", waylineId);
            }
        }
        return Ok(new { code = 0, message = "success", data = new { } });
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private IActionResult SetFavorited(JObject body, bool favorited)
    {
        var ids = (body["ids"] as JArray)?.Select(t => t.ToString()).ToHashSet() ?? new HashSet<string>();
        lock (_indexLock)
        {
            var index = LoadIndex();
            foreach (var entry in index.Where(w => ids.Contains(w["id"]?.ToString() ?? "")))
                entry["favorited"] = favorited;
            SaveIndex(index);
        }
        return Ok(new { code = 0, message = "success", data = new { } });
    }

    private List<JObject> LoadIndex()
    {
        // #2.7: lectura con recuperación desde .bak si el índice está corrupto
        return Services.AtomicJsonFile.ReadWithRecovery(
            IndexFilePath,
            json => JsonConvert.DeserializeObject<List<JObject>>(json),
            recoveredFrom => _logger.LogWarning(
                "[Wayline] Índice corrupto — recuperado desde {Backup}", recoveredFrom))
            ?? new List<JObject>();
    }

    private void SaveIndex(List<JObject> index)
    {
        Directory.CreateDirectory(WaylinesDir);
        // #2.7: escritura atómica con backup
        Services.AtomicJsonFile.Write(IndexFilePath, JsonConvert.SerializeObject(index, Formatting.Indented));
    }
}
