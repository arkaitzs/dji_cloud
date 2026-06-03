namespace DjiCloudServer.Models;

/// <summary>Un frame de telemetría capturado durante el vuelo.</summary>
public record TelemetryFrame(
    long   Ts,           // Unix ms
    double Lat,
    double Lon,
    double Alt,
    double Heading,
    double GimbalPitch,
    double GimbalRoll,
    double GimbalYaw,
    double Zoom
);

/// <summary>Resumen de un vuelo (sin frames — para listados).</summary>
public class FlightSummary
{
    public string    Id         { get; set; } = "";
    public string    DroneSn    { get; set; } = "";
    public DateTime  StartTime  { get; set; }
    public DateTime? EndTime    { get; set; }
    public bool      IsActive   { get; set; }
    public int       FrameCount { get; set; }
    public double    MaxAltM    { get; set; }
    public double    DistanceM  { get; set; }
    public string    Duration   => EndTime.HasValue
        ? (EndTime.Value - StartTime).ToString(@"hh\:mm\:ss")
        : (DateTime.UtcNow - StartTime).ToString(@"hh\:mm\:ss");
}

/// <summary>Sesión de vuelo completa con frames.</summary>
public class FlightSession : FlightSummary
{
    public List<TelemetryFrame> Frames { get; set; } = [];
}
