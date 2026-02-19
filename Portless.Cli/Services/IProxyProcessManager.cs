namespace Portless.Cli.Services;

public interface IProxyProcessManager
{
    Task StartAsync(int port);
    Task StopAsync();
    Task<bool> IsRunningAsync();
    Task<(bool isRunning, int? port, int? pid)> GetStatusAsync();
}
