using System.Text.Json.Nodes;

public interface IConfigService
{
    Task<JsonNode> Get(string configKey, bool shallow);
    Task<IEnumerable<string>> List();
    Task Set(string configKey, JsonNode jsonDocument);
}
