namespace Portless.Core.Services;

/// <summary>
/// Centralized proxy port and URL resolution.
/// Replaces all hardcoded "1355" references across the codebase.
/// </summary>
public static class ProxyConstants
{
    /// <summary>
    /// Default HTTP port for the Portless proxy.
    /// </summary>
    public const int DefaultHttpPort = 1355;

    /// <summary>
    /// Default HTTPS port for the Portless proxy.
    /// </summary>
    public const int DefaultHttpsPort = 1356;

    /// <summary>
    /// Environment variable name for overriding the proxy port.
    /// </summary>
    public const string PortEnvVar = "PORTLESS_PORT";

    /// <summary>
    /// Gets the proxy HTTP port from environment variable or default.
    /// </summary>
    public static int GetHttpPort()
    {
        var envPort = Environment.GetEnvironmentVariable(PortEnvVar);
        return int.TryParse(envPort, out var port) ? port : DefaultHttpPort;
    }

    /// <summary>
    /// Gets the proxy base URL (e.g. "http://localhost:1355").
    /// </summary>
    public static string GetProxyBaseUrl()
    {
        return $"http://localhost:{GetHttpPort()}";
    }
}
