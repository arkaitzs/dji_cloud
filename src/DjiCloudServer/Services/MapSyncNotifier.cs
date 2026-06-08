using DjiCloudServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace DjiCloudServer.Services;

// ──────────────────────────────────────────────────────────────────────────────
// Payloads del protocolo WebSocket de DJI Cloud API
// Referencia: https://developer.dji.com/doc/cloud-api-tutorial/en/
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Mensaje raíz que DJI Pilot 2 espera recibir por WebSocket para eventos de mapa.
/// El campo "biz_code" determina el tipo de evento.
/// </summary>
public class DjiMapPushMessage
{
    [JsonProperty("biz_code")]
    public string BizCode { get; init; } = null!;

    [JsonProperty("version")]
    public string Version { get; init; } = "1.0";

    [JsonProperty("timestamp")]
    public long Timestamp { get; init; }

    [JsonProperty("data")]
    public object Data { get; init; } = null!;
}

/// <summary>
/// Payload de datos para eventos de elemento (create / update).
/// </summary>
public class DjiElementEventData
{
    [JsonProperty("id")]
    public string Id { get; init; } = null!;

    [JsonProperty("group_id")]
    public string GroupId { get; init; } = null!;

    [JsonProperty("name")]
    public string Name { get; init; } = "";

    [JsonProperty("resource")]
    public DjiElementResource Resource { get; init; } = null!;
}

/// <summary>
/// Payload de datos para el evento de eliminación de elemento.
/// </summary>
public class DjiElementDeleteData
{
    [JsonProperty("id")]
    public string Id { get; init; } = null!;

    [JsonProperty("group_id")]
    public string GroupId { get; init; } = null!;
}

/// <summary>
/// Payload de datos para eventos de grupo (create / update).
/// </summary>
public class DjiGroupEventData
{
    [JsonProperty("id")]
    public string Id { get; init; } = null!;

    [JsonProperty("name")]
    public string Name { get; init; } = "";

    [JsonProperty("type")]
    public int Type { get; init; }

    [JsonProperty("is_lock")]
    public bool IsLock { get; init; }
}

/// <summary>
/// Payload de datos para el evento de eliminación de grupo.
/// </summary>
public class DjiGroupDeleteData
{
    [JsonProperty("id")]
    public string Id { get; init; } = null!;
}

/// <summary>
/// Recurso de elemento tal como lo serializa DJI Cloud API.
/// IMPORTANTE: "type" (int) debe coincidir con geometry.type (string):
///   0 → "Point", 1 → "LineString", 2 → "Polygon"
/// </summary>
public class DjiElementResource
{
    [JsonProperty("user_name")]
    public string UserName { get; init; } = "Server";

    [JsonProperty("type")]
    public int Type { get; init; }  // 0=Point, 1=LineString, 2=Polygon

    [JsonProperty("content")]
    public DjiElementContent Content { get; init; } = null!;
}

public class DjiElementContent
{
    [JsonProperty("type")]
    public string Type { get; init; } = "Feature";

    [JsonProperty("properties")]
    public DjiElementProperties Properties { get; init; } = null!;

    [JsonProperty("geometry")]
    public DjiElementGeometry Geometry { get; init; } = null!;
}

public class DjiElementProperties
{
    [JsonProperty("color")]
    public string Color { get; init; } = "#0091FF";

    [JsonProperty("clampToGround")]
    public bool ClampToGround { get; init; }

