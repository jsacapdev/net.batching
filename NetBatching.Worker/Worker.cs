namespace NetBatching.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    private readonly IMessageReceiver _receiver;

    public Worker(ILogger<Worker> logger,
                  IMessageReceiver receiver)
    {
        _logger = logger;

        _receiver = receiver;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _receiver.Initialize();

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}
