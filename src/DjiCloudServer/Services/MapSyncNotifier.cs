using DjiCloudServer.Hubs;
using DjiCloudServer.Models;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DjiCloudServer.Services;

public interface IMapSyncNotifier
{
    // ── Elementos de mapa ────────────────────────────────────────────────────
    Task NotifyCreateAsync(MapElement element);
    Task NotifyUpdateAsync(MapElement element);
    Task NotifyDeleteAsync(string elementId, string groupId);
    Task NotifyGroupRefreshAsync(IEnumerable<string> groupIds);

    // ── Situational Awareness (TSA) — Spec DJI doc 58 ────────────────────────
    /// <summary>
    /// Push device_online al RC vía WS — payload TopologyDeviceDTO como
    /// pushDeviceOnlineTopo de la demo Java (sn, online_status, device_model, gateway_sn...).
    /// DJI Pilot 2 reacciona solicitando GET /manage/api/.../devices/topologies.
    /// </summary>
    Task NotifyDeviceOnlineAsync(object? deviceTopo = null);

    /// <summary>
    /// Push device_offline al RC vía WS — payload {sn, online_status: false}
    /// como pushDeviceOfflineTopo de la demo Java.
    /// </summary>
    Task NotifyDeviceOfflineAsync(string? sn = null);

    /// <summary>
    /// Push device_osd del RC/gateway a los pilotos — host reducido {latitude, longitude, height},
    /// igual que el handler osdRemoteControl → pushOsdDataToPilot de la demo Java.
    /// </summary>
    Task NotifyGatewayOsdAsync(string sn, double latitude, double longitude, double height);

    /// <summary>
    /// Push device_osd al RC vía WS con la posición de un dron.
    /// El RC actualiza el icono del dron en su mapa en tiempo real.
    /// Spec DJI doc 58 — payload: host.{latitude, longitude, height, attitude_head, elevation, horizontal_speed, vertical_speed}
    /// </summary>
    Task NotifyDeviceOsdAsync(
        string sn,
        double latitude, double longitude, double height,
        double attitudeHead, double elevation,
        double horizontalSpeed, double verticalSpeed);

    /// <summary>
    /// Push device_update_topo — spec DJI doc 58. Se emite cuando la topología cambia
    /// sin un online/offline completo (p.ej. la aeronave se desempareja del gateway).
    /// El RC reacciona solicitando GET /manage/api/.../devices/topologies.
    /// </summary>
    Task NotifyDeviceUpdateTopoAsync();
}

public class MapSyncNotifier : IMapSyncNotifier
{
    private readonly IDjiWebSocketManager _ws;
    private readonly IHubContext<TelemetryHub> _hub;
    // Workspace del despliegue — scoping de mensajes WS (multi-workspace ready)
    private readonly string _workspaceId;

    public MapSyncNotifier(
        IDjiWebSocketManager ws,
        IHubContext<TelemetryHub> hub,
        Microsoft.Extensions.Options.IOptions<DjiCloudOptions> options)
    {
        _ws  = ws;
        _hub = hub;
        _workspaceId = options.Value.WorkspaceId;
    }

    // ────────────────────────────────────────────────────────────────────────
    // MAP ELEMENTS
    // ────────────────────────────────────────────────────────────────────────

    public async Task NotifyCreateAsync(MapElement element)
    {
        var wsMsg = BuildWsMessage("map_element_create", new
        {
            id       = element.Id,
            group_id = element.GroupId,
            name     = element.Name,
            resource = BuildResourceWithUserName(element)
        });
        await _ws.BroadcastAsync(wsMsg, _workspaceId);
        await _hub.Clients.All.SendAsync("MapElementCreate",
            element.Id, element.GroupId, element.Name,
            element.Resource?.ToString(Formatting.None) ?? "{}");
    }

