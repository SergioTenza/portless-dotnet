using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Portless.Proxy;
using Xunit;
using Xunit.Abstractions;

namespace Portless.Tests;

/// <summary>
/// Base class for integration tests providing common setup and teardown.
/// Creates an isolated temp directory, sets PORTLESS_STATE_DIR env var,
/// writes empty routes.json, and provides factory methods for creating
/// the proxy web application and HTTP clients.
/// </summary>
public abstract class IntegrationTestBase : IDisposable, IAsyncLifetime
{
    /// <summary>
    /// The isolated temp directory created for this test instance.
    /// </summary>
    protected string TempDir { get; private set; } = null!;

    private readonly List<WebApplicationFactory<Program>> _factories = new();
    private readonly List<HttpClient> _clients = new();
    private bool _disposed;

    /// <summary>
    /// Prefix for the temp directory name. Override to customize.
    /// Defaults to "portless-test-{TypeName}".
    /// </summary>
    protected virtual string TempDirPrefix => $"portless-test-{GetType().Name}";

    /// <summary>
    /// Whether to create an empty routes.json in the temp dir during initialization.
    /// Override and set to false if the test manages its own routes file.
    /// </summary>
    protected virtual bool CreateRoutesJson => true;

    /// <summary>
    /// Optional test output helper for logging. Set by derived classes in their constructor.
    /// </summary>
    protected ITestOutputHelper Output { get; set; } = null!;

    public virtual async Task InitializeAsync()
    {
        TempDir = Path.Combine(Path.GetTempPath(), $"{TempDirPrefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(TempDir);
        Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", TempDir);

        if (CreateRoutesJson)
        {
            await File.WriteAllTextAsync(Path.Combine(TempDir, "routes.json"), "[]");
        }
    }

    /// <summary>
    /// Creates a new WebApplicationFactory for the proxy application.
    /// The factory is tracked and will be disposed automatically by the base class.
    /// </summary>
    /// <param name="configure">Optional action to configure the web host builder.</param>
    /// <returns>A new WebApplicationFactory instance.</returns>
    protected WebApplicationFactory<Program> CreateProxyApp(
        Action<IWebHostBuilder>? configure = null)
    {
        WebApplicationFactory<Program> factory = configure != null
            ? new WebApplicationFactory<Program>().WithWebHostBuilder(configure)
            : new WebApplicationFactory<Program>();

        _factories.Add(factory);
        return factory;
    }

    /// <summary>
    /// Creates an HttpClient from the given factory and tracks it for disposal.
    /// </summary>
    /// <param name="factory">The factory to create the client from.</param>
    /// <returns>A new HttpClient instance.</returns>
    protected HttpClient CreateHttpClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        _clients.Add(client);
        return client;
    }

    public virtual Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    public virtual void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var client in _clients)
            client?.Dispose();
        foreach (var factory in _factories)
            factory?.Dispose();

        Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", null);

        try
        {
            if (TempDir != null && Directory.Exists(TempDir))
            {
                Directory.Delete(TempDir, true);
            }
        }
        catch { }
    }
}
