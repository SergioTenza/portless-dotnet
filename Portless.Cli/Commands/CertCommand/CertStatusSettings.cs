using Spectre.Console.Cli;

namespace Portless.Cli.Commands.CertCommand;

public class CertStatusSettings : CommandSettings
{
    [CommandOption("--verbose")]
    public bool Verbose { get; set; }
}
