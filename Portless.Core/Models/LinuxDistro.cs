namespace Portless.Core.Models;

/// <summary>
/// Supported Linux distributions for certificate trust installation.
/// </summary>
public enum LinuxDistro
{
    /// <summary>Ubuntu Linux</summary>
    Ubuntu,

    /// <summary>Debian Linux</summary>
    Debian,

    /// <summary>Fedora Linux</summary>
    Fedora,

    /// <summary>Red Hat Enterprise Linux</summary>
    RHEL,

    /// <summary>Arch Linux</summary>
    Arch,

    /// <summary>Unknown or unsupported distribution</summary>
    Unknown
}
