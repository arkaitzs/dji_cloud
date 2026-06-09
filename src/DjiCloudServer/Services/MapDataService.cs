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

    public MapDataService(ILogger<MapDataService> logger)
    {
        _logger = logger;
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "map_elements.json");
        Load();

        lock (_lock)
        {
            if (_store.Groups.Count == 0)
            {
                _store.Groups.Add(new ElementGroup
                {
                    Id = DefaultGroupId,
                    Name = "APP",
                    Type = 1,
                    IsLock = false,
                    CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                Save();
            }
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
            var g = _store.Groups.FirstOrDefault();
            if (g != null) return g;

            g = new ElementGroup { Id = DefaultGroupId, Name = "APP" };
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
