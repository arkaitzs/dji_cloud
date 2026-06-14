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
    public IActionResult GetElementGroups(
        string workspaceId,
        [FromQuery(Name = "group_id")] string? groupId = null,
        [FromQuery(Name = "is_distributed")] bool? isDistributed = null)
    {
        // Filtrado como el servidor oficial (GroupServiceImpl): por group_id si viene,
        // y por is_distributed (el RC pide is_distributed=true → solo capas distribuidas).
        var groups = _mapData.GetGroups()
            .Where(g => groupId == null || g.Id == groupId)
            .Where(g => isDistributed == null || g.IsDistributed == isDistributed.Value)
            .ToList();
        // FORMATO conforme a la spec oficial (doc 42 map.ElementGroupOutput):
        // id, type, name, is_lock, create_time, elements[].
        // IMPORTANTE: la spec SOLO define resource.type 0=pin, 1=line, 2=polygon y
        // geometrías Point/LineString/Polygon. Elementos no estándar (p.ej. Circle/type 7,
        // extensión del RC) se EXCLUYEN del listado: si Pilot 2 valida estrictamente la
        // lista al inicializar y encuentra un tipo desconocido, puede abortar el modo de
        // sincronización de subida (descarga sí, POST de dibujos nuevos no).
        var result = groups.Select(g => new
        {
            id             = g.Id,
            name           = g.Name,
            type           = g.Type,
            is_lock        = g.IsLock,
            is_distributed = g.IsDistributed,
            create_time    = g.CreateTime,
            elements       = _mapData.GetElementsByGroup(g.Id)
                .Where(IsSpecCompliantElement)
                .Select(e => new
                {
                    id          = e.Id,
                    name        = e.Name,
                    create_time = e.CreateTime,
                    update_time = e.UpdateTime,
                    resource    = e.Resource
                })
        });

        _logger.LogInformation("[MapController] GET element-groups workspace={WorkspaceId} → {GroupCount} grupos, {ElementCount} elementos conformes",
            workspaceId, groups.Count, groups.Sum(g => _mapData.GetElementsByGroup(g.Id).Count(IsSpecCompliantElement)));
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
        var elements = _mapData.GetElementsByGroup(groupId)
            .Where(IsSpecCompliantElement)
            .Select(e => new
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

        // Si el payload viene en formato oficial DJI spec (content a nivel raíz en lugar de envuelto en resource)
        if (patchResource == null && body["content"] is JObject content)
        {
            patchResource = new JObject
            {
                ["content"] = content
            };

            var existingElement = _mapData.GetElement(elementId);
            var existingType = existingElement?.Resource?["type"]?.Value<int>();
            if (existingType != null)
            {
                patchResource["type"] = existingType;
            }
        }

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

    // ── Helper: ¿el elemento cumple el esquema oficial DJI? ─────────────────────
    // Spec doc 42: resource.type ∈ {0=pin, 1=line, 2=polygon} y geometry.type ∈
    // {Point, LineString, Polygon}. Cualquier otra cosa (Circle/type 7 del RC) NO es
    // estándar y se excluye del GET para no romper la inicialización del módulo map.
    private static bool IsSpecCompliantElement(MapElement e)
    {
        var resType = e.Resource?["type"]?.Value<int?>();
        if (resType is not (0 or 1 or 2)) return false;

        var geomType = e.Resource?["content"]?["geometry"]?["type"]?.ToString();
        return geomType is "Point" or "LineString" or "Polygon";
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

    // GET /map/api/v1/workspaces/{workspaceId}/element-icons
    // DJI Pilot 2 (versiones recientes) llama a este endpoint durante la inicialización
    // del módulo de mapa para obtener el catálogo de iconos disponibles. NO está en la
    // documentación oficial v1.11, pero el RC lo invoca repetidamente. Si devuelve 404,
    // Pilot 2 puede dejar el módulo de mapa en modo SOLO-LECTURA (descarga elementos pero
    // NUNCA hace POST de los dibujados por el piloto) — mismo patrón degradado que
    // flight-areas/url. Devolvemos una lista vacía válida para habilitar la subida.
    [HttpGet("map/api/v1/workspaces/{workspaceId}/element-icons")]
    public IActionResult GetElementIcons(string workspaceId)
    {
        _logger.LogInformation("[MapController] GET element-icons workspace={WorkspaceId} → lista vacía OK (evita modo degradado)", workspaceId);
        return Ok(new { code = 0, message = "success", data = new List<object>() });
    }
}
