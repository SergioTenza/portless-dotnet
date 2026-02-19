using System.Text.Json;
using System.Text.Json.Serialization;
using Portless.Core.Models;

namespace Portless.Core.Services;

public class RouteStore : IRouteStore, IDisposable
{
    private const string MutexName = "Portless.Routes.Lock";
    private const int MutexTimeoutMs = 5000; // 5 seconds
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly string _routesFilePath;
    private Mutex? _mutex;

    public RouteStore()
    {
        _routesFilePath = StateDirectoryProvider.GetRoutesFilePath();
    }

    public async Task<RouteInfo[]> LoadRoutesAsync(CancellationToken cancellationToken = default)
    {
        // Create or reuse mutex
        if (_mutex == null)
        {
            _mutex = new Mutex(false, MutexName);
        }

        try
        {
            var acquired = _mutex.WaitOne(MutexTimeoutMs);
            if (!acquired)
            {
                throw new IOException("Timeout acquiring route store lock");
            }

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

                var routes = JsonSerializer.Deserialize<RouteInfo[]>(json, _jsonOptions);
                return routes ?? Array.Empty<RouteInfo>();
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }
        catch (AbandonedMutexException)
        {
            // Mutex was abandoned by another process, we now own it
            // Continue with the operation
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

                var routes = JsonSerializer.Deserialize<RouteInfo[]>(json, _jsonOptions);
                return routes ?? Array.Empty<RouteInfo>();
            }
            finally
            {
                _mutex!.ReleaseMutex();
            }
        }
    }

    public async Task SaveRoutesAsync(RouteInfo[] routes, CancellationToken cancellationToken = default)
    {
        // Create or reuse mutex
        if (_mutex == null)
        {
            _mutex = new Mutex(false, MutexName);
        }

        try
        {
            var acquired = _mutex.WaitOne(MutexTimeoutMs);
            if (!acquired)
            {
                throw new IOException("Timeout acquiring route store lock");
            }

            try
            {
                // Atomic write via temp file in same directory
                var targetDir = Path.GetDirectoryName(_routesFilePath) ?? ".";
                var tempFileName = Path.Combine(targetDir, Path.GetRandomFileName());

                try
                {
                    var json = JsonSerializer.Serialize(routes, _jsonOptions);
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
                _mutex.ReleaseMutex();
            }
        }
        catch (AbandonedMutexException)
        {
            // Mutex was abandoned by another process, we now own it
            // Continue with the operation
            try
            {
                // Atomic write via temp file in same directory
                var targetDir = Path.GetDirectoryName(_routesFilePath) ?? ".";
                var tempFileName = Path.Combine(targetDir, Path.GetRandomFileName());

                try
                {
                    var json = JsonSerializer.Serialize(routes, _jsonOptions);
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
                _mutex!.ReleaseMutex();
            }
        }
    }

    public void Dispose()
    {
        _mutex?.Dispose();
    }
}
