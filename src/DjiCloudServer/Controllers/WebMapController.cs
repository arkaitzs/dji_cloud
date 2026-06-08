using DjiCloudServer.Models;
using DjiCloudServer.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DjiCloudServer.Controllers;

// ──────────────────────────────────────────────────────────────────────────────
// WebMapController — API para el cliente PC (aplicación web de control)
//
// Base URL: /api/web-map/workspaces/{workspaceId}
//
// Este controlador es la contraparte de MapController: mientras que
// MapController atiende las peticiones del RC (DJI Pilot 2), este controlador
// atiende las peticiones del operador desde el PC (mapa web).
//
// Tras cada operación CRUD se notifica al RC vía WebSocket y a otros PC
// clientes vía SignalR, asegurando sincronización bidireccional completa.
// ──────────────────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/web-map/workspaces/{workspaceId}")]
[Produces("application/json")]
public class WebMapController : ControllerBase
{
    private readonly IMapDataService _mapData;
    private readonly IMapSyncNotifier _sync;
    private readonly ILogger<WebMapController> _logger;
    private readonly IDjiWebSocketManager _wsManager;
    private readonly IFlightAreaService _flightArea;

    public WebMapController(
        IMapDataService mapData,
        IMapSyncNotifier sync,
        ILogger<WebMapController> logger,
        IDjiWebSocketManager wsManager,
        IFlightAreaService flightArea)
    {
        _mapData    = mapData;
        _sync       = sync;
        _logger     = logger;
        _wsManager  = wsManager;
        _flightArea = flightArea;
    }

    // =========================================================================
    // GRUPOS desde el PC
    // =========================================================================

    /// <summary>
    /// GET /api/web-map/workspaces/{workspaceId}/groups
    ///
    /// Devuelve todos los grupos con sus elementos para renderizar el mapa en el PC.
    /// </summary>
    [HttpGet("ws-status")]
    public IActionResult GetWsStatus(string workspaceId)
    {
        var count = _wsManager.GetConnectedCount(workspaceId);
        return Ok(new { success = true, connected_rc = count });
    }

    [HttpGet("groups")]
    public IActionResult GetGroups(string workspaceId)
    {
        var groups = _mapData.GetWorkspaceGroups(workspaceId);
        return Ok(new { success = true, data = groups });
    }

    /// <summary>
    /// POST /api/web-map/workspaces/{workspaceId}/groups
    ///
    /// El operador de PC crea un nuevo grupo de capas.
    /// Body: { "name": "Zona Norte", "type": 2, "is_lock": false }
    /// </summary>
    [HttpPost("groups")]
    public async Task<IActionResult> CreateGroup(
        string workspaceId,
        [FromBody] GroupCreateInput input)
    {
        if (input == null)
            return BadRequest(new { error = "Body inválido" });

        var group = _mapData.CreateGroup(workspaceId, input);
        await _sync.NotifyGroupCreatedAsync(workspaceId, group);

        _logger.LogInformation("[WebMap] Grupo creado: {Id} en workspace {W}", group.Id, workspaceId);
        return Ok(new { success = true, id = group.Id });
    }

    [HttpPut("groups/{groupId}")]
    public async Task<IActionResult> UpdateGroup(
        string workspaceId,
        string groupId,
        [FromBody] GroupUpdateInput input)
    {
        if (input == null)
            return BadRequest(new { error = "Body inválido" });

        var group = _mapData.UpdateGroup(workspaceId, groupId, input);
        if (group == null)
            return NotFound(new { error = "Grupo no encontrado" });

        await _sync.NotifyGroupUpdatedAsync(workspaceId, group);

        _logger.LogInformation("[WebMap] Grupo actualizado: {Id}", groupId);
        return Ok(new { success = true, id = groupId });
    }

