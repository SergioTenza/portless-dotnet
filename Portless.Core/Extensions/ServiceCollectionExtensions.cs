using Microsoft.Extensions.DependencyInjection;
using Portless.Core.Services;
using Portless.Core.Configuration;

namespace Portless.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPortlessPersistence(this IServiceCollection services)
    {
        // Register route store as singleton (mutex is instance-based)
        services.AddSingleton<IRouteStore, RouteStore>();

        // Register cleanup service as hosted service
        services.AddHostedService<RouteCleanupService>();

        return services;
    }

    public static IServiceCollection AddRouteFileWatcher(this IServiceCollection services)
    {
        // Register file watcher as hosted service
        services.AddHostedService<RouteFileWatcher>();

        return services;
    }
}
