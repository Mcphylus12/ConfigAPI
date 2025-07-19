

namespace ConfigAPI;

public interface IStore
{
    Task Delete(string key);
    IStore ForType(string type);
    Task<IEnumerable<string>> List(string? prefix);
    Task Set(string key, byte[] bytes);
    Task<byte[]?> Get(string key);
    Task<bool> Exists(string key);
}

public static class StoreExtensions
{
    public static async Task TransferTo(this IStore source, IStore target, Func<string, string>? translator = null)
    {
        foreach (var item in await source.List(null))
        {
            var targetItem = translator is not null ? translator(item) : item;
            if (!await target.Exists(targetItem))
            {
                var data = await source.Get(item);
                await target.Set(targetItem, data);
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
        Directory.CreateDirectory(basePath);
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

    public Task<byte[]?> Get(string key)
    {
        try
        {
            var path = Path.Combine(basePath, key);
            return Task.FromResult<byte[]?>(File.ReadAllBytes(path));
        }
        catch (Exception)
        {
            return Task.FromResult<byte[]?>(null);
        }
    }

    public Task<IEnumerable<string>> List(string? prefix)
    {
        return Task.FromResult(Directory.EnumerateFiles(basePath, prefix + "*").Select(s => Path.GetFileName(s)));
    }

    public Task Set(string key, byte[] bytes)
    {
        var path = Path.Combine(basePath, key);
        File.WriteAllBytes(path, bytes);
        return Task.CompletedTask;
    }
}
