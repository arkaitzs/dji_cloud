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
    /// Identificador del workspace DJI (UUID). Debe coincidir en RC (DJI Pilot 2),
    /// servidor y cliente web para que la sincronización de elementos funcione.
    /// </summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>
    /// Ruta relativa al KML de zonas de vuelo personalizadas, dentro de wwwroot.
    /// Ejemplo: "flight-areas/zonas.kml". Vacío = sin KML.
    /// </summary>
    public string FlightAreaKmlFile { get; set; } = string.Empty;
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
