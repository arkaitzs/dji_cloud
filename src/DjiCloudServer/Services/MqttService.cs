using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Microsoft.Extensions.Options;
using System.Text;

namespace DjiCloudServer.Services;

public interface IMqttService
{
    Task PublishAsync(string topic, string payload, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce);
    Task SubscribeAsync(string topicFilter);
    bool IsConnected { get; }
    event EventHandler<MqttMessageReceivedEventArgs>? MessageReceived;
}

public class MqttMessageReceivedEventArgs : EventArgs
{
    public string Topic   { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
}

/// <summary>
/// Cliente MQTT que se comunica con el broker embebido para enviar/recibir
/// mensajes DJI Cloud API. Incluye reconexión automática con back-off exponencial
/// y re-suscripción completa tras cada reconexión (necesario con CleanSession=true).
/// </summary>
public class MqttService : IMqttService, IHostedService, IAsyncDisposable
{
    private readonly ILogger<MqttService> _logger;
    private readonly MqttOptions          _options;
    private readonly IMqttClient          _client;

    // Guardamos las opciones para poder reconectar con los mismos parámetros
    private MqttClientOptions? _connectOptions;

    // Flag para distinguir desconexión voluntaria (StopAsync) de un corte de red
    private volatile bool _stopping = false;

    // Token de la aplicación para cancelar el bucle de reconexión al apagar
    private CancellationToken _appToken = CancellationToken.None;

    public bool IsConnected => _client.IsConnected;

    public event EventHandler<MqttMessageReceivedEventArgs>? MessageReceived;

    public MqttService(ILogger<MqttService> logger, IOptions<DjiCloudOptions> options)
    {
        _logger  = logger;
        _options = options.Value.Mqtt;

        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();

        _client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _client.DisconnectedAsync               += OnDisconnectedAsync;
    }

    // ─── IHostedService ──────────────────────────────────────────────────────

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _appToken = cancellationToken;

        _connectOptions = new MqttClientOptionsBuilder()
            .WithClientId(_options.ClientId)
            .WithTcpServer(_options.Host, _options.Port)
            .WithCredentials(_options.Username, _options.Password)
            .WithCleanSession()
            .Build();

        try
        {
            await _client.ConnectAsync(_connectOptions, cancellationToken);
            _logger.LogInformation("✅ MQTT conectado a {Host}:{Port} (ClientId={Id})",
                _options.Host, _options.Port, _options.ClientId);
            await SubscribeToAllTopicsAsync();
        }
        catch (Exception ex)
        {
            // LogCritical porque sin MQTT el servidor no puede enviar comandos al dron
            _logger.LogCritical(ex,
                "❌ CRÍTICO: No se pudo conectar al broker MQTT en {Host}:{Port}. " +
                "Verifica que el broker esté activo y que las credenciales sean correctas. " +
                "El servidor arrancará pero NO podrá enviar comandos de stream.",
                _options.Host, _options.Port);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stopping = true;
        if (_client.IsConnected)
            await _client.DisconnectAsync(cancellationToken: cancellationToken);
    }

    // ─── IMqttService ─────────────────────────────────────────────────────────

    public async Task PublishAsync(string topic, string payload,
        MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce)
    {
        if (!_client.IsConnected)
        {
            _logger.LogError(
                "[MQTT] ❌ Cliente desconectado — no se puede publicar en {Topic}. " +
                "Espera a la reconexión automática o reinicia el servidor.", topic);
            throw new InvalidOperationException(
                $"El cliente MQTT no está conectado. No se publicó en '{topic}'.");
        }

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(qos)
            .Build();

        _logger.LogInformation("[MQTT] 📤 Publicando → {Topic}", topic);
        await _client.PublishAsync(message);
        _logger.LogInformation("[MQTT] ✅ Publicado → {Topic}", topic);
    }

    public async Task SubscribeAsync(string topicFilter)
    {
        var opts = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(topicFilter, MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _client.SubscribeAsync(opts);
        _logger.LogDebug("📡 MQTT suscrito → {Topic}", topicFilter);
    }

    // ─── Handlers ─────────────────────────────────────────────────────────────

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);
        _logger.LogDebug("📥 MQTT ← {Topic}", args.ApplicationMessage.Topic);

        MessageReceived?.Invoke(this, new MqttMessageReceivedEventArgs
        {
            Topic   = args.ApplicationMessage.Topic,
            Payload = payload
        });

        return Task.CompletedTask;
    }

    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        // Desconexión voluntaria al apagar — no reconectar
        if (_stopping) return;

        var reason    = args.Reason.ToString();
        var exMessage = args.Exception?.Message ?? "sin excepción";
        _logger.LogWarning(
            "⚠️  MQTT desconectado. Razón: {Reason} | Error: {Exception}",
            reason, exMessage);

        // Bucle de reconexión con back-off exponencial (5 s → 10 s → 20 s … máx 60 s)
        var delay = TimeSpan.FromSeconds(5);
        while (!_appToken.IsCancellationRequested && !_stopping)
        {
            _logger.LogInformation("🔄 MQTT: reintentando conexión en {Delay} s...", delay.TotalSeconds);
            try
            {
                await Task.Delay(delay, _appToken);
            }
            catch (OperationCanceledException) { break; }

            try
            {
                await _client.ConnectAsync(_connectOptions!, CancellationToken.None);
                _logger.LogInformation("✅ MQTT reconectado a {Host}:{Port}", _options.Host, _options.Port);

                // Con CleanSession=true el broker descarta las suscripciones anteriores.
                // Hay que re-suscribirse completamente tras cada reconexión.
                await SubscribeToAllTopicsAsync();
                return; // reconexión exitosa → salir del bucle
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ MQTT: fallo al reconectar. Reintentando en {Delay} s...", delay.TotalSeconds);

                // Back-off exponencial, tope en 60 s
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 60));
            }
        }
    }

    // ─── Helpers internos ─────────────────────────────────────────────────────

    /// <summary>
    /// Suscripción completa a todos los topics DJI Cloud API.
    /// Se llama tanto en el arranque como tras cada reconexión exitosa.
    /// </summary>
    private async Task SubscribeToAllTopicsAsync()
    {
        await SubscribeAsync("thing/product/+/state");
        await SubscribeAsync("thing/product/+/osd");
        await SubscribeAsync("thing/product/+/events");
        await SubscribeAsync("thing/product/+/services_reply");
        await SubscribeAsync("sys/product/+/status");
        await SubscribeAsync("sys/product/+/network/probe_reply");
        await SubscribeAsync("thing/product/+/drc/up");
        _logger.LogInformation("📡 MQTT: suscripciones DJI Cloud API registradas.");
    }

    // ─── IAsyncDisposable ─────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        _stopping = true;
        _client.ApplicationMessageReceivedAsync -= OnMessageReceivedAsync;
        _client.DisconnectedAsync               -= OnDisconnectedAsync;
        if (_client.IsConnected)
        {
            try { await _client.DisconnectAsync(); } catch { }
        }
        _client.Dispose();
    }
}
