using System.Text.Json.Nodes;

public class MemoryConfigCache : IConfigCache
{
    private Dictionary<string, JsonNode> cache = [];

    public Task Clear(string configPrefix)
    {
        var keysToRemove = cache.Keys
            .Where(k => k.StartsWith(configPrefix))
            .ToList();

        foreach (var key in keysToRemove)
        {
            cache.Remove(key);
        }

        return Task.CompletedTask;
    }

    public Task Set(string configKey, JsonObject freshDocument)
    {
        cache[configKey] = freshDocument;
        return Task.CompletedTask;
    }

    public Task<bool> TryGet(string configKey, out JsonNode? cachedConfig)
    {
        return Task.FromResult(cache.TryGetValue(configKey, out cachedConfig));
    }
}
