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
    // URL de descarga del .kmz — Pilot 2 la usa para bajar la misión antes de volar.
    [HttpGet("wayline/api/v1/workspaces/{workspaceId}/waylines/{waylineId}/url")]
    public IActionResult GetWaylineUrl(string workspaceId, string waylineId)
    {
        var entry = LoadIndex().FirstOrDefault(w => w["id"]?.ToString() == waylineId);
        if (entry == null)
            return Ok(new { code = -1, message = "wayline not found", data = new { } });

        var fileName = entry["file_name"]?.ToString() ?? $"{waylineId}.kmz";
        var url = $"{Request.Scheme}://{Request.Host}/waylines/{fileName}";

        _logger.LogInformation("[Wayline] URL de descarga solicitada id={Id} → {Url}", waylineId, url);
        return Ok(new { code = 0, message = "success", data = url });
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
        try
        {
            if (!System.IO.File.Exists(IndexFilePath)) return new List<JObject>();
            var json = System.IO.File.ReadAllText(IndexFilePath);
            return JsonConvert.DeserializeObject<List<JObject>>(json) ?? new List<JObject>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Wayline] Error leyendo el índice — usando lista vacía");
            return new List<JObject>();
        }
    }

    private void SaveIndex(List<JObject> index)
    {
        Directory.CreateDirectory(WaylinesDir);
        System.IO.File.WriteAllText(IndexFilePath, JsonConvert.SerializeObject(index, Formatting.Indented));
    }
}