    [HttpDelete("groups/{groupId}")]
    public async Task<IActionResult> DeleteGroup(string workspaceId, string groupId)
    {
        var deleted = _mapData.DeleteGroup(workspaceId, groupId);
        if (!deleted)
            return NotFound(new { error = "Grupo no encontrado" });

        await _sync.NotifyGroupDeletedAsync(workspaceId, groupId);
        return Ok(new { success = true, id = groupId });
    }

    // =========================================================================
    // ELEMENTOS desde el PC — CRUD completo
    // =========================================================================

    /// <summary>
    /// GET /api/web-map/workspaces/{workspaceId}/elements
    ///
    /// Lista todos los elementos del workspace (de todos los grupos).
    /// Query: group_id (opcional) — filtra por grupo
    /// </summary>
    [HttpGet("elements")]
    public IActionResult GetElements(
        string workspaceId,
        [FromQuery(Name = "group_id")] string? groupId = null)
    {
        var groups = _mapData.GetWorkspaceGroups(workspaceId);
        if (!string.IsNullOrEmpty(groupId))
            groups = groups.Where(g => g.Id == groupId).ToList();

        return Ok(new { success = true, data = groups });
    }

    /// <summary>
    /// POST /api/web-map/workspaces/{workspaceId}/elements
    ///
    /// El PC dibuja un elemento nuevo y lo envía al servidor.
    /// Se persiste, se notifica al RC (WebSocket DJI) y a otros PC (SignalR).
    ///
    /// Body:
    /// {
    ///   "id": "uuid",
    ///   "group_id": "uuid-del-grupo",
    ///   "name": "Mi Punto",
    ///   "resource": {
    ///     "user_name": "Operator",
    ///     "type": 0,
    ///     "content": {
    ///       "type": "Feature",
    ///       "properties": { "color": "#FF6600", "clampToGround": false },
    ///       "geometry": { "type": "Point", "coordinates": [lon, lat, alt] }
    ///     }
    ///   }
    /// }
    /// </summary>
    [HttpPost("elements")]
    public async Task<IActionResult> CreateElement(
        string workspaceId,
        [FromBody] WebElementInput input)
    {
        if (input == null || string.IsNullOrEmpty(input.Id) || string.IsNullOrEmpty(input.GroupId))
            return BadRequest(new { error = "id y group_id son obligatorios" });

        var createInput = new ElementCreateInput
        {
            Id       = input.Id,
            Name     = input.Name,
            Resource = input.Resource
        };

        var (element, groupId) = _mapData.AddElement(workspaceId, input.GroupId, createInput);

        // map_element_create para clientes SignalR (web) + map_group_refresh para RC
        // Pilot 2 no renderiza map_element_create push del servidor; solo re-fetcha al recibir map_group_refresh
        await _sync.NotifyElementCreatedAsync(workspaceId, groupId, element);
        await _sync.NotifyMapRefreshAsync(workspaceId, groupId);

        _logger.LogInformation("[WebMap] Elemento creado: {Id} en grupo {G}", element.Id, groupId);
        return Ok(new { success = true, id = element.Id });
    }

    /// <summary>
    /// PUT /api/web-map/workspaces/{workspaceId}/elements/{id}
    ///
    /// El PC actualiza un elemento existente (mover, cambiar color, renombrar...).
    ///
    /// Body: { "name"?: "...", "content"?: { "properties"?: {...}, "geometry"?: {...} } }
    /// </summary>
    [HttpPut("elements/{id}")]
    public async Task<IActionResult> UpdateElement(
        string workspaceId,
        string id,
        [FromBody] ElementUpdateInput input)
    {
        if (input == null)
            return BadRequest(new { error = "Body inválido" });

        var element = _mapData.UpdateElement(workspaceId, id, input);
        if (element == null)
            return NotFound(new { error = "Elemento no encontrado" });

        var groups  = _mapData.GetWorkspaceGroups(workspaceId);
        var groupId = groups.FirstOrDefault(g => g.Elements.Any(e => e.Id == id))?.Id ?? "unknown";

        await _sync.NotifyElementUpdatedAsync(workspaceId, groupId, element);
        await _sync.NotifyMapRefreshAsync(workspaceId, groupId);

        _logger.LogInformation("[WebMap] Elemento actualizado: {Id}", id);
        return Ok(new { success = true, id });
    }

