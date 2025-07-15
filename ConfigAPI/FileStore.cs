
namespace ConfigAPI;

public class FileStore : IConfigStore
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

    public Task<IEnumerable<string>> List()
    {
        return Task.FromResult(Directory.EnumerateFiles(baseDir));
    }

    public Task Set(string configKey, string v)
    {
        return File.WriteAllTextAsync(Path.Combine(baseDir, (Path.GetFileName(configKey))), v);
    }
}
