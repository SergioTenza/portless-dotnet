using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Portless.Core.Services;

public class TcpForwardingService : ITcpForwardingService, IHostedService, IDisposable
{
    private readonly ILogger<TcpForwardingService> _logger;
    private readonly ConcurrentDictionary<string, TcpListenerEntry> _listeners = new();

    public TcpForwardingService(ILogger<TcpForwardingService> logger)
    {
        _logger = logger;
    }

    public async Task StartListenerAsync(string name, int listenPort, string targetHost, int targetPort, CancellationToken ct = default)
    {
        if (_listeners.ContainsKey(name))
        {
            _logger.LogWarning("TCP listener '{Name}' already exists", name);
            return;
        }

        var listener = new TcpListener(IPAddress.Any, listenPort);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            listener.Start();
            var entry = new TcpListenerEntry(listener, cts, targetHost, targetPort);
            _listeners[name] = entry;

            _ = AcceptLoopAsync(entry, name);
            _logger.LogInformation("TCP listener '{Name}' started on port {Port} -> {TargetHost}:{TargetPort}",
                name, listenPort, targetHost, targetPort);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start TCP listener '{Name}' on port {Port}", name, listenPort);
            listener.Stop();
            throw;
        }
    }

    public Task StopListenerAsync(string name)
    {
        if (_listeners.TryRemove(name, out var entry))
        {
            entry.Cts.Cancel();
            entry.Listener.Stop();
            _logger.LogInformation("TCP listener '{Name}' stopped", name);
        }
        return Task.CompletedTask;
    }

    public Dictionary<string, int> GetActiveListeners()
    {
        return _listeners.ToDictionary(kvp => kvp.Key, kvp => 
            ((IPEndPoint)kvp.Value.Listener.LocalEndpoint).Port);
    }

    private async Task AcceptLoopAsync(TcpListenerEntry entry, string name)
    {
        while (!entry.Cts.IsCancellationRequested)
        {
            try
            {
                var client = await entry.Listener.AcceptTcpClientAsync(entry.Cts.Token);
                _ = RelayConnectionAsync(client, entry.TargetHost, entry.TargetPort, name);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting connection on TCP listener '{Name}'", name);
                await Task.Delay(100);
            }
        }
    }

    private async Task RelayConnectionAsync(TcpClient client, string targetHost, int targetPort, string name)
    {
        TcpClient? backend = null;
        try
        {
            backend = new TcpClient();
            await backend.ConnectAsync(targetHost, targetPort);

            var clientStream = client.GetStream();
            var backendStream = backend.GetStream();

            _logger.LogDebug("TCP relay: {Name} - new connection -> {TargetHost}:{TargetPort}", name, targetHost, targetPort);

            // Bidirectional copy
            var t1 = clientStream.CopyToAsync(backendStream);
            var t2 = backendStream.CopyToAsync(clientStream);
            await Task.WhenAny(t1, t2);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "TCP relay connection error for '{Name}'", name);
        }
        finally
        {
            client.Dispose();
            backend?.Dispose();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var kvp in _listeners)
        {
            kvp.Value.Cts.Cancel();
            kvp.Value.Listener.Stop();
        }
        _listeners.Clear();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        foreach (var kvp in _listeners)
        {
            kvp.Value.Cts.Cancel();
            kvp.Value.Listener.Stop();
        }
    }

    private record TcpListenerEntry(TcpListener Listener, CancellationTokenSource Cts, string TargetHost, int TargetPort);
}
