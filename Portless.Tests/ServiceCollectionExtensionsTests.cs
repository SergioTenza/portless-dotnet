using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portless.Core.Extensions;
using Portless.Core.Models;
using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPortlessPersistence_RegistersAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddPortlessPersistence();

        // Assert - Verify all services are registered
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IPortPool>());
        Assert.NotNull(provider.GetService<IPortAllocator>());
        Assert.NotNull(provider.GetService<IRouteStore>());
        Assert.NotNull(provider.GetService<IProcessManager>());
        Assert.NotNull(provider.GetService<IProjectNameDetector>());
        Assert.NotNull(provider.GetService<IFrameworkDetector>());
        Assert.NotNull(provider.GetService<IYarpConfigFactory>());
        Assert.NotNull(provider.GetService<IPortlessConfigLoader>());
        Assert.NotNull(provider.GetService<ITcpForwardingService>());
    }

    [Fact]
    public void AddPortlessPersistence_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddPortlessPersistence();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddPortlessPersistence_RegistersSingletons()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPortlessPersistence();
        var provider = services.BuildServiceProvider();

        // Act - resolve twice
        var portPool1 = provider.GetService<IPortPool>();
        var portPool2 = provider.GetService<IPortPool>();
        var routeStore1 = provider.GetService<IRouteStore>();
        var routeStore2 = provider.GetService<IRouteStore>();
        var processManager1 = provider.GetService<IProcessManager>();
        var processManager2 = provider.GetService<IProcessManager>();

        // Assert - same instance for singletons
        Assert.Same(portPool1, portPool2);
        Assert.Same(routeStore1, routeStore2);
        Assert.Same(processManager1, processManager2);
    }

    [Fact]
    public void AddPluginSystem_RegistersPluginLoader()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddPluginSystem();

        // Assert
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IPluginLoader>());
    }

    [Fact]
    public void AddPluginSystem_WithPluginsPath_RegistersPluginLoader()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddPluginSystem("/custom/plugins/path");

        // Assert
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IPluginLoader>());
    }

    [Fact]
    public void AddPluginSystem_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddPluginSystem();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddRequestInspector_RegistersRequestInspector()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddRequestInspector();

        // Assert
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IRequestInspector>());
    }

    [Fact]
    public void AddRequestInspector_WithCustomCapacity_RegistersRequestInspector()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddRequestInspector(capacity: 500);

        // Assert
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IRequestInspector>());
    }

    [Fact]
    public void AddRequestInspector_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRequestInspector();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddPortlessCertificates_RegistersAllCertificateServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddPortlessCertificates();

        // Assert
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IPlatformDetectorService>());
        Assert.NotNull(provider.GetService<ICertificatePermissionService>());
        Assert.NotNull(provider.GetService<ICertificateStorageService>());
        Assert.NotNull(provider.GetService<ICertificateService>());
        Assert.NotNull(provider.GetService<ICertificateManager>());
        Assert.NotNull(provider.GetService<ICertificateTrustService>());
        Assert.NotNull(provider.GetService<ICertificateTrustServiceFactory>());
    }

    [Fact]
    public void AddPortlessCertificates_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddPortlessCertificates();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddPortlessCertificates_RegistersPlatformDetectorAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPortlessCertificates();
        var provider = services.BuildServiceProvider();

        // Act
        var detector1 = provider.GetService<IPlatformDetectorService>();
        var detector2 = provider.GetService<IPlatformDetectorService>();

        // Assert
        Assert.Same(detector1, detector2);
    }

    [Fact]
    public void AddPortlessCertificateMonitoring_RegistersMonitoringService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder().Build();

        // Act
        services.AddPortlessCertificateMonitoring(config);

        // Assert - verify the service descriptor is registered
        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ICertificateMonitoringService));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddPortlessCertificateMonitoring_WithEnabled_SetsOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder().Build();

        // Act
        services.AddPortlessCertificateMonitoring(config, isEnabled: true);

        // Assert - check the options are configured
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CertificateMonitoringOptions>>();
        Assert.True(options.Value.IsEnabled);
    }

    [Fact]
    public void AddPortlessCertificateMonitoring_WithEnvironmentVariables_SetsOptions()
    {
        // Arrange
        Environment.SetEnvironmentVariable("PORTLESS_CERT_CHECK_INTERVAL_HOURS", "12");
        Environment.SetEnvironmentVariable("PORTLESS_CERT_WARNING_DAYS", "60");
        Environment.SetEnvironmentVariable("PORTLESS_AUTO_RENEW", "false");
        Environment.SetEnvironmentVariable("PORTLESS_ENABLE_MONITORING", "true");

        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            // Act
            services.AddPortlessCertificateMonitoring(config);

            // Assert
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<CertificateMonitoringOptions>>();
            Assert.Equal(12, options.Value.CheckIntervalHours);
            Assert.Equal(60, options.Value.WarningDays);
            Assert.False(options.Value.AutoRenew);
            Assert.True(options.Value.IsEnabled);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PORTLESS_CERT_CHECK_INTERVAL_HOURS", null);
            Environment.SetEnvironmentVariable("PORTLESS_CERT_WARNING_DAYS", null);
            Environment.SetEnvironmentVariable("PORTLESS_AUTO_RENEW", null);
            Environment.SetEnvironmentVariable("PORTLESS_ENABLE_MONITORING", null);
        }
    }

    [Fact]
    public void AddPortlessCertificateMonitoring_DefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder().Build();

        // Act
        services.AddPortlessCertificateMonitoring(config);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CertificateMonitoringOptions>>();
        Assert.Equal(6, options.Value.CheckIntervalHours);
        Assert.Equal(30, options.Value.WarningDays);
        Assert.True(options.Value.AutoRenew);
        Assert.False(options.Value.IsEnabled);
    }

    [Fact]
    public void AddRouteFileWatcher_RegistersHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddRouteFileWatcher();

        // Assert - should not throw
        var result = services.AddRouteFileWatcher();
        Assert.Same(services, result);
    }

    [Fact]
    public void AddConfigFileWatcher_RegistersHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddConfigFileWatcher();

        // Assert - should not throw
        var result = services.AddConfigFileWatcher();
        Assert.Same(services, result);
    }

    [Fact]
    public void AddPortlessCertificates_RegistersPlatformSpecificTrustServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddPortlessCertificates();

        // Assert - verify transient service descriptors are registered
        var macDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(CertificateTrustServiceMacOS));
        var linuxDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(CertificateTrustServiceLinux));

        Assert.NotNull(macDescriptor);
        Assert.Equal(ServiceLifetime.Transient, macDescriptor.Lifetime);
        Assert.NotNull(linuxDescriptor);
        Assert.Equal(ServiceLifetime.Transient, linuxDescriptor.Lifetime);
    }
}
