using DjiCloudServer.Models;
using Newtonsoft.Json;

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

    // DJI Pilot 2 always uses this zero-UUID as the group_id when posting new map elements.
    // Without a matching group entry in the store, GET /element-groups will never return
    // Pilot 2 elements even though they are stored correctly.
    private const string PilotSharedGroupId = "00000000-0000-0000-0000-000000000000";

    public MapDataService(ILogger<MapDataService> logger)
    {
        _logger = logger;
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "map_elements.json");
        Load();

        lock (_lock)
        {
            bool changed = false;

            // ── CRÍTICO: grupo App Shared (zero-UUID) ──────────────────────────────
            // DJI Pilot 2 siempre envía POST elements a group_id="00000000-...".
            // Sin esta entrada en el store, GET /element-groups no devuelve esos
            // elementos aunque estén guardados en disco. Este grupo DEBE existir.
            if (!_store.Groups.Any(g => g.Id == PilotSharedGroupId))
            {
                _store.Groups.Insert(0, new ElementGroup
                {
                    Id = PilotSharedGroupId,
                    Name = "APP",
                    Type = 2,   // App Shared Element Group — requerido por DJI Pilot 2
                    IsLock = false,
                    CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                _logger.LogInformation(
                    "[MapData] Grupo App Shared (id=00000000-..., type=2) creado. " +
                    "DJI Pilot 2 sincronizará elementos de mapa en este grupo.");
                changed = true;
            }
            else
            {
                // Migración: si el grupo ya existe pero tiene tipo incorrecto, corregirlo
                var pilotGroup = _store.Groups.First(g => g.Id == PilotSharedGroupId);
                if (pilotGroup.Type != 2)
                {
                    _logger.LogWarning(
                        "[MapData] Corrigiendo type del grupo App Shared (00000000-...): {OldType} → 2",
                        pilotGroup.Type);
                    pilotGroup.Type = 2;
                    changed = true;
                }
            }

            // ── Grupo por defecto para el mapa web ─────────────────────────────────
            if (!_store.Groups.Any(g => g.Id == DefaultGroupId))
            {
                _store.Groups.Add(new ElementGroup
                {
                    Id = DefaultGroupId,
                    Name = "Web",
                    Type = 1,   // Default Element Group para elementos creados desde el navegador
                    IsLock = false,
                    CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                changed = true;
            }

            if (changed) Save();
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            var json = File.ReadAllText(_filePath);
            _store = JsonConvert.DeserializeObject<MapStore>(json) ?? new MapStore();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[MapData] Error cargando map_elements.json — usando store vacío");
            _store = new MapStore();
        }
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(_filePath, JsonConvert.SerializeObject(_store, Formatting.Indented));
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
            // Prefer the App Shared group (zero-UUID) so web-created elements
            // are visible on the RC and other clients that only check that group.
            var g = _store.Groups.FirstOrDefault(x => x.Id == PilotSharedGroupId)
                 ?? _store.Groups.FirstOrDefault();
            if (g != null) return g;

            g = new ElementGroup { Id = PilotSharedGroupId, Name = "APP", Type = 2 };
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
