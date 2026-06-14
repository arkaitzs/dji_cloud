using DjiCloudServer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DjiCloudServer.Services;

public interface IMapDataService
{
    List<ElementGroup> GetGroups();
    ElementGroup GetOrCreateDefaultGroup();
    List<MapElement> GetAllElements();
    List<MapElement> GetElementsByGroup(string groupId);
    MapElement? GetElement(string id);
    MapElement AddElement(string groupId, MapElement element);
    MapElement? UpdateElement(string id, MapElement updated);
    bool DeleteElement(string id);
}

public class MapDataService : IMapDataService
{
    private readonly string _filePath;
    private readonly ILogger<MapDataService> _logger;
    private readonly object _lock = new();
    private MapStore _store = new();

    private const string DefaultGroupId = "e3dea0f5-37f2-4d79-ae58-490af3228001";

    // Capa "Pilot Share Layer" (type 2) — DEBE tener un UUID REAL y is_distributed=1,
    // EXACTAMENTE como el seed oficial de DJI (verificado 2026-06-14 contra el servidor
    // oficial: el RC sube los dibujos EN TIEMPO REAL a este grupo). El zero-UUID que
    // usábamos antes Pilot lo trataba como capa LOCAL → solo subía en batch al reconectar.
    private const string PilotSharedGroupId    = "e3dea0f5-37f2-4d79-ae58-490af3228060";
    // UUID antiguo (zero) — migramos sus elementos al UUID real arriba.
    private const string OldPilotSharedGroupId = "00000000-0000-0000-0000-000000000000";

    public MapDataService(ILogger<MapDataService> logger)
    {
        _logger = logger;
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "map_elements.json");
        Load();

