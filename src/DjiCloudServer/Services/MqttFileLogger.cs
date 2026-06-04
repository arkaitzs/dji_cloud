using System.IO;
using System.Text;
using System.Threading.Channels;

namespace DjiCloudServer.Services;

public interface IMqttFileLogger
{
    void Log(string source, string level, string topic = "", string payload = "");
}

public sealed class MqttFileLogger : IMqttFileLogger, IHostedService
{
    private readonly Channel<string> _channel;
    private readonly string _logDirectory;
    private readonly ILogger<MqttFileLogger> _logger;
    private Task? _processingTask;
    private CancellationTokenSource? _cts;

    public MqttFileLogger(ILogger<MqttFileLogger> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _logDirectory = Path.Combine(env.WebRootPath, "mqtt_logs");
        Directory.CreateDirectory(_logDirectory);

        // Canales sin límite (Unbounded) con optimización de un solo lector (SingleReader = true)
        _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public void Log(string source, string level, string topic = "", string payload = "")
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("o"); // ISO 8601 string
        
        string loggedPayload = payload;

        var escSource = EscapeCsv(source);
        var escLevel = EscapeCsv(level);
        var escTopic = EscapeCsv(topic);
        var escPayload = EscapeCsv(loggedPayload);

        var csvLine = $"{timestamp},{escSource},{escLevel},{escTopic},{escPayload}";
        _channel.Writer.TryWrite(csvLine);
    }

    private static string EscapeCsv(string? field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        
        // Si contiene comillas, comas o saltos de línea, envolver en comillas dobles y duplicar las internas
        if (field.Contains('"') || field.Contains(',') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = new CancellationTokenSource();
        _processingTask = ProcessLogsAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts != null)
        {
            await _cts.CancelAsync();
        }

        _channel.Writer.Complete();

        if (_processingTask != null)
        {
            try
            {
                await _processingTask;
            }
            catch (OperationCanceledException)
            {
                // Ignorar cancelación normal
            }
        }
    }

    private async Task ProcessLogsAsync(CancellationToken ct)
    {
        while (await _channel.Reader.WaitToReadAsync(ct))
        {
            while (_channel.Reader.TryRead(out var csvLine))
            {
                try
                {
                    var logFile = Path.Combine(_logDirectory, $"mqtt_traffic_{DateTime.Today:yyyyMMdd}.csv");
                    bool exists = File.Exists(logFile);

                    // Escribir asíncronamente con bloqueo para evitar conflictos concurrentes
                    await using var writer = new StreamWriter(logFile, append: true, encoding: Encoding.UTF8);
                    if (!exists)
                    {
                        // Escribir cabecera BOM para que Excel detecte UTF-8 automáticamente
                        await writer.WriteAsync("\uFEFF");
                        await writer.WriteLineAsync("Timestamp,Source,Level,Topic,Payload");
                    }
                    await writer.WriteLineAsync(csvLine);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[MqttFileLogger] Error escribiendo log MQTT al disco.");
                }
            }
        }
    }
}
