using System.Diagnostics;
using Portless.Core.Services;

namespace Portless.Cli.Services;

public class ProxyProcessManager : IProxyProcessManager
{
    private const string DefaultPort = "1355";
    private readonly string _stateDirectory;
    private readonly string _pidFilePath;

    public ProxyProcessManager()
    {
        _stateDirectory = StateDirectoryProvider.GetStateDirectory();
        _pidFilePath = Path.Combine(_stateDirectory, "proxy.pid");

        // Ensure state directory exists
        Directory.CreateDirectory(_stateDirectory);
    }

    public async Task StartAsync(int port)
    {
        // Check if already running
        if (await IsRunningAsync())
        {
            throw new InvalidOperationException("Proxy is already running. Use 'portless proxy stop' first");
        }

        // Build path to Portless.Proxy.csproj
        var assemblyLocation = typeof(ProxyProcessManager).Assembly.Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation) ?? ".";
        var solutionRoot = Path.GetFullPath(Path.Combine(assemblyDirectory, "../../.."));
        var proxyProjectPath = Path.Combine(solutionRoot, "Portless.Proxy", "Portless.Proxy.csproj");

        if (!File.Exists(proxyProjectPath))
        {
            throw new InvalidOperationException($"Proxy project not found at: {proxyProjectPath}");
        }

        // Create process start info for detached execution
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{proxyProjectPath}\" --urls http://*:{port}",
            UseShellExecute = true,  // Required for detached execution on Windows
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        // Set environment variables
        startInfo.Environment["PORTLESS_PORT"] = port.ToString();
        startInfo.Environment["DOTNET_MODIFIABLE_ASSEMBLIES"] = "debug"; // Hot reload support

        // Start the process
        var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start proxy process");
        }

        // Write PID to file immediately after successful start
        await File.WriteAllTextAsync(_pidFilePath, process.Id.ToString());

        // Allow startup verification
        await Task.Delay(500);

        // Return without waiting for process to complete (detached execution)
    }

    public async Task StopAsync()
    {
        // Check if PID file exists
        if (!File.Exists(_pidFilePath))
        {
            throw new InvalidOperationException("Proxy is not running");
        }

        // Read PID from file
        var pidContent = await File.ReadAllTextAsync(_pidFilePath);
        if (!int.TryParse(pidContent.Trim(), out var pid))
        {
            // Invalid PID file, delete it
            File.Delete(_pidFilePath);
            throw new InvalidOperationException("Invalid PID file");
        }

        try
        {
            // Get the process
            var process = Process.GetProcessById(pid);

            // Kill the process with entire process tree for clean shutdown
            process.Kill(entireProcessTree: true);

            // Delete PID file
            File.Delete(_pidFilePath);
        }
        catch (ArgumentException)
        {
            // PID doesn't exist (process already terminated)
            // Delete stale PID file and return
            File.Delete(_pidFilePath);
        }
    }

    public async Task<bool> IsRunningAsync()
    {
        // Check if PID file exists
        if (!File.Exists(_pidFilePath))
        {
            return false;
        }

        // Parse PID from file
        var pidContent = await File.ReadAllTextAsync(_pidFilePath);
        if (!int.TryParse(pidContent.Trim(), out var pid))
        {
            return false;
        }

        try
        {
            // Check if process is still running
            var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            // Process doesn't exist
            return false;
        }
    }

    public async Task<(bool isRunning, int? port, int? pid)> GetStatusAsync()
    {
        if (!await IsRunningAsync())
        {
            return (false, null, null);
        }

        // Parse PID from file
        var pidContent = await File.ReadAllTextAsync(_pidFilePath);
        if (!int.TryParse(pidContent.Trim(), out var pid))
        {
            return (false, null, null);
        }

        // For now, return default port
        // Port tracking can be enhanced later by storing port in a separate file
        return (true, int.Parse(DefaultPort), pid);
    }
}
