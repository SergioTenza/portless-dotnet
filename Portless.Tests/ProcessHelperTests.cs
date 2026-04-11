using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class ProcessHelperTests
{
    [Fact]
    public async Task RunAsync_SuccessfulCommand_ReturnsCorrectOutput()
    {
        // Act
        var result = await ProcessHelper.RunAsync("echo", "hello world");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("hello world", result.StandardOutput);
        Assert.Empty(result.StandardError);
    }

    [Fact]
    public async Task RunAsync_FailingCommand_ReturnsNonZeroExitCode()
    {
        // Act
        var result = await ProcessHelper.RunAsync("false", "");

        // Assert
        Assert.False(result.Success);
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task RunAsync_CommandWithStderr_CapturesStderr()
    {
        // Act
        var result = await ProcessHelper.RunAsync("bash", "-c \"echo error >&2 && exit 1\"");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("error", result.StandardError);
    }

    [Fact]
    public async Task RunAsync_WithCancellation_ThrowsOperationCanceled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ProcessHelper.RunAsync("sleep", "10", cts.Token));
    }

    [Fact]
    public async Task RunAsync_InvalidCommand_Throws()
    {
        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            ProcessHelper.RunAsync("nonexistent_command_12345", ""));
    }

    [Fact]
    public void ProcessResult_Success_PropertyReturnsTrue()
    {
        var result = new ProcessResult { ExitCode = 0, StandardOutput = "out", StandardError = "err" };
        Assert.True(result.Success);
    }

    [Fact]
    public void ProcessResult_Failure_PropertyReturnsFalse()
    {
        var result = new ProcessResult { ExitCode = 1, StandardOutput = "", StandardError = "error" };
        Assert.False(result.Success);
    }

    [Fact]
    public void ProcessResult_DefaultValues_AreCorrect()
    {
        var result = new ProcessResult();
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.StandardOutput);
        Assert.Equal(string.Empty, result.StandardError);
        Assert.True(result.Success);
    }

    [Fact]
    public void RunToString_SuccessfulCommand_ReturnsOutput()
    {
        var result = ProcessHelper.RunToString("echo", "test-output");
        Assert.Equal("test-output", result);
    }

    [Fact]
    public void RunToString_FailingCommand_ReturnsNull()
    {
        var result = ProcessHelper.RunToString("false", "");
        Assert.Null(result);
    }

    [Fact]
    public void RunToString_TrimsOutput()
    {
        var result = ProcessHelper.RunToString("echo", "  trimmed  ");
        Assert.Equal("trimmed", result);
    }

    [Fact]
    public async Task RunAsync_MultiLineOutput_CapturesAllLines()
    {
        var result = await ProcessHelper.RunAsync("bash", "-c \"echo line1 && echo line2 && echo line3\"");
        Assert.True(result.Success);
        Assert.Contains("line1", result.StandardOutput);
        Assert.Contains("line2", result.StandardOutput);
        Assert.Contains("line3", result.StandardOutput);
    }

    [Fact]
    public async Task RunAsync_CapturesBothStdoutAndStderr()
    {
        var result = await ProcessHelper.RunAsync("bash", "-c \"echo stdout_msg && echo stderr_msg >&2\"");
        Assert.True(result.Success);
        Assert.Contains("stdout_msg", result.StandardOutput);
        Assert.Contains("stderr_msg", result.StandardError);
    }
}
