using ConfigAPI;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Text.Json.Nodes;
using static System.Net.Mime.MediaTypeNames;

var site = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Index.html"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IConfigCache, MemoryConfigCache>();
builder.Services.AddSingleton<IConfigService, ConfigService>();
builder.Services.AddSingleton<ISchemaService, SchemaService>();
builder.Services.AddSingleton<IConfigStore, MemoryStore>();

var app = builder.Build();

app.MapOpenApi();

app.UseCors();

app.MapGet("/", () => Results.Content(site, MediaTypeNames.Text.Html));

app.MapPost("/api/schema/{configKey}", async ([FromRoute] string configKey, [FromBody] JsonNode updateDoc, [FromServices] ISchemaService schemaService) =>
{
    await schemaService.Set(configKey, updateDoc);
    return Results.Ok();
});

app.MapGet("/api/schema", async ([FromServices] ISchemaService schemaService) =>
{
    var schemas = await schemaService.Get();
    return Results.Ok(schemas.Select(schema => schema.Key));
});

app.MapGet("/api/schema/{configKey}", async ([FromRoute] string configKey, [FromServices] ISchemaService schemaService) =>
{
    var schema = await schemaService.GetFromSchemaKey(configKey);

    if (schema is null) return Results.NotFound();

    return Results.Ok(schema.Node);
});

app.MapGet("/api/configs", async ([FromServices] IConfigService configService) =>
{
    return Results.Ok(await configService.List());
});

app.MapGet("/api/{configKey}", async ([FromRoute] string configKey, [FromServices] IConfigService configService, [FromQuery] bool shallow = false) =>
{
    return Results.Ok(await configService.Get(configKey, shallow));
});

app.MapPost("/api/{configKey}", async ([FromRoute] string configKey, [FromBody]JsonNode updateDoc, [FromServices] IConfigService configService) =>
{
    await configService.Set(configKey, updateDoc);
    return Results.Ok();
});


app.Run();

public interface IConfigService
{
    Task<JsonNode> Get(string configKey, bool shallow);
    Task<IEnumerable<string>> List();
    Task Set(string configKey, JsonNode jsonDocument);
}

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

internal class SchemaValidationResult
{
    public bool Ok { get; internal set; }

    public void Add(SchemaValidationResult newData)
    {
        Ok = Ok && newData.Ok;
    }

    internal Exception ToException()
    {
        throw new Exception();
    }
}

public interface IConfigCache
{
    Task Clear(string configPrefix);
    Task Set(string configKey, JsonObject freshDocument);
    Task<bool> TryGet(string configKey, out JsonNode? cachedConfig);
}

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

public interface IConfigStore
{
    Task<string?> Get(string configKey);
    Task<IEnumerable<string>> List();
    Task Set(string configKey, string v);
}

public class MemoryStore : IConfigStore
{
    private Dictionary<string, string> data = [];

    public Task<string?> Get(string configKey)
    {
        return Task.FromResult(data.TryGetValue(configKey, out var c) ? c : null);
    }

    public Task<IEnumerable<string>> List() => Task.FromResult<IEnumerable<string>>(data.Keys);

    public Task Set(string configKey, string newData)
    {
        data[configKey] = newData;
        return Task.CompletedTask;
    }
}

public interface IUpdateNotifier
{
    Task Notify(string key);
}