    public async Task NotifyUpdateAsync(MapElement element)
    {
        var wsMsg = BuildWsMessage("map_element_update", new
        {
            id       = element.Id,
            group_id = element.GroupId,
            name     = element.Name,
            resource = BuildResourceWithUserName(element)
        });
        await _ws.BroadcastAsync(wsMsg, _workspaceId);
        await _hub.Clients.All.SendAsync("MapElementUpdate",
            element.Id, element.GroupId, element.Name,
            element.Resource?.ToString(Formatting.None) ?? "{}");
    }

    public async Task NotifyDeleteAsync(string elementId, string groupId)
    {
        var wsMsg = BuildWsMessage("map_element_delete", new { id = elementId, group_id = groupId });
        await _ws.BroadcastAsync(wsMsg, _workspaceId);
        await _hub.Clients.All.SendAsync("MapElementDelete", elementId, groupId);
    }

    public async Task NotifyGroupRefreshAsync(IEnumerable<string> groupIds)
    {
        var ids = groupIds.ToList();
        var wsMsg = BuildWsMessage("map_group_refresh", new { ids });
        await _ws.BroadcastAsync(wsMsg, _workspaceId);
        await _hub.Clients.All.SendAsync("MapGroupRefresh", ids);
    }

    // ────────────────────────────────────────────────────────────────────────
    // SITUATIONAL AWARENESS (TSA)
    // ────────────────────────────────────────────────────────────────────────

    public async Task NotifyDeviceOnlineAsync(object? deviceTopo = null)
    {
        var wsMsg = BuildWsMessage("device_online", deviceTopo ?? new { });
        await _ws.BroadcastAsync(wsMsg, _workspaceId);
    }

    public async Task NotifyDeviceOfflineAsync(string? sn = null)
    {
        object data = sn != null
            ? new { sn, online_status = false }
            : new { };
        var wsMsg = BuildWsMessage("device_offline", data);
        await _ws.BroadcastAsync(wsMsg, _workspaceId);
    }

    public async Task NotifyGatewayOsdAsync(string sn, double latitude, double longitude, double height)
    {
        // Demo Java (osdRemoteControl → pushOsdDataToPilot): el RC recibe device_osd
        // del gateway con host reducido — solo posición, sin actitud ni velocidades.
        var wsMsg = BuildWsMessage("device_osd", new
        {
            sn,
            host = new { latitude, longitude, height }
        });
        await _ws.BroadcastAsync(wsMsg, _workspaceId);
    }

    public async Task NotifyDeviceOsdAsync(
        string sn,
        double latitude, double longitude, double height,
        double attitudeHead, double elevation,
        double horizontalSpeed, double verticalSpeed)
    {
        var wsMsg = BuildWsMessage("device_osd", new
        {
            sn,
            host = new
            {
                latitude,
                longitude,
                height,
                attitude_head    = attitudeHead,
                elevation,
                horizontal_speed = horizontalSpeed,
                vertical_speed   = verticalSpeed
            }
        });
        await _ws.BroadcastAsync(wsMsg, _workspaceId);
    }

    public async Task NotifyDeviceUpdateTopoAsync()
    {
        // Spec doc 58: data vacío — el RC reacciona pidiendo la topología completa
        var wsMsg = BuildWsMessage("device_update_topo", new { });
        await _ws.BroadcastAsync(wsMsg, _workspaceId);
    }

    // ────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Construye el objeto resource para mensajes WS incluyendo user_name.
    /// La spec DJI (doc 57) exige: { user_name, type, content }.
    /// Clonamos el resource existente en lugar de mutarlo.
    /// </summary>
    private static JObject BuildResourceWithUserName(MapElement element)
    {
        var resource = element.Resource != null
            ? (JObject)element.Resource.DeepClone()
            : new JObject();

        // Inyectar user_name al nivel raíz del resource (junto a type y content)
        if (!resource.ContainsKey("user_name"))
            resource["user_name"] = element.UserName ?? "Cloud";

        return resource;
    }

    private static string BuildWsMessage(string bizCode, object data) =>
        JsonConvert.SerializeObject(new
        {
            biz_code  = bizCode,
            version   = "1.0",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            data
        });
}
