using System.Net.Sockets;

namespace Portless.Core.Services;

/// <summary>
/// Helper for checking proxy availability.
/// Consolidates the duplicated IsProxyRunningAsync pattern from RunCommand and UpCommand.
/// </summary>
public interface IProxyConnectionHelper
{
    /// <summary>
    /// Checks if the proxy is currently accepting connections.
    /// </summary>
    Task<bool> IsProxyRunningAsync();
}

public class ProxyConnectionHelper : IProxyConnectionHelper
{
    public async Task<bool> IsProxyRunningAsync()
    {
        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync("localhost", ProxyConstants.GetHttpPort());
            return true;
        }
        catch
        {
            return false;
        }
    }
}
