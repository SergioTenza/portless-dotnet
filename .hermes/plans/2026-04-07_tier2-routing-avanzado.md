# TIER 2: Routing Avanzado - Implementation Plan

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Implement advanced routing features for Portless.NET: config file, path routing, load balancing, and TCP proxying.

**Architecture:** Incremental on top of existing YARP-based proxy. Expand RouteInfo model, consolidate duplicated helpers, add YAML config parsing, and leverage YARP built-in features (load balancing, direct forwarding for TCP).

**Tech Stack:** .NET 10, YARP 2.3.0, YamlDotNet (new), Spectre.Console.Cli 0.53.1, xUnit 2.9.3

**Branch:** feature/v2.0-dx (from current HEAD)

---

## Phase 0: Refactor Base (Debt Cleanup)

These tasks clean up technical debt and expand the model so all Tier 2 features have a solid foundation.

### Task 0.1: Consolidate CreateRoute/CreateCluster into shared service

**Objective:** Extract duplicated YARP helper methods into a shared service in Portless.Core

**Files:**
- Create: `Portless.Core/Services/IYarpConfigFactory.cs`
- Create: `Portless.Core/Services/YarpConfigFactory.cs`
- Modify: `Portless.Proxy/PortlessApiEndpoints.cs` — remove private helpers, use IYarpConfigFactory
- Modify: `Portless.Proxy/Program.cs` — remove static helpers, use IYarpConfigFactory

**Step 1: Create IYarpConfigFactory**

```csharp
// Portless.Core/Services/IYarpConfigFactory.cs
using Yarp.ReverseProxy.Configuration;

namespace Portless.Core.Services;

/// <summary>
/// Factory for creating YARP route and cluster configurations.
/// Centralizes the creation logic to avoid duplication between proxy and API endpoints.
/// </summary>
public interface IYarpConfigFactory
{
    RouteConfig CreateRoute(string hostname, string clusterId, string? path = null);
    ClusterConfig CreateCluster(string clusterId, string[] backendUrls);
    (RouteConfig Route, ClusterConfig Cluster) CreateRouteClusterPair(string hostname, string[] backendUrls, string? path = null);
}
```

**Step 2: Implement YarpConfigFactory**

```csharp
// Portless.Core/Services/YarpConfigFactory.cs
using System.Security.Authentication;
using Yarp.ReverseProxy.Configuration;

namespace Portless.Core.Services;

public class YarpConfigFactory : IYarpConfigFactory
{
    public RouteConfig CreateRoute(string hostname, string clusterId, string? path = null)
    {
        return new RouteConfig
        {
            RouteId = $"route-{hostname}",
            ClusterId = clusterId,
            Match = new RouteMatch
            {
                Hosts = new[] { hostname },
                Path = path ?? "/{**catch-all}"
            }
        };
    }

    public ClusterConfig CreateCluster(string clusterId, string[] backendUrls)
    {
        var destinations = new Dictionary<string, DestinationConfig>();
        for (int i = 0; i < backendUrls.Length; i++)
        {
            destinations[$"backend{i + 1}"] = new DestinationConfig { Address = backendUrls[i] };
        }

        return new ClusterConfig
        {
            ClusterId = clusterId,
            Destinations = destinations,
            HttpClient = new HttpClientConfig
            {
                DangerousAcceptAnyServerCertificate = true,
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            }
        };
    }

    public (RouteConfig Route, ClusterConfig Cluster) CreateRouteClusterPair(
        string hostname, string[] backendUrls, string? path = null)
    {
        var clusterId = $"cluster-{hostname}";
        return (CreateRoute(hostname, clusterId, path), CreateCluster(clusterId, backendUrls));
    }
}
```

**Step 3: Register in DI** — Add to `Portless.Core/Extensions/ServiceCollectionExtensions.cs` in `AddPortlessPersistence()`:

```csharp
services.AddSingleton<IYarpConfigFactory, YarpConfigFactory>();
```

**Step 4: Update PortlessApiEndpoints.cs** — Replace private `CreateRoute`/`CreateCluster` calls with injected `IYarpConfigFactory`. Inject via parameter to `MapPortlessApi`:

