extern alias Cli;
using Moq;
using Spectre.Console.Cli;
using Xunit;
using CompletionCommand = Cli::Portless.Cli.Commands.CompletionCommand.CompletionCommand;
using CompletionSettings = Cli::Portless.Cli.Commands.CompletionCommand.CompletionSettings;

namespace Portless.Tests.CliCommands;

public class CompletionCommandTests
{
    private readonly CompletionCommand _command;

    public CompletionCommandTests()
    {
        _command = new CompletionCommand();
    }

    [Fact]
    public async Task ExecuteAsync_Bash_Returns0()
    {
        var settings = new CompletionSettings { Shell = "bash" };
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_Zsh_Returns0()
    {
        var settings = new CompletionSettings { Shell = "zsh" };
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_Fish_Returns0()
    {
        var settings = new CompletionSettings { Shell = "fish" };
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_PowerShell_Returns0()
    {
        var settings = new CompletionSettings { Shell = "powershell" };
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownShell_Returns1()
    {
        var settings = new CompletionSettings { Shell = "unknown" };
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_NullShell_DefaultsToBash_Returns0()
    {
        var settings = new CompletionSettings { Shell = null };
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_CaseInsensitive_BASH_Returns0()
    {
        var settings = new CompletionSettings { Shell = "BASH" };
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);
        Assert.Equal(0, result);
    }
}
