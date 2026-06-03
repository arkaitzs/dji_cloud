using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace DjiCloudServer.Controllers;

/// <summary>
/// Almacenamiento CRUD de rutas de waypoints en wwwroot/routes/ como JSON.
/// </summary>
[ApiController]
[Route("api/routes")]
public class RouteStoreController(IWebHostEnvironment env) : ControllerBase
{
    private string RoutesDir => Path.Combine(env.WebRootPath, "routes");

    // ─── Listar rutas ─────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult List()
    {
        Directory.CreateDirectory(RoutesDir);
        var summaries = Directory.GetFiles(RoutesDir, "*.json")
            .Select(f =>
            {
                try
                {
                    using var stream = System.IO.File.OpenRead(f);
                    var doc = JsonDocument.Parse(stream);
                    var root = doc.RootElement;
                    return new
                    {
                        id           = root.GetProperty("id").GetString(),
                        name         = root.TryGetProperty("name",  out var n) ? n.GetString() : "Ruta",
                        color        = root.TryGetProperty("color", out var c) ? c.GetString() : "#6366f1",
                        droneAssigned= root.TryGetProperty("droneAssigned", out var d) ? d.GetString() : null,
                        created      = root.TryGetProperty("created",  out var cr) ? cr.GetString() : null,
                        updated      = root.TryGetProperty("updated",  out var u)  ? u.GetString() : null,
                        waypointCount= root.TryGetProperty("waypoints", out var wps) && wps.ValueKind == JsonValueKind.Array
                                        ? wps.GetArrayLength() : 0
                    };
                }
                catch { return null; }
            })
            .Where(x => x != null)
            .OrderByDescending(x => x!.updated ?? x.created)
            .ToList();

        return Ok(summaries);
    }

    // ─── Obtener ruta completa ────────────────────────────────────────────────

    [HttpGet("{id}")]
    public IActionResult Get(string id)
    {
        var path = SafePath(id);
        if (path is null || !System.IO.File.Exists(path)) return NotFound();
        return Content(System.IO.File.ReadAllText(path), "application/json");
    }

    // ─── Guardar / actualizar ruta ────────────────────────────────────────────
    // Se lee el body directamente del stream para evitar el conflicto entre
    // AddNewtonsoftJson() (configurado globalmente) y JsonElement (System.Text.Json).

    [HttpPost]
    public async Task<IActionResult> Save()
    {
        Directory.CreateDirectory(RoutesDir);

        string rawBody;
        using (var reader = new System.IO.StreamReader(Request.Body, Encoding.UTF8))
            rawBody = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(rawBody))
            return BadRequest(new { error = "Cuerpo vacío" });

        using var doc = JsonDocument.Parse(rawBody);
        var body = doc.RootElement;

        string id;
        if (body.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(idProp.GetString()))
        {
            id = idProp.GetString()!;
        }
        else
        {
            id = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid().ToString("N")[..6]}";
        }

        var path = SafePath(id);
        if (path is null) return BadRequest(new { error = "ID inválido" });

        // Enriquecer con id y timestamps preservando el resto del JSON intacto
        var dict = body.EnumerateObject()
                       .ToDictionary(p => p.Name, p => (object?)p.Value.Clone());

        dict["id"]      = id;
        dict["updated"] = DateTimeOffset.UtcNow.ToString("O");
        if (!dict.ContainsKey("created") || dict["created"] is null)
            dict["created"] = DateTimeOffset.UtcNow.ToString("O");

        var json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
        await System.IO.File.WriteAllTextAsync(path, json);

        return Ok(new { id });
    }

    // ─── Eliminar ruta ────────────────────────────────────────────────────────

    [HttpDelete("{id}")]
    public IActionResult Delete(string id)
    {
        var path = SafePath(id);
        if (path is null || !System.IO.File.Exists(path)) return NotFound();
        System.IO.File.Delete(path);
        return Ok(new { deleted = id });
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private string? SafePath(string id)
    {
        // Evitar path traversal
        var safe = Path.GetFileName(id);
        if (string.IsNullOrWhiteSpace(safe) || safe.Contains("..")) return null;
        return Path.Combine(RoutesDir, $"{safe}.json");
    }
}
