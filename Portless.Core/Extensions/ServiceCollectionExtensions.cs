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

    /// <summary>
    /// Registers certificate-related services for HTTPS support.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPortlessCertificates(this IServiceCollection services)
    {
        // Register certificate permission service as singleton
        services.AddSingleton<ICertificatePermissionService, CertificatePermissionService>();

        // Register certificate storage service as singleton
        services.AddSingleton<ICertificateStorageService, CertificateStorageService>();

        // Register certificate generation service as singleton
        services.AddSingleton<ICertificateService, CertificateService>();

        // Register certificate manager as singleton
        services.AddSingleton<ICertificateManager, CertificateManager>();

        return services;
    }
}
