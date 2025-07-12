using System.Text.Json.Nodes;

public interface IConfigCache
{
    Task Clear(string configPrefix);
    Task Set(string configKey, JsonObject freshDocument);
    Task<bool> TryGet(string configKey, out JsonNode? cachedConfig);
}
