using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace DjiCloudServer.Services;

public interface IDjiWebSocketManager
{
    void Add(WebSocket socket);
    void Remove(WebSocket socket);
    Task BroadcastAsync(string message, CancellationToken ct = default);
    int Count { get; }
}

public class DjiWebSocketManager : IDjiWebSocketManager
{
    private readonly ConcurrentDictionary<WebSocket, byte> _sockets = new();
    private readonly ILogger<DjiWebSocketManager> _logger;

    public DjiWebSocketManager(ILogger<DjiWebSocketManager> logger) => _logger = logger;

    public void Add(WebSocket socket) => _sockets.TryAdd(socket, 0);

    public void Remove(WebSocket socket) => _sockets.TryRemove(socket, out _);

    public int Count => _sockets.Count;

    public async Task BroadcastAsync(string message, CancellationToken ct = default)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(bytes);

        foreach (var (ws, _) in _sockets)
        {
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
}