    /// <summary>
    /// DELETE /api/web-map/workspaces/{workspaceId}/elements/{id}
    ///
    /// El PC elimina un elemento individual.
    /// </summary>
    [HttpDelete("elements/{id}")]
    public async Task<IActionResult> DeleteElement(string workspaceId, string id)
    {
        var deleted = _mapData.DeleteElement(workspaceId, id, out var groupId);
        if (!deleted)
            return NotFound(new { error = "Elemento no encontrado" });

        await _sync.NotifyElementDeletedAsync(workspaceId, groupId!, id);
        await _sync.NotifyMapRefreshAsync(workspaceId, groupId!);

        _logger.LogInformation("[WebMap] Elemento eliminado: {Id}", id);
        return Ok(new { success = true, id });
    }

    /// <summary>
    /// DELETE /api/web-map/workspaces/{workspaceId}/elements
    ///
    /// Eliminación en lote desde el PC.
    ///
    /// Body: { "ids": ["uuid1", "uuid2", ...] }
    /// </summary>
    [HttpDelete("elements")]
    public async Task<IActionResult> DeleteElements(
        string workspaceId,
        [FromBody] BatchDeleteInput input)
    {
        if (input?.Ids == null || input.Ids.Count == 0)
            return BadRequest(new { error = "La lista de IDs no puede estar vacía" });

        var count = _mapData.DeleteElements(workspaceId, input.Ids, out var deleted);

        // Notificar cada eliminación al PC (SignalR) y un solo refresh al RC por grupo
        foreach (var (elId, groupId) in deleted)
            await _sync.NotifyElementDeletedAsync(workspaceId, groupId, elId);

        foreach (var groupId in deleted.Select(d => d.groupId).Distinct())
            await _sync.NotifyMapRefreshAsync(workspaceId, groupId);

        _logger.LogInformation("[WebMap] Batch delete: {N} elementos eliminados", count);
        return Ok(new { success = true, deleted_count = count, ids = deleted.Select(d => d.id) });
    }

    // =========================================================================
    // IMPORTAR / EXPORTAR KML
    // =========================================================================