```csharp
public static IEndpointRouteBuilder MapPortlessApi(
    this IEndpointRouteBuilder endpoints,
    DynamicConfigProvider configProvider,
    IRouteStore routeStore,
    IYarpConfigFactory configFactory)
```

Replace in add-host:
```csharp
var (newRoute, newCluster) = configFactory.CreateRouteClusterPair(
    request.Hostname, new[] { request.BackendUrl });
```

Remove the private `CreateRoute` and `CreateCluster` methods.

**Step 5: Update Program.cs** — Remove the two static helper methods. Replace route loading loop:

```csharp
var configFactory = app.Services.GetRequiredService<IYarpConfigFactory>();
// ...
foreach (var route in deduplicatedRoutes)
{
    var backendUrl = $"{route.BackendProtocol}://localhost:{route.Port}";
    var (routeConfig, clusterConfig) = configFactory.CreateRouteClusterPair(route.Hostname, new[] { backendUrl });
    routeConfigs.Add(routeConfig);
    clusterConfigs.Add(clusterConfig);
}
```

**Step 6: Build and run existing tests**

Run: `dotnet build Portless.slnx && dotnet test Portless.Tests/Portless.Tests.csproj`
Expected: 0 errors, all tests pass (same as before, just refactored)

**Step 7: Commit**

```bash
git add -A && git commit -m "refactor: extract CreateRoute/CreateCluster into IYarpConfigFactory"
```

---

### Task 0.2: Expand RouteInfo model for Tier 2

**Objective:** Add new fields to RouteInfo needed by all Tier 2 features

**Files:**
- Modify: `Portless.Core/Models/RouteInfo.cs`

**Step 1: Expand RouteInfo**

```csharp
// Portless.Core/Models/RouteInfo.cs
namespace Portless.Core.Models;

/// <summary>
/// Type of route: HTTP proxy or raw TCP forwarding.
/// </summary>
public enum RouteType
{
    Http = 0,
    Tcp = 1
}

/// <summary>
/// Load balancing policy for multi-backend clusters.
/// </summary>
public enum LoadBalancingPolicy
{
    First = 0,
    RoundRobin = 1,
    LeastRequests = 2,
    Random = 3,
    PowerOfTwoChoices = 4
}

public class RouteInfo
{
    // === Existing fields (backward compatible) ===
    public string Hostname { get; init; } = string.Empty;
    public int Port { get; init; }
    public int Pid { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastSeen { get; set; }
    public string BackendProtocol { get; init; } = "http";

    // === Tier 2 fields (nullable for backward compat) ===

    /// <summary>
    /// Optional path prefix for path-based routing (e.g. "/api").
    /// Null means catch-all "/{**catch-all}".
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Multiple backend URLs for load balancing.
    /// If null or empty, falls back to single backend derived from Port + BackendProtocol.
    /// </summary>
    public string[]? BackendUrls { get; init; }

    /// <summary>
    /// Load balancing policy for multi-backend clusters.
    /// Defaults to PowerOfTwoChoices (YARP default, best for most cases).
    /// </summary>
    public LoadBalancingPolicy LoadBalancingPolicy { get; init; } = LoadBalancingPolicy.PowerOfTwoChoices;

    /// <summary>
    /// Route type: HTTP (default) or TCP.
    /// </summary>
    public RouteType Type { get; init; } = RouteType.Http;

    /// <summary>
    /// TCP listen port (only for RouteType.Tcp).
    /// The proxy will listen on this port and forward to the backend.
    /// </summary>
    public int? TcpListenPort { get; init; }

    /// <summary>
    /// Helper: resolves the effective backend URLs.
    /// If BackendUrls is set, uses those. Otherwise derives from Port + BackendProtocol.
    /// </summary>
    public string[] GetBackendUrls()
    {
        if (BackendUrls is { Length: > 0 })
            return BackendUrls;

        return [$"{BackendProtocol}://localhost:{Port}"];
    }
}
```

**Step 2: Build to verify backward compatibility**

