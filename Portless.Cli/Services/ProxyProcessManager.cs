using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Portless.Core.Services;

namespace Portless.Cli.Services;

public class ProxyProcessManager : IProxyProcessManager
{
    private const string DefaultPort = "1355";
    private const string HttpsPort = "1356";
    private readonly string _stateDirectory;
    private readonly string _pidFilePath;
    private readonly string _managedPidsFilePath;
    private readonly HashSet<int> _managedPids = new HashSet<int>();

    public ProxyProcessManager()
    {
        _stateDirectory = StateDirectoryProvider.GetStateDirectory();
        _pidFilePath = Path.Combine(_stateDirectory, "proxy.pid");
        _managedPidsFilePath = Path.Combine(_stateDirectory, "managed-pids.json");

        // Ensure state directory exists
        Directory.CreateDirectory(_stateDirectory);
    }

    public async Task StartAsync(int port, bool enableHttps = false)
    {
        // Check if already running
        if (await IsRunningAsync())
        {
            throw new InvalidOperationException("Proxy is already running. Use 'portless proxy stop' first");
        }

        // Log deprecation warning if PORTLESS_PORT is set
        var portlessPort = Environment.GetEnvironmentVariable("PORTLESS_PORT");
        if (!string.IsNullOrEmpty(portlessPort))
        {
            Console.WriteLine($"Warning: PORTLESS_PORT environment variable is deprecated. Fixed ports: HTTP=1355, HTTPS=1356");
        }

        // Build path to Portless.Proxy.csproj
        var assemblyLocation = typeof(ProxyProcessManager).Assembly.Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation) ?? ".";
        // Navigate from bin/Debug/net10.0/ up to solution root (4 levels up)
        var solutionRoot = Path.GetFullPath(Path.Combine(assemblyDirectory, "../../../../"));
        var proxyProjectPath = Path.Combine(solutionRoot, "Portless.Proxy", "Portless.Proxy.csproj");

        if (!File.Exists(proxyProjectPath))
        {
            throw new InvalidOperationException($"Proxy project not found at: {proxyProjectPath}");
        }

        // Create process start info for detached execution (cross-platform)
        ProcessStartInfo startInfo;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c set PORTLESS_PORT={DefaultPort} && set PORTLESS_HTTPS_ENABLED={enableHttps.ToString().ToLower()} && set DOTNET_MODIFIABLE_ASSEMBLIES=debug && dotnet run --project \"{proxyProjectPath}\" --urls http://*:{DefaultPort}",
                UseShellExecute = true,  // Required for detached execution on Windows
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
        }
        else
        {
            // Linux/macOS: use sh for detached execution
            startInfo = new ProcessStartInfo
            {
                FileName = "/bin/sh",
                Arguments = $"-c \"PORTLESS_PORT={DefaultPort} PORTLESS_HTTPS_ENABLED={enableHttps.ToString().ToLower()} DOTNET_MODIFIABLE_ASSEMBLIES=debug dotnet run --project '{proxyProjectPath}' --urls http://*:{DefaultPort}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

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

    public async Task<int[]> GetActiveManagedProcessesAsync()
    {
        await LoadManagedPidsAsync();

        // Filter out dead PIDs
        var alivePids = _managedPids.Where(pid =>
        {
            try
            {
                var p = Process.GetProcessById(pid);
                return !p.HasExited;
            }
            catch (ArgumentException)
            {
                return false; // Process doesn't exist
            }
        }).ToArray();

        return alivePids;
    }

    public async Task KillManagedProcessesAsync(int[] pids)
    {
        foreach (var pid in pids)
        {
            try
            {
                var p = Process.GetProcessById(pid);
                p.Kill(entireProcessTree: true);
            }
            catch (ArgumentException)
            {
                // Process already dead, ignore
            }
        }

        // Remove from tracked PIDs
        foreach (var pid in pids)
        {
            _managedPids.Remove(pid);
        }

        await SaveManagedPidsAsync();
    }

    public async Task RegisterManagedProcessAsync(int pid)
    {
        _managedPids.Add(pid);
        await SaveManagedPidsAsync();
    }

    private async Task LoadManagedPidsAsync()
    {
        if (!File.Exists(_managedPidsFilePath))
        {
            _managedPids.Clear();
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_managedPidsFilePath);
            _managedPids.Clear();
            var loaded = JsonSerializer.Deserialize<HashSet<int>>(json);
            if (loaded != null)
            {
                foreach (var pid in loaded)
                {
                    _managedPids.Add(pid);
                }
            }
        }
        catch
        {
            _managedPids.Clear();
        }
    }

    private async Task SaveManagedPidsAsync()
    {
        var json = JsonSerializer.Serialize(_managedPids);
        await File.WriteAllTextAsync(_managedPidsFilePath, json);
    }
}