    /// <summary>
    /// POST /api/web-map/workspaces/{workspaceId}/elements/import-kml
    ///
    /// Importa elementos desde un fichero KML estándar.
    /// Convierte Placemarks KML a elementos DJI y los añade al grupo especificado.
    ///
    /// Content-Type: multipart/form-data
    /// Form fields:
    ///   file     — fichero .kml
    ///   group_id — ID del grupo destino (opcional, usa el grupo por defecto si no se indica)
    ///
    /// Soporta: Point, LineString, Polygon con colores ExtendedData DJI o colores KML.
    /// </summary>
    [HttpPost("elements/import-kml")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportKml(
        string workspaceId,
        IFormFile file,
        [FromForm(Name = "group_id")] string? groupId = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Fichero KML requerido" });

        if (!file.FileName.EndsWith(".kml", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Solo se aceptan ficheros .kml" });

        string kmlContent;
        using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
            kmlContent = await reader.ReadToEndAsync();

        var targetGroupId = string.IsNullOrEmpty(groupId)
            ? _mapData.GetOrCreateDefaultGroup(workspaceId).Id
            : groupId;

        List<MapElement> imported;
        try
        {
            imported = ParseKmlToElements(kmlContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WebMap] Error parseando KML");
            return BadRequest(new { error = "KML inválido: " + ex.Message });
        }

        var results = new List<string>();
        foreach (var el in imported)
        {
            var input = new ElementCreateInput
            {
                Id       = el.Id,
                Name     = el.Name,
                Resource = el.Resource
            };
            var (element, _) = _mapData.AddElement(workspaceId, targetGroupId, input);
            await _sync.NotifyElementCreatedAsync(workspaceId, targetGroupId, element);
            results.Add(element.Id);
        }

        _logger.LogInformation("[WebMap] KML importado: {N} elementos en grupo {G}", results.Count, targetGroupId);
        return Ok(new { success = true, imported_count = results.Count, ids = results, group_id = targetGroupId });
    }

    /// <summary>
    /// GET /api/web-map/workspaces/{workspaceId}/elements/export-kml
    ///
    /// Exporta los elementos del workspace a formato KML estándar.
    /// Compatible con Google Earth, QGIS y DJI Terra.
    ///
    /// Query: group_id (opcional) — exporta solo un grupo específico
    /// </summary>
    [HttpGet("elements/export-kml")]
    public IActionResult ExportKml(
        string workspaceId,
        [FromQuery(Name = "group_id")] string? groupId = null)
    {
        var groups = _mapData.GetWorkspaceGroups(workspaceId);
        if (!string.IsNullOrEmpty(groupId))
            groups = groups.Where(g => g.Id == groupId).ToList();

        var kml = BuildKml(groups);
        var bytes = Encoding.UTF8.GetBytes(kml);
        var fileName = $"dji_map_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.kml";

        return File(bytes, "application/vnd.google-earth.kml+xml", fileName);
    }

    // =========================================================================
    // Endpoint legado — mantener compatibilidad con frontend existente
    // =========================================================================

    /// <summary>
    /// POST /api/web-map/workspaces/{workspaceId}/send-element
    ///
    /// Endpoint legado. Se recomienda usar POST /elements en su lugar.
    /// </summary>
    [HttpPost("send-element")]
    public async Task<IActionResult> SendElement(
        string workspaceId,
        [FromBody] WebElementInput input)
    {
        if (input == null || string.IsNullOrEmpty(input.Id) || string.IsNullOrEmpty(input.GroupId))
            return BadRequest(new { error = "El ID del elemento y el ID de grupo son obligatorios." });

        var createInput = new ElementCreateInput
        {
            Id       = input.Id,
            Name     = input.Name,
            Resource = input.Resource
        };
        var (element, groupId) = _mapData.AddElement(workspaceId, input.GroupId, createInput);
        await _sync.NotifyElementCreatedAsync(workspaceId, groupId, element);

        _logger.LogInformation("[WebMap] SendElement (legado): {Id} → grupo {G}", element.Id, groupId);
        return Ok(new { success = true, id = element.Id });
    }

    // =========================================================================
    // KML helpers — Parser y Builder
    // =========================================================================

    /// <summary>
    /// Parsea un documento KML y devuelve una lista de MapElement DJI.
    ///
    /// Mapeo KML → DJI:
    ///   kml:Point      → resource.type=0, geometry.type="Point"
    ///   kml:LineString → resource.type=1, geometry.type="LineString"
    ///   kml:Polygon    → resource.type=2, geometry.type="Polygon"
    ///
    /// Color: usa ExtendedData/dji:color si existe; si no, kml:Style/LineStyle/color
    /// (formato KML: aabbggrr → convierte a #rrggbb para DJI).
    /// </summary>
    private static List<MapElement> ParseKmlToElements(string kmlXml)
    {
        XNamespace kml = "http://www.opengis.net/kml/2.2";
        var doc = XDocument.Parse(kmlXml);

        // Soportar documentos con y sin namespace explícito
        var placemarks = doc.Descendants()
            .Where(e => e.Name.LocalName == "Placemark")
            .ToList();

        var elements = new List<MapElement>();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        foreach (var pm in placemarks)
        {
            var name  = pm.Elements().FirstOrDefault(e => e.Name.LocalName == "name")?.Value ?? "Importado";
            var color = ExtractKmlColor(pm) ?? "#0091FF";

            // Intentar leer ID desde ExtendedData o generar uno nuevo
            var id = ExtractExtendedDataValue(pm, "dji:id") ?? Guid.NewGuid().ToString();

            // Detectar geometría
            MapGeometry? geometry = null;
            int resourceType = 0;

            var pointEl = pm.Descendants().FirstOrDefault(e => e.Name.LocalName == "Point");
            var lineEl  = pm.Descendants().FirstOrDefault(e => e.Name.LocalName == "LineString");
            var polyEl  = pm.Descendants().FirstOrDefault(e => e.Name.LocalName == "Polygon");

            if (pointEl != null)
            {
                var coords = ParseKmlCoordinatesSingle(
                    pointEl.Descendants().First(e => e.Name.LocalName == "coordinates").Value);
                geometry     = new MapGeometry { Type = "Point",      Coordinates = coords };
                resourceType = 0;
            }
            else if (lineEl != null)
            {
                var coords = ParseKmlCoordinatesMulti(
                    lineEl.Descendants().First(e => e.Name.LocalName == "coordinates").Value);
                geometry     = new MapGeometry { Type = "LineString", Coordinates = coords };
                resourceType = 1;
            }
            else if (polyEl != null)
            {
                // Solo anillo exterior (outerBoundaryIs)
                var outerRing = polyEl.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "outerBoundaryIs")
                    ?.Descendants().First(e => e.Name.LocalName == "coordinates");

                if (outerRing != null)
                {
                    var ring   = ParseKmlCoordinatesMulti(outerRing.Value);
                    geometry     = new MapGeometry { Type = "Polygon", Coordinates = new[] { ring } };
                    resourceType = 2;
                }
            }

            if (geometry == null) continue; // Saltar geometrías no soportadas

            elements.Add(new MapElement
            {
                Id         = id,
                Name       = name,
                CreateTime = now,
                UpdateTime = now,
                Resource   = new MapResource
                {
                    Type     = resourceType,
                    UserName = "Import",
                    Content  = new MapContent
                    {
                        Type       = "Feature",
                        Properties = new MapProperties { Color = color, ClampToGround = false },
                        Geometry   = geometry
                    }
                }
            });
        }

        return elements;
    }

    /// <summary>
    /// Genera un documento KML a partir de los grupos y elementos DJI.
    /// Incluye ExtendedData con el color y el ID DJI para importación posterior.
    /// </summary>
    private static string BuildKml(List<MapElementGroup> groups)
    {
        XNamespace kmlNs = "http://www.opengis.net/kml/2.2";
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(kmlNs + "kml",
                new XElement(kmlNs + "Document",
                    new XElement(kmlNs + "name", "DJI Cloud Map Export"),
                    new XElement(kmlNs + "description",
                        $"Exportado el {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"),
                    groups.SelectMany(g => g.Elements.Select(el => BuildKmlPlacemark(kmlNs, g, el)))
                )
            )
        );

        using var sw = new StringWriter();
        doc.Save(sw);
        return sw.ToString();
    }

    private static XElement BuildKmlPlacemark(XNamespace kmlNs, MapElementGroup group, MapElement el)
    {
        var geo    = el.Resource.Content.Geometry;
        var props  = el.Resource.Content.Properties;
        var pm = new XElement(kmlNs + "Placemark",
            new XElement(kmlNs + "name", el.Name),
            new XElement(kmlNs + "description", $"Grupo: {group.Name}"),
            new XElement(kmlNs + "ExtendedData",
                new XElement(kmlNs + "Data", new XAttribute("name", "dji:id"),
                    new XElement(kmlNs + "value", el.Id)),
                new XElement(kmlNs + "Data", new XAttribute("name", "dji:color"),
                    new XElement(kmlNs + "value", props.Color)),
                new XElement(kmlNs + "Data", new XAttribute("name", "dji:group_id"),
                    new XElement(kmlNs + "value", group.Id)),
                new XElement(kmlNs + "Data", new XAttribute("name", "dji:resource_type"),
                    new XElement(kmlNs + "value", el.Resource.Type))
            )
        );

        switch (geo.Type)
        {
            case "Point":
            {
                var coord = FormatCoordinatesSingle(geo.Coordinates);
                pm.Add(new XElement(kmlNs + "Point",
                    new XElement(kmlNs + "coordinates", coord)));
                break;
            }
            case "LineString":
            {
                var coords = FormatCoordinatesMulti(geo.Coordinates);
                pm.Add(new XElement(kmlNs + "LineString",
                    new XElement(kmlNs + "coordinates", coords)));
                break;
            }
            case "Polygon":
            {
                var ring   = FormatPolygonRing(geo.Coordinates);
                pm.Add(new XElement(kmlNs + "Polygon",
                    new XElement(kmlNs + "outerBoundaryIs",
                        new XElement(kmlNs + "LinearRing",
                            new XElement(kmlNs + "coordinates", ring)))));
                break;
            }
        }

        return pm;
    }

    // ── Helpers de coordenadas ────────────────────────────────────────────────

    // KML: "lon,lat,alt" → double[3]
    private static double[] ParseKmlCoordinatesSingle(string raw)
    {
        var parts = raw.Trim().Split(',');
        return new[]
        {
            double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
            double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
            parts.Length > 2 ? double.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture) : 0.0
        };
    }

