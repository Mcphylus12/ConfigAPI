
namespace ConfigAPI;

public class FileStore : IStore
{
    private readonly string baseDir;

    public FileStore(string baseDir)
    {
        this.baseDir = baseDir;
        var directoryInfo = new DirectoryInfo(baseDir);
        directoryInfo.Create();
    }

    public Task<bool> Exists(string configKey)
    {
        return Task.FromResult(File.Exists(Path.Combine(baseDir, (Path.GetFileName(configKey)))));
    }

    public async Task<string?> Get(string configKey)
    {
        try
        {
            return await File.ReadAllTextAsync(Path.Combine(baseDir, (Path.GetFileName(configKey))));
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<IEnumerable<string>> List()
    {
        return Directory.EnumerateFiles(baseDir).Select(f => Path.GetFileName(f));
    }

    public Task Set(string configKey, string v)
    {
        return File.WriteAllTextAsync(Path.Combine(baseDir, (Path.GetFileName(configKey))), v);
    }
}
