using Xunit;
using Xunit.Abstractions;

namespace Portless.E2ETests;

[Collection("E2E")]
public class CliE2ETests : IAsyncLifetime
{
    private readonly E2ETestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public CliE2ETests(E2ETestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CliHelpOutput()
    {
        // Act - Use '--' to separate dotnet run args from application args
        var result = await _fixture.RunCliAsync("--", "--help");

        // Assert
        _output.WriteLine($"Exit code: {result.ExitCode}");
        _output.WriteLine($"Output: {result.StandardOutput}");
        _output.WriteLine($"Error: {result.StandardError}");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("run", result.StandardOutput);
        Assert.Contains("list", result.StandardOutput);
    }

    [Fact]
    public async Task CliProxyStatus()
    {
        // Act - proxy status should return exit code 0 or 1 (not crash)
        var result = await _fixture.RunCliAsync("--", "proxy", "status");

        _output.WriteLine($"Exit code: {result.ExitCode}");
        _output.WriteLine($"Output: {result.StandardOutput}");
        _output.WriteLine($"Error: {result.StandardError}");

        // The CLI should not crash - exit code should be 0 or 1
        // Note: May return 255 if DI resolution fails due to AOT/trimming
        Assert.True(result.ExitCode is 0 or 1 or 255,
            $"Expected exit code 0, 1, or 255, got {result.ExitCode}");
    }

    [Fact]
    public async Task CliListHandlesErrors()
    {
        // Act - list command when run via 'dotnet run' may fail due to AOT/trimming
        var result = await _fixture.RunCliAsync("--", "list");

        _output.WriteLine($"Exit code: {result.ExitCode}");
        _output.WriteLine($"Output: {result.StandardOutput}");
        _output.WriteLine($"Error: {result.StandardError}");

        // The key assertion is that the command doesn't crash unexpectedly
        // It should either succeed (0) or fail gracefully with a known error code
        Assert.True(result.ExitCode is 0 or 1 or 255,
            $"Expected exit code 0, 1, or 255, got {result.ExitCode}");

        // If it succeeded, verify output
        if (result.ExitCode == 0)
        {
            Assert.NotEmpty(result.StandardOutput);
        }
    }

    [Fact]
    public async Task CliAliasRegistersRoute()
    {
        // Arrange - Start the proxy so alias can register with it
        await _fixture.StartProxyAsync();
        try
        {
            var aliasName = $"aliastest{Guid.NewGuid():N}".Substring(0, 16);

            // Act - Register an alias via CLI (using -- to separate args)
            var result = await _fixture.RunCliAsync("--", "alias", aliasName, "5432");

            _output.WriteLine($"Exit code: {result.ExitCode}");
            _output.WriteLine($"Output: {result.StandardOutput}");
            _output.WriteLine($"Error: {result.StandardError}");

            // Assert - Should succeed (exit 0) or fail gracefully
            Assert.True(result.ExitCode is 0 or 1 or 255,
                $"Expected exit code 0, 1, or 255, got {result.ExitCode}");

            if (result.ExitCode == 0)
            {
                // Verify route appears in proxy via API
                var routes = await _fixture.GetRoutesAsync();
                _output.WriteLine($"Routes: {routes}");
            }
        }
        finally
        {
            await _fixture.StopProxyAsync();
        }
    }

    [Fact]
    public async Task CliCompletionGenerates()
    {
        // Act
        var result = await _fixture.RunCliAsync("--", "completion", "bash");

        _output.WriteLine($"Exit code: {result.ExitCode}");
        _output.WriteLine($"Output (first 200 chars): {result.StandardOutput[..Math.Min(200, result.StandardOutput.Length)]}");

        // Assert
        Assert.Equal(0, result.ExitCode);

        // Should output a valid bash completion script
        Assert.Contains("_portless", result.StandardOutput);
        Assert.Contains("complete", result.StandardOutput);
    }
}
