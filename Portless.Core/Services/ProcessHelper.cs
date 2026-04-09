using System.Diagnostics;

namespace Portless.Core.Services;

/// <summary>
/// Result of an external process execution.
/// </summary>
public sealed class ProcessResult
{
    public int ExitCode { get; init; }
    public string StandardOutput { get; init; } = string.Empty;
    public string StandardError { get; init; } = string.Empty;

    public bool Success => ExitCode == 0;
}

/// <summary>
/// Helper for running external processes with standardized output capture.
/// Eliminates the duplicated ProcessStartInfo + ReadToEndAsync + WaitForExitAsync
/// boilerplate across CertificateTrustServiceMacOS, CertificateTrustServiceLinux,
/// and PlatformDetectorService.
/// </summary>
public static class ProcessHelper
{
    /// <summary>
    /// Runs an external process and captures its output.
    /// </summary>
    /// <param name="fileName">The executable to run.</param>
    /// <param name="arguments">Command-line arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A ProcessResult with exit code, stdout, and stderr.</returns>
    public static async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = output,
            StandardError = error
        };
    }

    /// <summary>
    /// Runs an external process synchronously and returns the trimmed stdout.
    /// Convenience overload for simple commands (e.g. "id -u").
    /// </summary>
    public static string? RunToString(string fileName, string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();

        return process.ExitCode == 0 ? output : null;
    }
}
