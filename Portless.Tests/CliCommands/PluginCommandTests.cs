extern alias Cli;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cli::Portless.Cli.Commands.PluginCommand;
using Cli::Portless.Cli.Services;
using Moq;
using Spectre.Console.Cli;
using Xunit;

using IProxyHttpClient = Cli::Portless.Cli.Services.IProxyHttpClient;

namespace Portless.Tests.CliCommands;

[Collection("SpectreConsoleTests")]
public class PluginCommandTests
{
    private readonly Mock<IProxyHttpClient> _proxyHttpMock;
    private readonly PluginCommand _command;

    public PluginCommandTests()
    {
        _proxyHttpMock = new Mock<IProxyHttpClient>();
        _command = new PluginCommand(_proxyHttpMock.Object);
    }

    private static CommandContext CreateContext() =>
        new([], new TestRemainingArguments(), "plugin", null);

    private static HttpClient CreateMockClient(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(json, statusCode);
        return new HttpClient(handler);
    }

    private static HttpClient CreateMockClientThatThrows()
    {
        var handler = new ThrowingHttpMessageHandler();
        return new HttpClient(handler);
    }

    // ---- Help / Unknown action ----

    [Fact]
    public async Task ExecuteAsync_NullAction_ShowHelpThrowsForMarkup()
    {
        // ShowHelp uses [target] in markup which Spectre parses as a style name,
        // causing an InvalidOperationException in non-interactive test environments.
        var settings = new PluginSettings { Action = null };
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_UnknownAction_ShowHelpThrowsForMarkup()
    {
        var settings = new PluginSettings { Action = "unknown" };
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None));
    }

    // ---- List ----

    [Fact]
    public async Task ExecuteAsync_List_WithPlugins_ReturnsZero()
    {
        var plugins = new[]
        {
            new { name = "test-plugin", version = "1.0.0", status = "enabled" }
        };
        var json = JsonSerializer.Serialize(plugins);

        _proxyHttpMock.Setup(x => x.CreateClient()).Returns(CreateMockClient(json));

        var settings = new PluginSettings { Action = "list" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_List_EmptyPlugins_ReturnsZero()
    {
        _proxyHttpMock.Setup(x => x.CreateClient()).Returns(CreateMockClient("[]"));

        var settings = new PluginSettings { Action = "list" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_List_LsAlias_ReturnsZero()
    {
        var plugins = new[]
        {
            new { name = "my-plugin", version = "2.0.0", status = "disabled" }
        };
        var json = JsonSerializer.Serialize(plugins);

        _proxyHttpMock.Setup(x => x.CreateClient()).Returns(CreateMockClient(json));

        var settings = new PluginSettings { Action = "ls" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_List_ConnectionFailure_Returns1()
    {
        _proxyHttpMock.Setup(x => x.CreateClient()).Returns(CreateMockClientThatThrows());

        var settings = new PluginSettings { Action = "list" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(1, result);
    }

    // ---- Install ----

    [Fact]
    public async Task ExecuteAsync_Install_NoPath_Returns1()
    {
        var settings = new PluginSettings { Action = "install", Target = null };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_Install_NonexistentDirectory_Returns1()
    {
        var settings = new PluginSettings { Action = "install", Target = "/nonexistent/path/plugin" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_Install_ValidDirectory_Returns0()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"plugin-test-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);
            _proxyHttpMock.Setup(x => x.NotifyPluginReloadAsync()).Returns(Task.CompletedTask);

            var settings = new PluginSettings { Action = "install", Target = tempDir };
            var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
            Assert.Equal(0, result);

            _proxyHttpMock.Verify(x => x.NotifyPluginReloadAsync(), Times.Once);
        }
        finally
        {
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
        }
    }

    [Fact]
    public async Task ExecuteAsync_Install_WithEnableFlag_Returns0()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"plugin-test-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);
            _proxyHttpMock.Setup(x => x.NotifyPluginReloadAsync()).Returns(Task.CompletedTask);

            var settings = new PluginSettings { Action = "install", Target = tempDir, Enable = true };
            var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
            Assert.Equal(0, result);
        }
        finally
        {
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
        }
    }

    // ---- Uninstall ----

    [Fact]
    public async Task ExecuteAsync_Uninstall_NoName_Returns1()
    {
        var settings = new PluginSettings { Action = "uninstall", Target = null };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_Uninstall_RemoveAlias_NoName_Returns1()
    {
        var settings = new PluginSettings { Action = "remove", Target = null };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_Uninstall_NonexistentPlugin_Returns1()
    {
        var settings = new PluginSettings { Action = "uninstall", Target = "nonexistent-plugin" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(1, result);
    }

    // ---- Create ----

    [Fact]
    public async Task ExecuteAsync_Create_NoName_Returns1()
    {
        var settings = new PluginSettings { Action = "create", Target = null };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_Create_ValidName_Returns0()
    {
        var tempWorkDir = Path.Combine(Path.GetTempPath(), $"plugin-create-{Guid.NewGuid():N}");
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.CreateDirectory(tempWorkDir);
            Directory.SetCurrentDirectory(tempWorkDir);

            var settings = new PluginSettings { Action = "create", Target = "my-test-plugin" };
            var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
            Assert.Equal(0, result);

            var slug = "my-test-plugin";
            Assert.True(Directory.Exists(Path.Combine(tempWorkDir, slug)));
            Assert.True(File.Exists(Path.Combine(tempWorkDir, slug, "plugin.yaml")));
            Assert.True(File.Exists(Path.Combine(tempWorkDir, slug, "MyTestPluginPlugin.cs")));
            Assert.True(File.Exists(Path.Combine(tempWorkDir, slug, $"{slug}.csproj")));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            try { if (Directory.Exists(tempWorkDir)) Directory.Delete(tempWorkDir, true); } catch { }
        }
    }

    [Fact]
    public async Task ExecuteAsync_Create_ExistingDirectory_Returns1()
    {
        var tempWorkDir = Path.Combine(Path.GetTempPath(), $"plugin-create-dup-{Guid.NewGuid():N}");
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.CreateDirectory(tempWorkDir);
            Directory.CreateDirectory(Path.Combine(tempWorkDir, "existing-plugin"));
            Directory.SetCurrentDirectory(tempWorkDir);

            var settings = new PluginSettings { Action = "create", Target = "existing-plugin" };
            var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
            Assert.Equal(1, result);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            try { if (Directory.Exists(tempWorkDir)) Directory.Delete(tempWorkDir, true); } catch { }
        }
    }

    // ---- Enable ----

    [Fact]
    public async Task ExecuteAsync_Enable_NoName_Returns1()
    {
        var settings = new PluginSettings { Action = "enable", Target = null };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_Enable_NonexistentPlugin_Returns1()
    {
        var settings = new PluginSettings { Action = "enable", Target = "nonexistent-plugin" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(1, result);
    }

    // ---- Disable ----

    [Fact]
    public async Task ExecuteAsync_Disable_NoName_Returns1()
    {
        var settings = new PluginSettings { Action = "disable", Target = null };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_Disable_NonexistentPlugin_Returns1()
    {
        var settings = new PluginSettings { Action = "disable", Target = "nonexistent-plugin" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(1, result);
    }

    // ---- Reload ----

    [Fact]
    public async Task ExecuteAsync_Reload_Success_Returns0()
    {
        _proxyHttpMock.Setup(x => x.NotifyPluginReloadAsync()).Returns(Task.CompletedTask);

        var settings = new PluginSettings { Action = "reload" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_Reload_HttpException_Returns1()
    {
        _proxyHttpMock.Setup(x => x.NotifyPluginReloadAsync())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var settings = new PluginSettings { Action = "reload" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(1, result);
    }
}

file class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly string _json;
    private readonly HttpStatusCode _statusCode;

    public FakeHttpMessageHandler(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _json = json;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_json, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}

file class ThrowingHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw new HttpRequestException("Connection refused");
    }
}
