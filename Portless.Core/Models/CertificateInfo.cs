using System.Text.Json.Serialization;

namespace Portless.Core.Models;

/// <summary>
/// Certificate metadata stored in cert-info.json for lifecycle management.
/// </summary>
public class CertificateInfo
{
    /// <summary>
    /// Version of the certificate metadata format.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// SHA-256 thumbprint of the certificate.
    /// </summary>
    [JsonPropertyName("sha256Thumbprint")]
    public string Sha256Thumbprint { get; set; } = string.Empty;

    /// <summary>
    /// Creation date in ISO 8601 format (UTC).
    /// </summary>
    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>
    /// Expiration date in ISO 8601 format (UTC).
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public string ExpiresAt { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 thumbprint of the CA certificate that signed this certificate.
    /// </summary>
    [JsonPropertyName("caThumbprint")]
    public string CaThumbprint { get; set; } = string.Empty;

    /// <summary>
    /// Creation date as Unix timestamp (seconds since epoch).
    /// </summary>
    [JsonPropertyName("createdAtUnix")]
    public long CreatedAtUnix { get; set; }

    /// <summary>
    /// Expiration date as Unix timestamp (seconds since epoch).
    /// </summary>
    [JsonPropertyName("expiresAtUnix")]
    public long ExpiresAtUnix { get; set; }
}
