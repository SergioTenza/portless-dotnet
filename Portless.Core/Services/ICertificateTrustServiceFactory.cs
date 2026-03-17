// Portless.Core/Services/ICertificateTrustServiceFactory.cs
namespace Portless.Core.Services;

/// <summary>
/// Factory for creating platform-specific certificate trust services.
/// </summary>
public interface ICertificateTrustServiceFactory
{
    /// <summary>
    /// Creates the appropriate certificate trust service for the current platform.
    /// </summary>
    /// <returns>An implementation of ICertificateTrustService for the detected platform.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown when the platform is not supported.</exception>
    ICertificateTrustService CreateTrustService();
}
