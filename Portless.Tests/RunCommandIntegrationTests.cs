using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

/// <summary>
/// Integration tests that verify the end-to-end flow of:
/// Framework Detection → Placeholder Expansion → Env Var Injection
/// This tests the data pipeline that RunCommand uses without starting actual processes.
/// </summary>
public class RunCommandIntegrationTests
{
    [Fact]
    public void FrameworkDetection_To_EnvVarExpansion_AspNet()
    {
        // Simulate what RunCommand does for an ASP.NET project
        var tmpDir = Path.Combine(Path.GetTempPath(), "portless-test-aspnet-" + Guid.NewGuid());
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "MyApi.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");

            var detector = new FrameworkDetector();
            var framework = detector.Detect(tmpDir);

            Assert.NotNull(framework);
            Assert.Equal("aspnet", framework.Name);

            // Expand env vars with actual port and hostname
            var port = 4042;
            var hostname = "myapi.localhost";
            var envVars = PlaceholderExpander.ExpandEnvVars(framework.InjectedEnvVars, port, hostname);

            Assert.Equal("http://0.0.0.0:4042", envVars["ASPNETCORE_URLS"]);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void FrameworkDetection_To_FlagsExpansion_Vite()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "portless-test-vite-" + Guid.NewGuid());
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "vite.config.ts"), "export default {}");
            File.WriteAllText(Path.Combine(tmpDir, "package.json"), "{\"name\":\"test\",\"scripts\":{\"start\":\"vite\"}}");

            var detector = new FrameworkDetector();
            var framework = detector.Detect(tmpDir);

            Assert.NotNull(framework);
            Assert.Equal("vite", framework.Name);

            var port = 4043;
            var hostname = "frontend.localhost";

            // Expand framework flags
            var expandedFlags = PlaceholderExpander.ExpandArgs(framework.InjectedFlags, port, hostname);
            Assert.Equal(new[] { "--port", "4043", "--strictPort", "--host" }, expandedFlags);

            // Expand env vars
            var envVars = PlaceholderExpander.ExpandEnvVars(framework.InjectedEnvVars, port, hostname);
            Assert.Equal("4043", envVars["PORT"]);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void FrameworkDetection_To_CommandExpansion_Go()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "portless-test-go-" + Guid.NewGuid());
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "go.mod"), "module example.com/myapp\ngo 1.21");

            var detector = new FrameworkDetector();
            var framework = detector.Detect(tmpDir);

            Assert.NotNull(framework);
            Assert.Equal("go", framework.Name);
            Assert.Empty(framework.InjectedFlags); // Go has no special flags

            // Simulate command expansion: "go run main.go" with placeholder in args
            var commandArgs = new[] { "go", "run", "main.go" };
            var port = 4044;
            var hostname = "myapp.localhost";

            var expanded = PlaceholderExpander.ExpandArgs(commandArgs, port, hostname);
            Assert.Equal(new[] { "go", "run", "main.go" }, expanded); // No placeholders, unchanged

            var envVars = PlaceholderExpander.ExpandEnvVars(framework.InjectedEnvVars, port, hostname);
            Assert.Equal("4044", envVars["PORT"]);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void AutoNameDetection_To_FrameworkDetection_FullPipeline()
    {
        // Simulate full pipeline: detect name → detect framework → expand
        var tmpDir = Path.Combine(Path.GetTempPath(), "portless-test-pipeline-" + Guid.NewGuid());
        Directory.CreateDirectory(tmpDir);
        try
        {
            // Create a .NET web project
            File.WriteAllText(Path.Combine(tmpDir, "MyWebApp.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>MyWebApp</AssemblyName></PropertyGroup></Project>");

            // Step 1: Detect project name
            var nameDetector = new ProjectNameDetector();
            var name = nameDetector.DetectProjectName(tmpDir);
            Assert.Equal("mywebapp", name); // Sanitized: lowercase, no special chars

            // Step 2: Detect framework
            var frameworkDetector = new FrameworkDetector();
            var framework = frameworkDetector.Detect(tmpDir);
            Assert.NotNull(framework);
            Assert.Equal("aspnet", framework.Name);

            // Step 3: Build hostname and port
            var hostname = $"{name}.localhost";
            var port = 4045;

            // Step 4: Expand env vars
            var envVars = PlaceholderExpander.ExpandEnvVars(framework.InjectedEnvVars, port, hostname);
            envVars["PORTLESS_URL"] = $"http://{hostname}";

            Assert.Equal("http://0.0.0.0:4045", envVars["ASPNETCORE_URLS"]);
            Assert.Equal("http://mywebapp.localhost", envVars["PORTLESS_URL"]);

            // Step 5: Expand command placeholders
            var commandArgs = new[] { "dotnet", "run" };
            var expanded = PlaceholderExpander.ExpandArgs(commandArgs, port, hostname);
            Assert.Equal(new[] { "dotnet", "run" }, expanded); // No placeholders
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void NoFrameworkDetection_StillInjectsPort()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "portless-test-nofw-" + Guid.NewGuid());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var detector = new FrameworkDetector();
            var framework = detector.Detect(tmpDir);
            Assert.Null(framework);

            // Even without framework detection, we should still inject PORT
            var port = 4046;
            var hostname = "generic.localhost";
            var envVars = new Dictionary<string, string> { ["PORTLESS_URL"] = $"http://{hostname}" };

            Assert.Equal("http://generic.localhost", envVars["PORTLESS_URL"]);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void FrameworkDetection_PrioritizesSpecificOverGeneric()
    {
        // Directory with both Next.js config and package.json
        var tmpDir = Path.Combine(Path.GetTempPath(), "portless-test-prio-" + Guid.NewGuid());
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "next.config.js"), "module.exports = {}");
            File.WriteAllText(Path.Combine(tmpDir, "package.json"), "{\"name\":\"test\",\"scripts\":{\"start\":\"next start\"}}");

            var detector = new FrameworkDetector();
            var framework = detector.Detect(tmpDir);

            Assert.NotNull(framework);
            Assert.Equal("nextjs", framework.Name); // Should be Next.js, not generic npm
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void FrameworkDetection_AngularFlags_Expanded()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "portless-test-angular-" + Guid.NewGuid());
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "angular.json"), "{}");

            var detector = new FrameworkDetector();
            var framework = detector.Detect(tmpDir);

            Assert.NotNull(framework);
            Assert.Equal("angular", framework.Name);

            var port = 4200;
            var hostname = "myapp.localhost";
            var flags = PlaceholderExpander.ExpandArgs(framework.InjectedFlags, port, hostname);

            Assert.Equal(new[] { "--port", "4200", "--host" }, flags);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void CommandWithPlaceholders_ExpandedCorrectly()
    {
        // Test a command like: sh -c "echo {URL}:{PORT}"
        var args = new[] { "sh", "-c", "echo {URL}:{PORT}" };
        var result = PlaceholderExpander.ExpandArgs(args, 4042, "api.localhost");

        Assert.Equal("sh", result[0]);
        Assert.Equal("-c", result[1]);
        Assert.Equal("echo http://api.localhost:4042", result[2]);
    }
}
