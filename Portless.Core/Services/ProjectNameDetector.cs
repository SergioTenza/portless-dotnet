using System.Xml.Linq;

namespace Portless.Core.Services;

public class ProjectNameDetector : IProjectNameDetector
{
    public string? DetectProjectName(string? workingDirectory = null)
    {
        var dir = workingDirectory ?? Directory.GetCurrentDirectory();

        // Priority 1: .csproj AssemblyName or PackageId
        var csprojName = DetectFromCsproj(dir);
        if (!string.IsNullOrEmpty(csprojName)) return SanitizeName(csprojName);

        // Priority 2: Directory.Build.props ProjectName
        var dbpName = DetectFromDirectoryBuildProps(dir);
        if (!string.IsNullOrEmpty(dbpName)) return SanitizeName(dbpName);

        // Priority 3: Git repository root directory name
        var gitName = DetectFromGitRoot(dir);
        if (!string.IsNullOrEmpty(gitName)) return SanitizeName(gitName);

        // Priority 4: Current working directory basename
        return SanitizeName(new DirectoryInfo(dir).Name);
    }

    private static string? DetectFromCsproj(string dir)
    {
        var csprojFiles = Directory.GetFiles(dir, "*.csproj");
        if (csprojFiles.Length == 0)
        {
            // Search one level down (common for src/Project pattern)
            foreach (var subDir in Directory.GetDirectories(dir))
            {
                csprojFiles = Directory.GetFiles(subDir, "*.csproj");
                if (csprojFiles.Length > 0) break;
            }
        }

        if (csprojFiles.Length == 0) return null;

        // If single csproj, use it; otherwise use directory name
        if (csprojFiles.Length == 1)
        {
            return ExtractNameFromCsproj(csprojFiles[0]);
        }

        // Multiple csprojs - look for one with web SDK
        foreach (var csproj in csprojFiles)
        {
            var name = ExtractNameFromCsproj(csproj);
            if (name != null && IsWebProject(csproj)) return name;
        }

        return null;
    }

    private static string? ExtractNameFromCsproj(string csprojPath)
    {
        try
        {
            var doc = XDocument.Load(csprojPath);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            // Check for <AssemblyName>
            var assemblyName = doc.Root?.Element(ns + "PropertyGroup")?.Element(ns + "AssemblyName")?.Value;
            if (!string.IsNullOrEmpty(assemblyName)) return assemblyName;

            // Check for <PackageId>
            var packageId = doc.Root?.Element(ns + "PropertyGroup")?.Element(ns + "PackageId")?.Value;
            if (!string.IsNullOrEmpty(packageId)) return packageId;

            // Fall back to file name without extension
            return Path.GetFileNameWithoutExtension(csprojPath);
        }
        catch
        {
            return Path.GetFileNameWithoutExtension(csprojPath);
        }
    }

    private static bool IsWebProject(string csprojPath)
    {
        try
        {
            var content = File.ReadAllText(csprojPath);
            return content.Contains("Microsoft.AspNetCore") || content.Contains("Microsoft.NET.Sdk.Web");
        }
        catch
        {
            return false;
        }
    }

    private static string? DetectFromDirectoryBuildProps(string dir)
    {
        var dbpPath = Path.Combine(dir, "Directory.Build.props");
        if (!File.Exists(dbpPath)) return null;

        try
        {
            var doc = XDocument.Load(dbpPath);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
            return doc.Root?.Element(ns + "PropertyGroup")?.Element(ns + "ProjectName")?.Value;
        }
        catch
        {
            return null;
        }
    }

    private static string? DetectFromGitRoot(string dir)
    {
        try
        {
            var gitDir = FindGitDirectory(dir);
            if (gitDir == null) return null;

            // The git root is the parent of .git
            return new DirectoryInfo(gitDir).Name;
        }
        catch
        {
            return null;
        }
    }

    private static string? FindGitDirectory(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }

    private static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;

        // Remove common prefixes/suffixes
        name = name.Trim();

        // Convert to lowercase, replace dots and spaces with hyphens
        name = name.Replace('.', '-').Replace(' ', '-').ToLowerInvariant();

        // Remove characters that aren't valid in hostnames
        var sanitized = new System.Text.StringBuilder();
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '-')
            {
                sanitized.Append(c);
            }
        }

        var result = sanitized.ToString().Trim('-');

        // Remove trailing "-dll" or "-exe" from project names
        result = System.Text.RegularExpressions.Regex.Replace(result, @"-(dll|exe)$", "");

        return result;
    }
}
