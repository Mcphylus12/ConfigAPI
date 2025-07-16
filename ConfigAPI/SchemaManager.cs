using Json.Schema;
using System.IO;
using System.Text.Json.Nodes;
using static SchemaValidationResult;

namespace ConfigAPI;

public interface ISchemaService
{
    Task Set(string configKey, JsonNode updateDoc);
    Task<IEnumerable<JsonSchema>> Get(string configKey);
    Task<IEnumerable<JsonSchema>> Get();
    Task<JsonSchema?> GetFromSchemaKey(string schemaKey);
    Task<bool> Exists(string configKey);
}

public class SchemaService : ISchemaService
{
    private SchemaTree root = new SchemaTree();
    private readonly IStore store;

    public SchemaService([FromKeyedServices("schemaStore")] IStore store)
    {
        this.store = store;

        foreach (var item in store.List().Result)
        {
            Set(item.Replace("_x_", "*"), JsonNode.Parse(store.Get(item).Result!)!).Wait();
        }
    }

    public async Task Set(string configKey, JsonNode updateDoc)
    {
        var schema = new JsonSchema(updateDoc, configKey);
        root.Add(configKey.Split("."), schema);
        await store.Set(configKey.Replace("*", "_x_"), updateDoc.ToJsonString());
    }

    public Task<IEnumerable<JsonSchema>> Get(string configKey)
    {
        List<JsonSchema> results = [];
        root.GetSchemas(configKey.Split("."), results);

        return Task.FromResult<IEnumerable<JsonSchema>>(results);
    }

    public Task<JsonSchema?> GetFromSchemaKey(string schemaKey)
    {
        return Task.FromResult(root.GetSchema(schemaKey.Split(".")));
    }

    public Task<IEnumerable<JsonSchema>> Get()
    {
        List<JsonSchema> results = [];
        root.GetSchemas(results);

        return Task.FromResult<IEnumerable<JsonSchema>>(results);
    }

    public Task<bool> Exists(string configKey)
    {
        return Task.FromResult(root.GetSchema(configKey.Split(".")) != null);
    }
}

public class JsonSchema
{
    private readonly JsonNode node;
    private readonly Json.Schema.JsonSchema schema;
    private readonly string configKey;

    public JsonSchema(JsonNode updateDoc, string configKey)
    {
        node = updateDoc;
        schema = Json.Schema.JsonSchema.FromText(node.ToJsonString());
        this.configKey = configKey;
    }

    public string Key => configKey;

    public JsonNode? Node => node;

    internal async Task<SchemaValidationResult> Validate(JsonNode fullNewConfig)
    {
        var result = schema.Evaluate(fullNewConfig, new EvaluationOptions
        {
            OutputFormat = OutputFormat.List
        });

        return new SchemaValidationResult
        {
            Ok = result.IsValid,
            Errors = new List<ValidationError>
            {
                new ValidationError
                {
                    SchemaName = configKey,
                    Errors = result.Details.Where(d => d.HasErrors).SelectMany(d => d.Errors).ToDictionary()
                }
            }
        };
    }
}

public class SchemaTree
{
    private Dictionary<string, SchemaTree> children { get; set; } = [];
    private SchemaTree? Default { get; set; }
    private JsonSchema? Value { get; set; }

    internal void Add(string[] configKey, JsonSchema updateDoc)
    {
        if (configKey.Length == 0)
        {
            Value = updateDoc;
            return;
        }

        string firstPart = configKey[0];
        string[] rest = configKey[1..];

        if (firstPart == "*")
        {
            Default ??= new SchemaTree();
            Default.Add(rest, updateDoc);
            return;
        }

        if (!children.ContainsKey(firstPart))
        {
            children[firstPart] = new SchemaTree();
        }

        children[firstPart].Add(rest, updateDoc);
    }

    internal JsonSchema? GetSchema(string[] schemaKey)
    {
        if (schemaKey.Length == 0)
        {
            return Value;
        }

        string firstPart = schemaKey[0];
        string[] rest = schemaKey[1..];

        if (Default is not null && firstPart == "*")
        {
            return Default.GetSchema(rest);
        }

        if (children.TryGetValue(firstPart, out var child))
        {
            return child.GetSchema(rest);
        }

        return null;
    }

    internal void GetSchemas(string[] configKey, List<JsonSchema> results)
    {
        if (configKey.Length == 0)
        {
            if (Value is not null)
            {
                results.Add(Value);
            }
            return;
        }

        string firstPart = configKey[0];
        string[] rest = configKey[1..];

        if (children.TryGetValue(firstPart, out var child))
        {
            child.GetSchemas(rest, results);
        }

        if (Default is not null)
        {
            Default.GetSchemas(rest, results);
        }
    }

    internal void GetSchemas(List<JsonSchema> results)
    {
        if (Value is not null)
        {
            results.Add(Value);
        }

        foreach (var child in children)
        {
            child.Value.GetSchemas(results);
        }

        if (Default is not null)
        {
            Default.GetSchemas(results);
        }
    }
}