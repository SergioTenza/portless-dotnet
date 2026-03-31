// Portless.Tests/CertificateTrustServiceFactoryTests.cs
using Portless.Core.Services;
using Xunit;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Portless.Tests;

public class CertificateTrustServiceFactoryTests
{
    [Fact]
    public void CreateTrustService_ReturnsCorrectImplementation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPlatformDetectorService, PlatformDetectorService>();
        services.AddSingleton<ICertificateTrustServiceFactory, CertificateTrustServiceFactory>();
        services.AddSingleton<ICertificateManager, CertificateManager>();
#pragma warning disable CA1416 // Platform-specific services registered for factory resolution
        services.AddTransient<CertificateTrustService>();
        services.AddTransient<CertificateTrustServiceMacOS>();
        services.AddTransient<CertificateTrustServiceLinux>();
#pragma warning restore CA1416

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<ICertificateTrustServiceFactory>();

        // Act
        var service = factory.CreateTrustService();

        // Assert
        Assert.NotNull(service);
        Assert.IsAssignableFrom<ICertificateTrustService>(service);
    }
}
