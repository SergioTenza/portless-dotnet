# TIER 3: Observabilidad y Distribución - Implementation Plan

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Add observability (health, metrics), validate Native AOT, prepare NuGet packaging, and add shell completion.

**Architecture:** Incremental on existing proxy. Add ASP.NET Core health checks + Prometheus.NET metrics to the proxy. Validate AOT trimming. Centralize build config with Directory.Build.props. Generate completion scripts.

**Tech Stack:** .NET 10, YARP 2.3.0, ASP.NET Core Health Checks, prometheus-net 8.x (or OpenTelemetry), YamlDotNet 16.3.0, Spectre.Console.Cli 0.53.1

**Branch:** feature/v2.0-dx (from current HEAD: 4e0a715)

---

## Phase 1: Observability (Health + Metrics)

### Task 1.1: Health check endpoint + GET /api/v1/routes

**Objective:** Add `/health` endpoint and a GET route listing endpoint to the proxy.

**Files:**
- Modify: `Portless.Proxy/Portless.Proxy.csproj` — add health check packages
- Modify: `Portless.Proxy/Program.cs` — add health check services + endpoint
- Modify: `Portless.Proxy/PortlessApiEndpoints.cs` — add GET /api/v1/routes endpoint
- Create: `Portless.Tests/HealthCheckTests.cs`

**Step 1: Add health check NuGet to proxy csproj:**
```bash
dotnet add Portless.Proxy/Portless.Proxy.csproj package AspNetCore.HealthChecks.System
```
Actually, for simplicity, use built-in ASP.NET Core health checks (no extra package needed).

**Step 2: Add health check services in Program.cs:**
```csharp
builder.Services.AddHealthChecks()
    .AddCheck("proxy", () => HealthCheckResult.Healthy("Proxy is running"), tags: new[] { "ready" });
```
Map endpoint:
```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```
Actually, keep it simple - just the basic built-in writer.

**Step 3: Add GET /api/v1/routes endpoint in PortlessApiEndpoints.cs:**
```csharp
endpoints.MapGet("/api/v1/routes", async (IRouteStore routeStore) =>
{
    var routes = await routeStore.LoadRoutesAsync();
    return Results.Ok(routes.Select(r => new
    {
        r.Hostname,
        r.Port,
        r.Pid,
        r.Path,
        r.Type,
        r.BackendProtocol,
        backends = r.GetBackendUrls(),
        r.LoadBalancingPolicy,
        r.TcpListenPort,
        r.CreatedAt,
        r.LastSeen
    }));
});
```

**Step 4: Write tests, build, commit**

---

### Task 1.2: Prometheus metrics endpoint

**Objective:** Expose `/metrics` endpoint with request counters, histograms, and proxy-specific gauges.

**Files:**
- Modify: `Portless.Proxy/Portless.Proxy.csproj` — add prometheus-net
- Create: `Portless.Core/Services/IMetricsService.cs`
- Create: `Portless.Core/Services/PrometheusMetricsService.cs`
- Modify: `Portless.Proxy/Program.cs` — add metrics middleware
- Modify: `Portless.Proxy/RequestLoggingMiddleware` (or create MetricsMiddleware)

**Step 1: Add prometheus-net NuGet:**
```bash
dotnet add Portless.Proxy/Portless.Proxy.csproj package prometheus-net
dotnet add Portless.Proxy/Portless.Proxy.csproj package prometheus-net.AspNetCore
```

**Step 2: Create metrics service in Core:**
```csharp
// IMetricsService.cs
namespace Portless.Core.Services;

public interface IMetricsService
{
    void RecordRequest(string hostname, int statusCode, double durationMs);
    void RecordActiveRoutes(int count);
    void RecordActiveTcpListeners(int count);
}
```

```csharp
// PrometheusMetricsService.cs
using Prometheus;

namespace Portless.Core.Services;

public class PrometheusMetricsService : IMetricsService
{
    private readonly Counter _requestTotal = Metrics.CreateCounter(
        "portless_proxy_requests_total",
        "Total proxy requests",
        "hostname", "status_code");

    private readonly Histogram _requestDuration = Metrics.CreateHistogram(
        "portless_proxy_request_duration_seconds",
        "Request duration in seconds",
        "hostname",
        new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(0.001, 2, 10) });

    private readonly Gauge _activeRoutes = Metrics.CreateGauge(
        "portless_active_routes",
        "Number of active proxy routes");

    private readonly Gauge _activeTcpListeners = Metrics.CreateGauge(
        "portless_active_tcp_listeners",
        "Number of active TCP proxy listeners");

    public void RecordRequest(string hostname, int statusCode, double durationMs)
    {
        _requestTotal.WithLabels(hostname, statusCode.ToString()).Inc();
        _requestDuration.WithLabels(hostname).Observe(durationMs / 1000.0);
    }

    public void RecordActiveRoutes(int count) => _activeRoutes.Set(count);
    public void RecordActiveTcpListeners(int count) => _activeTcpListeners.Set(count);
}
```

