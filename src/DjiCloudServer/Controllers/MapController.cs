using DjiCloudServer.Models;
using DjiCloudServer.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DjiCloudServer.Controllers;

// ──────────────────────────────────────────────────────────────────────────────
// MapController — Endpoints que consume DJI Pilot 2 (RC / mando)
//
// Base URL: /map/api/v1/workspaces/{workspaceId}
//
// Todos los endpoints devuelven el envelope DJI:
//   { "code": 0, "message": "success", "data": { ... } }
//
// Referencia: https://developer.dji.com/doc/cloud-api-tutorial/en/
//             api-reference/pilot-to-cloud/https/map-elements/
// ──────────────────────────────────────────────────────────────────────────────

[ApiController]
[Route("map/api/v1/workspaces/{workspaceId}")]
[Produces("application/json")]
public class MapController : ControllerBase
{
    private readonly IMapDataService _mapData;
    private readonly IMapSyncNotifier _sync;
    private readonly ILogger<MapController> _logger;

    public MapController(
        IMapDataService mapData,
        IMapSyncNotifier sync,
        ILogger<MapController> logger)
    {
        _mapData = mapData;
        _sync    = sync;
        _logger  = logger;
    }

    // =========================================================================
    // GRUPOS
    // =========================================================================

    /// <summary>
    /// GET /map/api/v1/workspaces/{workspaceId}/element-groups
    ///
    /// DJI Pilot 2 llama a este endpoint al arrancar y tras reconexión.
    /// Es la sincronización inicial RC ← Servidor.
    ///
    /// Query: group_id (opcional) — filtra por grupo específico
    /// </summary>
    [HttpGet("element-groups")]
    public IActionResult GetElementGroups(
        string workspaceId,
        [FromQuery(Name = "group_id")] string? groupId = null,
        [FromQuery(Name = "is_distributed")] bool? isDistributed = null)
    {
#pragma warning disable CA1873
        _logger.LogInformation("[MapCtrl] GET element-groups workspace={W} groupId={G}", workspaceId, groupId);
#pragma warning restore CA1873

        var groups = _mapData.GetWorkspaceGroups(workspaceId);
        if (!string.IsNullOrEmpty(groupId))
            groups = groups.Where(g => g.Id == groupId).ToList();

        // is_distributed=true → grupos compartidos (type 1 y 2); false → sólo personales (type 0)
        if (isDistributed.HasValue)
            groups = [.. groups.Where(g => isDistributed.Value ? g.Type != 0 : g.Type == 0)];

        return Ok(DjiApiResponse<List<MapElementGroup>>.Success(groups));
    }

    /// <summary>
    /// POST /map/api/v1/workspaces/{workspaceId}/element-groups
    ///
    /// El RC crea un nuevo grupo (p.ej. "Zona de Vuelo #2").
    /// Body: { "id"?: "uuid", "name": "...", "type": 2, "is_lock": false }
    /// </summary>
    [HttpPost("element-groups")]
    public async Task<IActionResult> CreateGroup(
        string workspaceId,
        [FromBody] Newtonsoft.Json.Linq.JObject body)
    {
        if (body == null)
            return BadRequest(DjiApiResponse.Fail(400, "Body vacío"));

        _logger.LogInformation("[MapCtrl] POST element-groups workspace={W}", workspaceId);

        var input = body.ToObject<GroupCreateInput>();
        if (input == null)
            return BadRequest(DjiApiResponse.Fail(400, "JSON de grupo inválido"));

        var group = _mapData.CreateGroup(workspaceId, input);
        await _sync.NotifyGroupCreatedAsync(workspaceId, group);

        return Ok(DjiApiResponse<object>.Success(new { id = group.Id }));
    }

