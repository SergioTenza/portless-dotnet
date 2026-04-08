using Microsoft.Extensions.Logging.Abstractions;
using Portless.Core.Services;
using Portless.Plugin.SDK;

namespace Portless.Tests;

public sealed class PluginLoaderTests
{
    private static PluginLoader CreateLoader()
        => new(NullLogger<PluginLoader>.Instance);

    // 1. GetLoadedPlugins_NoPlugins_ReturnsEmpty
    [Fact]
    public void GetLoadedPlugins_NoPlugins_ReturnsEmpty()
    {
        var loader = CreateLoader();
        var plugins = loader.GetLoadedPlugins();

        Assert.NotNull(plugins);
        Assert.Empty(plugins);
    }

    // 2. FireBeforeProxyAsync_NoPlugins_ReturnsNull
    [Fact]
    public async Task FireBeforeProxyAsync_NoPlugins_ReturnsNull()
    {
        var loader = CreateLoader();
        var context = NewProxyContext();

        var result = await loader.FireBeforeProxyAsync(context);
        Assert.Null(result);
    }

    // 3. FireBeforeProxyAsync_PluginReturnsResult_ReturnsResult
    [Fact]
    public async Task FireBeforeProxyAsync_PluginReturnsResult_ReturnsResult()
    {
        var loader = CreateLoader();
        var shortCircuitResult = new ProxyResult
        {
            StatusCode = 403,
            DurationMs = 0,
            ResponseBody = "blocked"
        };
        var plugin = new DelegatePlugin("blocker",
            beforeProxy: _ => Task.FromResult<ProxyResult?>(shortCircuitResult));
        InjectPlugin(loader, plugin);

        var result = await loader.FireBeforeProxyAsync(NewProxyContext());

        Assert.NotNull(result);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal("blocked", result.ResponseBody);
    }

    // 4. FireBeforeProxyAsync_MultiplePlugins_FirstShortCircuits
    [Fact]
    public async Task FireBeforeProxyAsync_MultiplePlugins_FirstShortCircuits()
    {
        var loader = CreateLoader();
        var shortCircuitResult = new ProxyResult { StatusCode = 200, DurationMs = 0 };
        var first = new DelegatePlugin("first",
            beforeProxy: _ => Task.FromResult<ProxyResult?>(shortCircuitResult));
        var second = new DelegatePlugin("second",
            beforeProxy: _ => Task.FromResult<ProxyResult?>(new ProxyResult { StatusCode = 201, DurationMs = 0 }));
        InjectPlugin(loader, first);
        InjectPlugin(loader, second);

        var result = await loader.FireBeforeProxyAsync(NewProxyContext());

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        // Second plugin should never have been invoked
    }

    // 5. FireAfterProxyAsync_MultiplePlugins_AllCalled
    [Fact]
    public async Task FireAfterProxyAsync_MultiplePlugins_AllCalled()
    {
        var loader = CreateLoader();
        var tracking1 = new TrackingPlugin("tracker-1");
        var tracking2 = new TrackingPlugin("tracker-2");

        InjectPlugin(loader, tracking1);
        InjectPlugin(loader, tracking2);

        var proxyResult = new ProxyResult { StatusCode = 200, DurationMs = 50 };

        await loader.FireAfterProxyAsync(NewProxyContext(), proxyResult);

        Assert.Equal(1, tracking1.AfterProxyCallCount);
        Assert.Equal(1, tracking2.AfterProxyCallCount);
    }

    // 6. FireErrorAsync_PluginReturnsResponse_Returned
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

    // 7. FireErrorAsync_NoPluginHandles_ReturnsNull
    [Fact]
    public async Task FireErrorAsync_NoPluginHandles_ReturnsNull()
    {
        var loader = CreateLoader();
        var passivePlugin = new DelegatePlugin("passive",
            onError: _ => Task.FromResult<ErrorResponse?>(null));
        InjectPlugin(loader, passivePlugin);

        var errorCtx = new ErrorContext
        {
            Hostname = "test.localhost",
            Path = "/api",
            StatusCode = 502,
            ErrorMessage = "Bad Gateway"
        };

        var response = await loader.FireErrorAsync(errorCtx);
        Assert.Null(response);
    }

