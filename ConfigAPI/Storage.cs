

namespace ConfigAPI;

public interface IStore
{
    Task Delete(string key);
    IStore ForType(string type);
    Task<IEnumerable<string>> List(string? prefix);
    Task Set(string key, byte[] bytes);
    Task<byte[]> Get(string key);
    Task<bool> Exists(string key);
}

public static class StoreExtensions
{
    public static async Task TransferTo(this IStore source, IStore target)
    {
        foreach (var item in await source.List(null))
        {
            if (!await target.Exists(item))
            {
                var data = await source.Get(item);
                await target.Set(item, data);
            }
        }
    }
}

public class FileStore : IStore
{
    private readonly string basePath;

    public FileStore(string basePath)
    {
        this.basePath = basePath;
    }

    public Task Delete(string key)
    {
        var path = Path.Combine(basePath, key);
        File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<bool> Exists(string key)
    {
        var path = Path.Combine(basePath, key);
        return Task.FromResult(File.Exists(path));
    }

    public IStore ForType(string type)
    {
        return new FileStore(Path.Combine(basePath, type));
    }

    public Task<byte[]> Get(string key)
    {
        var path = Path.Combine(basePath, key);
        return Task.FromResult(File.ReadAllBytes(path));
    }

    public Task<IEnumerable<string>> List(string? prefix)
    {
        return Task.FromResult(Directory.EnumerateFiles(basePath, prefix + "*"));
    }

    public Task Set(string key, byte[] bytes)
    {
        var path = Path.Combine(basePath, key);
        File.WriteAllBytes(path, bytes);
        return Task.CompletedTask;
    }
}
