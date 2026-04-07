namespace Portless.Core.Services;

public interface ITcpForwardingService
{
    /// <summary>
    /// Starts a TCP listener that forwards connections to the target endpoint.
    /// </summary>
    Task StartListenerAsync(string name, int listenPort, string targetHost, int targetPort, CancellationToken ct = default);

    /// <summary>
    /// Stops a TCP listener by name.
    /// </summary>
    Task StopListenerAsync(string name);

    /// <summary>
    /// Gets all active TCP listeners.
    /// </summary>
    Dictionary<string, int> GetActiveListeners();
}
