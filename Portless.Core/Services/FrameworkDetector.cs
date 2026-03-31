using System.Text.RegularExpressions;

namespace Portless.Core.Services;

public class FrameworkDetector : IFrameworkDetector
{
    public DetectedFramework? Detect(string? workingDirectory = null)
    {
        var dir = workingDirectory ?? Directory.GetCurrentDirectory();

        // Detection order matters - more specific first
        return DetectAspNet(dir)
            ?? DetectVite(dir)
            ?? DetectNextJs(dir)
            ?? DetectAstro(dir)
            ?? DetectAngular(dir)
            ?? DetectExpo(dir)
            ?? DetectReactNative(dir)
            ?? DetectNpm(dir)
            ?? DetectPython(dir)
            ?? DetectGo(dir)
            ?? DetectRust(dir);
    }

    private static DetectedFramework? DetectAspNet(string dir)
    {
        var csprojFiles = Directory.GetFiles(dir, "*.csproj");
        if (csprojFiles.Length == 0)
        {
            // Check subdirectories
            foreach (var subDir in Directory.GetDirectories(dir))
            {
                csprojFiles = [.. Directory.GetFiles(subDir, "*.csproj")];
                if (csprojFiles.Length > 0) break;
            }
        }

        if (csprojFiles.Length == 0) return null;

        foreach (var csproj in csprojFiles)
        {
            try
            {
                var content = File.ReadAllText(csproj);
                if (content.Contains("Microsoft.AspNetCore") || content.Contains("Microsoft.NET.Sdk.Web"))
                {
                    return new DetectedFramework
                    {
                        Name = "aspnet",
                        DisplayName = "ASP.NET Core",
                        InjectedFlags = [],
                        InjectedEnvVars = [$"ASPNETCORE_URLS=http://0.0.0.0:{{PORT}}"]
                    };
                }
            }
            catch { continue; }
        }

        // Generic .NET project (not web)
        if (csprojFiles.Length > 0)
        {
            return new DetectedFramework
            {
                Name = "dotnet",
                DisplayName = ".NET",
                InjectedFlags = [],
                InjectedEnvVars = [$"PORT={{PORT}}"]
            };
        }

        return null;
    }

    private static DetectedFramework? DetectVite(string dir)
    {
        if (!HasFile(dir, "vite.config.*")) return null;

        return new DetectedFramework
        {
            Name = "vite",
            DisplayName = "Vite",
            InjectedFlags = ["--port", "{PORT}", "--strictPort", "--host"],
            InjectedEnvVars = [$"PORT={{PORT}}"]
        };
    }

    private static DetectedFramework? DetectNextJs(string dir)
    {
        if (!HasFile(dir, "next.config.*")) return null;

        return new DetectedFramework
        {
            Name = "nextjs",
            DisplayName = "Next.js",
            InjectedFlags = [],
            InjectedEnvVars = [$"PORT={{PORT}}"]
        };
    }

    private static DetectedFramework? DetectAstro(string dir)
    {
        if (!HasFile(dir, "astro.config.*")) return null;

        return new DetectedFramework
        {
            Name = "astro",
            DisplayName = "Astro",
            InjectedFlags = ["--port", "{PORT}", "--host"],
            InjectedEnvVars = [$"PORT={{PORT}}"]
        };
    }

    private static DetectedFramework? DetectAngular(string dir)
    {
        if (!HasFile(dir, "angular.json")) return null;

        return new DetectedFramework
        {
            Name = "angular",
            DisplayName = "Angular",
            InjectedFlags = ["--port", "{PORT}", "--host"],
            InjectedEnvVars = [$"PORT={{PORT}}"]
        };
    }

    private static DetectedFramework? DetectExpo(string dir)
    {
        var packageJson = ReadPackageJson(dir);
        if (packageJson == null) return null;

        if (!packageJson.Contains("\"expo\"")) return null;

        return new DetectedFramework
        {
            Name = "expo",
            DisplayName = "Expo",
            InjectedFlags = ["--port", "{PORT}", "--host", "localhost"],
            InjectedEnvVars = [$"PORT={{PORT}}"]
        };
    }

    private static DetectedFramework? DetectReactNative(string dir)
    {
        if (!HasFile(dir, "metro.config.*")) return null;

        return new DetectedFramework
        {
            Name = "react-native",
            DisplayName = "React Native",
            InjectedFlags = ["--port", "{PORT}", "--host", "localhost"],
            InjectedEnvVars = [$"PORT={{PORT}}"]
        };
    }

    private static DetectedFramework? DetectNpm(string dir)
    {
        var packageJson = ReadPackageJson(dir);
        if (packageJson == null) return null;

        // Check if it has a start script
        if (!packageJson.Contains("\"start\"")) return null;

        return new DetectedFramework
        {
            Name = "npm",
            DisplayName = "Node.js",
            InjectedFlags = [],
            InjectedEnvVars = [$"PORT={{PORT}}"]
        };
    }

    private static DetectedFramework? DetectPython(string dir)
    {
        if (!HasFile(dir, "requirements.txt") && !HasFile(dir, "pyproject.toml") &&
            !HasFile(dir, "Pipfile") && !HasFile(dir, "setup.py")) return null;

        return new DetectedFramework
        {
            Name = "python",
            DisplayName = "Python",
            InjectedFlags = [],
            InjectedEnvVars = [$"PORT={{PORT}}"]
        };
    }

    private static DetectedFramework? DetectGo(string dir)
    {
        if (!HasFile(dir, "go.mod")) return null;

        return new DetectedFramework
        {
            Name = "go",
            DisplayName = "Go",
            InjectedFlags = [],
            InjectedEnvVars = [$"PORT={{PORT}}"]
        };
    }

    private static DetectedFramework? DetectRust(string dir)
    {
        if (!HasFile(dir, "Cargo.toml")) return null;

        return new DetectedFramework
        {
            Name = "rust",
            DisplayName = "Rust",
            InjectedFlags = [],
            InjectedEnvVars = [$"PORT={{PORT}}"]
        };
    }

    private static bool HasFile(string dir, string globPattern)
    {
        var (directory, pattern) = SplitGlob(globPattern);
        var searchDir = Path.Combine(dir, directory);
        if (!Directory.Exists(searchDir)) return false;

        var regex = new Regex("^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$", RegexOptions.IgnoreCase);
        try
        {
            return Directory.GetFiles(searchDir).Any(f => regex.IsMatch(Path.GetFileName(f)));
        }
        catch
        {
            return false;
        }
    }

    private static (string dir, string pattern) SplitGlob(string glob)
    {
        var parts = glob.Split('/');
        if (parts.Length == 1) return (".", parts[0]);
        return (string.Join("/", parts[..^1]), parts[^1]);
    }

    private static string? ReadPackageJson(string dir)
    {
        var path = Path.Combine(dir, "package.json");
        if (!File.Exists(path)) return null;
        try { return File.ReadAllText(path); }
        catch { return null; }
    }
}
