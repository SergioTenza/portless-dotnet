using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Portless.Core.Services;

/// <summary>
/// Process manager implementation for spawning background processes with PORT injection.
/// </summary>
public class ProcessManager : IProcessManager
{
    private readonly ILogger<ProcessManager> _logger;

    public ProcessManager(ILogger<ProcessManager> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Process StartManagedProcess(string command, string args, int port, string workingDirectory)
    {
        _logger.LogDebug("Starting process: {Command} {Args} with PORT={Port} in {WorkingDirectory}",
            command, args, port, workingDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = args,
            UseShellExecute = false,  // Required for environment variable injection
            RedirectStandardOutput = false,  // Inherit stdout for real-time visibility
            RedirectStandardError = false,   // Inherit stderr for real-time visibility
            CreateNoWindow = true,          // Background execution
            WorkingDirectory = workingDirectory
        };

        // Inject PORT environment variable (preserves existing environment)
        startInfo.Environment["PORT"] = port.ToString();

        _logger.LogDebug("Process start info configured: UseShellExecute={UseShellExecute}, CreateNoWindow={CreateNoWindow}",
            startInfo.UseShellExecute, startInfo.CreateNoWindow);

        var process = Process.Start(startInfo);
        if (process == null)
        {
            _logger.LogError("Failed to start process: {Command} {Args}", command, args);
            throw new InvalidOperationException($"Failed to start process: {command} {args}");
        }

        _logger.LogInformation("Started process {Pid}: {Command} {Args} with PORT={Port}",
            process.Id, command, args, port);

        return process;
    }

    /// <inheritdoc />
    public Task<ProcessStatus> GetProcessStatusAsync(int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            var status = new ProcessStatus(
                IsRunning: !process.HasExited,
                StartTime: process.StartTime,
                ExitTime: process.HasExited ? process.ExitTime : null
            );

            _logger.LogDebug("Process {Pid} status: IsRunning={IsRunning}, StartTime={StartTime}",
                pid, status.IsRunning, status.StartTime);

            return Task.FromResult(status);
        }
        catch (ArgumentException)
        {
            // PID doesn't exist
            _logger.LogDebug("Process {Pid} does not exist", pid);
            return Task.FromResult(new ProcessStatus(IsRunning: false, StartTime: null, ExitTime: null));
        }
    }
}
