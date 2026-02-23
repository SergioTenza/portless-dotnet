using Spectre.Console.Cli;

namespace Portless.Cli.Commands.CertCommand;

public class CertCheckSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets whether to display verbose information.
    /// </summary>
    public bool Verbose { get; set; }
}
