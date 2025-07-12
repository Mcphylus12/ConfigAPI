using ConfigAPI;
using System.Text.Json.Nodes;

public class ConfigService : IConfigService
{
    private readonly IConfigCache cache;
    private readonly IConfigStore store;
    private readonly IEnumerable<IUpdateNotifier> notifiers;
    private readonly ISchemaService schemaService;

    public ConfigService(IConfigCache cache, IConfigStore store, IEnumerable<IUpdateNotifier> notifiers, ISchemaService schemaService)
    {
        this.cache = cache;
        this.store = store;
        this.notifiers = notifiers;
        this.schemaService = schemaService;
    }

    public async Task<JsonNode> Get(string configKey, bool shallow)
    {
        if (shallow)
        {
            var text = await store.Get(configKey);

            var newDocument = text is null ? null : JsonNode.Parse(text);

            return newDocument ?? new JsonObject();
        }

        if (await cache.TryGet(configKey, out var cachedItem))
        {
            return cachedItem!;
        }

        var parts = configKey.Split(".");

        var freshDocument = new JsonObject();

        for (int i = 0; i < parts.Length; i++)
        {
            var key = string.Join(".", parts[0..(i+1)]);

            var text = await store.Get(key);

            var newDocument = text is null ? null : JsonNode.Parse(text);

            if (newDocument is not null)
            {
                DeepMerge(freshDocument, newDocument);
            }
        }

        await cache.Set(configKey, freshDocument);
        return freshDocument;
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

    public async Task Set(string configKey, JsonNode jsonDocument)
    {
        var schemas = await schemaService.Get(configKey);

        if (schemas.Any())
        {
            var oldConfig = await Get(configKey, shallow: true);
            await cache.Clear(configKey);
            await store.Set(configKey, jsonDocument.ToJsonString());
            var fullNewConfig = await Get(configKey, false);

            SchemaValidationResult result = new SchemaValidationResult()
            {
                Ok = true
            };
            foreach (var schema in schemas)
            {
                result.Add(await schema.Validate(fullNewConfig));
            }

            if (!result.Ok)
            {
                await cache.Clear(configKey);
                await store.Set(configKey, oldConfig.ToJsonString());
                throw result.ToException();
            }
        }
        else
        {
            await cache.Clear(configKey);
            await store.Set(configKey, jsonDocument.ToJsonString());
        }

        await Task.WhenAll(notifiers.Select(n => n.Notify(configKey)));
    }

    public Task<IEnumerable<string>> List() => store.List();
}
