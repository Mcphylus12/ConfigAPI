using System.Text;
using System.Text.Json.Nodes;

namespace ConfigAPI;

public class JsonConfig
{
    private readonly JsonNode node;

    public JsonConfig(JsonNode node)
    {
        this.node = node;
    }

    internal string ToJsonString() => node.ToJsonString();

    internal void DeepMerge(JsonNode newDocument)
    {
        DeepMerge(node, newDocument);
    }

    static void DeepMerge(JsonNode existingConfig, JsonNode newConfig)
    {
        if (existingConfig is JsonObject existingObject && newConfig is JsonObject newObject)
        {
            foreach (var newObjectProperty in newObject)
            {
                if (existingObject[newObjectProperty.Key] is JsonObject exsitingChildObject && newObjectProperty.Value is JsonObject newChildObject)
                {
                    DeepMerge(exsitingChildObject, newChildObject);
                }
                else
                {
                    existingObject[newObjectProperty.Key] = newObjectProperty.Value?.DeepClone();
                }
            }
        }
    }
}

public class ConfigService
{
    private readonly IStore store;

    public ConfigService(IStore store)
    {
        this.store = store.ForType("config");
    }

    public Task Delete(string configKey) => store.Delete(configKey);

    public async Task<JsonConfig?> Get(string configKey, bool shallow)
    {
        if (shallow)
        {
            var text = await store.Get(configKey);

            var newDocument = text is null ? null : JsonNode.Parse(text);

            return new JsonConfig(newDocument ?? new JsonObject());
        }

        var parts = configKey.Split(".");

        var freshDocument = new JsonConfig(new JsonObject());

        for (int i = 0; i < parts.Length; i++)
        {
            var key = string.Join(".", parts[0..(i + 1)]);

            var text = await store.Get(key);

            var newDocument = text is null ? null : JsonNode.Parse(text);

            if (newDocument is not null)
            {
                freshDocument.DeepMerge(newDocument);
            }
        }
        return freshDocument;
    }

    public Task<IEnumerable<string>> List(string? prefix) => store.List(prefix);

    public Task Set(string configKey, JsonConfig jsonConfig) => store.Set(configKey, Encoding.UTF8.GetBytes(jsonConfig.ToJsonString()));
}
