using System.Net;
using System.Net.Sockets;

namespace Portless.Cli.Services;

public class PortAllocator : IPortAllocator
{
    public async Task<int> AssignFreePortAsync()
    {
        var random = new Random();
        const int maxAttempts = 50; // Prevent infinite loop
        int attempt = 0;

        while (attempt < maxAttempts)
        {
            // Generate random port in range
            var port = random.Next(4000, 5000);

            if (await IsPortFreeAsync(port))
            {
                return port;
            }

            attempt++;
        }

        throw new InvalidOperationException("Failed to allocate port after 50 attempts. Port range 4000-4999 may be exhausted.");
    }

    public async Task<bool> IsPortFreeAsync(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true; // Port is free if we can bind to it
        }
        catch (SocketException) when (port >= 4000 && port <= 4999)
        {
            return false; // Port is in use
        }
    }

    public Task ReleasePortAsync(int port)
    {
        // Placeholder for future port pooling
        // Currently no-op since ports are not tracked after allocation
        return Task.CompletedTask;
    }
}
