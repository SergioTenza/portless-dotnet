using Spectre.Console.Cli;

namespace Portless.Cli.Commands.ProxyCommand;

public class ProxyStatusSettings : CommandSettings
{
    [CommandOption("-p|--protocol")]
    [Description("Show detailed protocol information")]
    public bool Protocol { get; set; }
}
