using DjiCloudServer.Models;
using DjiCloudServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

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
    private readonly IDjiWebSocketManager _wsManager;
    // #1.7: refresh de compatibilidad para Pilot 2 v17 (configurable en appsettings)
    private readonly bool _legacyGroupRefresh;

    public WebMapController(
        IMapDataService mapData,
        IMapSyncNotifier notifier,
        IDjiWebSocketManager wsManager,
        IOptions<DjiCloudOptions> options)
    {
        _mapData = mapData;
        _notifier = notifier;
        _wsManager = wsManager;
        _legacyGroupRefresh = options.Value.LegacyGroupRefresh;
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

        var resource = body["resource"] as JObject;
        // ── Normalizar para compatibilidad con DJI Pilot 2 ────────────────────────
        // El RC omite silenciosamente cualquier elemento cuya geometría carezca del
        // campo "radius". Todos los elementos del RC lo incluyen (0.0 para puntos/lineas/polígonos).
        //
        // CRÍTICO: DJI Pilot 2 ignora elementos con coordenadas 3D [lon, lat, alt].
        // Los elementos nativos del mando usan siempre 2D [lon, lat]. Si map.html
        // envía altitud=0 como tercer componente, el RC falla silenciosamente al
        // parsear la geometría y no muestra el elemento en el mapa.
        // → Forzar coordenadas 2D eliminando el tercer componente (altitud).
        if (resource?["content"]?["geometry"] is JObject geom)
        {
            // radius es obligatorio en todos los elementos DJI (0.0 para Point/Line/Polygon, valor real para Circle)
            if (!geom.ContainsKey("radius"))
                geom["radius"] = 0.0;
            // radius como entero 0 → float 0.0 (el RC puede rechazar el tipo incorrecto)
            else if (geom["radius"]?.Type == JTokenType.Integer)
                geom["radius"] = 0.0;

            // FORMATO NATIVO DEL RC (verificado con POST reales de DJI Pilot 2 v17.1.5.15):
            //   Point:  coordinates [lon, lat, 0.0] (3D) + clampToGround: true
            //   Circle: coordinates [lon, lat] (2D)      + clampToGround: false
            var geoType = geom["type"]?.ToString();
            if (geoType == "Point")
            {
                // Asegurar coords 3D (añadir altitud 0.0 si viene en 2D)
                if (geom["coordinates"] is JArray coords && coords.Count == 2)
                    geom["coordinates"] = new JArray(coords[0], coords[1], 0.0);

                // clampToGround: true — formato nativo del mando para puntos
                if (resource["content"]?["properties"] is JObject props
                    && props["clampToGround"]?.Value<bool>() != true)
                {
                    props["clampToGround"] = true;
                }
            }

            // PALETA DJI (spec doc 40): el RC solo renderiza estos 6 colores y descarta
            // silenciosamente cualquier otro. Mapear colores fuera de paleta al más cercano.
            if (resource["content"]?["properties"] is JObject colorProps
                && colorProps["color"] is JToken colorTok)
            {
                colorProps["color"] = NormalizeToDjiPalette(colorTok.ToString());
            }
        }

        var webUserName = body["user_name"]?.ToString() ?? body["operator"]?.ToString() ?? "Web";

        // user_name DENTRO del resource, persistido — igual que la demo Java.
        // Aparece en GET element-groups (lo que descarga el RC) y en los push WS.
        if (resource != null && string.IsNullOrEmpty(resource["user_name"]?.ToString()))
            resource["user_name"] = webUserName;

        var element = new MapElement
        {
            Id       = body["id"]?.ToString() ?? Guid.NewGuid().ToString(),
            Name     = body["name"]?.ToString() ?? "Nuevo elemento",
            UserName = webUserName,
            Resource = resource
        };

        var created = _mapData.AddElement(groupId, element);
        await _notifier.NotifyCreateAsync(created);

        // VERIFICADO 2026-06-10: Pilot 2 v17.1.5.15 IGNORA el push map_element_create
        // (la demo Java solo envía ese, pero este firmware no lo procesa).
        // El único push que dispara la re-descarga en el RC es map_group_refresh.
        // #1.7: configurable (DjiCloud:LegacyGroupRefresh) — false = 100% alineado con demo.
        if (_legacyGroupRefresh)
            _ = _notifier.NotifyGroupRefreshAsync(new[] { created.GroupId });

        return Ok(new { success = true, id = created.Id });
    }

    // PUT /api/web-map/workspaces/{wid}/elements/{id}
    [HttpPut("workspaces/{workspaceId}/elements/{elementId}")]
    public async Task<IActionResult> UpdateElement(string workspaceId, string elementId, [FromBody] JObject body)
    {
        var patchResource = body["resource"] as JObject;

        // Preservar user_name del elemento almacenado si el body no lo trae
        if (patchResource != null && string.IsNullOrEmpty(patchResource["user_name"]?.ToString()))
        {
            var existingUserName = _mapData.GetElement(elementId)?.Resource?["user_name"]?.ToString();
            patchResource["user_name"] = !string.IsNullOrEmpty(existingUserName) ? existingUserName : "Web";
        }

        var patch = new MapElement
        {
            Name     = body["name"]?.ToString() ?? "",
            Resource = patchResource
        };

        var result = _mapData.UpdateElement(elementId, patch);
        if (result != null)
        {
            await _notifier.NotifyUpdateAsync(result);
            // Pilot 2 v17 ignora map_element_update — forzar re-descarga del grupo
            if (_legacyGroupRefresh)
                _ = _notifier.NotifyGroupRefreshAsync(new[] { result.GroupId });
        }

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

        // Pilot 2 v17 ignora map_element_delete — forzar re-descarga del grupo
        if (_legacyGroupRefresh && !string.IsNullOrEmpty(groupId))
            _ = _notifier.NotifyGroupRefreshAsync(new[] { groupId });

        return Ok(new { success = true });
    }

    // DELETE /api/web-map/workspaces/{wid}/elements  (batch)
    [HttpDelete("workspaces/{workspaceId}/elements")]
    public async Task<IActionResult> BatchDeleteElements(string workspaceId, [FromBody] JObject body)
    {
        if (body["ids"] is JArray ids)
        {
            var touchedGroups = new HashSet<string>();
            foreach (var id in ids)
            {
                var elementId = id.ToString();
                var element = _mapData.GetElement(elementId);
                var groupId = element?.GroupId ?? "";
                _mapData.DeleteElement(elementId);
                await _notifier.NotifyDeleteAsync(elementId, groupId);
                if (!string.IsNullOrEmpty(groupId)) touchedGroups.Add(groupId);
            }
            // Pilot 2 v17 ignora map_element_delete — un único refresh por grupo afectado
            if (_legacyGroupRefresh && touchedGroups.Count > 0)
                _ = _notifier.NotifyGroupRefreshAsync(touchedGroups);
        }
        return Ok(new { success = true });
    }

    // POST /api/web-map/admin/refresh
    // Dispara map_group_refresh manualmente hacia el mando RC via WebSocket.
    // Envía todos los group_ids para que el RC descargue todos los grupos actualizados.
    [HttpPost("admin/refresh")]
    public async Task<IActionResult> TriggerMapGroupRefresh()
    {
        var allGroupIds = _mapData.GetGroups().Select(g => g.Id).ToArray();
        await _notifier.NotifyGroupRefreshAsync(allGroupIds);
        return Ok(new { success = true, message = $"map_group_refresh enviado al RC ({allGroupIds.Length} grupos)" });
    }

    // POST /api/web-map/admin/disconnect-ws
    // Cierra forzosamente todas las conexiones WebSocket activas.
    // Rompe conexiones zombie: el mando RC detecta el cierre y reconecta,
    // disparando un nuevo batch-upload de todos sus elementos locales pendientes.
    [HttpPost("admin/disconnect-ws")]
    public async Task<IActionResult> ForceDisconnectWs()
    {
        var count = _wsManager.Count;
        await _wsManager.CloseAllAsync("Admin forced disconnect — please reconnect");
        return Ok(new { success = true, message = $"WebSocket cerrado ({count} conexiones). El mando reconectará automáticamente." });
    }

    // POST /api/web-map/admin/block-ws
    // Bloquea nuevas conexiones WS Y cierra las actuales.
    // Flujo para crear elementos offline: 1) block-ws  2) crear elemento en Pilot 2  3) unblock-ws
    // Al desbloquear, el RC reconecta y sube en batch los elementos creados mientras estaba offline.
    [HttpPost("admin/block-ws")]
    public async Task<IActionResult> BlockWs()
    {
        _wsManager.BlockNewConnections = true;
        var count = _wsManager.Count;
        await _wsManager.CloseAllAsync("Server offline — reconecta cuando se desbloquee");
        return Ok(new
        {
            success = true,
            message = $"WS bloqueado ({count} conexiones cerradas). Crea el elemento en Pilot 2 AHORA y luego llama /admin/unblock-ws"
        });
    }

    // POST /api/web-map/admin/unblock-ws
    // Desbloquea nuevas conexiones WS. El mando RC reconectará automáticamente
    // y hará el batch upload de los elementos creados mientras estaba offline.
    [HttpPost("admin/unblock-ws")]
    public IActionResult UnblockWs()
    {
        _wsManager.BlockNewConnections = false;
        return Ok(new
        {
            success = true,
            message = "WS desbloqueado. El mando RC reconectará en unos segundos y subirá los elementos pendientes."
        });
    }

    // GET /api/web-map/admin/ws-status
    // Estado actual del WebSocket (número de conexiones y si está bloqueado).
    [HttpGet("admin/ws-status")]
    public IActionResult WsStatus() =>
        Ok(new
        {
            connections = _wsManager.Count,
            blocked = _wsManager.BlockNewConnections
        });

    // ── Paleta de colores DJI (spec doc 40 Create Map Elements) ────────────────
    // DJI Pilot 2 solo renderiza estos 6 colores; cualquier otro hace que el
    // elemento se descarte silenciosamente al dibujar la capa cloud.
    private static readonly (string Hex, int R, int G, int B)[] DjiPalette =
    {
        ("#2D8CF0", 0x2D, 0x8C, 0xF0), // BLUE
        ("#19BE6B", 0x19, 0xBE, 0x6B), // GREEN
        ("#FFBB00", 0xFF, 0xBB, 0x00), // YELLOW
        ("#B620E0", 0xB6, 0x20, 0xE0), // ORANGE (según enum DJI)
        ("#E23C39", 0xE2, 0x3C, 0x39), // RED
        ("#212121", 0x21, 0x21, 0x21), // PURPLE (según enum DJI)
    };

    internal static string NormalizeToDjiPalette(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return "#2D8CF0";

        var hex = color.Trim().TrimStart('#');
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
        if (hex.Length != 6 || !int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var rgb))
            return "#2D8CF0";

        int r = (rgb >> 16) & 0xFF, g = (rgb >> 8) & 0xFF, b = rgb & 0xFF;

        // Si ya es un color de paleta, devolverlo tal cual normalizado a "#RRGGBB"
        // y si no, elegir el más cercano por distancia euclídea RGB.
        var best = DjiPalette[0].Hex;
        var bestDist = int.MaxValue;
        foreach (var (pHex, pr, pg, pb) in DjiPalette)
        {
            var dist = (r - pr) * (r - pr) + (g - pg) * (g - pg) + (b - pb) * (b - pb);
            if (dist < bestDist) { bestDist = dist; best = pHex; }
        }
        return best;
    }
}
