using DjiCloudServer.Hubs;
using DjiCloudServer.Models;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace DjiCloudServer.Services;

public interface IMapSyncNotifier
{
    Task NotifyCreateAsync(MapElement element);
    Task NotifyUpdateAsync(MapElement element);
    Task NotifyDeleteAsync(string elementId, string groupId);
    Task NotifyGroupRefreshAsync(IEnumerable<string> groupIds);
}

public class MapSyncNotifier : IMapSyncNotifier
{
    private readonly IDjiWebSocketManager _ws;
    private readonly IHubContext<TelemetryHub> _hub;

    public MapSyncNotifier(IDjiWebSocketManager ws, IHubContext<TelemetryHub> hub)
    {
        _ws = ws;
        _hub = hub;
    }

    public async Task NotifyCreateAsync(MapElement element)
    {
        var wsMsg = BuildWsMessage("map_element_create", new
        {
            id = element.Id,
            group_id = element.GroupId,
            name = element.Name,
            resource = element.Resource
        });
        await _ws.BroadcastAsync(wsMsg);
        await _hub.Clients.All.SendAsync("MapElementCreate",
            element.Id, element.GroupId, element.Name,
            element.Resource?.ToString(Formatting.None) ?? "{}");
    }

    public async Task NotifyUpdateAsync(MapElement element)
    {
        var wsMsg = BuildWsMessage("map_element_update", new
        {
            id = element.Id,
            group_id = element.GroupId,
            name = element.Name,
            resource = element.Resource
        });
        await _ws.BroadcastAsync(wsMsg);
        await _hub.Clients.All.SendAsync("MapElementUpdate",
            element.Id, element.GroupId, element.Name,
            element.Resource?.ToString(Formatting.None) ?? "{}");
    }

    public async Task NotifyDeleteAsync(string elementId, string groupId)
    {
        var wsMsg = BuildWsMessage("map_element_delete", new { id = elementId, group_id = groupId });
        await _ws.BroadcastAsync(wsMsg);
        await _hub.Clients.All.SendAsync("MapElementDelete", elementId, groupId);
    }

    public async Task NotifyGroupRefreshAsync(IEnumerable<string> groupIds)
    {
        var ids = groupIds.ToList();
        var wsMsg = BuildWsMessage("map_group_refresh", new { ids });
        await _ws.BroadcastAsync(wsMsg);
        await _hub.Clients.All.SendAsync("MapGroupRefresh", ids);
    }

    private static string BuildWsMessage(string bizCode, object data) =>
        JsonConvert.SerializeObject(new
        {
            biz_code = bizCode,
            version = "1.0",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            data
        });
}
