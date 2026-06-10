using DjiCloudServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace DjiCloudServer.Controllers;

[ApiController]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    private static readonly HashSet<string> VideoExtensions =
        [".mp4", ".webm", ".mkv", ".ts", ".mov", ".avi", ".m4v"];

    public MediaController(IWebHostEnvironment env) => _env = env;

    // ─── Vídeos grabados ──────────────────────────────────────────────────────

    [HttpGet("videos")]
    public IActionResult ListVideos()
    {
        var dir = Path.Combine(_env.WebRootPath, "videos");
        if (!Directory.Exists(dir)) return Ok(Array.Empty<object>());

        return Ok(Directory.GetFiles(dir)
            .Select(f => new FileInfo(f))
            .Where(fi => VideoExtensions.Contains(fi.Extension.ToLowerInvariant()))
            .OrderByDescending(fi => fi.LastWriteTimeUtc)
            .Select(fi => new
            {
                name = fi.Name,
                url  = $"/videos/{fi.Name}",
                size = fi.Length,
                date = fi.LastWriteTimeUtc.ToString("O")
            }));
    }

    [HttpPost("upload-video")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadVideo(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Archivo vacío" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!VideoExtensions.Contains(ext))
            return BadRequest(new { error = $"Extensión '{ext}' no soportada. Usa: {string.Join(", ", VideoExtensions)}" });

        var dir = Path.Combine(_env.WebRootPath, "videos");
        Directory.CreateDirectory(dir);

        var safeName = Path.GetFileName(file.FileName);
        await using var stream = System.IO.File.Create(Path.Combine(dir, safeName));
        await file.CopyToAsync(stream);

        return Ok(new { name = safeName, url = $"/videos/{safeName}", size = file.Length });
    }

    [HttpDelete("videos/{name}")]
    public IActionResult DeleteVideo(string name)
    {
        var path = Path.Combine(_env.WebRootPath, "videos", Path.GetFileName(name));
        if (!System.IO.File.Exists(path)) return NotFound();
        System.IO.File.Delete(path);
        return Ok(new { deleted = name });
    }

    // ─── Ficheros KLV ────────────────────────────────────────────────────────

    [HttpGet("klv")]
    public IActionResult ListKlv()
    {
        var dir = Path.Combine(_env.WebRootPath, "klv");
        if (!Directory.Exists(dir)) return Ok(Array.Empty<object>());

        return Ok(Directory.GetFiles(dir, "*.klv")
            .Select(f => new FileInfo(f))
            .OrderByDescending(fi => fi.LastWriteTimeUtc)
            .Select(fi => new { name = fi.Name, size = fi.Length, date = fi.LastWriteTimeUtc.ToString("O") }));
    }

    [HttpPost("upload-klv")]
    [RequestSizeLimit(200 * 1024 * 1024)] // 200 MB
    public async Task<IActionResult> UploadKlv(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Archivo vacío" });

        if (!file.FileName.EndsWith(".klv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Solo se aceptan archivos .klv" });

        var dir = Path.Combine(_env.WebRootPath, "klv");
        Directory.CreateDirectory(dir);

        var safeName = Path.GetFileName(file.FileName);
        var dest = Path.Combine(dir, safeName);

        await using (var stream = System.IO.File.Create(dest))
            await file.CopyToAsync(stream);

        var data   = await System.IO.File.ReadAllBytesAsync(dest);
        var frames = KlvParser.ParseFile(data);

        return Ok(new { name = safeName, size = file.Length, frameCount = frames.Count });
    }

    /// <summary>Devuelve los frames de un fichero KLV con paginación.</summary>
    [HttpGet("klv/{name}")]
    public async Task<IActionResult> GetKlv(string name,
        [FromQuery] int page = 0, [FromQuery] int pageSize = 200)
    {
        var path = Path.Combine(_env.WebRootPath, "klv", Path.GetFileName(name));
        if (!System.IO.File.Exists(path)) return NotFound();

        var data   = await System.IO.File.ReadAllBytesAsync(path);
        var frames = KlvParser.ParseFile(data);

        var total   = frames.Count;
        var slice   = frames.Skip(page * pageSize).Take(pageSize).ToList();

        return Ok(new { total, page, pageSize, frames = slice });
    }

    [HttpDelete("klv/{name}")]
    public IActionResult DeleteKlv(string name)
    {
        var path = Path.Combine(_env.WebRootPath, "klv", Path.GetFileName(name));
        if (!System.IO.File.Exists(path)) return NotFound();
        System.IO.File.Delete(path);
        return Ok(new { deleted = name });
    }

    // ─── Mock STS y S3 para DJI Pilot 2 / Dock ────────────────────────────────

    /// <summary>
    /// Mock endpoint para que DJI Pilot 2 obtenga credenciales STS de almacenamiento en la nube local.
    /// Esto evita errores 404 en el mando cuando se suben imágenes/vídeos o durante la conexión inicial.
    /// </summary>
    [HttpPost("/storage/api/v1/workspaces/{workspaceId}/sts")]
    public IActionResult GetStsCredentials(string workspaceId)
    {
        var requestHost = Request.Host.Host;
        var requestPort = Request.Host.Port ?? 5072;
        var apiScheme = Request.Scheme;

        return Ok(new
        {
            code = 0,
            message = "success",
            data = new
            {
                bucket = "local-media-bucket",
                credentials = new
                {
                    access_key_id = "mock_access_key_id",
                    access_key_secret = "mock_access_key_secret",
                    expire = 3600,
                    security_token = "mock_security_token"
                },
                endpoint = $"{apiScheme}://{requestHost}:{requestPort}/api/media/mock-s3",
                object_key_prefix = workspaceId,
                provider = "minio",
                region = "local-lan"
            }
        });
    }

    /// <summary>
    /// Endpoint S3-compatible (estilo MinIO) para las subidas de DJI Pilot 2.
    /// El cuerpo del PUT/POST se PERSISTE en wwwroot/media/{object_key} — antes
    /// se descartaba y el ciclo de media terminaba sin guardar nada (gap #1.9).
    /// </summary>
    [HttpPut("/api/media/mock-s3/{**path}")]
    [HttpPost("/api/media/mock-s3/{**path}")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> MockS3Upload(string path)
    {
        // Sanitizar el object_key: impedir path traversal fuera de wwwroot/media
        var mediaRoot = Path.GetFullPath(Path.Combine(_env.WebRootPath, "media"));
        var target    = Path.GetFullPath(Path.Combine(mediaRoot, path.Replace('\\', '/')));
        if (!target.StartsWith(mediaRoot, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "object_key inválido" });

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        await using (var fs = System.IO.File.Create(target))
        {
            await Request.Body.CopyToAsync(fs);
        }

        // Registrar el object_key como subido para que fast-upload lo detecte
        _uploadedKeys.TryAdd(path, 0);
        return Ok();
    }

    // ─── DJI Cloud API — ciclo de subida de media ─────────────────────────────

    // POST /media/api/v1/workspaces/{wid}/fast-upload
    // DJI Pilot 2 llama antes de subir un fichero para comprobar si ya existe.
    // Si devuelve 404, el RC sube igualmente pero nunca puede confirmar la subida
    // y entra en bucle infinito. Con esta respuesta el ciclo avanza correctamente.
    [HttpPost("/media/api/v1/workspaces/{workspaceId}/fast-upload")]
    public IActionResult FastUpload(string workspaceId, [FromBody] Newtonsoft.Json.Linq.JObject body)
    {
        var objectKey = body?["object_key"]?.ToString() ?? "";
        var name      = body?["name"]?.ToString() ?? "";

        // Normalizar: buscar si el objeto ya fue subido al mock-S3
        // La clave en _uploadedKeys incluye el prefijo del bucket: "local-media-bucket/wid/filename"
        var fullKey = $"local-media-bucket/{workspaceId}/{name}";
        var alreadyUploaded = !string.IsNullOrEmpty(fullKey) && _uploadedKeys.ContainsKey(fullKey);

        return Ok(new
        {
            code    = 0,
            message = "success",
            data    = new
            {
                exist       = alreadyUploaded,
                upload_id   = (string?)null,
                file_parts  = Array.Empty<object>()
            }
        });
    }

    // POST /media/api/v1/workspaces/{wid}/upload-callback
    // DJI Pilot 2 llama tras completar la subida S3 para confirmar que el servidor
    // registró el fichero. Sin este endpoint (404), el RC entra en bucle infinito
    // resubiendo el mismo fichero indefinidamente. Esta respuesta rompe el bucle.
    [HttpPost("/media/api/v1/workspaces/{workspaceId}/upload-callback")]
    public IActionResult UploadCallback(string workspaceId, [FromBody] Newtonsoft.Json.Linq.JObject body)
    {
        var objectKey = body?["object_key"]?.ToString() ?? "";
        // Marcar como "confirmado" para que fast-upload lo detecte en futuras sesiones
        if (!string.IsNullOrEmpty(objectKey))
            _uploadedKeys.TryAdd($"local-media-bucket/{objectKey}", 0);

        return Ok(new { code = 0, message = "success", data = new { } });
    }

    // Registro en memoria de object_keys subidos en esta sesión.
    // Permite que fast-upload devuelva exist=true para ficheros ya recibidos,
    // evitando resubidas innecesarias y el bucle infinito de upload-callback.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _uploadedKeys = new();
}
