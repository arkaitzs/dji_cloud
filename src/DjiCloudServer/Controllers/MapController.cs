using DjiCloudServer.Models;
using DjiCloudServer.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace DjiCloudServer.Controllers;

/// <summary>
/// Endpoints DJI Cloud API consumidos por DJI Pilot 2 / RC Pro Enterprise.
/// Prefijo: /map/api/v1/workspaces/{workspaceId}/...
/// </summary>
[ApiController]
public class MapController : ControllerBase
{
    private readonly IMapDataService _mapData;
    private readonly IMapSyncNotifier _notifier;
    private readonly ILogger<MapController> _logger;

    public MapController(IMapDataService mapData, IMapSyncNotifier notifier, ILogger<MapController> logger)
    {
        _mapData = mapData;
        _notifier = notifier;
        _logger = logger;
    }

    // GET /map/api/v1/workspaces/{workspaceId}/element-groups
    [HttpGet("map/api/v1/workspaces/{workspaceId}/element-groups")]
    public IActionResult GetElementGroups(string workspaceId)
    {
        var groups = _mapData.GetGroups();
        var result = groups.Select(g => new
        {
            id = g.Id,
            name = g.Name,
            type = g.Type,
            is_lock = g.IsLock,
            create_time = g.CreateTime,
            elements = _mapData.GetElementsByGroup(g.Id).Select(e => new
            {
                id = e.Id,
                name = e.Name,
                create_time = e.CreateTime,
                update_time = e.UpdateTime,
                resource = e.Resource
            })
        });

        _logger.LogInformation("[MapController] GET element-groups workspace={WorkspaceId} → {GroupCount} grupos, {ElementCount} elementos",
            workspaceId, groups.Count, groups.Sum(g => _mapData.GetElementsByGroup(g.Id).Count));
        return Ok(new { code = 0, message = "success", data = result });
    }

    // POST /map/api/v1/workspaces/{workspaceId}/element-groups/{groupId}/elements
    [HttpPost("map/api/v1/workspaces/{workspaceId}/element-groups/{groupId}/elements")]
    public async Task<IActionResult> CreateElement(string workspaceId, string groupId, [FromBody] JObject body)
    {
        var element = new MapElement
        {
            Id       = body["id"]?.ToString() ?? Guid.NewGuid().ToString(),
            Name     = body["name"]?.ToString() ?? "",
            Resource = body["resource"] as JObject
        };

        var created = _mapData.AddElement(groupId, element);
        _logger.LogInformation("[MapController] Elemento CREADO id={Id} nombre='{Name}' grupo={GroupId} resource={Resource}",
            created.Id, created.Name, groupId,
            element.Resource?.ToString(Newtonsoft.Json.Formatting.None) ?? "null");
        _ = _notifier.NotifyCreateAsync(created);

        return Ok(new { code = 0, message = "success", data = new { id = created.Id } });
    }

    // PUT /map/api/v1/workspaces/{workspaceId}/elements/{elementId}
    [HttpPut("map/api/v1/workspaces/{workspaceId}/elements/{elementId}")]
    public async Task<IActionResult> UpdateElement(string workspaceId, string elementId, [FromBody] JObject body)
    {
        var patch = new MapElement
        {
            Name     = body["name"]?.ToString() ?? "",
            Resource = body["resource"] as JObject
        };

        var result = _mapData.UpdateElement(elementId, patch);
        if (result == null)
        {
            _logger.LogInformation("[MapController] PUT elemento id={Id} no encontrado — respuesta idempotente OK", elementId);
            return Ok(new { code = 0, message = "success", data = new { } });
        }

        _logger.LogInformation("[MapController] Elemento ACTUALIZADO id={Id} nombre='{Name}'", elementId, result.Name);
        _ = _notifier.NotifyUpdateAsync(result);

        return Ok(new { code = 0, message = "success", data = new { } });
    }

    // DELETE /map/api/v1/workspaces/{workspaceId}/elements/{elementId}
    [HttpDelete("map/api/v1/workspaces/{workspaceId}/elements/{elementId}")]
    public async Task<IActionResult> DeleteElement(string workspaceId, string elementId)
    {
        var element = _mapData.GetElement(elementId);
        var groupId = element?.GroupId ?? "";

        _mapData.DeleteElement(elementId);
        _logger.LogInformation("[MapController] Elemento ELIMINADO id={Id} grupo={GroupId}", elementId, groupId);
        _ = _notifier.NotifyDeleteAsync(elementId, groupId);

        return Ok(new { code = 0, message = "success", data = new { } });
    }
}
