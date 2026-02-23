namespace Portless.Cli.Services;

public interface IProxyProcessManager
{
    Task StartAsync(int port, bool enableHttps = false);
    Task StopAsync();
    Task<bool> IsRunningAsync();
    Task<(bool isRunning, int? port, int? pid)> GetStatusAsync();
    Task<int[]> GetActiveManagedProcessesAsync();
    Task KillManagedProcessesAsync(int[] pids);
    Task RegisterManagedProcessAsync(int pid);
}
