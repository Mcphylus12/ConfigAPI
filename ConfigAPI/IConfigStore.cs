public interface IConfigStore
{
    Task<string?> Get(string configKey);
    Task<IEnumerable<string>> List();
    Task Set(string configKey, string v);
}
