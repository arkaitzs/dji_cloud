using System.Collections.Concurrent;

namespace DjiCloudServer.Services;

/// <summary>
/// Registro de eventos en memoria para el panel de administración (#2.8).
///
/// Extraído de AdminDataService (que concentraba estado de dispositivos + logs +
/// HMS + topología + capacidades). Además corrige la pérdida de eventos: el buffer
/// único de 100 entradas hacía que el ruido de polling HTTP expulsara los eventos
/// relevantes en segundos. Ahora cada categoría tiene su propio ring buffer:
///   HTTP  → 200 entradas (ruidoso, baja señal)
///   MQTT  → 200 entradas (ruidoso)
///   resto → 400 entradas (WebSocket, Topología, DRC, errores... la señal útil)
/// GetLogs() los fusiona ordenados por timestamp.
/// </summary>
public interface IEventLogService
{
    void Add(string level, string source, string message);
    List<LogEventDto> GetLogs();
}

public class EventLogService : IEventLogService
{
    private readonly ConcurrentQueue<LogEventDto> _httpLogs    = new();
    private readonly ConcurrentQueue<LogEventDto> _mqttLogs    = new();
    private readonly ConcurrentQueue<LogEventDto> _generalLogs = new();

    private const int MaxHttpLogs    = 200;
    private const int MaxMqttLogs    = 200;
    private const int MaxGeneralLogs = 400;

    private readonly IMqttFileLogger _fileLogger;

    public EventLogService(IMqttFileLogger fileLogger) => _fileLogger = fileLogger;

    public void Add(string level, string source, string message)
    {
        var entry = new LogEventDto
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Level     = level,
            Source    = source,
            Message   = message
        };

        var (queue, max) = level switch
        {
            "HTTP" => (_httpLogs, MaxHttpLogs),
            "MQTT" => (_mqttLogs, MaxMqttLogs),
            _      => (_generalLogs, MaxGeneralLogs)
        };

        queue.Enqueue(entry);
        while (queue.Count > max)
            queue.TryDequeue(out _);

        // Registrar en el log de archivo unificado, EXCEPTO nivel MQTT (el tráfico
        // MQTT ya se registra en detalle con payload y topic en Program.cs)
        if (level != "MQTT")
            _fileLogger.Log(source, level, payload: message);
    }

    public List<LogEventDto> GetLogs() =>
        _httpLogs.Concat(_mqttLogs).Concat(_generalLogs)
            .OrderBy(l => l.Timestamp)
            .ToList();
}
