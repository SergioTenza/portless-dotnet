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
            if (parent == dir) break;
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
            Pid = 0,
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
