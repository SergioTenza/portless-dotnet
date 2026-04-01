namespace Portless.Core.Services;

/// <summary>
/// Process manager interface for spawning and tracking application processes.
/// </summary>
public interface IProcessManager
{
    /// <summary>
    /// Starts a managed process with the specified command, arguments, and PORT variable injection.
    /// </summary>
    /// <param name="command">The command to execute (e.g., "dotnet", "node").</param>
    /// <param name="args">The command arguments (e.g., "run --project MyApi.csproj").</param>
    /// <param name="port">The port number to inject via the PORT environment variable.</param>
    /// <param name="workingDirectory">The working directory for the process execution.</param>
    /// <returns>The started Process object with valid PID for tracking.</returns>
    /// <exception cref="InvalidOperationException">Thrown when process fails to start.</exception>
    System.Diagnostics.Process StartManagedProcess(string command, string args, int port, string workingDirectory);

    /// <summary>
    /// Starts a managed process with additional environment variables for framework-specific injection.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="args">The command arguments.</param>
    /// <param name="port">The port number to inject via PORT env var.</param>
    /// <param name="workingDirectory">The working directory for the process.</param>
    /// <param name="additionalEnvVars">Additional environment variables to inject (e.g., ASPNETCORE_URLS, PORTLESS_URL).</param>
    /// <returns>The started Process object with valid PID for tracking.</returns>
    /// <exception cref="InvalidOperationException">Thrown when process fails to start.</exception>
    System.Diagnostics.Process StartManagedProcess(string command, string args, int port, string workingDirectory, Dictionary<string, string> additionalEnvVars);

    /// <summary>
    /// Gets the current status of a process by its PID.
    /// </summary>
    /// <param name="pid">The process ID to check.</param>
    /// <returns>A ProcessStatus record containing running state, start time, and exit time.</returns>
    Task<ProcessStatus> GetProcessStatusAsync(int pid);
}

/// <summary>
/// Process status information returned by process health checks.
/// </summary>
/// <param name="IsRunning">True if the process is currently running; false if it has exited.</param>
/// <param name="StartTime">The process start time, if available.</param>
/// <param name="ExitTime">The process exit time, if the process has exited.</param>
public record ProcessStatus(bool IsRunning, DateTime? StartTime, DateTime? ExitTime);
