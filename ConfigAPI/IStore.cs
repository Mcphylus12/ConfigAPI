public interface IStore
{
    Task<bool> Exists(string configKey);
    Task<string?> Get(string configKey);
    Task<IEnumerable<string>> List();
    Task Set(string configKey, string v);
}
