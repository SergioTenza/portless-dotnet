using Spectre.Console.Cli;

namespace Portless.Cli.Commands.ProxyCommand;

public class ProxyStatusSettings : CommandSettings
{
    [CommandOption("-p|--protocol")]
    public bool Protocol { get; set; }
}
