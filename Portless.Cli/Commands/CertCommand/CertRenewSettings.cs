using Spectre.Console.Cli;

namespace Portless.Cli.Commands.CertCommand;

public class CertRenewSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets whether to force renewal regardless of expiration status.
    /// </summary>
    public bool Force { get; set; }

    /// <summary>
    /// Gets or sets whether to disable auto-renewal for this invocation.
    /// </summary>
    public bool DisableAutoRenew { get; set; }
}
