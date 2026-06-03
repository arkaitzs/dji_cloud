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
}
