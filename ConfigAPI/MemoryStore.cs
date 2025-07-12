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
