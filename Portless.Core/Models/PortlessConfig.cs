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
    [YamlMember(Alias = "host")]
    public string? Host { get; set; }

    [YamlMember(Alias = "path")]
    public string? Path { get; set; }

    [YamlMember(Alias = "backends")]
    public List<string> Backends { get; set; } = new();

    [YamlMember(Alias = "loadBalance")]
    public string? LoadBalancePolicy { get; set; }

    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "http";

    [YamlMember(Alias = "listenPort")]
    public int? ListenPort { get; set; }
}
