using System.Net.Http.Json;
using Portless.Core.Services;
using Spectre.Console;

namespace Portless.Cli.Services;

/// <summary>
/// Factory for creating HttpClient instances configured to communicate with the proxy.
/// Centralizes timeout configuration and eliminates inconsistent HttpClient creation.
/// Also provides common proxy API helpers (reload, etc).
/// </summary>
public interface IProxyHttpClient
{
    /// <summary>
    /// Creates a new HttpClient configured for proxy communication.
    /// </summary>
    HttpClient CreateClient();

    /// <summary>
    /// Sends a POST to the proxy plugin reload endpoint. Silently fails if proxy not running.
    /// </summary>
    Task NotifyPluginReloadAsync();
}

public class ProxyHttpClient : IProxyHttpClient
{
    private static readonly string ProxyBaseUrl = ProxyConstants.GetProxyBaseUrl();

    public HttpClient CreateClient()
    {
        return new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    public async Task NotifyPluginReloadAsync()
    {
        try
        {
            using var http = CreateClient();
            await http.PostAsync($"{ProxyBaseUrl}/api/v1/plugins/reload", null);
        }
        catch (HttpRequestException)
        {
            // Proxy may not be running; operation still succeeds locally
        }
    }
}

/// <summary>
/// Helper for resolving plugin directories from PORTLESS_STATE_DIR.
/// Eliminates duplicated state directory resolution across PluginCommand methods.
/// </summary>
public static class PluginDirectoryResolver
{
    public static string GetPluginsDirectory()
    {
        var stateDir = Environment.GetEnvironmentVariable("PORTLESS_STATE_DIR")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".portless");
        var pluginsDir = Path.Combine(stateDir, "plugins");
        Directory.CreateDirectory(pluginsDir);
        return pluginsDir;
    }

    public static string GetPluginDirectory(string pluginName)
    {
        return Path.Combine(GetPluginsDirectory(), pluginName);
    }
}
