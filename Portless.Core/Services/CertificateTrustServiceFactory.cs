// Portless.Core/Services/CertificateTrustServiceFactory.cs
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>
/// Factory that creates platform-specific certificate trust services.
/// </summary>
public class CertificateTrustServiceFactory : ICertificateTrustServiceFactory
{
    private readonly IPlatformDetectorService _platformDetector;
    private readonly ILogger<CertificateTrustServiceFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

    public CertificateTrustServiceFactory(
        IPlatformDetectorService platformDetector,
        ILogger<CertificateTrustServiceFactory> logger,
        IServiceProvider serviceProvider)
    {
        _platformDetector = platformDetector;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public ICertificateTrustService CreateTrustService()
    {
        var platformInfo = _platformDetector.GetPlatformInfo();

        // CA1416: Platform-specific services are selected based on detected platform at runtime
#pragma warning disable CA1416
        if (platformInfo.OSPlatform == OSPlatform.Windows)
        {
            _logger.LogInformation("Creating Windows certificate trust service");
            return _serviceProvider.GetRequiredService<CertificateTrustService>();
        }

        if (platformInfo.OSPlatform == OSPlatform.OSX)
        {
            _logger.LogInformation("Creating macOS certificate trust service");
            return _serviceProvider.GetRequiredService<CertificateTrustServiceMacOS>();
        }

        if (platformInfo.OSPlatform == OSPlatform.Linux)
        {
            _logger.LogInformation("Creating Linux certificate trust service for distro: {Distro}", platformInfo.LinuxDistro);
            return _serviceProvider.GetRequiredService<CertificateTrustServiceLinux>();
        }
#pragma warning restore CA1416

        throw new PlatformNotSupportedException($"Unsupported platform: {platformInfo.OSPlatform}");
    }

    /// <summary>
    /// Gets the platform detector service for external access.
    /// </summary>
    public IPlatformDetectorService PlatformDetector => _platformDetector;
}
