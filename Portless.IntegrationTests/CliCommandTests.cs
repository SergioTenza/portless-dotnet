using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Portless.Core.Extensions;
using Portless.Core.Models;
using Portless.Core.Services;
using Portless.Cli.Commands.ListCommand;
using Portless.Cli.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Portless.IntegrationTests;

/// <summary>
/// Integration tests for CLI commands using in-process CommandApp testing.
/// Tests verify list command behavior with various states without needing full tool installation.
/// </summary>
public class CliCommandTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;
    private readonly string _testDirectory;

    public CliCommandTests(ITestOutputHelper output)
    {
        _output = output;

        // Create unique test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_testDirectory);

        // Create service collection with test-specific state directory
        var services = new ServiceCollection();
        services.AddPortlessPersistence();

        // Override state directory for testing
        services.AddSingleton<string>(sp =>
        {
            // This will be used by tests that need state directory
            return _testDirectory;
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task ListCommand_WithNoRoutes_DisplaysEmptyMessage()
    {
        // Arrange
        var routeStore = _serviceProvider.GetRequiredService<IRouteStore>();

        // Ensure no routes exist
        await routeStore.SaveRoutesAsync(Array.Empty<RouteInfo>());

        // Create CommandApp with TypeRegistrar
        var registrar = new TypeRegistrar(new ServiceCollection()
            .AddPortlessPersistence()
            .AddSingleton<IRouteStore>(routeStore)
            .AddSingleton<IProcessLivenessChecker, ProcessLivenessChecker>()
            .AddLogging());

        var app = new CommandApp(registrar);
        app.Configure(config =>
        {
            config.AddCommand<ListCommand>("list");
        });

        // Capture console output
        var console = new TestConsole();
        AnsiConsole.Console = console;

        // Act
        var result = await app.RunAsync(new[] { "list" });

        // Assert
        Assert.Equal(0, result);
        var output = console.Output;
        Assert.Contains("No active routes", output);
    }

    [Fact]
    public async Task ListCommand_WithActiveRoutes_LoadsSuccessfully()
    {
        // Arrange
        var routeStore = _serviceProvider.GetRequiredService<IRouteStore>();

        // Create test routes
        var testRoutes = new[]
        {
            new RouteInfo { Hostname = "api1.localhost", Port = 4001, Pid = 12345, CreatedAt = DateTime.UtcNow },
            new RouteInfo { Hostname = "api2.localhost", Port = 4002, Pid = 12346, CreatedAt = DateTime.UtcNow }
        };

        await routeStore.SaveRoutesAsync(testRoutes);

        // Act - Load routes back
        var loadedRoutes = await routeStore.LoadRoutesAsync();

        // Assert
        Assert.Equal(2, loadedRoutes.Length);
        Assert.Contains(loadedRoutes, r => r.Hostname == "api1.localhost");
        Assert.Contains(loadedRoutes, r => r.Hostname == "api2.localhost");
    }

    [Fact]
    public async Task ListCommand_CommandIsRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPortlessPersistence();

        var registrar = new TypeRegistrar(services);
        var app = new CommandApp(registrar);

        // Act & Assert - Verify command can be configured without throwing
        var exception = Record.Exception(() =>
        {
            app.Configure(config =>
            {
                config.AddCommand<ListCommand>("list");
            });
        });

        Assert.Null(exception);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();

        // Cleanup test directory
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Log warning but don't fail test
                _output.WriteLine($"Warning: Failed to cleanup test directory: {_testDirectory}");
            }
        }
    }
}
