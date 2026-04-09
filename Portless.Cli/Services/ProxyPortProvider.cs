using Portless.Core.Services;

namespace Portless.Cli.Services;

/// <summary>
/// Provides the proxy port from environment or default.
/// Delegates to ProxyConstants for centralized port resolution.
/// </summary>
public static class ProxyPortProvider
{
    public static int GetProxyPort() => ProxyConstants.GetHttpPort();
}
