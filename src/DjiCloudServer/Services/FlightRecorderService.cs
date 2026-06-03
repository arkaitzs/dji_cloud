using DjiCloudServer.Models;
using System.Text.Json;

namespace DjiCloudServer.Services;

public interface IFlightRecorderService
{
    void AddFrame(string sn, double lat, double lon, double alt,
                  double heading, double gimbalPitch, double gimbalRoll, double gimbalYaw, double zoom);
    FlightSummary? GetActiveSummary(string sn);
    IReadOnlyList<FlightSummary> GetActiveSummaries();
}

/// <summary>
/// Graba telemetría en sesiones de vuelo.
/// Inicia una sesión automáticamente al recibir el primer frame de un SN,
/// la cierra cuando no llega telemetría en más de 30 segundos.
/// </summary>
public sealed class FlightRecorderService : BackgroundService, IFlightRecorderService
{
    private readonly ILogger<FlightRecorderService> _logger;
    private readonly string _flightsDir;

    // Sesiones activas (SN → sesión) — protegidas por _lock
    private readonly Dictionary<string, FlightSession> _active = new();
    private readonly Lock _lock = new();

    private static readonly JsonSerializerOptions _json =
        new() { WriteIndented = false, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public FlightRecorderService(ILogger<FlightRecorderService> logger, IWebHostEnvironment env)
    {
        _logger     = logger;
        _flightsDir = Path.Combine(env.WebRootPath, "flights");
        Directory.CreateDirectory(_flightsDir);
    }

    // ─── IFlightRecorderService ───────────────────────────────────────────────

    public void AddFrame(string sn, double lat, double lon, double alt,
                         double heading, double gimbalPitch, double gimbalRoll, double gimbalYaw, double zoom)
    {
        var frame = new TelemetryFrame(
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            lat, lon, alt, heading, gimbalPitch, gimbalRoll, gimbalYaw, zoom);

        lock (_lock)
        {
            if (!_active.TryGetValue(sn, out var session))
            {
                session = new FlightSession
                {
                    Id        = $"{sn}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                    DroneSn   = sn,
                    StartTime = DateTime.UtcNow,
                    IsActive  = true
                };
                _active[sn] = session;
                _logger.LogInformation("[FlightRecorder] Vuelo iniciado: {Id}", session.Id);
            }

            session.Frames.Add(frame);
            session.FrameCount = session.Frames.Count;
            session.MaxAltM    = Math.Max(session.MaxAltM, alt);
        }
    }

    public FlightSummary? GetActiveSummary(string sn)
    {
        lock (_lock)
        {
            if (!_active.TryGetValue(sn, out var s)) return null;
            return BuildSummary(s);
        }
    }

    public IReadOnlyList<FlightSummary> GetActiveSummaries()
    {
        lock (_lock)
            return [.._active.Values.Select(BuildSummary)];
    }

    // ─── Background: cierre de sesiones inactivas ─────────────────────────────

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(20_000, ct);
            await CheckAndCloseInactiveAsync();
        }
    }

    private async Task CheckAndCloseInactiveAsync()
    {
        List<(string sn, FlightSession session)> toClose = [];

        lock (_lock)
        {
            var cutoff = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 30_000; // 30 s sin telemetría
            foreach (var (sn, session) in _active)
            {
                var lastTs = session.Frames.Count > 0
                    ? session.Frames[^1].Ts
                    : 0;
                if (lastTs < cutoff)
                    toClose.Add((sn, session));
            }
            foreach (var (sn, _) in toClose)
                _active.Remove(sn);
        }

        foreach (var (_, session) in toClose)
            await PersistAsync(session);
    }

    // ─── Persistencia ─────────────────────────────────────────────────────────

    private async Task PersistAsync(FlightSession session)
    {
        try
        {
            session.IsActive  = false;
            session.EndTime   = DateTime.UtcNow;
            session.DistanceM = CalculateDistance(session.Frames);

            var path = Path.Combine(_flightsDir, $"{session.Id}.json");
            await using var file = File.Create(path);
            await JsonSerializer.SerializeAsync(file, session, _json);
            _logger.LogInformation("[FlightRecorder] Vuelo guardado: {Id} ({Count} frames, {Dist:F0}m)",
                session.Id, session.Frames.Count, session.DistanceM);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FlightRecorder] Error persistiendo vuelo {Id}", session.Id);
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static FlightSummary BuildSummary(FlightSession s) => new()
    {
        Id         = s.Id,
        DroneSn    = s.DroneSn,
        StartTime  = s.StartTime,
        EndTime    = s.EndTime,
        IsActive   = s.IsActive,
        FrameCount = s.Frames.Count,
        MaxAltM    = s.MaxAltM,
        DistanceM  = s.DistanceM > 0 ? s.DistanceM : CalculateDistance(s.Frames)
    };

    private static double CalculateDistance(List<TelemetryFrame> frames)
    {
        if (frames.Count < 2) return 0;
        const double R = 6371000, toRad = Math.PI / 180;
        double total = 0;
        for (int i = 1; i < frames.Count; i++)
        {
            var (p, c) = (frames[i - 1], frames[i]);
            double dLat = (c.Lat - p.Lat) * toRad, dLon = (c.Lon - p.Lon) * toRad;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(p.Lat * toRad) * Math.Cos(c.Lat * toRad)
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            total += R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
        return total;
    }
}
