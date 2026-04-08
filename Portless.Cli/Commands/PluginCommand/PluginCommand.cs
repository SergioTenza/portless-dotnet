using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http.Json;

namespace Portless.Cli.Commands.PluginCommand;

public sealed class PluginSettings : CommandSettings
{
    [CommandArgument(0, "[action]")]
    public string? Action { get; set; }

    [CommandArgument(1, "[target]")]
    public string? Target { get; set; }
}

public sealed class PluginCommand : AsyncCommand<PluginSettings>
{
    private static readonly string ProxyBaseUrl = $"http://localhost:{Environment.GetEnvironmentVariable("PORTLESS_PORT") ?? "1355"}";

    public override async Task<int> ExecuteAsync(CommandContext context, PluginSettings settings, CancellationToken cancellationToken)
    {
        return settings.Action?.ToLowerInvariant() switch
        {
            "list" or "ls" => await ListPlugins(),
            "install" => await InstallPlugin(settings.Target),
            "uninstall" or "remove" => await UninstallPlugin(settings.Target),
            "create" => await CreatePlugin(settings.Target),
            "reload" => await ReloadPlugins(),
            _ => ShowHelp()
        };
    }

    private async Task<int> ListPlugins()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await http.GetAsync($"{ProxyBaseUrl}/api/v1/plugins");
            response.EnsureSuccessStatusCode();

            var plugins = await response.Content.ReadFromJsonAsync<List<PluginInfo>>();
            if (plugins == null || plugins.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No plugins loaded.[/]");
                return 0;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold]Loaded Plugins[/]")
                .AddColumn("Name")
                .AddColumn("Version");

            foreach (var p in plugins)
            {
                table.AddRow(
                    Markup.Escape(p.Name),
                    Markup.Escape(p.Version)
                );
            }

