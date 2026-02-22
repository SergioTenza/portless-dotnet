using Spectre.Console;
using Spectre.Console.Cli;

namespace Portless.Cli.Commands.ProxyCommand;

public class ProxyStartSettings : CommandSettings
{
    [CommandOption("--port <PORT>")]
    [Description("Proxy port (default: 1355)")]
    public int Port { get; set; } = 1355;
}
