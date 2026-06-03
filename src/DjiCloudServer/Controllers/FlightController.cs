using DjiCloudServer.Models;
using DjiCloudServer.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace DjiCloudServer.Controllers;

[ApiController]
[Route("api/flights")]
public class FlightController(IFlightRecorderService recorder, IWebHostEnvironment env) : ControllerBase
{
    // Opciones cacheadas para evitar CA1869
    private static readonly JsonSerializerOptions ReadOpts =
        new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions WriteOpts =
        new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private string FlightsDir => Path.Combine(env.WebRootPath, "flights");

    // ─── Listar vuelos (activos + completados) ────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var active = recorder.GetActiveSummaries()
            .Select(SummaryDto)
            .ToList();

        var completed = new List<object>();
        if (Directory.Exists(FlightsDir))
        {
            foreach (var path in Directory.GetFiles(FlightsDir, "*.json")
                                          .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc))
            {
                try
                {
                    await using var stream = System.IO.File.OpenRead(path);
                    using var doc = await JsonDocument.ParseAsync(stream);
                    var r = doc.RootElement;
                    completed.Add(new
                    {
                        id         = r.TryGetProperty("id",         out var v) ? v.GetString()  : null,
                        droneSn    = r.TryGetProperty("droneSn",    out v)     ? v.GetString()  : null,
                        startTime  = r.TryGetProperty("startTime",  out v)     ? v.GetString()  : null,
                        endTime    = r.TryGetProperty("endTime",    out v)     ? v.GetString()  : null,
                        isActive   = false,
                        frameCount = r.TryGetProperty("frameCount", out v)     ? v.GetInt32()   : 0,
                        maxAltM    = r.TryGetProperty("maxAltM",    out v)     ? v.GetDouble()  : 0,
                        distanceM  = r.TryGetProperty("distanceM",  out v)     ? v.GetDouble()  : 0,
                        duration   = r.TryGetProperty("duration",   out v)     ? v.GetString()  : "—"
                    });
                }
                catch { /* skip corrupt file */ }
            }
        }

        return Ok(new { active, completed });
    }

    // ─── Obtener vuelo completo (frames incluidos) ────────────────────────────

    [HttpGet("{id}")]
    public IActionResult Get(string id)
    {
        var path = SafePath(id);
        if (path is null || !System.IO.File.Exists(path)) return NotFound();
        return Content(System.IO.File.ReadAllText(path), "application/json");
    }

    // ─── Exportar en diferentes formatos ─────────────────────────────────────

    [HttpGet("{id}/export")]
    public async Task<IActionResult> Export(string id, [FromQuery] string format = "json")
    {
        var path = SafePath(id);
        if (path is null || !System.IO.File.Exists(path)) return NotFound();

        await using var stream = System.IO.File.OpenRead(path);
        var session = await JsonSerializer.DeserializeAsync<FlightSession>(stream, ReadOpts);
        if (session is null) return NotFound();

        return format.ToLowerInvariant() switch
        {
            "csv" => ExportCsv(session),
            "kml" => ExportKml(session),
            _     => ExportJson(session)
        };
    }

    // ─── Borrar vuelo ─────────────────────────────────────────────────────────

    [HttpDelete("{id}")]
    public IActionResult Delete(string id)
    {
        var path = SafePath(id);
        if (path is null || !System.IO.File.Exists(path)) return NotFound();
        System.IO.File.Delete(path);
        return Ok(new { deleted = id });
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static object SummaryDto(FlightSummary s) => new
    {
        id         = s.Id,
        droneSn    = s.DroneSn,
        startTime  = s.StartTime,
        endTime    = s.EndTime,
        isActive   = s.IsActive,
        frameCount = s.FrameCount,
        maxAltM    = s.MaxAltM,
        distanceM  = s.DistanceM,
        duration   = s.Duration
    };

    private static FileContentResult ExportJson(FlightSession s)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(s, WriteOpts);
        return new FileContentResult(bytes, "application/json")
        {
            FileDownloadName = $"vuelo_{s.Id}.json"
        };
    }

    private static FileContentResult ExportCsv(FlightSession s)
    {
        var sb = new StringBuilder();
        sb.AppendLine("timestamp_ms,lat,lon,alt_m,heading_deg,gimbal_pitch,gimbal_roll,gimbal_yaw,zoom");
        foreach (var f in s.Frames)
            sb.AppendLine(
                $"{f.Ts},{f.Lat:F7},{f.Lon:F7},{f.Alt:F2},{f.Heading:F2},{f.GimbalPitch:F2},{f.GimbalRoll:F2},{f.GimbalYaw:F2},{f.Zoom:F2}");
        return new FileContentResult(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv")
        {
            FileDownloadName = $"vuelo_{s.Id}.csv"
        };
    }

    private static FileContentResult ExportKml(FlightSession s)
    {
        var coords   = string.Join("\n", s.Frames.Select(f => $"{f.Lon:F7},{f.Lat:F7},{f.Alt:F1}"));
        var startStr = s.StartTime.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
        var kml = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <kml xmlns="http://www.opengis.net/kml/2.2">
              <Document>
                <name>Vuelo {s.DroneSn} — {startStr}</name>
                <description>Frames: {s.FrameCount} · Distancia: {s.DistanceM:F0}m · Alt. máx: {s.MaxAltM:F1}m</description>
                <Style id="flightLine">
                  <LineStyle><color>ff0066ff</color><width>2</width></LineStyle>
                </Style>
                <Placemark>
                  <name>Trayectoria</name>
                  <styleUrl>#flightLine</styleUrl>
                  <LineString>
                    <extrude>1</extrude>
                    <altitudeMode>relativeToGround</altitudeMode>
                    <coordinates>{coords}</coordinates>
                  </LineString>
                </Placemark>
              </Document>
            </kml>
            """;
        return new FileContentResult(Encoding.UTF8.GetBytes(kml), "application/vnd.google-earth.kml+xml")
        {
            FileDownloadName = $"vuelo_{s.Id}.kml"
        };
    }

    private string? SafePath(string id)
    {
        var safe = Path.GetFileName(id);
        if (string.IsNullOrWhiteSpace(safe) || safe.Contains("..")) return null;
        return Path.Combine(FlightsDir, $"{safe}.json");
    }
}
