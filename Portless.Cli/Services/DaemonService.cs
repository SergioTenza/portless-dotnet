using System.Diagnostics;
using Spectre.Console;

namespace Portless.Cli.Services;

/// <summary>
/// Manages the Portless proxy as a systemd user service (daemon mode).
/// </summary>
public class DaemonService : IDaemonService
{
    private const string ServiceName = "portless-proxy";
    private const string UnitFileName = "portless-proxy.service";

    private readonly string _unitFilePath;
    private readonly string _stateDirectory;

    public DaemonService()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var systemdDir = Path.Combine(homeDir, ".config", "systemd", "user");
        _unitFilePath = Path.Combine(systemdDir, UnitFileName);
        _stateDirectory = Path.Combine(homeDir, ".portless");
    }

    public async Task InstallAsync(bool enableHttps, bool enableNow)
    {
        // Ensure the systemd user directory exists
        var dir = Path.GetDirectoryName(_unitFilePath)!;
        Directory.CreateDirectory(dir);

        // Resolve proxy project path
        var (execPath, workingDir) = ResolveProxyPath();

        var stateDir = _stateDirectory;
        var httpsEnv = enableHttps ? "true" : "false";

        var unitContent = $@"[Unit]
Description=Portless.NET Local Proxy
After=network.target

[Service]
Type=simple
WorkingDirectory={workingDir}
ExecStart={execPath}
Environment=PORTLESS_PORT=1355
Environment=PORTLESS_STATE_DIR={stateDir}
Environment=PORTLESS_HTTPS_ENABLED={httpsEnv}
Environment=DOTNET_ENVIRONMENT=Production
Restart=on-failure
RestartSec=5

[Install]
WantedBy=default.target
";

        await File.WriteAllTextAsync(_unitFilePath, unitContent);

        // Reload systemd
        await RunSystemctlAsync("daemon-reload");

        // Start the service
        await RunSystemctlAsync("start", ServiceName);

        if (enableNow)
        {
            await RunSystemctlAsync("enable", ServiceName);
        }
    }

    public async Task UninstallAsync()
    {
        // Stop the service (ignore errors if not running)
        try
        {
            await RunSystemctlAsync("stop", ServiceName);
        }
        catch
        {
            // Service may not be running
        }

        // Disable the service (ignore errors if not enabled)
        try
        {
            await RunSystemctlAsync("disable", ServiceName);
        }
        catch
        {
            // Service may not be enabled
        }

        // Remove the unit file
        if (File.Exists(_unitFilePath))
        {
            File.Delete(_unitFilePath);
        }

        // Reload systemd
        await RunSystemctlAsync("daemon-reload");
    }

    public async Task<(bool isInstalled, bool isEnabled, bool isRunning, int? pid)> GetStatusAsync()
    {
        var isInstalled = File.Exists(_unitFilePath);

        if (!isInstalled)
        {
            return (false, false, false, null);
        }

        // Check if enabled
        var isEnabled = await CheckSystemctlPropertyAsync("UnitFileState", "enabled");

        // Check if running
        var isActive = await CheckSystemctlPropertyAsync("ActiveState", "active");

        int? pid = null;
        if (isActive)
        {
            pid = await GetMainPidAsync();
        }

        return (true, isEnabled, isActive, pid);
    }

    public async Task EnableAsync()
    {
        if (!File.Exists(_unitFilePath))
        {
            throw new InvalidOperationException("Daemon is not installed. Run 'portless daemon install' first.");
        }

        await RunSystemctlAsync("enable", ServiceName);
    }

    public async Task DisableAsync()
    {
        if (!File.Exists(_unitFilePath))
        {
            throw new InvalidOperationException("Daemon is not installed. Run 'portless daemon install' first.");
        }

        await RunSystemctlAsync("disable", ServiceName);
    }

    /// <summary>
    /// Resolves the proxy executable path. Supports both source (dotnet run) and AOT binary modes.
    /// </summary>
    internal static (string execPath, string workingDir) ResolveProxyPath()
    {
        var assemblyDir = AppContext.BaseDirectory;

        // Try to find the solution root by looking for Portless.Proxy.csproj
        var solutionRoot = Path.GetFullPath(Path.Combine(assemblyDir, "../../../../"));
        var proxyProjectPath = Path.Combine(solutionRoot, "Portless.Proxy", "Portless.Proxy.csproj");

        if (File.Exists(proxyProjectPath))
        {
            // Running from source
            var dotnetPath = "dotnet";
            return ($"{dotnetPath} run --project \"{proxyProjectPath}\"", solutionRoot);
        }

        // Check if we're running as an installed tool - look for Proxy assembly nearby
        var proxyDllPath = Path.Combine(assemblyDir, "Portless.Proxy.dll");
        if (File.Exists(proxyDllPath))
        {
            return ($"dotnet \"{proxyDllPath}\"", assemblyDir);
        }

        // Fallback: try to find the proxy project relative to common locations
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var altSolutionRoot = Path.Combine(homeDir, "portless-dotnet");
        var altProxyPath = Path.Combine(altSolutionRoot, "Portless.Proxy", "Portless.Proxy.csproj");

        if (File.Exists(altProxyPath))
        {
            return ($"dotnet run --project \"{altProxyPath}\"", altSolutionRoot);
        }

        throw new InvalidOperationException(
            "Cannot locate Portless.Proxy project. Ensure you are running from the solution root or the tool is properly installed.");
    }

    private static async Task RunSystemctlAsync(params string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "systemctl",
            Arguments = "--user " + string.Join(" ", args),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start systemctl. Is systemd installed?");

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException(
                $"systemctl --user {string.Join(" ", args)} failed (exit code {process.ExitCode}): {error.Trim()}");
        }
    }

    private static async Task<bool> CheckSystemctlPropertyAsync(string property, string expectedValue)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "systemctl",
                Arguments = $"--user show {ServiceName} --property={property} --value",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return output.Trim().Equals(expectedValue, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<int?> GetMainPidAsync()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "systemctl",
                Arguments = $"--user show {ServiceName} --property=MainPID --value",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return int.TryParse(output.Trim(), out var pid) && pid > 0 ? pid : null;
        }
        catch
        {
            return null;
        }
    }
}
