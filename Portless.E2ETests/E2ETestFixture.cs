using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Portless.E2ETests;

/// <summary>
/// Shared fixture for E2E tests that manages proxy lifecycle and test backends.
/// Uses xUnit's IAsyncLifetime for proper setup/teardown and ICollectionFixture
/// to share across tests in the same collection (preventing parallel execution).
/// </summary>
public class E2ETestFixture : IAsyncLifetime
{
    private Process? _proxyProcess;
    private readonly List<Process> _backendProcesses = new();
    private readonly List<(HttpListener listener, CancellationTokenSource cts)> _httpListeners = new();
    private readonly List<string> _tempDirectories = new();
    private string _stateDirectory = "";
    private string _solutionRoot = "";

    /// <summary>
    /// The port the proxy listens on (always 1355 as hardcoded in the proxy).
    /// </summary>
    public int ProxyPort => 1355;

    /// <summary>
    /// Base URL for the proxy.
    /// </summary>
    public string ProxyBaseUrl => $"http://localhost:{ProxyPort}";

    /// <summary>
    /// HttpClient pre-configured for the proxy.
    /// </summary>
    public HttpClient HttpClient { get; private set; } = null!;

    /// <summary>
    /// HttpClient that does NOT follow redirects, useful for testing proxy behavior.
    /// </summary>
    public HttpClient NoRedirectHttpClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Discover solution root from test assembly location
        var assemblyLocation = typeof(E2ETestFixture).Assembly.Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation) ?? ".";
        _solutionRoot = Path.GetFullPath(Path.Combine(assemblyDirectory, "../../../../"));

        // Create isolated state directory for this test run
        _stateDirectory = Path.Combine(Path.GetTempPath(), $"portless-e2e-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_stateDirectory);
        _tempDirectories.Add(_stateDirectory);

        // Create HttpClients
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
        };
        HttpClient = new HttpClient(handler) { BaseAddress = new Uri(ProxyBaseUrl), Timeout = TimeSpan.FromSeconds(10) };
        NoRedirectHttpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = new Uri(ProxyBaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public async Task DisposeAsync()
    {
        // Kill proxy process
        await KillProcessAsync(_proxyProcess, "proxy");

        // Kill all backend processes
        foreach (var backend in _backendProcesses)
        {
            await KillProcessAsync(backend, "backend");
        }

        // Stop all HttpListener backends
        foreach (var (listener, cts) in _httpListeners)
        {
            cts.Cancel();
            listener.Stop();
            cts.Dispose();
        }

        // Cleanup HttpClients
        HttpClient.Dispose();
        NoRedirectHttpClient.Dispose();

        // Cleanup temp directories
        foreach (var dir in _tempDirectories)
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, recursive: true);
                    }
                    break;
                }
                catch when (attempt < 4)
                {
                    await Task.Delay(500 * (attempt + 1));
                }
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Starts the proxy as a real process and waits for it to become healthy.
    /// </summary>
    public async Task StartProxyAsync()
    {
        var proxyProjectPath = Path.Combine(_solutionRoot, "Portless.Proxy", "Portless.Proxy.csproj");

        if (!File.Exists(proxyProjectPath))
        {
            throw new FileNotFoundException($"Proxy project not found at: {proxyProjectPath}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{proxyProjectPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = _solutionRoot
        };

        // Set environment for isolation
        startInfo.Environment["PORTLESS_STATE_DIR"] = _stateDirectory;
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["PORTLESS_HTTPS_ENABLED"] = "false";

        _proxyProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start proxy process");

        // Wire up output forwarding for debugging
        _proxyProcess.BeginOutputReadLine();
        _proxyProcess.BeginErrorReadLine();

        // Wait for proxy to be healthy
        await WaitForHealthAsync(timeout: TimeSpan.FromSeconds(45));
    }

    /// <summary>
    /// Stops the proxy process.
    /// </summary>
    public async Task StopProxyAsync()
    {
        await KillProcessAsync(_proxyProcess, "proxy");
        _proxyProcess = null;
    }

    /// <summary>
    /// Waits for the proxy's /health endpoint to return 200.
    /// </summary>
    public async Task WaitForHealthAsync(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);
        var deadline = DateTime.UtcNow + timeout.Value;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await HttpClient.GetAsync("/health");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // Proxy not yet accepting connections
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Proxy did not become healthy within {timeout.Value.TotalSeconds}s");
    }

    /// <summary>
    /// Registers a route with the proxy via the /api/v1/add-host endpoint.
    /// </summary>
    public async Task<HttpResponseMessage> RegisterRouteAsync(string hostname, int targetPort, string? path = null)
    {
        var payload = new
        {
            Hostname = hostname,
            BackendUrl = $"http://localhost:{targetPort}",
            Path = path
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await HttpClient.PostAsync("/api/v1/add-host", content);
    }

    /// <summary>
    /// Removes a route from the proxy via DELETE /api/v1/remove-host.
    /// </summary>
    public async Task<HttpResponseMessage> RemoveRouteAsync(string hostname)
    {
        return await HttpClient.DeleteAsync($"/api/v1/remove-host?hostname={Uri.EscapeDataString(hostname)}");
    }

    /// <summary>
    /// Starts a simple test backend on the specified port that returns predictable responses.
    /// The backend returns "{backendId}:{requestPath}" for each request.
    /// Uses HttpListener for simplicity - no compilation needed.
    /// </summary>
    public async Task<int> StartTestBackendAsync(string backendId, int? preferredPort = null)
    {
        var port = preferredPort ?? GetFreePort();
        var cts = new CancellationTokenSource();

        // Start HttpListener in background
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();

        // Process requests in background
        _ = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var ctx = await listener.GetContextAsync();
                    var response = $"{backendId}:{ctx.Request.Url?.AbsolutePath ?? "/"}";
                    var buffer = System.Text.Encoding.UTF8.GetBytes(response);
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "text/plain";
                    await ctx.Response.OutputStream.WriteAsync(buffer, cts.Token);
                    ctx.Response.Close();
                }
                catch (HttpListenerException) when (cts.Token.IsCancellationRequested)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Ignore individual request errors
                }
            }
        }, cts.Token);

        // Track for cleanup
        _httpListeners.Add((listener, cts));

        // Verify the backend is responding
        await WaitForPortAsync(port, timeout: TimeSpan.FromSeconds(10));

        return port;
    }

    /// <summary>
    /// Gets the list of registered routes from the proxy.
    /// </summary>
    public async Task<JsonElement> GetRoutesAsync()
    {
        var response = await HttpClient.GetAsync("/api/v1/routes");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }

    /// <summary>
    /// Gets the proxy status from the proxy.
    /// </summary>
    public async Task<JsonElement> GetStatusAsync()
    {
        var response = await HttpClient.GetAsync("/api/v1/status");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }

    /// <summary>
    /// Runs a CLI command as a real process and returns the result.
    /// </summary>
    public async Task<(int ExitCode, string StandardOutput, string StandardError)> RunCliAsync(
        params string[] args)
    {
        var cliProjectPath = Path.Combine(_solutionRoot, "Portless.Cli", "Portless.Cli.csproj");
        var allArgs = $"run --project \"{cliProjectPath}\" {string.Join(" ", args)}";

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = allArgs,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = _solutionRoot
        };

        // Set environment for CLI isolation
        startInfo.Environment["PORTLESS_STATE_DIR"] = _stateDirectory;

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start CLI process");

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
        }

        return (process.ExitCode, output, error);
    }

    /// <summary>
    /// Gets the state directory used for this test run.
    /// </summary>
    public string StateDirectory => _stateDirectory;

    /// <summary>
    /// Gets the solution root directory.
    /// </summary>
    public string SolutionRoot => _solutionRoot;

    private static async Task KillProcessAsync(Process? process, string label)
    {
        if (process == null || process.HasExited) return;

        try
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
        catch
        {
            // Best-effort cleanup
        }
    }

    private static async Task WaitForPortAsync(int port, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var response = await client.GetAsync($"http://localhost:{port}/");
                return; // Port is responding
            }
            catch
            {
                await Task.Delay(300);
            }
        }
        throw new TimeoutException($"Backend on port {port} did not start within {timeout.TotalSeconds}s");
    }

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
