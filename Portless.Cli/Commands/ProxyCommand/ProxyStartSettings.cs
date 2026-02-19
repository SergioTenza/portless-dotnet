using Spectre.Console;
using Spectre.Console.Cli;

namespace Portless.Cli.Commands.ProxyCommand;

public class ProxyStartSettings : CommandSettings
{
    [CommandOption("--port <PORT>")]
    public int Port { get; set; } = 1355;
}