            AnsiConsole.Write(table);
            return 0;
        }
        catch (HttpRequestException)
        {
            AnsiConsole.MarkupLine("[red]Error: Cannot connect to proxy. Is it running?[/]");
            return 1;
        }
    }

    private async Task<int> InstallPlugin(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            AnsiConsole.MarkupLine("[red]Error: Specify a plugin directory path to install.[/]");
            AnsiConsole.MarkupLine("[dim]Usage: portless plugin install /path/to/plugin[/]");
            return 1;
        }

        if (!Directory.Exists(path))
        {
            AnsiConsole.MarkupLine($"[red]Error: Directory not found: {Markup.Escape(path)}[/]");
            return 1;
        }

        var stateDir = Environment.GetEnvironmentVariable("PORTLESS_STATE_DIR")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".portless");
        var pluginsDir = Path.Combine(stateDir, "plugins");
        Directory.CreateDirectory(pluginsDir);

        var pluginName = new DirectoryInfo(path).Name;
        var destDir = Path.Combine(pluginsDir, pluginName);

        if (Directory.Exists(destDir))
        {
            Directory.Delete(destDir, true);
        }

        CopyDirectory(path, destDir);

        // Reload plugins
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        await http.PostAsync($"{ProxyBaseUrl}/api/v1/plugins/reload", null);

        AnsiConsole.MarkupLine($"[green]Plugin '{Markup.Escape(pluginName)}' installed successfully.[/]");
        return 0;
    }

    private async Task<int> UninstallPlugin(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            AnsiConsole.MarkupLine("[red]Error: Specify a plugin name to uninstall.[/]");
            return 1;
        }

        var stateDir = Environment.GetEnvironmentVariable("PORTLESS_STATE_DIR")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".portless");
        var pluginDir = Path.Combine(stateDir, "plugins", name);

        if (!Directory.Exists(pluginDir))
        {
            AnsiConsole.MarkupLine($"[red]Error: Plugin '{Markup.Escape(name)}' not found.[/]");
            return 1;
        }

        Directory.Delete(pluginDir, true);

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        await http.PostAsync($"{ProxyBaseUrl}/api/v1/plugins/reload", null);

        AnsiConsole.MarkupLine($"[green]Plugin '{Markup.Escape(name)}' uninstalled.[/]");
        return 0;
    }

    private async Task<int> CreatePlugin(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            AnsiConsole.MarkupLine("[red]Error: Specify a plugin name to create.[/]");
            AnsiConsole.MarkupLine("[dim]Usage: portless plugin create my-plugin[/]");
            return 1;
        }

        var slug = name.ToLowerInvariant().Replace(" ", "-");
        var dir = Path.Combine(Directory.GetCurrentDirectory(), slug);
        if (Directory.Exists(dir))
        {
            AnsiConsole.MarkupLine($"[red]Error: Directory '{slug}' already exists.[/]");
            return 1;
        }

        Directory.CreateDirectory(dir);

        // Create plugin.yaml
        var yaml = $"""
            name: {slug}
            version: 1.0.0
            description: A Portless.NET plugin
            author: developer
            hooks:
              - before-proxy
              - after-proxy
            config:
              example-key: example-value
            entryAssembly: {slug}.dll
            """;
        await File.WriteAllTextAsync(Path.Combine(dir, "plugin.yaml"), yaml);

        // Create C# source
        var className = string.Join("", slug.Split('-').Select(p => char.ToUpper(p[0]) + p[1..]));
        var ns = slug.Replace("-", "");
        var cs = string.Join(Environment.NewLine, [
            "using Portless.Plugin.SDK;",
            "",
            $"namespace {ns};",
            "",
            $"public class {className}Plugin : PortlessPlugin",
            "{",
            $"    public override string Name => \"{slug}\";",
            "    public override string Version => \"1.0.0\";",
            "",
            "    public override Task OnLoadAsync(IPluginContext context)",
            "    {",
            "        // Initialize your plugin here",
            "        return Task.CompletedTask;",
            "    }",
            "",
            "    public override Task<ProxyResult?> BeforeProxyAsync(ProxyContext context)",
            "    {",
            "        // Intercept or modify requests here",
            "        // Return a ProxyResult to short-circuit, or null to continue",
            "        return Task.FromResult<ProxyResult?>(null);",
            "    }",
            "",
            "    public override Task AfterProxyAsync(ProxyContext context, ProxyResult result)",
            "    {",
            "        // Log or process responses here",
            "        return Task.CompletedTask;",
            "    }",
            "}",
        ]);
        await File.WriteAllTextAsync(Path.Combine(dir, $"{className}Plugin.cs"), cs);

        // Create .csproj
        var csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Portless.NET.Plugin.SDK" Version="*" />
              </ItemGroup>
            </Project>
            """;
        await File.WriteAllTextAsync(Path.Combine(dir, $"{className}Plugin.csproj"), csproj);

        AnsiConsole.MarkupLine($"[green]Plugin scaffold created in ./{Markup.Escape(slug)}[/]");
        AnsiConsole.MarkupLine("[dim]Build with: dotnet build, then install with: portless plugin install ./" + slug + "[/]");
        return 0;
    }

    private async Task<int> ReloadPlugins()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            await http.PostAsync($"{ProxyBaseUrl}/api/v1/plugins/reload", null);
            AnsiConsole.MarkupLine("[green]Plugins reloaded.[/]");
            return 0;
        }
        catch (HttpRequestException)
        {
            AnsiConsole.MarkupLine("[red]Error: Cannot connect to proxy.[/]");
            return 1;
        }
    }

    private int ShowHelp()
    {
        AnsiConsole.MarkupLine("[bold]Portless Plugin Manager[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Usage: portless plugin <action> [target][/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("  [bold]list[/]              List loaded plugins");
        AnsiConsole.MarkupLine("  [bold]install[/] <path>    Install a plugin from directory");
        AnsiConsole.MarkupLine("  [bold]uninstall[/] <name>  Uninstall a plugin");
        AnsiConsole.MarkupLine("  [bold]create[/] <name>     Scaffold a new plugin project");
        AnsiConsole.MarkupLine("  [bold]reload[/]            Hot-reload all plugins");
        return 0;
    }

    private static void CopyDirectory(string source, string dest)
    {
        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dir.Replace(source, dest));
        }
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, dest), true);
        }
    }
}

internal record PluginInfo(string Name, string Version);
