namespace Portless.Core.Models;

/// <summary>
/// Represents the trust status of a certificate in the system certificate store.
/// </summary>
public enum TrustStatus
{
    /// <summary>
    /// Certificate is trusted and installed in the system store.
    /// </summary>
    Trusted,

    /// <summary>
    /// Certificate is not trusted or not installed in the system store.
    /// </summary>
    NotTrusted,

    /// <summary>
    /// Certificate is trusted but will expire within 30 days.
    /// </summary>
    ExpiringSoon,

    /// <summary>
    /// Trust status could not be determined (e.g., on non-Windows platforms).
    /// </summary>
    Unknown
}
