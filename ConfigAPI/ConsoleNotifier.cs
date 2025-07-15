
namespace ConfigAPI;

public class ConsoleNotifier : IUpdateNotifier
{
    private readonly ILogger<ConsoleNotifier> logger;

    public ConsoleNotifier(ILogger<ConsoleNotifier> logger)
    {
        this.logger = logger;
    }

    public Task Notify(string key)
    {
        logger.LogInformation("Config changed: {key}", key);
        return Task.CompletedTask;
    }
}