    [JsonProperty("is3d", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Is3d { get; init; }

    [JsonProperty("radius", NullValueHandling = NullValueHandling.Ignore)]
    public double? Radius { get; init; }
}

public class DjiElementGeometry
{
    [JsonProperty("type")]
    public string Type { get; init; } = "Point"; // "Point" | "LineString" | "Polygon" | "Circle"

    [JsonProperty("coordinates")]
    public object Coordinates { get; init; } = null!;
    // Point:      [lon, lat, alt]
    // LineString: [[lon,lat,alt], ...]
    // Polygon:    [[[lon,lat,alt], ...]]
    // Circle:     [lon, lat]

    /// <summary>Radio en metros. Solo para type = "Circle".</summary>
    [JsonProperty("radius", NullValueHandling = NullValueHandling.Ignore)]
    public double? Radius { get; init; }
}

// ──────────────────────────────────────────────────────────────────────────────
// Servicio central de sincronización
// ──────────────────────────────────────────────────────────────────────────────

public interface IMapSyncNotifier
{
    /// <summary>Notifica creación de elemento a RC (WebSocket) y PC (SignalR).</summary>
    Task NotifyElementCreatedAsync(string workspaceId, string groupId, MapElement element, string? excludeConnectionId = null);

    /// <summary>Notifica actualización de elemento a RC (WebSocket) y PC (SignalR).</summary>
    Task NotifyElementUpdatedAsync(string workspaceId, string groupId, MapElement element, string? excludeConnectionId = null);

    /// <summary>Notifica eliminación de elemento a RC (WebSocket) y PC (SignalR).</summary>
    Task NotifyElementDeletedAsync(string workspaceId, string groupId, string elementId, string? excludeConnectionId = null);

    /// <summary>Notifica creación de grupo a RC (WebSocket) y PC (SignalR).</summary>
    Task NotifyGroupCreatedAsync(string workspaceId, MapElementGroup group);

    /// <summary>Notifica actualización de grupo a RC (WebSocket) y PC (SignalR).</summary>
    Task NotifyGroupUpdatedAsync(string workspaceId, MapElementGroup group);

    /// <summary>Notifica eliminación de grupo a RC (WebSocket) y PC (SignalR).</summary>
    Task NotifyGroupDeletedAsync(string workspaceId, string groupId);

    /// <summary>
    /// Envía map_group_refresh al RC y MapElementRefresh por SignalR.
    /// Usar cuando múltiples elementos de un grupo cambian a la vez (arrastre de capa, importación masiva).
    /// El RC recibirá el group_id y rellamará al GET /element-groups para refrescar la lista completa.
    /// </summary>
    Task NotifyMapRefreshAsync(string workspaceId, string groupId);
}

public class MapSyncNotifier : IMapSyncNotifier
{
    private readonly IDjiWebSocketManager _wsManager;
    private readonly IHubContext<TelemetryHub> _hubContext;
    private readonly ILogger<MapSyncNotifier> _logger;

    public MapSyncNotifier(
        IDjiWebSocketManager wsManager,
        IHubContext<TelemetryHub> hubContext,
        ILogger<MapSyncNotifier> logger)
    {
        _wsManager  = wsManager;
        _hubContext = hubContext;
        _logger     = logger;
    }

    // ── Elementos ─────────────────────────────────────────────────────────────

    public Task NotifyElementCreatedAsync(string workspaceId, string groupId, MapElement element, string? excludeConnectionId = null)
    {
        var data = BuildElementEventData(groupId, element);
        return BroadcastAsync(workspaceId, "map_element_create", data, "MapElementCreate");
    }

    public Task NotifyElementUpdatedAsync(string workspaceId, string groupId, MapElement element, string? excludeConnectionId = null)
    {
        var data = BuildElementEventData(groupId, element);
        return BroadcastAsync(workspaceId, "map_element_update", data, "MapElementUpdate");
    }

    public Task NotifyElementDeletedAsync(string workspaceId, string groupId, string elementId, string? excludeConnectionId = null)
    {
        var data = new DjiElementDeleteData { Id = elementId, GroupId = groupId };
        return BroadcastAsync(workspaceId, "map_element_delete", data, "MapElementDelete");
    }

    // ── Grupos ────────────────────────────────────────────────────────────────

    public Task NotifyGroupCreatedAsync(string workspaceId, MapElementGroup group)
    {
        var data = BuildGroupEventData(group);
        return BroadcastAsync(workspaceId, "map_group_create", data, "MapGroupCreate");
    }

    public Task NotifyGroupUpdatedAsync(string workspaceId, MapElementGroup group)
    {
        var data = BuildGroupEventData(group);
        return BroadcastAsync(workspaceId, "map_group_update", data, "MapGroupUpdate");
    }

    public Task NotifyGroupDeletedAsync(string workspaceId, string groupId)
    {
        var data = new DjiGroupDeleteData { Id = groupId };
        return BroadcastAsync(workspaceId, "map_group_delete", data, "MapGroupDelete");
    }

    public Task NotifyMapRefreshAsync(string workspaceId, string groupId)
    {
        var data = new { ids = new[] { groupId } };
        return BroadcastAsync(workspaceId, "map_group_refresh", data, "MapElementRefresh");
    }

    // ── Core broadcast ────────────────────────────────────────────────────────

    /// <summary>
    /// Envía el mismo mensaje a:
    ///   1. Todos los mandos RC conectados vía WebSocket crudo DJI (topic = biz_code)
    ///   2. Todos los clientes web conectados vía SignalR (método = signalrMethod)
    /// </summary>
    private async Task BroadcastAsync(string workspaceId, string bizCode, object data, string signalrMethod)
    {
        var message = new DjiMapPushMessage
        {
            BizCode   = bizCode,
            Version   = "1.0",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Data      = data
        };

        var json = JsonConvert.SerializeObject(message, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });

        // 1. RC ← WebSocket DJI
        try
        {
            await _wsManager.BroadcastAsync(workspaceId, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MapSync] Error enviando {BizCode} por WebSocket al workspace {WId}", bizCode, workspaceId);
        }

        // 2. PC ← SignalR (usar JsonNode para evitar doble-serialización de System.Text.Json)
        try
        {
            var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(json);
            await _hubContext.Clients.All.SendAsync(signalrMethod, jsonNode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MapSync] Error enviando {Method} por SignalR", signalrMethod);
        }

        _logger.LogInformation("[MapSync] ✅ Broadcast {BizCode} → workspace {WId}", bizCode, workspaceId);
    }

    // ── Helpers de construcción de payloads ───────────────────────────────────

    private static DjiElementEventData BuildElementEventData(string groupId, MapElement el)
    {
        return new DjiElementEventData
        {
            Id      = el.Id,
            GroupId = groupId,
            Name    = el.Name,
            Resource = new DjiElementResource
            {
                UserName = el.Resource.UserName,
                Type     = el.Resource.Type,
                Content  = new DjiElementContent
                {
                    Type = el.Resource.Content.Type,
                    Properties = new DjiElementProperties
                    {
                        Color         = el.Resource.Content.Properties.Color,
                        ClampToGround = el.Resource.Content.Properties.ClampToGround,
                        Is3d          = el.Resource.Content.Properties.Is3d,
                        Radius        = el.Resource.Content.Properties.Radius
                    },
                    Geometry = new DjiElementGeometry
                    {
                        Type        = el.Resource.Content.Geometry.Type,
                        Coordinates = el.Resource.Content.Geometry.Coordinates,
                        Radius      = el.Resource.Content.Geometry.Radius
                    }
                }
            }
        };
    }

    private static DjiGroupEventData BuildGroupEventData(MapElementGroup g)
    {
        return new DjiGroupEventData
        {
            Id     = g.Id,
            Name   = g.Name,
            Type   = g.Type,
            IsLock = g.IsLock
        };
    }

}
