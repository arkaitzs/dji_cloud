using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace DjiCloudServer.Services;

public interface IDjiWebSocketManager
{
    /// <summary>
    /// Registra una conexión WS asociada a un workspace.
    /// Las conexiones del RC se asignan al workspace del despliegue
    /// (extraído de la query o el por defecto de configuración).
    /// </summary>
    void Add(WebSocket socket, string workspaceId);

    void Remove(WebSocket socket);

    /// <summary>
    /// Difunde un mensaje a las conexiones del workspace indicado.
    /// workspaceId=null → difusión a todas las conexiones (uso administrativo).
    /// </summary>
    Task BroadcastAsync(string message, string? workspaceId = null, CancellationToken ct = default);

    Task CloseAllAsync(string reason = "Server requested disconnect", CancellationToken ct = default);
    int Count { get; }

    /// <summary>
    /// Cuando true, el servidor rechaza nuevas conexiones WS (devuelve 503).
    /// Permite mantener el RC en estado offline mientras el usuario crea elementos,
    /// de modo que al desbloquear el RC los sube en batch.
    /// </summary>
    bool BlockNewConnections { get; set; }
}

public class DjiWebSocketManager : IDjiWebSocketManager
{
    // Valor: workspaceId al que pertenece la conexión (scoping multi-workspace)
    private readonly ConcurrentDictionary<WebSocket, string> _sockets = new();
    private readonly ILogger<DjiWebSocketManager> _logger;

    public DjiWebSocketManager(ILogger<DjiWebSocketManager> logger) => _logger = logger;

    public bool BlockNewConnections { get; set; } = false;

    public void Add(WebSocket socket, string workspaceId) => _sockets.TryAdd(socket, workspaceId);

    public void Remove(WebSocket socket) => _sockets.TryRemove(socket, out _);

    public int Count => _sockets.Count;

    public async Task BroadcastAsync(string message, string? workspaceId = null, CancellationToken ct = default)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(bytes);

        foreach (var (ws, wsWorkspace) in _sockets)
        {
            // Scoping: solo conexiones del mismo workspace (null = todas)
            if (workspaceId != null && !string.Equals(wsWorkspace, workspaceId, StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                if (ws.State == WebSocketState.Open)
                    await ws.SendAsync(segment, WebSocketMessageType.Text, true, ct);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[WS-Map] Error enviando mensaje a WebSocket");
            }
        }
    }

    /// <summary>
    /// Cierra forzosamente todas las conexiones WebSocket activas.
    /// El mando RC detectará el cierre y reconectará automáticamente,
    /// disparando un nuevo batch-upload de sus elementos locales.
    /// Útil para romper conexiones zombie donde el servidor cree que el
    /// WS está abierto pero el RC ya no recibe mensajes.
    /// </summary>
    public async Task CloseAllAsync(string reason = "Server requested disconnect", CancellationToken ct = default)
    {
        foreach (var (ws, _) in _sockets)
        {
            try
            {
                if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, ct);
                    _logger.LogInformation("[WS-Map] CloseAllAsync: conexión cerrada ({State})", ws.State);
                }
                else
                {
                    _logger.LogInformation("[WS-Map] CloseAllAsync: socket ya en estado {State}, omitido", ws.State);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WS-Map] CloseAllAsync: error cerrando WebSocket");
            }
        }
    }
}