Run: `dotnet build Portless.slnx`
Expected: 0 errors. JSON deserialization of existing `routes.json` will work because new fields are nullable with defaults.

**Step 3: Run tests**

Run: `dotnet test Portless.Tests/Portless.Tests.csproj`
Expected: All pass. Existing RouteInfo usages are unaffected.

**Step 4: Commit**

```bash
git add -A && git commit -m "feat(models): expand RouteInfo with Tier 2 fields (path, backends, load balancing, TCP)"
```

---

### Task 0.3: Fix RunCommand double-persistence and use IProxyRouteRegistrar

**Objective:** RunCommand and AliasCommand should use IProxyRouteRegistrar instead of direct HTTP calls. Remove duplicate persistence (the proxy API already persists).

**Files:**
- Modify: `Portless.Cli/Commands/RunCommand/RunCommand.cs`
- Modify: `Portless.Cli/Commands/AliasCommand/AliasCommand.cs`
- Modify: `Portless.Core/Services/IProxyRouteRegistrar.cs` — expand interface
- Modify: `Portless.Core/Services/ProxyRouteRegistrar.cs` — expand implementation
- Modify: `Portless.Cli/Program.cs` — register IProxyRouteRegistrar in DI

**Step 1: Expand IProxyRouteRegistrar**

```csharp
// Portless.Core/Services/IProxyRouteRegistrar.cs
namespace Portless.Core.Services;

/// <summary>
/// Registers and removes routes with the proxy server.
/// Centralizes the HTTP communication between CLI and Proxy.
/// </summary>
public interface IProxyRouteRegistrar
{
    /// <summary>
    /// Registers a route with the proxy server.
    /// </summary>
    Task<bool> RegisterRouteAsync(string hostname, string backendUrl);

    /// <summary>
    /// Removes a route from the proxy server.
    /// </summary>
    Task<bool> RemoveRouteAsync(string hostname);
}
```

