namespace DjiCloudServer;

/// <summary>
/// Opciones de configuración para DJI Cloud API, leídas desde appsettings.json.
/// </summary>
public class DjiCloudOptions
{
    public MqttOptions Mqtt { get; set; } = new();
    public string AppId    { get; set; } = string.Empty;
    public string AppKey   { get; set; } = string.Empty;
    public string License  { get; set; } = string.Empty;

    /// <summary>
    /// IP local del servidor en la red del dron (ej. "192.168.1.150").
    /// Cuando está configurada, se usa directamente en drc_mode_enter y live_start_push
    /// sin necesidad de selección manual. Dejar vacío para detección automática.
    /// </summary>
    public string ServerIp { get; set; } = string.Empty;

    /// <summary>
    /// Workspace por defecto del despliegue. Se usa para el scoping de mensajes
    /// WebSocket/SignalR (los clientes sin workspace explícito se asignan a éste).
    /// </summary>
    public string WorkspaceId { get; set; } = "e3dea0f5-37f2-4d79-ae58-490af3228069";

    /// <summary>
    /// Compatibilidad con DJI Pilot 2 v17.x: enviar map_group_refresh tras cada
    /// create/update/delete de elementos. La demo Java solo envía map_element_*,
    /// pero ese firmware los ignora y solo re-descarga con group_refresh (verificado
    /// empíricamente el 2026-06-10). Poner a false con firmwares que procesen los
    /// push map_element_* directamente, para quedar 100% alineado con la demo.
    /// </summary>
    public bool LegacyGroupRefresh { get; set; } = true;
}

public class MqttOptions
{
    /// <summary>Host del broker MQTT (p. ej. "localhost" o dirección del servidor)</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>Puerto MQTT estándar: 1883 (sin TLS), 8883 (con TLS)</summary>
    public int Port { get; set; } = 1883;

    /// <summary>Usuario del broker MQTT</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Contraseña del broker MQTT</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>ClientId del servidor al conectarse como cliente MQTT</summary>
    public string ClientId { get; set; } = "DjiCloudServer";
}
