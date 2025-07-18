
using ConfigAPI;
using System.Text.Json.Nodes;

internal class BootLoader : BackgroundService
{
    private readonly IStore bootConfigStore;
    private readonly IStore configStore;

    public BootLoader(IStore store)
    {
        this.bootConfigStore = store.ForType("boot").ForType("config");
        this.configStore = store.ForType("config");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await bootConfigStore.TransferTo(configStore);
    }
}