using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;
using Portless.Plugin.SDK;

namespace Portless.Tests;

/// <summary>
/// Test-only plugin implementation for unit testing PluginLoader.
/// </summary>
public class TestPlugin : PortlessPlugin
{
    public override string Name => "TestPlugin";
    public override string Version => "1.0.0";
}

public class TestPlugin2 : PortlessPlugin
{
    public override string Name => "TestPlugin2";
    public override string Version => "2.0.0";
}

public class PluginLoaderTests
{
    private readonly Mock<ILogger<PluginLoader>> _logger;
    private readonly PluginLoader _loader;

    public PluginLoaderTests()
    {
        _logger = new Mock<ILogger<PluginLoader>>();
        _loader = new PluginLoader(_logger.Object);
    }

    [Fact]
    public async Task LoadAllAsync_NonexistentDirectory_SkipsLoading()
    {
        await _loader.LoadAllAsync("/nonexistent/plugins/dir");
        Assert.Empty(_loader.GetLoadedPlugins());
    }

    [Fact]
    public async Task LoadAllAsync_EmptyDirectory_LoadsNoPlugins()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"portless-test-plugins-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        try
        {
            await _loader.LoadAllAsync(dir);
            Assert.Empty(_loader.GetLoadedPlugins());
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task LoadAsync_NoManifestFile_ReturnsNull()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"portless-test-plugin-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        try
        {
            var result = await _loader.LoadAsync(dir);
            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task LoadAsync_EmptyManifest_ReturnsNull()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"portless-test-plugin-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "plugin.yaml"), "");
            var result = await _loader.LoadAsync(dir);
            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task LoadAsync_ManifestMissingEntryAssembly_ReturnsNull()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"portless-test-plugin-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        try
        {
            var yaml = "name: TestPlugin\nversion: 1.0.0\n";
            await File.WriteAllTextAsync(Path.Combine(dir, "plugin.yaml"), yaml);
            var result = await _loader.LoadAsync(dir);
            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task LoadAsync_AssemblyNotFound_ReturnsNull()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"portless-test-plugin-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        try
        {
            var yaml = "name: TestPlugin\nversion: 1.0.0\nentry_assembly: NonExistent.dll\n";
            await File.WriteAllTextAsync(Path.Combine(dir, "plugin.yaml"), yaml);
            var result = await _loader.LoadAsync(dir);
            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void AddPluginForTesting_AddsPlugin()
    {
        var plugin = new TestPlugin();
        _loader.AddPluginForTesting(plugin);
        var plugins = _loader.GetLoadedPlugins();
        Assert.Single(plugins);
        Assert.Equal("TestPlugin", plugins[0].Name);
    }

    [Fact]
    public async Task UnloadAsync_UnknownPlugin_DoesNotThrow()
    {
        var plugin = new TestPlugin();
        _loader.AddPluginForTesting(plugin);
        await _loader.UnloadAsync("NonExistentPlugin");
        Assert.Single(_loader.GetLoadedPlugins());
    }

    [Fact]
    public async Task UnloadAsync_KnownPlugin_RemovesPlugin()
    {
        var plugin = new TestPlugin();
        _loader.AddPluginForTesting(plugin);
        await _loader.UnloadAsync("TestPlugin");
        Assert.Empty(_loader.GetLoadedPlugins());
    }

    [Fact]
    public async Task UnloadAsync_CaseInsensitive()
    {
        var plugin = new TestPlugin();
        _loader.AddPluginForTesting(plugin);
        await _loader.UnloadAsync("testplugin");
        Assert.Empty(_loader.GetLoadedPlugins());
    }

    [Fact]
    public async Task FireBeforeProxyAsync_NoPlugins_ReturnsNull()
    {
        var context = new ProxyContext
        {
            Hostname = "test.local",
            Path = "/",
            Method = "GET",
            Scheme = "https",
            RouteId = "route-test"
        };
        var result = await _loader.FireBeforeProxyAsync(context);
        Assert.Null(result);
    }

    [Fact]
    public async Task FireBeforeProxyAsync_PluginReturnsNull_Continues()
    {
        var plugin = new TestPlugin();
        _loader.AddPluginForTesting(plugin);
        var context = new ProxyContext
        {
            Hostname = "test.local",
            Path = "/",
            Method = "GET",
            Scheme = "https",
            RouteId = "route-test"
        };
        var result = await _loader.FireBeforeProxyAsync(context);
        Assert.Null(result);
    }

    [Fact]
    public async Task FireAfterProxyAsync_NoPlugins_DoesNotThrow()
    {
        var context = new ProxyContext
        {
            Hostname = "test.local",
            Path = "/",
            Method = "GET",
            Scheme = "https",
            RouteId = "route-test"
        };
        var proxyResult = new ProxyResult { StatusCode = 200, DurationMs = 100 };
        await _loader.FireAfterProxyAsync(context, proxyResult);
    }

    [Fact]
    public async Task FireAfterProxyAsync_WithPlugin_DoesNotThrow()
    {
        var plugin = new TestPlugin();
        _loader.AddPluginForTesting(plugin);
        var context = new ProxyContext
        {
            Hostname = "test.local",
            Path = "/",
            Method = "GET",
            Scheme = "https",
            RouteId = "route-test"
        };
        var proxyResult = new ProxyResult { StatusCode = 200, DurationMs = 100 };
        await _loader.FireAfterProxyAsync(context, proxyResult);
    }

    [Fact]
    public async Task FireRouteAddedAsync_NoPlugins_DoesNotThrow()
    {
        var route = new Portless.Plugin.SDK.RouteInfo { Hostname = "test.local", Port = 5000 };
        await _loader.FireRouteAddedAsync(route);
    }

    [Fact]
    public async Task FireRouteRemovedAsync_NoPlugins_DoesNotThrow()
    {
        var route = new Portless.Plugin.SDK.RouteInfo { Hostname = "test.local", Port = 5000 };
        await _loader.FireRouteRemovedAsync(route);
    }

    [Fact]
    public async Task FireErrorAsync_NoPlugins_ReturnsNull()
    {
        var context = new ErrorContext
        {
            Hostname = "test.local",
            Path = "/",
            StatusCode = 500,
            ErrorMessage = "test error"
        };
        var result = await _loader.FireErrorAsync(context);
        Assert.Null(result);
    }

    [Fact]
    public async Task FireErrorAsync_PluginReturnsNull_ReturnsNull()
    {
        var plugin = new TestPlugin();
        _loader.AddPluginForTesting(plugin);
        var context = new ErrorContext
        {
            Hostname = "test.local",
            Path = "/",
            StatusCode = 500,
            ErrorMessage = "test error"
        };
        var result = await _loader.FireErrorAsync(context);
        Assert.Null(result);
    }

    [Fact]
    public void GetLoadedPlugins_ReturnsAllPlugins()
    {
        _loader.AddPluginForTesting(new TestPlugin());
        _loader.AddPluginForTesting(new TestPlugin2());
        var plugins = _loader.GetLoadedPlugins();
        Assert.Equal(2, plugins.Count);
    }

    [Fact]
    public void GetLoadedPlugins_ReturnsSnapshot()
    {
        _loader.AddPluginForTesting(new TestPlugin());
        var plugins = _loader.GetLoadedPlugins();
        _loader.AddPluginForTesting(new TestPlugin2());
        Assert.Single(plugins);
        Assert.Equal(2, _loader.GetLoadedPlugins().Count);
    }
}
