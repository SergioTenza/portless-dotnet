namespace Portless.Cli.Services;

/// <summary>
/// Provides the proxy port from environment or default.
/// </summary>
public static class ProxyPortProvider
{
    private const int DefaultPort = 1355;

    public static int GetProxyPort()
    {
        var envPort = Environment.GetEnvironmentVariable("PORTLESS_PORT");
        return int.TryParse(envPort, out var port) ? port : DefaultPort;
    }
}
