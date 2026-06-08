using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DjiCloudServer.Services;

public interface IDjiWebSocketManager
{
    void RegisterSocket(string workspaceId, string connectionId, WebSocket socket);
    void UnregisterSocket(string workspaceId, string connectionId);
    Task BroadcastAsync(string workspaceId, string message);
    int GetConnectedCount(string workspaceId);
}

public class DjiWebSocketManager : IDjiWebSocketManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>> _workspaces = new();
    private readonly ILogger<DjiWebSocketManager> _logger;

    public DjiWebSocketManager(ILogger<DjiWebSocketManager> logger)
    {
        _logger = logger;
    }

    public void RegisterSocket(string workspaceId, string connectionId, WebSocket socket)
    {
        var sockets = _workspaces.GetOrAdd(workspaceId, _ => new ConcurrentDictionary<string, WebSocket>());
        sockets[connectionId] = socket;
        _logger.LogInformation("WebSocket registrado en workspace {WorkspaceId} con ConnectionId {ConnectionId}. Total activos en el workspace: {Count}", 
            workspaceId, connectionId, sockets.Count);
    }

    public void UnregisterSocket(string workspaceId, string connectionId)
    {
        if (_workspaces.TryGetValue(workspaceId, out var sockets))
        {
            if (sockets.TryRemove(connectionId, out _))
            {
                _logger.LogInformation("WebSocket eliminado de workspace {WorkspaceId} con ConnectionId {ConnectionId}. Quedan activos: {Count}", 
                    workspaceId, connectionId, sockets.Count);
            }
            if (sockets.IsEmpty)
            {
                _workspaces.TryRemove(workspaceId, out _);
            }
        }
    }

    public int GetConnectedCount(string workspaceId)
    {
        if (!_workspaces.TryGetValue(workspaceId, out var sockets)) return 0;
        return sockets.Count(kv => kv.Value.State == WebSocketState.Open);
    }

    public async Task BroadcastAsync(string workspaceId, string message)
    {
        if (!_workspaces.TryGetValue(workspaceId, out var sockets) || sockets.IsEmpty)
        {
            // ⚠️ Nivel Information para que sea visible en producción.
            // Si aparece este mensaje, el RC no está conectado o el workspaceId no coincide.
            _logger.LogInformation("[WS-Broadcast] Sin mandos conectados en workspace '{WorkspaceId}'. El push se descarta.",
                workspaceId);
            return;
        }

        var bytes  = Encoding.UTF8.GetBytes(message);
        var buffer = new ArraySegment<byte>(bytes);
        int sent   = 0;
        int total  = 0;

        // Snapshot de sockets para no modificar la colección durante la iteración
        var socketList = sockets.ToList();
        total = socketList.Count;

        foreach (var (connId, socket) in socketList)
        {
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    sent++;
                    _logger.LogInformation("[WS-Broadcast] ✅ Enviado a conexión {ConnId} | workspace={WId} | bytes={B}",
                        connId, workspaceId, bytes.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[WS-Broadcast] ❌ Error enviando a {ConnId}", connId);
                    UnregisterSocket(workspaceId, connId);
                }
            }
            else
            {
                _logger.LogWarning("[WS-Broadcast] Socket {ConnId} no está Open (estado={State}), se elimina.",
                    connId, socket.State);
                UnregisterSocket(workspaceId, connId);
            }
        }

        _logger.LogInformation("[WS-Broadcast] Push completado: {Sent}/{Total} mandos en workspace '{WId}'",
            sent, total, workspaceId);
    }
}
