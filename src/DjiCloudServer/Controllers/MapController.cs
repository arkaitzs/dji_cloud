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
        // FORMATO DE REFERENCIA (servidor Java DJI-Cloud-API-Demo):
        // GetMapElementsResponse: id, name, type, is_lock, elements[]
        // MapGroupElement: id, name, create_time, update_time, resource
        // NO incluir: is_distributed (solo es query param), status (no está en el spec GET),
        //             create_time de grupo (no está en GetMapElementsResponse Java)
        var result = groups.Select(g => new
        {
            id       = g.Id,
            name     = g.Name,
            type     = g.Type,
            is_lock  = g.IsLock,
            elements = _mapData.GetElementsByGroup(g.Id).Select(e => new
            {
                id          = e.Id,
                name        = e.Name,
                create_time = e.CreateTime,
                update_time = e.UpdateTime,
                resource    = e.Resource
            })
        });

        _logger.LogInformation("[MapController] GET element-groups workspace={WorkspaceId} → {GroupCount} grupos, {ElementCount} elementos",
            workspaceId, groups.Count, groups.Sum(g => _mapData.GetElementsByGroup(g.Id).Count));
        return Ok(new { code = 0, message = "success", data = result });
    }

    // GET /map/api/v1/workspaces/{workspaceId}/element-groups/{groupId}/elements
    // DJI Pilot 2 llama a este endpoint tras recibir map_group_refresh para descargar
    // los elementos de un grupo concreto. Sin este handler, ASP.NET devuelve 405
    // (solo teníamos POST) y el ciclo de refresh aborta antes de mostrar los nuevos elementos.
    [HttpGet("map/api/v1/workspaces/{workspaceId}/element-groups/{groupId}/elements")]
    public IActionResult GetElementsByGroup(
        string workspaceId, string groupId,
        [FromQuery(Name = "is_distributed")] bool isDistributed = true)
    {
        var elements = _mapData.GetElementsByGroup(groupId).Select(e => new
        {
            id          = e.Id,
            name        = e.Name,
            create_time = e.CreateTime,
            update_time = e.UpdateTime,
            resource    = e.Resource
        }).ToList();

        _logger.LogInformation(
            "[MapController] GET elements/{GroupId} workspace={WorkspaceId} isDistributed={IsDistributed} → {Count} elementos",
            groupId, workspaceId, isDistributed, elements.Count);

        return Ok(new { code = 0, message = "success", data = elements });
    }

    // POST /map/api/v1/workspaces/{workspaceId}/element-groups/{groupId}/elements
    [HttpPost("map/api/v1/workspaces/{workspaceId}/element-groups/{groupId}/elements")]
    public async Task<IActionResult> CreateElement(string workspaceId, string groupId, [FromBody] JObject body)
    {
        var elementName = body["name"]?.ToString() ?? "";
        var userName    = ExtractUserFromElementName(elementName) ?? "pilot";
        var resource    = body["resource"] as JObject;

        // user_name DENTRO del resource, persistido — igual que la demo Java:
        //   elementCreate.getResource().setUsername(claims.getUsername())
        // La demo lo guarda en BD y lo devuelve en GET element-groups y push WS.
        // El RC no lo envía en el body, así que lo derivamos del prefijo del nombre.
        if (resource != null && string.IsNullOrEmpty(resource["user_name"]?.ToString()))
            resource["user_name"] = userName;

        var element = new MapElement
        {
            Id       = body["id"]?.ToString() ?? Guid.NewGuid().ToString(),
            Name     = elementName,
            UserName = userName,
            Resource = resource
        };

        var created = _mapData.AddElement(groupId, element);
        _logger.LogInformation("[MapController] Elemento CREADO id={Id} nombre='{Name}' grupo={GroupId} resource={Resource}",
            created.Id, created.Name, groupId,
            element.Resource?.ToString(Newtonsoft.Json.Formatting.None) ?? "null");
        await _notifier.NotifyCreateAsync(created);

        return Ok(new { code = 0, message = "success", data = new { id = created.Id } });
    }

    // PUT /map/api/v1/workspaces/{workspaceId}/elements/{elementId}
    [HttpPut("map/api/v1/workspaces/{workspaceId}/elements/{elementId}")]
    public async Task<IActionResult> UpdateElement(string workspaceId, string elementId, [FromBody] JObject body)
    {
        var patchResource = body["resource"] as JObject;

        // Preservar user_name al actualizar: si el body no lo trae (el RC nunca lo envía),
        // conservar el del elemento almacenado para no perder la atribución del creador.
        if (patchResource != null && string.IsNullOrEmpty(patchResource["user_name"]?.ToString()))
        {
            var existingUserName = _mapData.GetElement(elementId)?.Resource?["user_name"]?.ToString();
            patchResource["user_name"] = !string.IsNullOrEmpty(existingUserName) ? existingUserName : "pilot";
        }

        var patch = new MapElement
        {
            Name     = body["name"]?.ToString() ?? "",
            Resource = patchResource
        };

        var result = _mapData.UpdateElement(elementId, patch);
        if (result == null)
        {
            _logger.LogInformation("[MapController] PUT elemento id={Id} no encontrado — respuesta idempotente OK", elementId);
            return Ok(new { code = 0, message = "success", data = new { } });
        }

        _logger.LogInformation("[MapController] Elemento ACTUALIZADO id={Id} nombre='{Name}'", elementId, result.Name);
        await _notifier.NotifyUpdateAsync(result);

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
        await _notifier.NotifyDeleteAsync(elementId, groupId);

        return Ok(new { code = 0, message = "success", data = new { } });
    }

    // ── Helper: extraer el "operador" del nombre automático del RC ──────────────
    // DJI Pilot 2 nombra los elementos como "<prefijo> <número>" (p.ej. "local_user 50").
    // El prefijo es el elementPreName configurado en platformLoadComponent("map", ...).
    private static string? ExtractUserFromElementName(string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        var lastSpace = name.LastIndexOf(' ');
        // Solo recortamos si la última palabra es un número (p.ej. "50")
        if (lastSpace > 0 && int.TryParse(name[(lastSpace + 1)..], out _))
            return name[..lastSpace];
        return name;
    }

    // GET /map/api/v1/project/{workspaceId}/flight-areas/url
    // DJI Pilot 2 llama a este endpoint al inicializar el módulo de mapa.
    // Si devuelve 404, Pilot 2 puede entrar en modo degradado y dejar de sincronizar elementos.
    // Devolvemos una respuesta vacía válida para evitar ese comportamiento.
    [HttpGet("map/api/v1/project/{workspaceId}/flight-areas/url")]
    public IActionResult GetFlightAreasUrl(string workspaceId)
    {
        _logger.LogDebug("[MapController] GET flight-areas/url workspace={WorkspaceId} → respuesta vacía OK", workspaceId);
        return Ok(new { code = 0, message = "success", data = new { url = "" } });
    }
}
