using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;

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
builder.Services.AddSingleton<IConfigStore, MemoryStore>();

var app = builder.Build();

app.MapOpenApi();

app.UseCors();

app.MapGet("/api/{configKey}", async ([FromRoute] string configKey, [FromServices] IConfigService configService) =>
{
    return Results.Ok(await configService.Get(configKey));
});

app.MapPost("/api/{configKey}", async ([FromRoute] string configKey, [FromBody]JsonNode updateDoc, [FromServices] IConfigService configService) =>
{
    await configService.Set(configKey, updateDoc);
    return Results.Ok();
});

app.Run();

public interface IConfigService
{
    Task<JsonNode> Get(string configKey);

    Task Set(string configKey, JsonNode jsonDocument);
}

public class ConfigService : IConfigService
{
    private readonly IConfigCache cache;
    private readonly IConfigStore store;
    private readonly IEnumerable<IUpdateNotifier> notifiers;

    public ConfigService(IConfigCache cache, IConfigStore store, IEnumerable<IUpdateNotifier> notifiers)
    {
        this.cache = cache;
        this.store = store;
        this.notifiers = notifiers;
    }

    public async Task<JsonNode> Get(string configKey)
    {
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
        await store.Set(configKey, jsonDocument.ToJsonString());
        await cache.Clear(configKey);
        await Task.WhenAll(notifiers.Select(n => n.Notify(configKey)));
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
    Task Set(string configKey, string v);
}

public class MemoryStore : IConfigStore
{
    private Dictionary<string, string> data = [];

    public Task<string?> Get(string configKey)
    {
        return Task.FromResult(data.TryGetValue(configKey, out var c) ? c : null);
    }

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
