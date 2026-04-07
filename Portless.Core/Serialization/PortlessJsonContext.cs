using System.Text.Json;
using System.Text.Json.Serialization;
using Portless.Core.Models;

namespace Portless.Core.Serialization;

[JsonSerializable(typeof(RouteInfo[]))]
[JsonSerializable(typeof(RouteInfo))]
[JsonSerializable(typeof(HashSet<int>))]
[JsonSerializable(typeof(CertificateInfo))]
[JsonSerializable(typeof(AddHostPayload))]
[JsonSerializable(typeof(RemoveHostPayload))]
[JsonSerializable(typeof(TcpProxyPayload))]
[JsonSerializable(typeof(RouteListEntry[]))]
[JsonSerializable(typeof(RouteDetailEntry))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
public partial class PortlessJsonContext : JsonSerializerContext;

/// <summary>
/// Typed payload for the /api/v1/add-host endpoint (replaces anonymous type in ProxyRouteRegistrar).
/// </summary>
public record AddHostPayload(
    string Hostname,
    string? BackendUrl = null,
    string? Path = null,
    string[]? BackendUrls = null,
    string? LoadBalancePolicy = null
);

/// <summary>
/// Typed payload for the /api/v1/remove-host endpoint (replaces anonymous type in ProxyRouteRegistrar).
/// </summary>
public record RemoveHostPayload(string Hostname);

/// <summary>
/// Typed payload for the /api/v1/tcp/add endpoint (replaces anonymous type in TcpCommand).
/// </summary>
public record TcpProxyPayload(
    string Name,
    int ListenPort,
    string TargetHost,
    int TargetPort
);

/// <summary>
/// JSON output entry for list command --json output.
/// </summary>
public record RouteListEntry(
    string Name,
    string Hostname,
    string Url,
    int Port,
    int Pid,
    string CreatedAt,
    string? LastSeen
);

/// <summary>
/// JSON output entry for get command --json output.
/// </summary>
public record RouteDetailEntry(
    string Name,
    string Hostname,
    string Url,
    int Port,
    int Pid
);
