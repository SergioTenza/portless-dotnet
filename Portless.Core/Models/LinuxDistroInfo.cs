namespace Portless.Core.Models;

/// <summary>
/// Information about a Linux distribution's certificate store configuration.
/// </summary>
/// <param name="CertificatePath">The file system path for CA certificates.</param>
/// <param name="UpdateCommand">The command to update the certificate store.</param>
/// <param name="CertificateFileName">The file name for the certificate (with extension).</param>
public record LinuxDistroInfo(
    string CertificatePath,
    string UpdateCommand,
    string CertificateFileName
);
