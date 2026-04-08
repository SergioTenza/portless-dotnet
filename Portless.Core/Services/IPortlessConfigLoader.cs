using Portless.Core.Models;

namespace Portless.Core.Services;

public interface IPortlessConfigLoader
{
    string? FindConfigFile(string? startDir = null);
    PortlessConfig Load(string? path = null);
    RouteInfo[] ToRouteInfos(PortlessConfig config);
}