(Interface stays the same for now — just ensure it's registered in DI.)

**Step 2: Register IProxyRouteRegistrar in CLI DI**

Add to `Portless.Cli/Program.cs`:
```csharp
services.AddSingleton<IProxyRouteRegistrar, ProxyRouteRegistrar>();
```

**Step 3: Update RunCommand** — Replace direct HTTP call (lines ~195-215) with:

```csharp
// Replace: var httpClient = _httpClientFactory.CreateClient(); ... PostAsync(...)
// With:
var registrar = // inject via constructor
```

Add `IProxyRouteRegistrar` to RunCommand constructor. Replace the manual HTTP POST block with:
```csharp
var registered = await _registrar.RegisterRouteAsync(hostname, $"http://localhost:{port}");
if (!registered)
{
    AnsiConsole.MarkupLine("[red]Error:[/] Failed to register route with proxy");
    return 1;
}
```

Remove the duplicate `_routeStore.SaveRoutesAsync()` call (the proxy's add-host endpoint already persists).

**Step 4: Update AliasCommand** — Same pattern: inject `IProxyRouteRegistrar`, replace direct HTTP calls.

**Step 5: Build and test**

Run: `dotnet build Portless.slnx && dotnet test Portless.Tests/Portless.Tests.csproj`
Expected: 0 errors, all tests pass

**Step 6: Commit**

```bash
git add -A && git commit -m "refactor: CLI uses IProxyRouteRegistrar, removes double-persistence"
```

---

## Phase 1: Config File (Feature 2.4)

The config file is the foundation — other features (path routing, load balancing, TCP) get their config surface here.

### Task 1.1: Add YamlDotNet dependency and create config models

**Objective:** Define the YAML schema and models for portless.config.yaml

**Files:**
- Add NuGet: `YamlDotNet` to `Portless.Core.csproj`
- Create: `Portless.Core/Models/PortlessConfig.cs`
- Create: `Portless.Core/Models/RouteConfig.cs` (Portless-specific, NOT YARP RouteConfig)

**Step 1: Add YamlDotNet**

```bash
cd /root/portless-dotnet && export PATH="/usr/local/dotnet:$PATH"
dotnet add Portless.Core/Portless.Core.csproj package YamlDotNet
```

**Step 2: Create config models**

```csharp
// Portless.Core/Models/PortlessConfig.cs
using YamlDotNet.Serialization;

namespace Portless.Core.Models;

/// <summary>
/// Root configuration model for portless.config.yaml
/// </summary>
public class PortlessConfig
{
    [YamlMember(Alias = "routes")]
    public List<PortlessRouteConfig> Routes { get; set; } = new();
}

/// <summary>
/// A single route definition in portless.config.yaml
/// </summary>
public class PortlessRouteConfig
{
    /// <summary>
    /// Hostname for the route (e.g. "api.localhost").
    /// Required for HTTP routes.
    /// </summary>
    [YamlMember(Alias = "host")]
    public string? Host { get; set; }

    /// <summary>
    /// Optional path prefix (e.g. "/api/v1").
    /// </summary>
    [YamlMember(Alias = "path")]
    public string? Path { get; set; }

    /// <summary>
    /// Backend URL(s) to forward to. Can be a single URL or a list.
    /// </summary>
    [YamlMember(Alias = "backends")]
    public List<string> Backends { get; set; } = new();

    /// <summary>
    /// Load balancing policy: RoundRobin, LeastRequests, Random, PowerOfTwoChoices.
    /// Defaults to PowerOfTwoChoices.
    /// </summary>
    [YamlMember(Alias = "loadBalance")]
    public string? LoadBalancePolicy { get; set; }

    /// <summary>
    /// Route type: "http" (default) or "tcp".
    /// </summary>
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "http";

    /// <summary>
    /// TCP listen port (only for type: tcp).
    /// </summary>
    [YamlMember(Alias = "listenPort")]
    public int? ListenPort { get; set; }
}
```

**Step 3: Write failing test**

```csharp
// Portless.Tests/PortlessConfigTests.cs
using Portless.Core.Models;
using YamlDotNet.Serialization;
using Xunit;

namespace Portless.Tests;

public class PortlessConfigTests
{
    [Fact]
    public void Deserialize_SimpleConfig()
    {
        var yaml = @"
routes:
  - host: api.localhost
    backends:
      - http://localhost:5000
  - host: web.localhost
    path: /app
    backends:
      - http://localhost:3000
      - http://localhost:3001
    loadBalance: RoundRobin
";
        var deserializer = new DeserializerBuilder().Build();
        var config = deserializer.Deserialize<PortlessConfig>(yaml);

        Assert.Equal(2, config.Routes.Count);
        Assert.Equal("api.localhost", config.Routes[0].Host);
        Assert.Single(config.Routes[0].Backends);
        Assert.Equal("http://localhost:5000", config.Routes[0].Backends[0]);
        Assert.Equal("/app", config.Routes[1].Path);
        Assert.Equal(2, config.Routes[1].Backends.Count);
        Assert.Equal("RoundRobin", config.Routes[1].LoadBalancePolicy);
    }
}
```

**Step 4: Run test**

Run: `dotnet test --filter "PortlessConfigTests" -v n`
Expected: PASS

**Step 5: Commit**

```bash
git add -A && git commit -m "feat(config): add YAML config models with YamlDotNet"
```

---

### Task 1.2: Create PortlessConfigLoader service

**Objective:** Service that discovers, loads, and watches portless.config.yaml

**Files:**
- Create: `Portless.Core/Services/IPortlessConfigLoader.cs`
- Create: `Portless.Core/Services/PortlessConfigLoader.cs`

**Step 1: Create interface**

```csharp
// Portless.Core/Services/IPortlessConfigLoader.cs
using Portless.Core.Models;

namespace Portless.Core.Services;

public interface IPortlessConfigLoader
{
    /// <summary>
    /// Searches for portless.config.yaml starting from cwd and walking up to root.
    /// Returns null if not found.
    /// </summary>
    string? FindConfigFile(string? startDir = null);

    /// <summary>
    /// Loads and parses the config file. Returns empty config if file not found.
    /// </summary>
    PortlessConfig Load(string? path = null);

    /// <summary>
    /// Converts PortlessConfig routes to RouteInfo[] for persistence/YARP.
    /// </summary>
    RouteInfo[] ToRouteInfos(PortlessConfig config);
}
```

**Step 2: Implement PortlessConfigLoader**

```csharp
// Portless.Core/Services/PortlessConfigLoader.cs
using Portless.Core.Models;
using YamlDotNet.Serialization;
using Microsoft.Extensions.Logging;

namespace Portless.Core.Services;

public class PortlessConfigLoader : IPortlessConfigLoader
{
    private const string ConfigFileName = "portless.config.yaml";
    private readonly ILogger<PortlessConfigLoader> _logger;
    private static readonly IDeserializer _deserializer = new DeserializerBuilder().Build();

    public PortlessConfigLoader(ILogger<PortlessConfigLoader> logger)
    {
        _logger = logger;
    }

    public string? FindConfigFile(string? startDir = null)
    {
        var dir = startDir ?? Directory.GetCurrentDirectory();

        while (dir != null)
        {
            var candidate = Path.Combine(dir, ConfigFileName);
            if (File.Exists(candidate))
                return candidate;

            var parent = Directory.GetParent(dir)?.FullName;
            if (parent == dir) break; // root reached
            dir = parent;
        }

        return null;
    }

    public PortlessConfig Load(string? path = null)
    {
        var filePath = path ?? FindConfigFile();
        if (filePath == null)
        {
            _logger.LogDebug("No {ConfigFile} found", ConfigFileName);
            return new PortlessConfig();
        }

        _logger.LogInformation("Loading config from {Path}", filePath);
        var yaml = File.ReadAllText(filePath);
        var config = _deserializer.Deserialize<PortlessConfig>(yaml) ?? new PortlessConfig();
        _logger.LogInformation("Loaded {Count} routes from config", config.Routes.Count);
        return config;
    }

    public RouteInfo[] ToRouteInfos(PortlessConfig config)
    {
        return config.Routes.Select(r => new RouteInfo
        {
            Hostname = r.Host ?? string.Empty,
            Path = r.Path,
            Port = ExtractPort(r.Backends.FirstOrDefault()),
            Pid = 0, // Config-file routes are static (no managed process)
            BackendProtocol = ExtractProtocol(r.Backends.FirstOrDefault()),
            BackendUrls = r.Backends.Count > 1 ? r.Backends.ToArray() : null,
            LoadBalancingPolicy = ParseLoadBalancePolicy(r.LoadBalancePolicy),
            Type = r.Type.Equals("tcp", StringComparison.OrdinalIgnoreCase) ? RouteType.Tcp : RouteType.Http,
            TcpListenPort = r.ListenPort,
        }).Where(r => !string.IsNullOrEmpty(r.Hostname) || r.Type == RouteType.Tcp).ToArray();
    }

    private static int ExtractPort(string? url)
    {
        if (string.IsNullOrEmpty(url)) return 0;
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri)) return uri.Port;
        return 0;
    }

    private static string ExtractProtocol(string? url)
    {
        if (string.IsNullOrEmpty(url)) return "http";
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri)) return uri.Scheme;
        return "http";
    }

    private static LoadBalancingPolicy ParseLoadBalancePolicy(string? policy)
    {
        return policy?.ToLowerInvariant() switch
        {
            "roundrobin" => LoadBalancingPolicy.RoundRobin,
            "leastrequests" => LoadBalancingPolicy.LeastRequests,
            "random" => LoadBalancingPolicy.Random,
            "first" => LoadBalancingPolicy.First,
            _ => LoadBalancingPolicy.PowerOfTwoChoices
        };
    }
}
```

**Step 3: Register in DI** — Add to `AddPortlessPersistence()`:
```csharp
services.AddSingleton<IPortlessConfigLoader, PortlessConfigLoader>();
```

**Step 4: Write tests**

Test: FindConfigFile walks up directories
Test: Load parses YAML correctly
Test: ToRouteInfos converts all fields

**Step 5: Build and test**

Run: `dotnet build Portless.slnx && dotnet test --filter "PortlessConfigLoaderTests" -v n`

**Step 6: Commit**

```bash
git add -A && git commit -m "feat(config): add PortlessConfigLoader service"
```

---

### Task 1.3: Add `portless up` command (reads config and registers routes)

**Objective:** New CLI command that reads portless.config.yaml and registers all routes with the running proxy.

**Files:**
- Create: `Portless.Cli/Commands/UpCommand/UpCommand.cs`
- Create: `Portless.Cli/Commands/UpCommand/UpSettings.cs`
- Modify: `Portless.Cli/Program.cs` — register command

**Step 1: Create UpSettings**

```csharp
// Portless.Cli/Commands/UpCommand/UpSettings.cs
using Spectre.Console.Cli;

namespace Portless.Cli.Commands.UpCommand;

public class UpSettings : CommandSettings
{
    [CommandOption("-f|--file <PATH>")]
    [Description("Path to portless.config.yaml (auto-detected if not specified)")]
    public string? ConfigFile { get; set; }
}
```

**Step 2: Create UpCommand**

```csharp
// Portless.Cli/Commands/UpCommand/UpCommand.cs
using Portless.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Portless.Cli.Commands.UpCommand;

public class UpCommand : AsyncCommand<UpSettings>
{
    private readonly IPortlessConfigLoader _configLoader;
    private readonly IProxyRouteRegistrar _registrar;
    private readonly IProxyProcessManager _proxyManager;

    public UpCommand(
        IPortlessConfigLoader configLoader,
        IProxyRouteRegistrar registrar,
        IProxyProcessManager proxyManager)
    {
        _configLoader = configLoader;
        _registrar = registrar;
        _proxyManager = proxyManager;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, UpSettings settings, CancellationToken ct)
    {
        var config = _configLoader.Load(settings.ConfigFile);

        if (config.Routes.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No routes found in config file.[/]");
            return 0;
        }

        // Ensure proxy is running (same pattern as RunCommand)
        // ... proxy check/start logic ...

        int registered = 0, failed = 0;
        foreach (var route in config.Routes)
        {
            if (route.Host == null || route.Backends.Count == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]Skipping route with missing host or backends[/]");
                failed++;
                continue;
            }

            var backend = route.Backends[0]; // Primary backend for registration
            var success = await _registrar.RegisterRouteAsync(route.Host, backend);
            if (success)
            {
                AnsiConsole.MarkupLine($"[green]✓[/] {route.Host} -> {string.Join(", ", route.Backends)}");
                registered++;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Failed to register {route.Host}");
                failed++;
            }
        }

        AnsiConsole.MarkupLine($"[blue]{registered}[/] routes registered, [red]{failed}[/] failed");
        return failed > 0 ? 1 : 0;
    }
}
```

**Step 3: Register in CLI** — Add to `Portless.Cli/Program.cs`:

```csharp
config.AddCommand<UpCommand>("up")
    .WithDescription("Start routes from portless.config.yaml")
    .WithExample("up")
    .WithExample("up", "-f", "./my-config.yaml");
```

**Step 4: Write test** — Test UpCommand with mock registrar

**Step 5: Build and test**

**Step 6: Commit**

```bash
git add -A && git commit -m "feat(cli): add 'portless up' command for config-file routes"
```

---

### Task 1.4: Proxy loads config file routes on startup

**Objective:** When proxy starts, it loads routes from portless.config.yaml (in addition to routes.json)

**Files:**
- Modify: `Portless.Proxy/Program.cs` — load config file routes on startup

**Step 1: Modify startup route loading** in Program.cs, after loading from RouteStore:

```csharp
// After loading from routeStore, also load from config file
var configLoader = app.Services.GetRequiredService<IPortlessConfigLoader>();
var fileConfig = configLoader.Load();
if (fileConfig.Routes.Count > 0)
{
    var configRoutes = configLoader.ToRouteInfos(fileConfig);
    foreach (var route in configRoutes.Where(r => r.Type == RouteType.Http))
    {
        var urls = route.GetBackendUrls();
        var (routeConfig, clusterConfig) = configFactory.CreateRouteClusterPair(
            route.Hostname, urls, route.Path);
        routeConfigs.Add(routeConfig);
        clusterConfigs.Add(clusterConfig);
    }
    logger.LogInformation("Loaded {Count} routes from config file", configRoutes.Length);
}
```

**Step 2: Test** — Start proxy with a test config file, verify routes are loaded

**Step 3: Commit**

```bash
git add -A && git commit -m "feat(proxy): load portless.config.yaml routes on startup"
```

---

## Phase 2: Path-Based Routing (Feature 2.1)

### Task 2.1: Support path in API endpoints and YarpConfigFactory

**Objective:** The add-host API and YarpConfigFactory support path-based routing

**Files:**
- Modify: `Portless.Proxy/PortlessApiEndpoints.cs` — expand AddHostRequest, use path
- Modify: `Portless.Core/Services/IYarpConfigFactory.cs` already done in Phase 0

**Step 1: Expand AddHostRequest**

```csharp
public record AddHostRequest(
    string Hostname,
    string BackendUrl,
    string? Path = null
);
```

**Step 2: Update add-host handler** — Pass path to configFactory:

```csharp
var (newRoute, newCluster) = configFactory.CreateRouteClusterPair(
    request.Hostname, new[] { request.BackendUrl }, request.Path);
```

**Step 3: Persist Path in RouteInfo** — Update the RouteInfo creation in add-host:

```csharp
var newRouteInfo = new RouteInfo
{
    Hostname = request.Hostname,
    Port = port,
    Pid = Environment.ProcessId,
    CreatedAt = DateTime.UtcNow,
    Path = request.Path  // NEW
};
```

**Step 4: Write tests**

Test: Add route with path "/api" -> verify RouteConfig.Match.Path is "/api/{**catch-all}"
Test: Add route without path -> verify default "/{**catch-all}"

**Step 5: Build and test, commit**

```bash
git add -A && git commit -m "feat(routing): path-based routing in API and config factory"
```

---

### Task 2.2: CLI support for --path flag in run and alias

**Objective:** `portless run --path /api myapi npm start` creates path-based route

**Files:**
- Modify: `Portless.Cli/Commands/RunCommand/RunSettings.cs` — add --path option
- Modify: `Portless.Cli/Commands/RunCommand/RunCommand.cs` — pass path to registrar
- Modify: `Portless.Cli/Commands/AliasCommand/AliasSettings.cs` — add --path option
- Modify: `Portless.Cli/Commands/AliasCommand/AliasCommand.cs` — pass path
- Modify: `Portless.Core/Services/IProxyRouteRegistrar.cs` — add path param
- Modify: `Portless.Core/Services/ProxyRouteRegistrar.cs` — pass path in payload

**Step 1: Expand IProxyRouteRegistrar**

```csharp
Task<bool> RegisterRouteAsync(string hostname, string backendUrl, string? path = null);
```

**Step 2: Expand ProxyRouteRegistrar** — Include path in JSON payload:

```csharp
var payload = new { hostname, backendUrl, path };
```

**Step 3: Add --path to RunSettings/AliasSettings**

```csharp
[CommandOption("-p|--path <PATH>")]
[Description("Path prefix for path-based routing (e.g. /api)")]
public string? Path { get; set; }
```

**Step 4: Update RunCommand/AliasCommand** — Pass settings.Path to RegisterRouteAsync

**Step 5: Write tests, build, commit**

```bash
git add -A && git commit -m "feat(cli): --path flag for run and alias commands"
```

---

## Phase 3: Multi-Backend Load Balancing (Feature 2.3)

### Task 3.1: Support multiple backends in API and YarpConfigFactory

**Objective:** Clusters can have multiple destinations with load balancing

**Files:**
- Modify: `Portless.Proxy/PortlessApiEndpoints.cs` — expand AddHostRequest for backends[]
- Modify: `Portless.Core/Services/IYarpConfigFactory.cs` — already done (accepts string[])
- Modify: `Portless.Proxy/Program.cs` — add `AddLoadBalancingPolicies()`

**Step 1: Add YARP load balancing to proxy**

In `Portless.Proxy/Program.cs`:
```csharp
builder.Services.AddLoadBalancingPolicies();
```

**Step 2: Expand AddHostRequest**

```csharp
public record AddHostRequest(
    string Hostname,
    string BackendUrl,
    string? Path = null,
    string[]? BackendUrls = null,
    string? LoadBalancePolicy = null
);
```

**Step 3: Update add-host handler** — Build destination list from BackendUrls or single BackendUrl. Set load balancing policy on cluster.

**Step 4: Write tests, build, commit**

```bash
git add -A && git commit -m "feat(lb): multi-backend load balancing in API"
```

---

### Task 3.2: CLI support for --backend flag

**Objective:** `portless run api --backend 5001 --backend 5002` registers load-balanced route

**Files:**
- Modify: `Portless.Cli/Commands/RunCommand/RunSettings.cs` — add --backend option
- Modify: `Portless.Cli/Commands/RunCommand/RunCommand.cs` — pass backends

**Step 1: Add --backend to RunSettings**

```csharp
[CommandOption("-b|--backend <URL>")]
[Description("Additional backend URL for load balancing (can be repeated)")]
public string[] Backends { get; set; } = Array.Empty<string>();
```

**Step 2: Update RunCommand** — If Backends specified, pass all to registrar

**Step 3: Tests, build, commit**

```bash
git add -A && git commit -m "feat(cli): --backend flag for load-balanced run"
```

---

## Phase 4: TCP Proxying (Feature 2.2)

This is the most complex feature. YARP supports TCP proxying via direct forwarding.

### Task 4.1: TCP proxy infrastructure in proxy

**Objective:** Proxy can listen on additional TCP ports and forward raw connections

**Files:**
- Modify: `Portless.Proxy/Program.cs` — dynamic Kestrel TCP listeners
- Create: `Portless.Core/Services/TcpForwardingService.cs` — background service for TCP forwarding
- Create: `Portless.Core/Services/ITcpForwardingService.cs`

**Step 1: Research YARP direct forwarding** — YARP's `IHttpForwarder` can do TCP with custom middleware. Alternative: raw `TcpListener` → `TcpClient` relay.

**Step 2: Implement simple TCP relay** — A background service that:
- Listens on specified port
- For each connection, opens connection to backend
- Relays bytes bidirectionally

**Step 3: Dynamic listener management** — Add API endpoint to add/remove TCP listeners at runtime

**Step 4: Tests, build, commit**

```bash
git add -A && git commit -m "feat(tcp): raw TCP proxying support"
```

---

### Task 4.2: CLI support for TCP routes

**Objective:** `portless tcp redis localhost:6379 --listen 6379` creates TCP proxy

**Files:**
- Create: `Portless.Cli/Commands/TcpCommand/TcpCommand.cs`
- Create: `Portless.Cli/Commands/TcpCommand/TcpSettings.cs`
- Modify: `Portless.Cli/Program.cs` — register command

**Step 1: Create TcpSettings/TcpCommand**

```bash
# Example usage:
portless tcp add redis localhost:6379 --listen 16379
portless tcp list
portless tcp remove redis
```

**Step 2: Tests, build, commit**

```bash
git add -A && git commit -m "feat(cli): TCP proxy commands"
```

---

## Verification

After all phases:

1. `dotnet build Portless.slnx` — 0 errors
2. `dotnet test Portless.Tests/Portless.Tests.csproj` — all pass
3. `dotnet test Portless.IntegrationTests/` — all pass
4. Manual test with portless.config.yaml containing all route types
5. Update AGENT-CONTEXT.md with Tier 2 progress
6. Tag release when stable

## Risks

- **TCP proxying complexity:** Raw TCP relay is simpler than YARP direct forwarding. Start simple.
- **Config file format changes:** YAML schema should be stable from day 1. Version field reserved.
- **Load balancing with YARP:** Requires `AddLoadBalancingPolicies()` — verify it works with in-memory config.
- **Route ordering:** Path-based routes need correct priority. YARP sorts by specificity automatically.
