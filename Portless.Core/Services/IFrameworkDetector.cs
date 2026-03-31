namespace Portless.Core.Services;

/// <summary>
/// Represents a detected framework and its configuration.
/// </summary>
public class DetectedFramework
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string[] InjectedFlags { get; init; } = [];
    public string[] InjectedEnvVars { get; init; } = [];
}

/// <summary>
/// Detects the framework used in a project directory and provides
/// the appropriate port/host injection flags and environment variables.
/// </summary>
public interface IFrameworkDetector
{
    /// <summary>
    /// Detects the framework from the given working directory.
    /// Returns null if no known framework is detected.
    /// </summary>
    DetectedFramework? Detect(string? workingDirectory = null);
}