        lock (_lock)
        {
            bool changed = false;

            // ── MIGRACIÓN: zero-UUID → UUID real del Pilot Share Layer ─────────────
            // El grupo type-2 pasó de zero-UUID a UUID real (e3dea0f5-...-3228060) para
            // que el RC suba en tiempo real. Movemos el grupo viejo y sus elementos.
            var legacyGroup = _store.Groups.FirstOrDefault(g => g.Id == OldPilotSharedGroupId);
            if (legacyGroup != null) { _store.Groups.Remove(legacyGroup); changed = true; }
            foreach (var el in _store.Elements.Where(e => e.GroupId == OldPilotSharedGroupId))
            {
                el.GroupId = PilotSharedGroupId;
                changed = true;
            }
            if (changed)
                _logger.LogInformation("[MapData] Migración Pilot Share Layer: zero-UUID → {NewId}", PilotSharedGroupId);

            // ── CRÍTICO: grupo Pilot Share Layer (type 2, UUID real, is_distributed=1)
            // Seed oficial DJI. El RC POSTea sus dibujos a este grupo EN TIEMPO REAL.
            if (!_store.Groups.Any(g => g.Id == PilotSharedGroupId))
            {
                _store.Groups.Insert(0, new ElementGroup
                {
                    Id = PilotSharedGroupId,
                    Name = "Pilot Share Layer",
                    Type = 2,   // App Shared Element Group — requerido por DJI Pilot 2
                    IsLock = false,
                    IsDistributed = true,
                    CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                _logger.LogInformation(
                    "[MapData] Grupo Pilot Share Layer (UUID real {Id}, type=2, is_distributed=1) creado.",
                    PilotSharedGroupId);
                changed = true;
            }
            else
            {
                // Migración: si el grupo ya existe, asegurar type=2 + is_distributed=1
                var pilotGroup = _store.Groups.First(g => g.Id == PilotSharedGroupId);
                if (pilotGroup.Type != 2) { pilotGroup.Type = 2; changed = true; }
                if (!pilotGroup.IsDistributed) { pilotGroup.IsDistributed = true; changed = true; }
                if (pilotGroup.Name == "APP") { pilotGroup.Name = "Pilot Share Layer"; changed = true; }
            }

            // ── Grupo por defecto para el mapa web (Default Layer, type 1) ────────
            // Igual que el seed de la demo Java: 'Default Layer' type=1 es la capa
            // distribuida donde van los elementos creados desde el lado web.
            var defaultGroup = _store.Groups.FirstOrDefault(g => g.Id == DefaultGroupId);
            if (defaultGroup == null)
            {
                _store.Groups.Add(new ElementGroup
                {
                    Id = DefaultGroupId,
                    Name = "Default Layer",
                    Type = 1,
                    IsLock = false,
                    CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                changed = true;
            }
            else if (defaultGroup.Name != "Default Layer")
            {
                defaultGroup.Name = "Default Layer";
                changed = true;
            }

            // ── Migración: elementos web fuera de la capa Pilot Share ──────────────
            // Los elementos creados desde map.html acababan en el grupo type=2
            // (Pilot Share Layer, zero-UUID) — el RC no renderiza lo descargado de su
            // propia capa de subida. La demo Java los pone en 'Default Layer' (type 1).
            foreach (var el in _store.Elements.Where(e =>
                         e.GroupId == PilotSharedGroupId &&
                         e.Resource?["user_name"]?.ToString() == "Web"))
            {
                el.GroupId    = DefaultGroupId;
                el.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                _logger.LogInformation(
                    "[MapData] Migración: elemento web '{Name}' movido a Default Layer (type 1)", el.Name);
                changed = true;
            }

            // ── Deduplicar elementos con el mismo UUID ─────────────────────────────
            // Ocurre cuando el RC re-sube sus elementos en cada reconexión antes del fix de upsert.
            // Conservamos la copia con mayor update_time (la más reciente).
            var before = _store.Elements.Count;
            _store.Elements = _store.Elements
                .GroupBy(e => e.Id)
                .Select(g => g.OrderByDescending(e => e.UpdateTime).First())
                .ToList();
            var removed = before - _store.Elements.Count;
            if (removed > 0)
            {
                _logger.LogInformation("[MapData] Deduplicación: eliminados {Count} elementos duplicados ({Before} → {After})",
                    removed, before, _store.Elements.Count);
                changed = true;
            }

            // ── Normalizar elementos sin radius en la geometría ────────────────────
            // DJI Pilot 2 incluye "radius" en TODOS sus elementos (0.0 para Point/Line/Polygon,
            // valor real para Circle). Sin este campo, el RC omite el elemento al renderizar
            // la capa de cloud elements. Los elementos creados desde map.html antes de este fix
            // carecen del campo — este paso los repara en disco al arrancar el servidor.
            int normalizedCount = 0;
            foreach (var el in _store.Elements)
            {
                // ── user_name dentro del resource (alineado con la demo Java) ──────
                // La demo persiste resource.user_name al crear y lo devuelve en
                // GET element-groups (lo que descarga el RC). Los elementos creados
                // antes de este fix carecen del campo — derivarlo del prefijo del nombre.
                if (el.Resource is JObject res && string.IsNullOrEmpty(res["user_name"]?.ToString()))
                {
                    var derivedUser = "pilot";
                    if (!string.IsNullOrEmpty(el.Name))
                    {
                        var lastSpace = el.Name.LastIndexOf(' ');
                        derivedUser = lastSpace > 0 && int.TryParse(el.Name[(lastSpace + 1)..], out _)
                            ? el.Name[..lastSpace] : el.Name;
                    }
                    res["user_name"] = derivedUser;
                    el.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    normalizedCount++;
                    changed = true;
                }

                if (el.Resource?.SelectToken("content.geometry") is not JObject geom) continue;

                // Añadir radius si falta
                if (!geom.ContainsKey("radius"))
                {
                    geom["radius"] = 0.0;
                    el.UpdateTime  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    normalizedCount++;
                    changed = true;
                }

                // FORMATO NATIVO DEL RC (verificado con POST reales de DJI Pilot 2 v17.1.5.15):
                //   Point:  coordinates [lon, lat, 0.0] (3D) + clampToGround: true
                //   Circle: coordinates [lon, lat] (2D)      + clampToGround: false
                // Normalizar los Points almacenados al formato 3D nativo del mando.
                if (geom["type"]?.ToString() == "Point"
                    && geom["coordinates"] is JArray coords2d && coords2d.Count == 2)
                {
                    geom["coordinates"] = new JArray(coords2d[0], coords2d[1], 0.0);
                    el.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    normalizedCount++;
                    changed = true;
                }

                // clampToGround: true para Points (formato nativo del RC)
                if (geom["type"]?.ToString() == "Point"
                    && el.Resource?.SelectToken("content.properties") is JObject props
                    && props["clampToGround"]?.Value<bool>() != true)
                {
                    props["clampToGround"] = true;
                    el.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    changed = true;
                }

                // PALETA DJI (spec doc 40): el RC solo renderiza 6 colores concretos
                // y descarta en silencio cualquier otro (p.ej. el cian #06b6d4 que
                // usaba map.html). Mapear al color de paleta más cercano.
                if (el.Resource?.SelectToken("content.properties") is JObject colorProps
                    && colorProps["color"] is JToken colorTok)
                {
                    var original   = colorTok.ToString();
                    var normalized = Controllers.WebMapController.NormalizeToDjiPalette(original);
                    if (!string.Equals(original, normalized, StringComparison.OrdinalIgnoreCase))
                    {
                        colorProps["color"] = normalized;
                        el.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        normalizedCount++;
                        changed = true;
                    }
                }
            }
            if (normalizedCount > 0)
                _logger.LogInformation(
                    "[MapData] Normalización: corregidos {Count} elemento(s) web-created (radius + coords 2D). " +
                    "Ahora son visibles en el mando RC.", normalizedCount);

            if (changed) Save();
        }
    }

    private void Load()
    {
        // #2.7: lectura con recuperación — si map_elements.json está corrupto
        // (crash a mitad de escritura), se restaura desde el backup .bak.
        var store = AtomicJsonFile.ReadWithRecovery(
            _filePath,
            json => JsonConvert.DeserializeObject<MapStore>(json),
            recoveredFrom => _logger.LogWarning(
                "[MapData] map_elements.json corrupto — recuperado desde {Backup}", recoveredFrom));

        if (store != null)
        {
            _store = store;
        }
        else if (File.Exists(_filePath))
        {
            _logger.LogError("[MapData] map_elements.json y su backup ilegibles — usando store vacío");
            _store = new MapStore();
        }
    }

    private void Save()
    {
        try
        {
            // #2.7: escritura atómica (tmp + File.Replace) con backup .bak —
            // un crash durante el guardado ya no puede corromper el store.
            AtomicJsonFile.Write(_filePath, JsonConvert.SerializeObject(_store, Formatting.Indented));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[MapData] Error guardando map_elements.json");
        }
    }

    public List<ElementGroup> GetGroups()
    {
        lock (_lock) { return _store.Groups.ToList(); }
    }

    public ElementGroup GetOrCreateDefaultGroup()
    {
        lock (_lock)
        {
            // Los elementos web van al grupo type=1 (Default Layer), igual que la demo Java:
            // el seed oficial pone los elementos del lado web en 'Default Layer' (type 1,
            // is_distributed) y reserva el grupo type=2 (Pilot Share Layer) como capa de
            // SUBIDA de los pilotos. El RC renderiza las capas distribuidas type 0/1,
            // pero no lo que descarga de su propia capa compartida type 2.
            var g = _store.Groups.FirstOrDefault(x => x.Id == DefaultGroupId)
                 ?? _store.Groups.FirstOrDefault(x => x.Type == 1);
            if (g != null) return g;

            g = new ElementGroup { Id = DefaultGroupId, Name = "Default Layer", Type = 1 };
            _store.Groups.Add(g);
            Save();
            return g;
        }
    }

    public List<MapElement> GetAllElements()
    {
        lock (_lock) { return _store.Elements.ToList(); }
    }

    public List<MapElement> GetElementsByGroup(string groupId)
    {
        lock (_lock) { return _store.Elements.Where(e => e.GroupId == groupId).ToList(); }
    }

    public MapElement? GetElement(string id)
    {
        lock (_lock) { return _store.Elements.FirstOrDefault(e => e.Id == id); }
    }

    public MapElement AddElement(string groupId, MapElement element)
    {
        lock (_lock)
        {
            element.GroupId = groupId;
            if (string.IsNullOrEmpty(element.Id)) element.Id = Guid.NewGuid().ToString();

            // ── UPSERT: si ya existe un elemento con el mismo UUID, actualizar en lugar de insertar.
            // DJI Pilot 2 re-sube todos sus elementos locales en cada reconexión (con los mismos UUIDs).
            // Sin este check, el store acumula duplicados que corrompen el mapa.
            var existing = _store.Elements.FirstOrDefault(e => e.Id == element.Id);
            if (existing != null)
            {
                existing.GroupId     = groupId;
                if (!string.IsNullOrEmpty(element.Name)) existing.Name = element.Name;
                if (element.Resource != null) existing.Resource = element.Resource;
                existing.UpdateTime  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                Save();
                return existing;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            element.CreateTime = now;
            element.UpdateTime = now;
            _store.Elements.Add(element);
            Save();
            return element;
        }
    }

    public MapElement? UpdateElement(string id, MapElement updated)
    {
        lock (_lock)
        {
            var existing = _store.Elements.FirstOrDefault(e => e.Id == id);
            if (existing == null) return null;

            if (!string.IsNullOrEmpty(updated.Name)) existing.Name = updated.Name;
            if (updated.Resource != null) existing.Resource = updated.Resource;
            existing.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            Save();
            return existing;
        }
    }

    public bool DeleteElement(string id)
    {
        lock (_lock)
        {
            var el = _store.Elements.FirstOrDefault(e => e.Id == id);
            if (el == null) return false;
            _store.Elements.Remove(el);
            Save();
            return true;
        }
    }
}
