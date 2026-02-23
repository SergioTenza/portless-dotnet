namespace Portless.Core.Models;

/// <summary>
/// Result of a certificate trust installation operation.
/// </summary>
/// <param name="Success">True if the operation completed successfully.</param>
/// <param name="AlreadyInstalled">True if the certificate was already installed.</param>
/// <param name="StoreAccessDenied">True if access to the certificate store was denied.</param>
/// <param name="ErrorMessage">Error message if the operation failed.</param>
public record TrustInstallResult(
    bool Success,
    bool AlreadyInstalled,
    bool StoreAccessDenied,
    string? ErrorMessage
);
