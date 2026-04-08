using Microsoft.Extensions.Logging.Abstractions;
using Portless.Core.Services;
using Portless.Plugin.SDK;

namespace Portless.Tests;

public sealed class PluginLoaderTests
{
    private static PluginLoader CreateLoader()
        => new(NullLogger<PluginLoader>.Instance);

    [Fact]
    public async Task LoadAllAsync_EmptyDirectory_NoPluginsLoaded()
    {
        var loader = CreateLoader();
        using var tempDir = new TempDirectory();

        await loader.LoadAllAsync(tempDir.Path);

        Assert.Empty(loader.GetLoadedPlugins());
    }

    [Fact]
    public async Task LoadAsync_MissingManifest_SkipsDirectory()
    {
        var loader = CreateLoader();
        using var tempDir = new TempDirectory();
        var pluginDir = Path.Combine(tempDir.Path, "no-manifest-plugin");
        Directory.CreateDirectory(pluginDir);

        var result = await loader.LoadAsync(pluginDir);

        Assert.Null(result);
        Assert.Empty(loader.GetLoadedPlugins());
    }

    [Fact]
    public void GetLoadedPlugins_NoPlugins_ReturnsEmpty()
    {
        var loader = CreateLoader();
        var plugins = loader.GetLoadedPlugins();

        Assert.NotNull(plugins);
        Assert.Empty(plugins);
    }

    [Fact]
    public async Task FireBeforeProxyAsync_NoPlugins_ReturnsNull()
    {
        var loader = CreateLoader();
        var context = new ProxyContext
        {
            Hostname = "test.localhost",
            Path = "/api",
            Method = "GET",
            Scheme = "http",
            RouteId = "cluster-test"
        };

        var result = await loader.FireBeforeProxyAsync(context);
        Assert.Null(result);
    }

    [Fact]
    public async Task FireAfterProxyAsync_MultiplePlugins_AllCalled()
    {
        var loader = CreateLoader();
        var tracking1 = new TrackingPlugin("tracker-1");
        var tracking2 = new TrackingPlugin("tracker-2");

        InjectPlugin(loader, tracking1);
        InjectPlugin(loader, tracking2);

        var context = new ProxyContext
        {
            Hostname = "test.localhost",
            Path = "/api",
            Method = "GET",
            Scheme = "http",
            RouteId = "cluster-test"
        };
        var result = new ProxyResult
        {
            StatusCode = 200,
            DurationMs = 50
        };

        await loader.FireAfterProxyAsync(context, result);

        Assert.Equal(1, tracking1.AfterProxyCallCount);
        Assert.Equal(1, tracking2.AfterProxyCallCount);
    }

    [Fact]
    public async Task FireErrorAsync_PluginReturnsResponse_Returned()
    {
        var loader = CreateLoader();
        var errorPlugin = new ErrorHandlingPlugin();
        InjectPlugin(loader, errorPlugin);

        var errorCtx = new ErrorContext
        {
            Hostname = "test.localhost",
            Path = "/api",
            StatusCode = 502,
            ErrorMessage = "Bad Gateway"
        };

        var response = await loader.FireErrorAsync(errorCtx);

        Assert.NotNull(response);
        Assert.Equal(503, response.StatusCode);
        Assert.Contains("maintenance", response.Body);
    }

    [Fact]
    public async Task UnloadAsync_NonexistentPlugin_DoesNothing()
    {
        var loader = CreateLoader();

        // Should not throw
        await loader.UnloadAsync("nonexistent");

        Assert.Empty(loader.GetLoadedPlugins());
    }

    #region Helpers

    private static void InjectPlugin(PluginLoader loader, PortlessPlugin plugin)
    {
        loader.AddPluginForTesting(plugin);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"portless-test-{Guid.NewGuid()}");
        public TempDirectory() => Directory.CreateDirectory(Path);
        public void Dispose() { try { Directory.Delete(Path, true); } catch { } }
    }

    #endregion

    #region Test Plugins

    private sealed class TrackingPlugin : PortlessPlugin
    {
        private readonly string _name;
        public TrackingPlugin(string name) => _name = name;
        public override string Name => _name;
        public override string Version => "1.0.0";
        public int AfterProxyCallCount { get; private set; }

        public override Task AfterProxyAsync(ProxyContext context, ProxyResult result)
        {
            AfterProxyCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class ErrorHandlingPlugin : PortlessPlugin
    {
        public override string Name => "error-handler";
        public override string Version => "1.0.0";

        public override Task<ErrorResponse?> OnErrorAsync(ErrorContext context)
        {
            return Task.FromResult<ErrorResponse?>(new ErrorResponse
            {
                StatusCode = 503,
                Body = "<html><body>Under maintenance</body></html>"
            });
        }
    }

    #endregion
}
