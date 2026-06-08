using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace DjiCloudServer.Services;

// ──────────────────────────────────────────────────────────────────────────────
// Modelos de dominio (schemas DJI Cloud API)
// Referencia: https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/
//             pilot-to-cloud/https/map-elements/obtain.html
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Propiedades visuales del elemento. El campo "color" es un hex ARGB o RGB.
/// "radius" sólo aplica cuando geometry.type = "Point" y se quiere mostrar un círculo.
/// </summary>
public class MapProperties
{
    [JsonProperty("color")]
    [JsonPropertyName("color")]
    public string Color { get; set; } = "#0091FF";

    [JsonProperty("clampToGround")]
    [JsonPropertyName("clampToGround")]
    public bool ClampToGround { get; set; }

    /// <summary>
    /// Cuando true, la línea/polígono se dibuja a la altitud indicada en las coordenadas (3D).
    /// Cuando false, DJI Pilot 2 aplana el elemento al suelo ignorando el eje Z.
    /// </summary>
    [JsonProperty("is3d", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("is3d")]
    public bool? Is3d { get; set; }

    /// <summary>Radio en metros. Sólo para círculos (geometry.type = "Circle").</summary>
    [JsonProperty("radius", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("radius")]
    public double? Radius { get; set; }
}

/// <summary>
/// Geometría GeoJSON.
/// type:        "Point" | "LineString" | "Polygon"
/// coordinates: varía según tipo:
///   Point      → [lon, lat, alt]          (double[3])
///   LineString → [[lon,lat,alt], ...]     (double[][])
///   Polygon    → [[[lon,lat,alt], ...]]   (double[][][])
/// </summary>
public class MapGeometry
{
    [JsonProperty("type")]
    [JsonPropertyName("type")]
    public string Type { get; set; } = "Point";

    [JsonProperty("coordinates")]
    [JsonPropertyName("coordinates")]
    public object Coordinates { get; set; } = null!;

    /// <summary>Radio en metros. Solo para type = "Circle" (DJI lo pone en geometry, no en properties).</summary>
    [JsonProperty("radius", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("radius")]
    public double? Radius { get; set; }
}

/// <summary>
/// Feature GeoJSON con propiedades DJI. "type" siempre es "Feature".
/// </summary>
public class MapContent
{
    [JsonProperty("type")]
    [JsonPropertyName("type")]
    public string Type { get; set; } = "Feature";

    [JsonProperty("properties")]
    [JsonPropertyName("properties")]
    public MapProperties Properties { get; set; } = new();

    [JsonProperty("geometry")]
    [JsonPropertyName("geometry")]
    public MapGeometry Geometry { get; set; } = new();
}

/// <summary>
/// Recurso del elemento. "type" int debe ser coherente con geometry.type string:
///   0 → "Point" | 1 → "LineString" | 2 → "Polygon"
/// </summary>
public class MapResource
{
    /// <summary>0 = Point, 1 = LineString, 2 = Polygon</summary>
    [JsonProperty("type")]
    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonProperty("content")]
    [JsonPropertyName("content")]
    public MapContent Content { get; set; } = new();

    [JsonProperty("user_name")]
    [JsonPropertyName("user_name")]
    public string UserName { get; set; } = "Pilot";
}

/// <summary>
/// Elemento del mapa. Unidad mínima de la topografía DJI.
/// </summary>
public class MapElement
{
    [JsonProperty("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonProperty("create_time")]
    [JsonPropertyName("create_time")]
    public long CreateTime { get; set; }

    [JsonProperty("update_time")]
    [JsonPropertyName("update_time")]
    public long UpdateTime { get; set; }

    [JsonProperty("resource")]
    [JsonPropertyName("resource")]
    public MapResource Resource { get; set; } = new();
}

/// <summary>
/// Grupo de elementos. "type" determina visibilidad:
///   0 = Personal (sólo el piloto que lo creó)
///   1 = Shared (todos los pilotos en el workspace)
///   2 = App Shared (creado por la app de control, compartido con todos)
/// </summary>
public class MapElementGroup
{
    [JsonProperty("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    /// <summary>0=Personal, 1=Shared, 2=AppShared</summary>
    [JsonProperty("type")]
    [JsonPropertyName("type")]
    public int Type { get; set; } = 2;

    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Capa Compartida";

    [JsonProperty("is_lock")]
    [JsonPropertyName("is_lock")]
    public bool IsLock { get; set; }

    [JsonProperty("create_time")]
    [JsonPropertyName("create_time")]
    public long CreateTime { get; set; }

    [JsonProperty("update_time")]
    [JsonPropertyName("update_time")]
    public long UpdateTime { get; set; }

    [JsonProperty("elements")]
    [JsonPropertyName("elements")]
    public List<MapElement> Elements { get; set; } = new();
}

// ──────────────────────────────────────────────────────────────────────────────
// DTOs de entrada
// ──────────────────────────────────────────────────────────────────────────────

public class ElementCreateInput
{
    [JsonProperty("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonProperty("resource")]
    [JsonPropertyName("resource")]
    public MapResource Resource { get; set; } = new();
}

public class ElementUpdateInput
{
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonProperty("content")]
    [JsonPropertyName("content")]
    public MapContent? Content { get; set; }
}

public class GroupCreateInput
{
    [JsonProperty("id")]
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Nueva Capa";

    [JsonProperty("type")]
    [JsonPropertyName("type")]
    public int Type { get; set; } = 2;

    [JsonProperty("is_lock")]
    [JsonPropertyName("is_lock")]
    public bool IsLock { get; set; }
}

public class GroupUpdateInput
{
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonProperty("is_lock")]
    [JsonPropertyName("is_lock")]
    public bool? IsLock { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────────
// Interfaz del servicio
// ──────────────────────────────────────────────────────────────────────────────

public interface IMapDataService
{
    // ── Grupos ────────────────────────────────────────────────────────────────
    List<MapElementGroup> GetWorkspaceGroups(string workspaceId);
    MapElementGroup? GetGroup(string workspaceId, string groupId);
    MapElementGroup GetOrCreateDefaultGroup(string workspaceId);
    MapElementGroup CreateGroup(string workspaceId, GroupCreateInput input);
    MapElementGroup? UpdateGroup(string workspaceId, string groupId, GroupUpdateInput input);
    bool DeleteGroup(string workspaceId, string groupId);

    // ── Elementos ─────────────────────────────────────────────────────────────
    List<MapElement> GetGroupElements(string workspaceId, string groupId);
    MapElement? GetElement(string workspaceId, string elementId);
    (MapElement element, string groupId) AddElement(string workspaceId, string groupId, ElementCreateInput input);
    MapElement? UpdateElement(string workspaceId, string elementId, ElementUpdateInput input);
    bool DeleteElement(string workspaceId, string elementId, out string? outGroupId);
    int DeleteElements(string workspaceId, IEnumerable<string> elementIds, out List<(string id, string groupId)> deleted);

    // ── Persistencia ──────────────────────────────────────────────────────────
    Task SaveAsync();
    Task LoadAsync();
}

// ──────────────────────────────────────────────────────────────────────────────
// Implementación con persistencia JSON
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Implementación de IMapDataService con:
///   - Almacenamiento en memoria (ConcurrentDictionary) para acceso rápido
///   - Persistencia en fichero JSON para sobrevivir reinicios
///   - Thread-safety mediante ReaderWriterLockSlim por workspace
///
/// NOTA ARQUITECTÓNICA: Para producción se recomienda reemplazar la capa de
/// persistencia JSON por EF Core + SQLite/PostgreSQL. La interfaz permanece
/// igual; sólo cambia la implementación de SaveAsync/LoadAsync.
/// </summary>
public class MapDataService : IMapDataService
{
    // workspace_id → (group_id → group)
    private readonly ConcurrentDictionary<string, Dictionary<string, MapElementGroup>> _workspaces = new();

    // Un lock por workspace para operaciones de lectura/escritura finas
    private readonly ConcurrentDictionary<string, ReaderWriterLockSlim> _locks = new();

    private readonly ILogger<MapDataService> _logger;
    private readonly string _persistencePath;

    // Timer de escritura diferida: guarda cambios 2 s después del último cambio
    private Timer? _saveTimer;
    private readonly object _saveTimerLock = new();

    public MapDataService(ILogger<MapDataService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _persistencePath = configuration["MapData:PersistencePath"]
            ?? Path.Combine(AppContext.BaseDirectory, "data", "map_elements.json");

        Directory.CreateDirectory(Path.GetDirectoryName(_persistencePath)!);
    }

    // ── Grupos ────────────────────────────────────────────────────────────────

    public List<MapElementGroup> GetWorkspaceGroups(string workspaceId)
    {
        EnsureWorkspace(workspaceId);
        var lk = GetLock(workspaceId);
        lk.EnterReadLock();
        try
        {
            return _workspaces[workspaceId].Values.Select(CloneGroup).ToList();
        }
        finally { lk.ExitReadLock(); }
    }

    public MapElementGroup? GetGroup(string workspaceId, string groupId)
    {
        EnsureWorkspace(workspaceId);
        var lk = GetLock(workspaceId);
        lk.EnterReadLock();
        try
        {
            return _workspaces[workspaceId].TryGetValue(groupId, out var g) ? CloneGroup(g) : null;
        }
        finally { lk.ExitReadLock(); }
    }

    public MapElementGroup GetOrCreateDefaultGroup(string workspaceId)
    {
        EnsureWorkspace(workspaceId);
        var lk = GetLock(workspaceId);
        lk.EnterUpgradeableReadLock();
        try
        {
            var groups = _workspaces[workspaceId];
            var shared = groups.Values.FirstOrDefault(g => g.Type == 2);
            if (shared != null) return CloneGroup(shared);

            lk.EnterWriteLock();
            try
            {
                // Double-check después de escalar el lock
                shared = groups.Values.FirstOrDefault(g => g.Type == 2);
                if (shared != null) return CloneGroup(shared);

                shared = CreateDefaultGroupInternal(workspaceId);
                groups[shared.Id] = shared;
                ScheduleSave();
                return CloneGroup(shared);
            }
            finally { lk.ExitWriteLock(); }
        }
        finally { lk.ExitUpgradeableReadLock(); }
    }

    public MapElementGroup CreateGroup(string workspaceId, GroupCreateInput input)
    {
        EnsureWorkspace(workspaceId);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var group = new MapElementGroup
        {
            Id         = string.IsNullOrEmpty(input.Id) ? Guid.NewGuid().ToString() : input.Id,
            Name       = input.Name,
            Type       = input.Type,
            IsLock     = input.IsLock,
            CreateTime = now,
            UpdateTime = now,
            Elements   = new List<MapElement>()
        };

        var lk = GetLock(workspaceId);
        lk.EnterWriteLock();
        try
        {
            _workspaces[workspaceId][group.Id] = group;
            ScheduleSave();
            return CloneGroup(group);
        }
        finally { lk.ExitWriteLock(); }
    }

    public MapElementGroup? UpdateGroup(string workspaceId, string groupId, GroupUpdateInput input)
    {
        var lk = GetLock(workspaceId);
        lk.EnterWriteLock();
        try
        {
            if (!_workspaces.TryGetValue(workspaceId, out var groups)) return null;
            if (!groups.TryGetValue(groupId, out var group)) return null;

            if (input.Name  != null) group.Name   = input.Name;
            if (input.IsLock.HasValue) group.IsLock = input.IsLock.Value;
            group.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            ScheduleSave();
            return CloneGroup(group);
        }
        finally { lk.ExitWriteLock(); }
    }

    public bool DeleteGroup(string workspaceId, string groupId)
    {
        var lk = GetLock(workspaceId);
        lk.EnterWriteLock();
        try
        {
            if (!_workspaces.TryGetValue(workspaceId, out var groups)) return false;
            var removed = groups.Remove(groupId);
            if (removed) ScheduleSave();
            return removed;
        }
        finally { lk.ExitWriteLock(); }
    }

    // ── Elementos ─────────────────────────────────────────────────────────────

    public List<MapElement> GetGroupElements(string workspaceId, string groupId)
    {
        var lk = GetLock(workspaceId);
        lk.EnterReadLock();
        try
        {
            if (!_workspaces.TryGetValue(workspaceId, out var groups)) return new();
            if (!groups.TryGetValue(groupId, out var group)) return new();
            return group.Elements.Select(CloneElement).ToList();
        }
        finally { lk.ExitReadLock(); }
    }

    public MapElement? GetElement(string workspaceId, string elementId)
    {
        var lk = GetLock(workspaceId);
        lk.EnterReadLock();
        try
        {
            if (!_workspaces.TryGetValue(workspaceId, out var groups)) return null;
            foreach (var g in groups.Values)
            {
                var el = g.Elements.FirstOrDefault(e => e.Id == elementId);
                if (el != null) return CloneElement(el);
            }
            return null;
        }
        finally { lk.ExitReadLock(); }
    }

    public (MapElement element, string groupId) AddElement(string workspaceId, string groupId, ElementCreateInput input)
    {
        EnsureWorkspace(workspaceId);

        var lk = GetLock(workspaceId);
        lk.EnterWriteLock();
        try
        {
            var groups = _workspaces[workspaceId];

            // Crear grupo si no existe (Pilot 2 puede crear grupos arbitrarios)
            if (!groups.TryGetValue(groupId, out var group))
            {
                var now2 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                group = new MapElementGroup
                {
                    Id         = groupId,
                    Type       = 2,
                    Name       = "Capa Compartida",
                    IsLock     = false,
                    CreateTime = now2,
                    UpdateTime = now2
                };
                groups[groupId] = group;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var element = new MapElement
            {
                Id         = input.Id,
                Name       = input.Name,
                CreateTime = now,
                UpdateTime = now,
                Resource   = NormalizeResource(input.Resource)
            };

            // Idempotencia: reemplazar si ya existe con el mismo ID
            group.Elements.RemoveAll(e => e.Id == element.Id);
            group.Elements.Add(element);

            ScheduleSave();
            return (CloneElement(element), groupId);
        }
        finally { lk.ExitWriteLock(); }
    }

    public MapElement? UpdateElement(string workspaceId, string elementId, ElementUpdateInput input)
    {
        var lk = GetLock(workspaceId);
        lk.EnterWriteLock();
        try
        {
            if (!_workspaces.TryGetValue(workspaceId, out var groups)) return null;
            foreach (var group in groups.Values)
            {
                var el = group.Elements.FirstOrDefault(e => e.Id == elementId);
                if (el == null) continue;

                if (input.Name != null)
                    el.Name = input.Name;

                if (input.Content != null)
                    ApplyContentPatch(el.Resource.Content, input.Content);

                el.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // Mantener coherencia entre resource.type (int) y geometry.type (string)
                el.Resource.Type = GeometryTypeToInt(el.Resource.Content.Geometry.Type);

                ScheduleSave();
                return CloneElement(el);
            }
            return null;
        }
        finally { lk.ExitWriteLock(); }
    }

    public bool DeleteElement(string workspaceId, string elementId, out string? outGroupId)
    {
        outGroupId = null;
        var lk = GetLock(workspaceId);
        lk.EnterWriteLock();
        try
        {
            if (!_workspaces.TryGetValue(workspaceId, out var groups)) return false;
            foreach (var group in groups.Values)
            {
                var removed = group.Elements.RemoveAll(e => e.Id == elementId);
                if (removed > 0)
                {
                    outGroupId = group.Id;
                    ScheduleSave();
                    return true;
                }
            }
            return false;
        }
        finally { lk.ExitWriteLock(); }
    }

    public int DeleteElements(string workspaceId, IEnumerable<string> elementIds,
        out List<(string id, string groupId)> deleted)
    {
        deleted = new();
        var ids = elementIds.ToHashSet();
        if (ids.Count == 0) return 0;

        var lk = GetLock(workspaceId);
        lk.EnterWriteLock();
        try
        {
            if (!_workspaces.TryGetValue(workspaceId, out var groups)) return 0;
            foreach (var group in groups.Values)
            {
                var toRemove = group.Elements.Where(e => ids.Contains(e.Id)).ToList();
                foreach (var el in toRemove)
                {
                    group.Elements.Remove(el);
                    deleted.Add((el.Id, group.Id));
                }
            }
            if (deleted.Count > 0) ScheduleSave();
            return deleted.Count;
        }
        finally { lk.ExitWriteLock(); }
    }

    // ── Persistencia ──────────────────────────────────────────────────────────

    /// <summary>
    /// Guarda el estado completo en fichero JSON.
    /// Escritura atómica: escribe en un fichero .tmp y luego hace File.Move.
    /// </summary>
    public async Task SaveAsync()
    {
        // Snapshot rápido bajo lock de lectura
        var snapshot = new Dictionary<string, List<MapElementGroup>>();
        foreach (var (wId, _) in _workspaces)
        {
            var lk = GetLock(wId);
            lk.EnterReadLock();
            try { snapshot[wId] = _workspaces[wId].Values.ToList(); }
            finally { lk.ExitReadLock(); }
        }

        var json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
        var tmpPath = _persistencePath + ".tmp";
        await File.WriteAllTextAsync(tmpPath, json);
        File.Move(tmpPath, _persistencePath, overwrite: true);

        _logger.LogDebug("[MapData] 💾 Estado guardado en {Path}", _persistencePath);
    }

    /// <summary>Carga el estado desde el fichero JSON al arrancar el servicio.</summary>
    public async Task LoadAsync()
    {
        if (!File.Exists(_persistencePath))
        {
            _logger.LogInformation("[MapData] No existe fichero de persistencia, comenzando en blanco.");
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_persistencePath);
            var data = JsonConvert.DeserializeObject<Dictionary<string, List<MapElementGroup>>>(json);
            if (data == null) return;

            foreach (var (wId, groups) in data)
            {
                var dict = new Dictionary<string, MapElementGroup>();
                foreach (var g in groups) dict[g.Id] = g;
                _workspaces[wId] = dict;
                MigrateNullGroup(wId, dict);
            }
            _logger.LogInformation("[MapData] ✅ Estado cargado: {N} workspaces desde {Path}",
                data.Count, _persistencePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MapData] ❌ Error al cargar estado. Se continuará en blanco.");
        }
    }

    // ── Helpers internos ──────────────────────────────────────────────────────

    private void MigrateNullGroup(string workspaceId, Dictionary<string, MapElementGroup> dict)
    {
        const string nullGroupId = "00000000-0000-0000-0000-000000000000";
        var oldDefaultId = $"default-{workspaceId}";
        bool changed = false;

        // Intentar obtener o preparar el nuevo grupo por defecto (que usa el workspaceId como ID)
        if (!dict.TryGetValue(workspaceId, out var defaultGroup))
        {
            defaultGroup = CreateDefaultGroupInternal(workspaceId);
        }

        // 1. Migrar elementos del grupo legacy "default-{workspaceId}" si existe en el dict
        if (dict.TryGetValue(oldDefaultId, out var oldDefaultGroup))
        {
            if (!dict.ContainsKey(workspaceId))
            {
                dict[workspaceId] = defaultGroup;
            }
            defaultGroup.Elements.AddRange(oldDefaultGroup.Elements);
            dict.Remove(oldDefaultId);
            changed = true;
            _logger.LogInformation("[MapData] Migrados {N} elementos de grupo legacy {OldDefaultId} -> {WorkspaceId}",
                oldDefaultGroup.Elements.Count, oldDefaultId, workspaceId);
        }

        // 2. Migrar elementos del grupo nulo
        if (dict.TryGetValue(nullGroupId, out var nullGroup) && nullGroup.Elements.Count > 0)
        {
            if (!dict.ContainsKey(workspaceId))
            {
                dict[workspaceId] = defaultGroup;
            }
            defaultGroup.Elements.AddRange(nullGroup.Elements);
            dict.Remove(nullGroupId);
            changed = true;
            _logger.LogInformation("[MapData] Migrados {N} elementos de grupo nulo -> {WorkspaceId}",
                nullGroup.Elements.Count, workspaceId);
        }
        else
        {
            dict.Remove(nullGroupId);
        }

        if (changed)
        {
            ScheduleSave();
        }
    }

    private void EnsureWorkspace(string workspaceId)
    {
        _workspaces.GetOrAdd(workspaceId, _ =>
        {
            var dict  = new Dictionary<string, MapElementGroup>();
            var group = CreateDefaultGroupInternal(workspaceId);
            dict[group.Id] = group;
            return dict;
        });
    }

    private ReaderWriterLockSlim GetLock(string workspaceId)
        => _locks.GetOrAdd(workspaceId, _ => new ReaderWriterLockSlim());

    private static MapElementGroup CreateDefaultGroupInternal(string workspaceId)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return new MapElementGroup
        {
            // ID determinista basado en workspace para que el RC lo reconozca tras reinicio (debe ser un UUID válido)
            Id         = workspaceId,
            Type       = 2, // AppShared
            Name       = "Capa de la Aplicación",
            IsLock     = false,
            CreateTime = now,
            UpdateTime = now
        };
    }

    /// <summary>
    /// Asegura la coherencia entre resource.type (int) y geometry.type (string).
    /// DJI Pilot 2 rechazará el elemento si no coinciden.
    /// </summary>
    /// <summary>
    /// Asegura la coherencia entre resource.type (int) y geometry.type (string).
    /// DJI Pilot 2 rechazará el elemento si no coinciden.
    /// </summary>
    private static MapResource NormalizeResource(MapResource src)
    {
        var geoType = src.Content?.Geometry?.Type ?? "Point";
        var rawColor = src.Content?.Properties?.Color ?? "0x2D8CF0";
        return new MapResource
        {
            Type     = GeometryTypeToInt(geoType),
            UserName = src.UserName ?? "Pilot",
            Content  = new MapContent
            {
                Type       = src.Content?.Type ?? "Feature",
                Properties = new MapProperties
                {
                    Color         = NormalizeDjiColor(rawColor),
                    ClampToGround = src.Content?.Properties?.ClampToGround ?? false,
                    Is3d          = src.Content?.Properties?.Is3d,
                    Radius        = src.Content?.Properties?.Radius
                },
                Geometry = new MapGeometry
                {
                    Type        = geoType,
                    Coordinates = src.Content?.Geometry?.Coordinates ?? Array.Empty<double>(),
                    Radius      = src.Content?.Geometry?.Radius
                }
            }
        };
    }

    private static void ApplyContentPatch(MapContent target, MapContent patch)
    {
        if (patch.Properties != null)
        {
            target.Properties.Color         = patch.Properties.Color != null ? NormalizeDjiColor(patch.Properties.Color) : target.Properties.Color;
            target.Properties.ClampToGround = patch.Properties.ClampToGround;
            if (patch.Properties.Is3d.HasValue)
                target.Properties.Is3d = patch.Properties.Is3d;
            if (patch.Properties.Radius.HasValue)
                target.Properties.Radius = patch.Properties.Radius;
        }
        if (patch.Geometry != null)
        {
            target.Geometry.Type        = patch.Geometry.Type ?? target.Geometry.Type;
            target.Geometry.Coordinates = patch.Geometry.Coordinates ?? target.Geometry.Coordinates;
            if (patch.Geometry.Radius.HasValue)
                target.Geometry.Radius = patch.Geometry.Radius;
        }
    }

    /// <summary>
    /// Normaliza cualquier representación de color al formato 0x estándar soportado por DJI Pilot 2.
    /// </summary>
    public static string NormalizeDjiColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return "0x2D8CF0"; // default Blue

        var clean = color.Trim().ToUpper();

        // 1. Mapeo directo de constantes
        if (clean == "BLUE" || clean == "0X2D8CF0") return "0x2D8CF0";
        if (clean == "GREEN" || clean == "0X19BE6B") return "0x19BE6B";
        if (clean == "YELLOW" || clean == "0XFFBB00") return "0xFFBB00";
        if (clean == "ORANGE" || clean == "0XB620E0") return "0xB620E0";
        if (clean == "RED" || clean == "0XE23C39") return "0xE23C39";
        if (clean == "PURPLE" || clean == "0X212121") return "0x212121";

        // 2. Mapeo desde formato hexadecimal CSS (#RRGGBB)
        if (clean.StartsWith("#"))
        {
            var hex = clean.Substring(1);
            if (hex == "2D8CF0") return "0x2D8CF0"; // Blue
            if (hex == "19BE6B") return "0x19BE6B"; // Green
            if (hex == "FFBB00") return "0xFFBB00"; // Yellow
            if (hex == "B620E0" || hex == "F97316" || hex == "FF8C00") return "0xB620E0"; // Orange (DJI B620E0)
            if (hex == "E23C39") return "0xE23C39"; // Red
            if (hex == "212121" || hex == "8B5CF6" || hex == "9D4EDD") return "0x212121"; // Purple (DJI 212121)
            return "0x2D8CF0"; // default Blue
        }

        // 3. Formato 0X con otros casos de mayúsculas/minúsculas
        if (clean.StartsWith("0X"))
        {
            var hex = clean.Substring(2);
            if (hex == "2D8CF0") return "0x2D8CF0";
            if (hex == "19BE6B") return "0x19BE6B";
            if (hex == "FFBB00") return "0xFFBB00";
            if (hex == "B620E0") return "0xB620E0";
            if (hex == "E23C39") return "0xE23C39";
            if (hex == "212121") return "0x212121";
        }

        return "0x2D8CF0"; // default Blue
    }

    /// <summary>
    /// Convierte geometry.type string al int que usa resource.type en DJI.
    /// </summary>
    public static int GeometryTypeToInt(string geometryType) => geometryType switch
    {
        "Point"      => 0,
        "LineString" => 1,
        "Polygon"    => 2,
        "Circle"     => 0,  // DJI trata círculos como tipo 0 (point-based con radius)
        _            => 0
    };

    /// <summary>
    /// Escritura diferida: evita flood de I/O cuando llegan muchos elementos seguidos.
    /// Se dispara 2 segundos después del último cambio.
    /// </summary>
    private void ScheduleSave()
    {
        lock (_saveTimerLock)
        {
            _saveTimer?.Dispose();
            _saveTimer = new Timer(async _ =>
            {
                try { await SaveAsync(); }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[MapData] Error en guardado diferido");
                }
            }, null, TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
        }
    }

    // ── Cloner Helpers para Thread-Safety durante la serialización JSON ─────────

    private static MapElementGroup CloneGroup(MapElementGroup src)
    {
        if (src == null) return null!;
        lock (src)
        {
            return new MapElementGroup
            {
                Id         = src.Id,
                Type       = src.Type,
                Name       = src.Name,
                IsLock     = src.IsLock,
                CreateTime = src.CreateTime,
                UpdateTime = src.UpdateTime,
                Elements   = src.Elements != null ? src.Elements.Select(CloneElement).ToList() : new List<MapElement>()
            };
        }
    }

    private static MapElement CloneElement(MapElement src)
    {
        if (src == null) return null!;
        lock (src)
        {
            return new MapElement
            {
                Id         = src.Id,
                Name       = src.Name,
                CreateTime = src.CreateTime,
                UpdateTime = src.UpdateTime,
                Resource   = src.Resource != null ? CloneResource(src.Resource) : new MapResource()
            };
        }
    }

    private static MapResource CloneResource(MapResource src)
    {
        if (src == null) return null!;
        lock (src)
        {
            return new MapResource
            {
                Type     = src.Type,
                UserName = src.UserName,
                Content  = src.Content != null ? CloneContent(src.Content) : new MapContent()
            };
        }
    }

    private static MapContent CloneContent(MapContent src)
    {
        if (src == null) return null!;
        lock (src)
        {
            return new MapContent
            {
                Type       = src.Type,
                Properties = src.Properties != null ? new MapProperties
                {
                    Color         = src.Properties.Color,
                    ClampToGround = src.Properties.ClampToGround,
                    Is3d          = src.Properties.Is3d,
                    Radius        = src.Properties.Radius
                } : new MapProperties(),
                Geometry = src.Geometry != null ? new MapGeometry
                {
                    Type        = src.Geometry.Type,
                    Coordinates = src.Geometry.Coordinates,
                    Radius      = src.Geometry.Radius
                } : new MapGeometry()
            };
        }
    }
}