**Step 3: Add metrics middleware and endpoint in Program.cs:**
```csharp
builder.Services.AddSingleton<IMetricsService, PrometheusMetricsService>();
// ...
app.UseMetricServer("/metrics");  // Exposes /metrics endpoint
```

**Step 4: Integrate with RequestLoggingMiddleware** — call `_metrics.RecordRequest(host, statusCode, duration)` after each request.

**Step 5: Update route count gauge** — after loading routes on startup and after add/remove.

**Step 6: Tests, build, commit**

---

## Phase 2: Native AOT Validation

### Task 2.1: Validate AOT compatibility and fix trimming issues

**Objective:** Ensure the CLI and Proxy can be published as Native AOT.

**Files:**
- Modify: `Portless.Cli/Portless.Cli.csproj` — already has PublishAot=true
- Modify: `Portless.Proxy/Portless.Proxy.csproj` — add PublishAot=true
- Create: `Directory.Build.props` — centralized AOT settings
- May need to add trimming annotations or root descriptors

**Step 1: Create Directory.Build.props at repo root:**
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Version>2.0.0</Version>
    <Authors>Portless.NET Contributors</Authors>
    <RepositoryUrl>https://github.com/SergioTenza/portless-dotnet</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
  </PropertyGroup>
</Project>
```

**Step 2: Try publishing CLI as AOT:**
```bash
dotnet publish Portless.Cli/Portless.Cli.csproj -r linux-x64 -c Release
```
Check for trimming/AOT errors. Spectre.Console.Cli is known to have issues (IL2xxx warnings we see). If errors, add:
```xml
<PublishTrimmed>true</PublishTrimmed>
<TrimmerRootAssembly>Portless.Cli</TrimmerRootAssembly>
```

**Step 3: Add AOT to Proxy:**
Add to Portless.Proxy.csproj:
```xml
<PublishAot>true</PublishAot>
```
ASP.NET Core 10 + YARP should be mostly AOT-compatible. Test publish.

**Step 4: Fix any AOT issues** — Common fixes:
- Add `[DynamicDependency]` attributes for reflection-heavy code
- Add trimming root descriptors for Spectre.Console
- Mark YAML serialization with `[UnconditionalSuppressMessage]`

**Step 5: Build, test publish, commit**

---

## Phase 3: NuGet Package Setup

### Task 3.1: Centralize versioning and prepare NuGet package

**Objective:** Central build config and proper NuGet metadata for dotnet tool publishing.

**Files:**
- Create: `Directory.Build.props` (from Task 2.1)
- Modify: `Portless.Cli/Portless.Cli.csproj` — update version from central props
- Create: `Portless.Proxy/Portless.Proxy.csproj` — packable if needed

**Step 1: Update Directory.Build.props with NuGet metadata**
**Step 2: Update CLI csproj** — remove duplicate properties that are now in Directory.Build.props
**Step 3: Test pack:** `dotnet pack Portless.Cli/Portless.Cli.csproj -c Release`
**Step 4: Build, test, commit**

---

## Phase 4: Shell Completion

### Task 4.1: Generate shell completion scripts

**Objective:** Provide bash, zsh, fish, and PowerShell completion scripts for the `portless` CLI.

**Files:**
- Create: `completions/portless.bash`
- Create: `completions/portless.zsh`
- Create: `completions/portless.fish`
- Create: `Portless.Cli/Commands/CompletionCommand/CompletionCommand.cs`
- Create: `Portless.Cli/Commands/CompletionCommand/CompletionSettings.cs`

**Approach:** Spectre.Console.Cli doesn't have built-in completion. We generate static completion scripts based on the known command structure. A `portless completion bash|zsh|fish|powershell` command outputs the script.

**Step 1: Create CompletionCommand that generates shell-specific completion:**

Static completions for known commands:
- Commands: run, list, get, alias, hosts, up, tcp, proxy (start/stop/status), cert (install/status/uninstall/check/renew)
- Flags: --path, --backend, --remove, --listen, --file, etc.

**Step 2: Generate bash completion script**
**Step 3: Generate zsh completion script**
**Step 4: Generate fish completion script**
**Step 5: Build, test, commit**

---

## Verification

After all phases:
1. `dotnet build Portless.slnx` — 0 errors
2. `dotnet test Portless.Tests/Portless.Tests.csproj` — all pass
3. `dotnet publish Portless.Cli -r linux-x64 -c Release` — AOT succeeds
4. `dotnet pack Portless.Cli/Portless.Cli.csproj -c Release` — package created
5. Start proxy → verify `/health`, `/metrics`, `/api/v1/routes` endpoints
6. Update AGENT-CONTEXT.md with Tier 3 progress

## Risks

- **AOT + Spectre.Console:** Spectre.Console.Cli uses reflection heavily. May need trimming suppressions. If too complex, skip AOT for CLI v2.0 and document as known limitation.
- **AOT + YARP:** YARP 2.x has improved AOT support but may need root descriptors.
- **AOT + YamlDotNet:** YamlDotNet uses reflection. May need `[UnconditionalSuppressMessage]` or source-generated serialization.
- **prometheus-net + AOT:** prometheus-net uses reflection for metrics. Verify compatibility.
