using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DjiCloudServer.Models;

public class MapElement
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("group_id")]
    public string GroupId { get; set; } = "";

    [JsonProperty("resource")]
    public JObject? Resource { get; set; }

    [JsonProperty("create_time")]
    public long CreateTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    [JsonProperty("update_time")]
    public long UpdateTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

public class ElementGroup
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("name")]
    public string Name { get; set; } = "Default";

    [JsonProperty("type")]
    public int Type { get; set; } = 1;

    [JsonProperty("is_lock")]
    public bool IsLock { get; set; } = false;

    [JsonProperty("create_time")]
    public long CreateTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

public class MapStore
{
    [JsonProperty("groups")]
    public List<ElementGroup> Groups { get; set; } = new();

    [JsonProperty("elements")]
    public List<MapElement> Elements { get; set; } = new();
}