    // 8. FireRouteAddedAsync_CallsAllPlugins
    [Fact]
    public async Task FireRouteAddedAsync_CallsAllPlugins()
    {
        var loader = CreateLoader();
        var routeTracker1 = new RouteTrackingPlugin("rt-1");
        var routeTracker2 = new RouteTrackingPlugin("rt-2");
        InjectPlugin(loader, routeTracker1);
        InjectPlugin(loader, routeTracker2);

        var route = new RouteInfo
        {
            Hostname = "myapp.localhost",
            Port = 8080,
            Backends = ["http://localhost:5000"]
        };

        await loader.FireRouteAddedAsync(route);

        Assert.Equal(1, routeTracker1.RouteAddedCount);
        Assert.Equal(0, routeTracker1.RouteRemovedCount);
        Assert.Equal(1, routeTracker2.RouteAddedCount);
        Assert.Equal(0, routeTracker2.RouteRemovedCount);
    }

    // 9. FireRouteRemovedAsync_CallsAllPlugins
    [Fact]
    public async Task FireRouteRemovedAsync_CallsAllPlugins()
    {
        var loader = CreateLoader();
        var routeTracker1 = new RouteTrackingPlugin("rt-1");
        var routeTracker2 = new RouteTrackingPlugin("rt-2");
        InjectPlugin(loader, routeTracker1);
        InjectPlugin(loader, routeTracker2);

        var route = new RouteInfo
        {
            Hostname = "myapp.localhost",
            Backends = ["http://localhost:5000"]
        };

        await loader.FireRouteRemovedAsync(route);

        Assert.Equal(0, routeTracker1.RouteAddedCount);
        Assert.Equal(1, routeTracker1.RouteRemovedCount);
        Assert.Equal(0, routeTracker2.RouteAddedCount);
        Assert.Equal(1, routeTracker2.RouteRemovedCount);
    }

    // 10. FireBeforeProxyAsync_PluginThrows_ExceptionHandled_ReturnsNull
    [Fact]
    public async Task FireBeforeProxyAsync_PluginThrows_ExceptionHandled_ReturnsNull()
    {
        var loader = CreateLoader();
        var throwingPlugin = new DelegatePlugin("thrower",
            beforeProxy: _ => throw new InvalidOperationException("plugin blew up"));
        InjectPlugin(loader, throwingPlugin);

        var result = await loader.FireBeforeProxyAsync(NewProxyContext());

        // Exception is caught internally, returns null (no short-circuit)
        Assert.Null(result);
    }

    #region Helpers

    private static void InjectPlugin(PluginLoader loader, PortlessPlugin plugin)
    {
        loader.AddPluginForTesting(plugin);
    }

    private static ProxyContext NewProxyContext() => new()
    {
        Hostname = "test.localhost",
        Path = "/api",
        Method = "GET",
        Scheme = "http",
        RouteId = "cluster-test"
    };

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

    private sealed class RouteTrackingPlugin : PortlessPlugin
    {
        private readonly string _name;
        public RouteTrackingPlugin(string name) => _name = name;
        public override string Name => _name;
        public override string Version => "1.0.0";

        public int RouteAddedCount { get; private set; }
        public int RouteRemovedCount { get; private set; }

        public override Task OnRouteAddedAsync(RouteInfo route)
        {
            RouteAddedCount++;
            return Task.CompletedTask;
        }

        public override Task OnRouteRemovedAsync(RouteInfo route)
        {
            RouteRemovedCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class DelegatePlugin : PortlessPlugin
    {
        private readonly string _name;
        private readonly Func<ProxyContext, Task<ProxyResult?>> _beforeProxy;
        private readonly Func<ErrorContext, Task<ErrorResponse?>> _onError;

        public DelegatePlugin(
            string name,
            Func<ProxyContext, Task<ProxyResult?>>? beforeProxy = null,
            Func<ErrorContext, Task<ErrorResponse?>>? onError = null)
        {
            _name = name;
            _beforeProxy = beforeProxy ?? (_ => Task.FromResult<ProxyResult?>(null));
            _onError = onError ?? (_ => Task.FromResult<ErrorResponse?>(null));
        }

        public override string Name => _name;
        public override string Version => "1.0.0";

        public override Task<ProxyResult?> BeforeProxyAsync(ProxyContext context) => _beforeProxy(context);
        public override Task<ErrorResponse?> OnErrorAsync(ErrorContext context) => _onError(context);
    }

    #endregion
}