    /// <summary>
    /// PUT /map/api/v1/workspaces/{workspaceId}/element-groups/{groupId}
    ///
    /// El RC actualiza metadatos de un grupo (nombre, bloqueo).
    /// Body: { "name"?: "...", "is_lock"?: true }
    /// </summary>
    [HttpPut("element-groups/{groupId}")]
    public async Task<IActionResult> UpdateGroup(
        string workspaceId,
        string groupId,
        [FromBody] Newtonsoft.Json.Linq.JObject body)
    {
        _logger.LogInformation("[MapCtrl] PUT element-groups/{G} workspace={W}", groupId, workspaceId);

        var input = body?.ToObject<GroupUpdateInput>();
        if (input == null)
            return BadRequest(DjiApiResponse.Fail(400, "JSON de actualización inválido"));

        var group = _mapData.UpdateGroup(workspaceId, groupId, input);
        if (group == null)
            return NotFound(DjiApiResponse.Fail(404, "Grupo no encontrado"));

        await _sync.NotifyGroupUpdatedAsync(workspaceId, group);
        return Ok(DjiApiResponse<object>.Success(new { id = groupId }));
    }

    /// <summary>
    /// DELETE /map/api/v1/workspaces/{workspaceId}/element-groups/{groupId}
    ///
    /// El RC elimina un grupo completo (y todos sus elementos).
    /// </summary>
    [HttpDelete("element-groups/{groupId}")]
    public async Task<IActionResult> DeleteGroup(
        string workspaceId,
        string groupId)
    {
        _logger.LogInformation("[MapCtrl] DELETE element-groups/{G} workspace={W}", groupId, workspaceId);

        var deleted = _mapData.DeleteGroup(workspaceId, groupId);
        if (!deleted)
            return NotFound(DjiApiResponse.Fail(404, "Grupo no encontrado"));

        await _sync.NotifyGroupDeletedAsync(workspaceId, groupId);
        return Ok(DjiApiResponse<object>.Success(new { id = groupId }));
    }

    // =========================================================================
    // ELEMENTOS
    // =========================================================================

    /// <summary>
    /// GET /map/api/v1/workspaces/{workspaceId}/element-groups/{groupId}/elements
    ///
    /// Lista los elementos de un grupo con paginación.
    /// Query: page (default 1), page_size (default 100, max 100)
    /// </summary>
    [HttpGet("element-groups/{groupId}/elements")]
    public IActionResult GetGroupElements(
        string workspaceId,
        string groupId,
        [FromQuery] int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 100)
    {
        _logger.LogInformation("[MapCtrl] GET elements group={G} workspace={W} page={P}/{S}",
            groupId, workspaceId, page, pageSize);

        pageSize = Math.Clamp(pageSize, 1, 100);
        var all   = _mapData.GetGroupElements(workspaceId, groupId);
        var paged = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(DjiApiResponse<object>.Success(new
        {
            list      = paged,
            total     = all.Count,
            page,
            page_size = pageSize
        }));
    }

    /// <summary>
    /// GET /map/api/v1/workspaces/{workspaceId}/elements/{id}
    ///
    /// Obtiene un elemento por su ID.
    /// </summary>
    [HttpGet("elements/{id}")]
    public IActionResult GetElement(string workspaceId, string id)
    {
        _logger.LogInformation("[MapCtrl] GET element/{Id} workspace={W}", id, workspaceId);

        var element = _mapData.GetElement(workspaceId, id);
        if (element == null)
            return NotFound(DjiApiResponse.Fail(404, "Elemento no encontrado"));

        return Ok(DjiApiResponse<MapElement>.Success(element));
    }

