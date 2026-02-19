using Portless.Core.Models;

namespace Portless.Core.Services;

public interface IRouteStore
{
    Task<RouteInfo[]> LoadRoutesAsync(CancellationToken cancellationToken = default);
    Task SaveRoutesAsync(RouteInfo[] routes, CancellationToken cancellationToken = default);
}
