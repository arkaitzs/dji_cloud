using DjiCloudServer.Models;
using DjiCloudServer.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace DjiCloudServer.Controllers;

/// <summary>
/// Endpoints web para gestión de elementos de mapa táctica desde el navegador.
/// Prefijo: /api/web-map/workspaces/{workspaceId}/...
/// </summary>
[ApiController]
[Route("api/web-map")]
public class WebMapController : ControllerBase
{
    private readonly IMapDataService _mapData;
    private readonly IMapSyncNotifier _notifier;

    public WebMapController(IMapDataService mapData, IMapSyncNotifier notifier)
    {
        _mapData = mapData;
        _notifier = notifier;
    }

    // GET /api/web-map/workspaces/{wid}/elements
    [HttpGet("workspaces/{workspaceId}/elements")]
    public IActionResult GetElements(string workspaceId) =>
        Ok(new { success = true, data = _mapData.GetAllElements() });

    // POST /api/web-map/workspaces/{wid}/elements
    [HttpPost("workspaces/{workspaceId}/elements")]
    public async Task<IActionResult> CreateElement(string workspaceId, [FromBody] JObject body)
    {
        var groupId = body["group_id"]?.ToString();
        if (string.IsNullOrEmpty(groupId))
            groupId = _mapData.GetOrCreateDefaultGroup().Id;

        var element = new MapElement
        {
            Id       = body["id"]?.ToString() ?? Guid.NewGuid().ToString(),
            Name     = body["name"]?.ToString() ?? "Nuevo elemento",
            Resource = body["resource"] as JObject
        };

        var created = _mapData.AddElement(groupId, element);
        await _notifier.NotifyCreateAsync(created);

        return Ok(new { success = true, id = created.Id });
    }

    // PUT /api/web-map/workspaces/{wid}/elements/{id}
    [HttpPut("workspaces/{workspaceId}/elements/{elementId}")]
    public async Task<IActionResult> UpdateElement(string workspaceId, string elementId, [FromBody] JObject body)
    {
        var patch = new MapElement
        {
            Name     = body["name"]?.ToString() ?? "",
            Resource = body["resource"] as JObject
        };

        var result = _mapData.UpdateElement(elementId, patch);
        if (result != null) await _notifier.NotifyUpdateAsync(result);

        return Ok(new { success = true });
    }

    // DELETE /api/web-map/workspaces/{wid}/elements/{id}
    [HttpDelete("workspaces/{workspaceId}/elements/{elementId}")]
    public async Task<IActionResult> DeleteElement(string workspaceId, string elementId)
    {
        var element = _mapData.GetElement(elementId);
        var groupId = element?.GroupId ?? "";

        _mapData.DeleteElement(elementId);
        await _notifier.NotifyDeleteAsync(elementId, groupId);

        return Ok(new { success = true });
    }

    // DELETE /api/web-map/workspaces/{wid}/elements  (batch)
    [HttpDelete("workspaces/{workspaceId}/elements")]
    public async Task<IActionResult> BatchDeleteElements(string workspaceId, [FromBody] JObject body)
    {
        if (body["ids"] is JArray ids)
        {
            foreach (var id in ids)
            {
                var elementId = id.ToString();
                var element = _mapData.GetElement(elementId);
                var groupId = element?.GroupId ?? "";
                _mapData.DeleteElement(elementId);
                await _notifier.NotifyDeleteAsync(elementId, groupId);
            }
        }
        return Ok(new { success = true });
    }
}
