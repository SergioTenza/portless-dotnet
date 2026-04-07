using System.Text.Json;
using Portless.Core.Models;
using Portless.Core.Serialization;

namespace Portless.Core.Services;

public class RouteStore : IRouteStore, IDisposable
{
    private const int LockRetryMs = 100;
    private const int LockTimeoutMs = 5_000; // 5 seconds

    private readonly string _routesFilePath;
    private readonly string _lockFilePath;

    public RouteStore()
    {
        _routesFilePath = StateDirectoryProvider.GetRoutesFilePath();
        _lockFilePath = _routesFilePath + ".lock";
    }

    public async Task<RouteInfo[]> LoadRoutesAsync(CancellationToken cancellationToken = default)
    {
        // Use file-based locking instead of Mutex
        await using var lockStream = await AcquireFileLockAsync(cancellationToken);

        try
        {
            // If file doesn't exist, return empty array
            if (!File.Exists(_routesFilePath))
            {
                return Array.Empty<RouteInfo>();
            }

            var json = await File.ReadAllTextAsync(_routesFilePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<RouteInfo>();
            }

            var routes = JsonSerializer.Deserialize(json, PortlessJsonContext.Default.RouteInfoArray);
            return routes ?? Array.Empty<RouteInfo>();
        }
        finally
        {
            await lockStream.DisposeAsync();
        }
    }

    public async Task SaveRoutesAsync(RouteInfo[] routes, CancellationToken cancellationToken = default)
    {
        // Use file-based locking instead of Mutex
        await using var lockStream = await AcquireFileLockAsync(cancellationToken);

        try
        {
            // Atomic write via temp file in same directory
            var targetDir = Path.GetDirectoryName(_routesFilePath) ?? ".";
            var tempFileName = Path.Combine(targetDir, Path.GetRandomFileName());

            try
            {
                var json = JsonSerializer.Serialize(routes, PortlessJsonContext.Default.RouteInfoArray);
                await File.WriteAllTextAsync(tempFileName, json, cancellationToken);

                // Atomic replace (only works on same volume)
                File.Move(tempFileName, _routesFilePath, overwrite: true);
            }
            finally
            {
                // Clean up temp file if move failed
                if (File.Exists(tempFileName))
                {
                    File.Delete(tempFileName);
                }
            }
        }
        finally
        {
            await lockStream.DisposeAsync();
        }
    }

    private async Task<FileStream> AcquireFileLockAsync(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        while (true)
        {
            try
            {
                // Try to open the lock file exclusively
                // This will fail if another process has it open
                return new FileStream(
                    _lockFilePath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 4096,
                    options: FileOptions.DeleteOnClose
                );
            }
            catch (IOException)
            {
                // Check timeout
                if ((DateTime.UtcNow - startTime).TotalMilliseconds > LockTimeoutMs)
                {
                    throw new IOException("Timeout acquiring route store lock");
                }

                // Wait before retry
                await Task.Delay(LockRetryMs, cancellationToken);
            }
        }
    }

    public void Dispose()
    {
        // No need to dispose anything with file-based locking
    }
}
