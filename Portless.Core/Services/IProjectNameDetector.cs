namespace Portless.Core.Services;

/// <summary>
/// Detects the project name from the current working directory.
/// Priority: .csproj AssemblyName > git root directory > cwd basename
/// </summary>
public interface IProjectNameDetector
{
    /// <summary>
    /// Detects the project name from the given working directory.
    /// </summary>
    string? DetectProjectName(string? workingDirectory = null);
}