    /// <summary>
    /// POST /map/api/v1/workspaces/{workspaceId}/element-groups/{groupId}/elements
    ///
    /// El RC crea un nuevo elemento (punto, línea o polígono).
    ///
    /// Schema JSON completo:
    /// {
    ///   "id": "550e8400-e29b-41d4-a716-446655440000",
    ///   "name": "Mi Punto de Interés",
    ///   "resource": {
    ///     "user_name": "Pilot",
    ///     "type": 0,
    ///     "content": {
    ///       "type": "Feature",
    ///       "properties": {
    ///         "color": "#0091FF",
    ///         "clampToGround": false,
    ///         "radius": null
    ///       },
    ///       "geometry": {
    ///         "type": "Point",
    ///         "coordinates": [-3.703790, 40.416775, 100.0]
    ///       }
    ///     }
    ///   }
    /// }
    ///
    /// Para líneas (type=1):
    ///   geometry.type = "LineString"
    ///   geometry.coordinates = [[-3.70, 40.41, 100], [-3.71, 40.42, 100], ...]
    ///
    /// Para polígonos (type=2):
    ///   geometry.type = "Polygon"
    ///   geometry.coordinates = [[[-3.70,40.41,0], [-3.71,40.41,0], ..., [-3.70,40.41,0]]]
    ///   (el anillo exterior debe cerrarse repitiendo el primer punto)
    /// </summary>
    [HttpPost("element-groups/{groupId}/elements")]
    public async Task<IActionResult> CreateElement(
        string workspaceId,
        string groupId,
        [FromBody] Newtonsoft.Json.Linq.JObject body)
    {
        if (body == null)
            return BadRequest(DjiApiResponse.Fail(400, "Body vacío"));

        _logger.LogInformation("[MapCtrl] POST element group={G} workspace={W} raw={J}",
            groupId, workspaceId, body.ToString(Newtonsoft.Json.Formatting.None));

        var input = body.ToObject<ElementCreateInput>();
        if (input == null || string.IsNullOrEmpty(input.Id))
            return BadRequest(DjiApiResponse.Fail(400, "Elemento o ID inválido"));

        var (element, _) = _mapData.AddElement(workspaceId, groupId, input);

        // Notificar al PC vía SignalR (el RC ya tiene el elemento porque él lo creó)
        await _sync.NotifyElementCreatedAsync(workspaceId, groupId, element);

        return Ok(DjiApiResponse<object>.Success(new { id = element.Id }));
    }

    /// <summary>
    /// PUT /map/api/v1/workspaces/{workspaceId}/elements/{id}
    ///
    /// El RC actualiza un elemento (mover punto, cambiar color, renombrar...).
    ///
    /// Body: { "name"?: "...", "content"?: { properties?: {...}, geometry?: {...} } }
    /// </summary>
    [HttpPut("elements/{id}")]
    public async Task<IActionResult> UpdateElement(
        string workspaceId,
        string id,
        [FromBody] Newtonsoft.Json.Linq.JObject body)
    {
        if (body == null)
            return BadRequest(DjiApiResponse.Fail(400, "Body vacío"));

        _logger.LogInformation("[MapCtrl] PUT element/{Id} workspace={W}", id, workspaceId);
        _logger.LogDebug("[MapCtrl] RAW UPDATE: {J}", body.ToString(Newtonsoft.Json.Formatting.None));

        var input = body.ToObject<ElementUpdateInput>();
        if (input == null)
            return BadRequest(DjiApiResponse.Fail(400, "JSON de actualización inválido"));

        var element = _mapData.UpdateElement(workspaceId, id, input);
        if (element == null)
            return NotFound(DjiApiResponse.Fail(404, "Elemento no encontrado"));

        var groups  = _mapData.GetWorkspaceGroups(workspaceId);
        var groupId = groups.FirstOrDefault(g => g.Elements.Any(e => e.Id == id))?.Id ?? "unknown";

        await _sync.NotifyElementUpdatedAsync(workspaceId, groupId, element);

        return Ok(DjiApiResponse<object>.Success(new { id }));
    }

    /// <summary>
    /// DELETE /map/api/v1/workspaces/{workspaceId}/elements/{id}
    ///
    /// El RC elimina un elemento.
    /// </summary>
    [HttpDelete("elements/{id}")]
    public async Task<IActionResult> DeleteElement(
        string workspaceId,
        string id)
    {
        _logger.LogInformation("[MapCtrl] DELETE element/{Id} workspace={W}", id, workspaceId);

        var deleted = _mapData.DeleteElement(workspaceId, id, out var groupId);
        if (!deleted)
            return NotFound(DjiApiResponse.Fail(404, "Elemento no encontrado"));

        await _sync.NotifyElementDeletedAsync(workspaceId, groupId!, id);

        return Ok(DjiApiResponse<object>.Success(new { id }));
    }
}
