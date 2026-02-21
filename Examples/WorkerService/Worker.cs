namespace WorkerService;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var port = Environment.GetEnvironmentVariable("PORT");
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                if (port != null)
                {
                    logger.LogInformation("Worker running at: http://localhost:{port} (assigned by Portless)", port);
                }
                else
                {
                    logger.LogInformation("Worker running at: {time} (no PORT assigned)", DateTimeOffset.Now);
                }
            }
            await Task.Delay(5000, stoppingToken);
        }
    }
}