    // KML: "lon,lat,alt lon,lat,alt ..." → double[][]
    private static double[][] ParseKmlCoordinatesMulti(string raw)
    {
        return raw.Trim().Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseKmlCoordinatesSingle)
            .ToArray();
    }

    private static string FormatCoordinatesSingle(object coords)
    {
        if (coords is Newtonsoft.Json.Linq.JArray ja && ja.Count >= 2)
            return $"{ja[0]},{ja[1]},{(ja.Count > 2 ? ja[2] : 0)}";
        if (coords is double[] da && da.Length >= 2)
            return $"{da[0]},{da[1]},{(da.Length > 2 ? da[2] : 0)}";
        return "0,0,0";
    }

    private static string FormatCoordinatesMulti(object coords)
    {
        if (coords is Newtonsoft.Json.Linq.JArray ja)
            return string.Join(" ", ja.Select(p => $"{p[0]},{p[1]},{(p.Count() > 2 ? p[2] : 0)}"));
        if (coords is double[][] da)
            return string.Join(" ", da.Select(p => $"{p[0]},{p[1]},{(p.Length > 2 ? p[2] : 0)}"));
        return "";
    }

    private static string FormatPolygonRing(object coords)
    {
        // coords es double[][][] (array de anillos), tomamos el primero
        if (coords is Newtonsoft.Json.Linq.JArray ja && ja.Count > 0)
            return FormatCoordinatesMulti(ja[0]!);
        if (coords is double[][][] da && da.Length > 0)
            return FormatCoordinatesMulti(da[0]);
        return "";
    }

    private static string? ExtractKmlColor(XElement placemark)
    {
        // 1. Intenta leer color DJI de ExtendedData
        var djiColor = ExtractExtendedDataValue(placemark, "dji:color");
        if (djiColor != null) return djiColor;

        // 2. Intenta leer color KML (formato aabbggrr) y convertir a #rrggbb
        var kmlColor = placemark.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "color")?.Value;
        if (kmlColor != null && kmlColor.Length == 8)
        {
            // KML: aabbggrr → RGB: #rrggbb
            var r = kmlColor.Substring(6, 2);
            var g = kmlColor.Substring(4, 2);
            var b = kmlColor.Substring(2, 2);
            return $"#{r}{g}{b}".ToUpper();
        }

        return null;
    }

    private static string? ExtractExtendedDataValue(XElement element, string dataName)
    {
        return element.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "Data"
                && e.Attribute("name")?.Value == dataName)
            ?.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "value")
            ?.Value;
    }

    // =========================================================================
    // FLIGHT AREA KML — Gestión del KML de zonas de vuelo para el RC
    // =========================================================================

    /// <summary>
    /// GET /api/web-map/workspaces/{workspaceId}/flight-area
    /// Lista los KMLs disponibles e indica cuál está activo.
    /// </summary>
    [HttpGet("flight-area")]
    public IActionResult GetFlightAreaStatus(string workspaceId)
    {
        _ = workspaceId;
        var available = _flightArea.GetAvailableKmls().ToList();
        var active    = _flightArea.GetActiveKmlRelativePath();
        var activeName = active == null ? null : Path.GetFileName(active);
        return Ok(new { available, active = activeName });
    }

    /// <summary>
    /// POST /api/web-map/workspaces/{workspaceId}/flight-area/upload
    /// Sube un KML y opcionalmente lo activa.
    /// Form: file (required), activate (bool, default true)
    /// </summary>
    [HttpPost("flight-area/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFlightAreaKml(
        string workspaceId,
        IFormFile file,
        [FromForm] bool activate = true)
    {
        _ = workspaceId;
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Archivo KML requerido" });

        await using var stream = file.OpenReadStream();
        var savedName = await _flightArea.SaveKmlAsync(file.FileName, stream);

        if (activate)
            await _flightArea.SetActiveKmlAsync(savedName);

#pragma warning disable CA1873
        _logger.LogInformation("[FlightArea] KML subido: {Name} (activo={Active})", savedName, activate);
#pragma warning restore CA1873
        return Ok(new { success = true, filename = savedName, active = activate });
    }

    /// <summary>
    /// PUT /api/web-map/workspaces/{workspaceId}/flight-area/active
    /// Cambia el KML activo a uno ya subido.
    /// Body: { "filename": "zonas.kml" }
    /// </summary>
    [HttpPut("flight-area/active")]
    public async Task<IActionResult> SetActiveFlightAreaKml(
        string workspaceId,
        [FromBody] FlightAreaActiveInput input)
    {
        _ = workspaceId;
        if (string.IsNullOrEmpty(input?.Filename))
            return BadRequest(new { error = "filename requerido" });

        try
        {
            await _flightArea.SetActiveKmlAsync(input.Filename);
#pragma warning disable CA1873
            _logger.LogInformation("[FlightArea] KML activo cambiado a: {Name}", input.Filename);
#pragma warning restore CA1873
            return Ok(new { success = true, active = input.Filename });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/web-map/workspaces/{workspaceId}/flight-area/{filename}
    /// Elimina un KML del servidor.
    /// </summary>
    [HttpDelete("flight-area/{filename}")]
    public IActionResult DeleteFlightAreaKml(string workspaceId, string filename)
    {
        _ = workspaceId;
        var deleted = _flightArea.DeleteKml(filename);
        if (!deleted) return NotFound(new { error = $"KML '{filename}' no encontrado" });
#pragma warning disable CA1873
        _logger.LogInformation("[FlightArea] KML eliminado: {Name}", filename);
#pragma warning restore CA1873
        return Ok(new { success = true });
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// DTOs específicos del WebMapController
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>Input para crear un elemento desde el PC (incluye group_id).</summary>
public class WebElementInput
{
    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    [JsonProperty("group_id")]
    public string GroupId { get; set; } = null!;

    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("resource")]
    public MapResource Resource { get; set; } = new();
}

/// <summary>Input para eliminación en lote.</summary>
public class BatchDeleteInput
{
    [JsonProperty("ids")]
    public List<string> Ids { get; set; } = new();
}

/// <summary>Input para cambiar el KML activo de zonas de vuelo.</summary>
public class FlightAreaActiveInput
{
    [JsonProperty("filename")]
    public string? Filename { get; set; }
}
