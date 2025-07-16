
using ConfigAPI;
using System.Text.Json.Nodes;

internal class StartupConfigLoader : BackgroundService
{
    private readonly ISchemaService schemaService;
    private readonly IConfigService configService;
    private readonly IStore configStore;
    private readonly IStore schemaStore;

    public StartupConfigLoader(ISchemaService schemaService, IConfigService configService, 
        [FromKeyedServices("configStore")]IStore configStore,
        [FromKeyedServices("schemaStore")]IStore schemaStore
        )
    {
        this.schemaService = schemaService;
        this.configService = configService;
        this.configStore = configStore;
        this.schemaStore = schemaStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var schemaDir = new DirectoryInfo("./Boot/Schemas");

        foreach (var item in schemaDir.EnumerateFiles())
        {
            var configKey = Path.GetFileNameWithoutExtension(item.Name).Replace("_x_", "*");
            if (!await schemaService.Exists(configKey))
            {
                await schemaService.Set(configKey, JsonNode.Parse(await File.ReadAllTextAsync(item.FullName))!);
            }
        }

        var configDir = new DirectoryInfo("./Boot/Configs");

        foreach (var item in configDir.EnumerateFiles().OrderBy(item => item.Name.Length))
        {
            var configKey = Path.GetFileNameWithoutExtension(item.Name);

            if (!await configStore.Exists(configKey))
            {
                await configService.Set(configKey, JsonNode.Parse(await File.ReadAllTextAsync(item.FullName))!);
            }
        }
    }
}