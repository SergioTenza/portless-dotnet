using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Portless.Core.Services;
using Portless.Core.Configuration;

namespace Portless.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPortlessPersistence(this IServiceCollection services)
    {
        // Register port pool as singleton for thread-safe tracking
        services.AddSingleton<IPortPool, PortPool>();

        // Register port allocator as singleton
        services.AddSingleton<IPortAllocator, PortAllocator>();

        // Register route store as singleton (mutex is instance-based)
        services.AddSingleton<IRouteStore, RouteStore>();

        // Register cleanup service as hosted service
        services.AddHostedService<RouteCleanupService>();

        // Register process manager as singleton
        services.AddSingleton<IProcessManager, ProcessManager>();

        // Register process health monitor as hosted service
        services.AddHostedService<ProcessHealthMonitor>();

        return services;
    }

    public static IServiceCollection AddRouteFileWatcher(this IServiceCollection services)
    {
        // Register file watcher as hosted service
        services.AddHostedService<RouteFileWatcher>();

        return services;
    }
}
