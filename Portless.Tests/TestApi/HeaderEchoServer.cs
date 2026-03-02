using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Portless.Tests.TestApi;

/// <summary>
/// Simple test API server that echoes received headers.
/// Used for testing X-Forwarded-Proto header preservation in integration tests.
/// </summary>
public class HeaderEchoServer : IAsyncDisposable
{
    private IHost? _host;
    private readonly ILogger<HeaderEchoServer> _logger;
    private readonly ConcurrentDictionary<string, string> _lastReceivedHeaders = new();

    /// <summary>
    /// Gets the headers received in the most recent request.
    /// </summary>
    public IReadOnlyDictionary<string, string> ReceivedHeaders => _lastReceivedHeaders;

    /// <summary>
    /// Gets the base URL where the server is listening.
    /// </summary>
    public string? BaseUrl { get; private set; }

    public HeaderEchoServer(ILogger<HeaderEchoServer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Starts the echo server on a dynamic port.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureServices(services =>
                {
                    // No additional services needed
                });

                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapPost("/echo-headers", async context =>
                        {
                            // Capture all headers
                            foreach (var header in context.Request.Headers)
                            {
                                _lastReceivedHeaders[header.Key] = header.Value.ToString();
                                _logger.LogDebug("Header: {Key} = {Value}", header.Key, header.Value);
                            }

                            // Return captured headers as JSON
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsJsonAsync(_lastReceivedHeaders, cancellationToken);
                        });

                        endpoints.MapGet("/echo-headers", async context =>
                        {
                            // Capture all headers
                            foreach (var header in context.Request.Headers)
                            {
                                _lastReceivedHeaders[header.Key] = header.Value.ToString();
                                _logger.LogDebug("Header: {Key} = {Value}", header.Key, header.Value);
                            }

                            // Return captured headers as JSON
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsJsonAsync(_lastReceivedHeaders, cancellationToken);
                        });
                    });
                });

                // Bind to dynamic port (let system choose)
                webBuilder.UseUrls("http://127.0.0.1:0");
            })
            .Build();

        await _host.StartAsync(cancellationToken);

        // Get the actual bound URL from the server
        var server = _host.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        var primaryAddress = addresses?.Addresses.FirstOrDefault();
        BaseUrl = primaryAddress ?? throw new InvalidOperationException("Failed to get server address");

        _logger.LogInformation("HeaderEchoServer started on {BaseUrl}", BaseUrl);
    }

    /// <summary>
    /// Stops the echo server and releases resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
            _logger.LogInformation("HeaderEchoServer stopped");
        }
    }

    /// <summary>
    /// Clears the received headers dictionary.
    /// </summary>
    public void ClearHeaders()
    {
        _lastReceivedHeaders.Clear();
    }

    /// <summary>
    /// Gets a specific header value from the most recent request.
    /// </summary>
    /// <param name="headerName">Name of the header to retrieve.</param>
    /// <returns>Header value or null if not found.</returns>
    public string? GetHeader(string headerName)
    {
        _lastReceivedHeaders.TryGetValue(headerName, out var value);
        return value;
    }

    /// <summary>
    /// Checks if a specific header was present in the most recent request.
    /// </summary>
    /// <param name="headerName">Name of the header to check.</param>
    /// <returns>True if the header was present.</returns>
    public bool HasHeader(string headerName)
    {
        return _lastReceivedHeaders.ContainsKey(headerName);
    }
}
