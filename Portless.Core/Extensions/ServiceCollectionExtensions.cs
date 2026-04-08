using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Portless.Core.Services;
using Portless.Core.Configuration;
using Portless.Core.Models;

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

        // Register project name detector
        services.AddSingleton<IProjectNameDetector, ProjectNameDetector>();

        // Register framework detector
        services.AddSingleton<IFrameworkDetector, FrameworkDetector>();

        // Register YARP config factory
        services.AddSingleton<IYarpConfigFactory, YarpConfigFactory>();

        // Register config loader
        services.AddSingleton<IPortlessConfigLoader, PortlessConfigLoader>();

        // Register TCP forwarding service
        services.AddSingleton<ITcpForwardingService, TcpForwardingService>();
        services.AddHostedService(sp => (TcpForwardingService)sp.GetRequiredService<ITcpForwardingService>());

        return services;
    }

    public static IServiceCollection AddRouteFileWatcher(this IServiceCollection services)
    {
        // Register file watcher as hosted service
        services.AddHostedService<RouteFileWatcher>();

        return services;
    }

    public static IServiceCollection AddConfigFileWatcher(this IServiceCollection services)
    {
        // Register config file watcher as hosted service
        services.AddHostedService<ConfigFileWatcher>();

        return services;
    }

    /// <summary>
    /// Registers certificate-related services for HTTPS support.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <summary>
    /// Registers the plugin system services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="pluginsPath">Optional path to plugins directory (default: ~/.portless/plugins).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPluginSystem(this IServiceCollection services, string? pluginsPath = null)
    {
        // PluginLoader constructor takes only ILogger; pluginsPath is passed to LoadAllAsync at startup.
        _ = pluginsPath; // suppress unused parameter warning
        services.AddSingleton<IPluginLoader, PluginLoader>();
        return services;
    }

    /// <summary>
    /// Registers the request inspector with a configurable ring buffer capacity.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="capacity">Ring buffer capacity (default: 1000).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRequestInspector(this IServiceCollection services, int capacity = 1000)
    {
        services.AddSingleton<IRequestInspector>(sp => new RequestInspectorService(capacity));
        return services;
    }

    public static IServiceCollection AddPortlessCertificates(this IServiceCollection services)
    {
        // Register platform detector service as singleton
        services.TryAddSingleton<IPlatformDetectorService, PlatformDetectorService>();

        // Register certificate permission service as singleton
        services.AddSingleton<ICertificatePermissionService, CertificatePermissionService>();

        // Register certificate storage service as singleton
        services.AddSingleton<ICertificateStorageService, CertificateStorageService>();

        // Register certificate generation service as singleton
        services.AddSingleton<ICertificateService, CertificateService>();

        // Register certificate manager as singleton
        services.AddSingleton<ICertificateManager, CertificateManager>();

        // Register certificate trust service (Windows-only)
        services.AddSingleton<ICertificateTrustService, CertificateTrustService>();

        // Register certificate trust service factory as singleton
        services.TryAddSingleton<ICertificateTrustServiceFactory, CertificateTrustServiceFactory>();

        // Register platform-specific certificate trust services
        services.AddTransient<CertificateTrustServiceMacOS>();
        services.AddTransient<CertificateTrustServiceLinux>();

        return services;
    }

    /// <summary>
    /// Registers certificate monitoring service for automatic expiration checks and renewal.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="isEnabled">Whether monitoring is enabled (default: from PORTLESS_ENABLE_MONITORING).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPortlessCertificateMonitoring(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isEnabled = false)
    {
        // Configure options from environment variables
        services.Configure<CertificateMonitoringOptions>(options =>
        {
            // Check interval (default: 6 hours)
            if (int.TryParse(configuration["PORTLESS_CERT_CHECK_INTERVAL_HOURS"], out var checkInterval))
            {
                options.CheckIntervalHours = checkInterval;
            }

            // Warning days (default: 30)
            if (int.TryParse(configuration["PORTLESS_CERT_WARNING_DAYS"], out var warningDays))
            {
                options.WarningDays = warningDays;
            }

            // Auto-renew (default: true)
            if (bool.TryParse(configuration["PORTLESS_AUTO_RENEW"], out var autoRenew))
            {
                options.AutoRenew = autoRenew;
            }

            // Enable monitoring (default: false)
            var enableFromConfig = configuration["PORTLESS_ENABLE_MONITORING"] == "true";
            options.IsEnabled = isEnabled || enableFromConfig;
        });

        // Register monitoring service as singleton and hosted service
        services.AddSingleton<ICertificateMonitoringService, CertificateMonitoringService>();
        services.AddHostedService<CertificateMonitoringService>(sp => (CertificateMonitoringService)sp.GetRequiredService<ICertificateMonitoringService>());

        return services;
    }
}
