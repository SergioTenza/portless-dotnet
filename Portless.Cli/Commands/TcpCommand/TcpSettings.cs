using System.ComponentModel;
using Spectre.Console.Cli;

namespace Portless.Cli.Commands.TcpCommand;

public class TcpSettings : CommandSettings
{
    [CommandArgument(0, "[NAME]")]
    [Description("Name for the TCP proxy")]
    public string? Name { get; set; }

    [CommandArgument(1, "[TARGET]")]
    [Description("Target host:port (e.g. localhost:6379)")]
    public string? Target { get; set; }

    [CommandOption("-l|--listen <PORT>")]
    [Description("Port to listen on")]
    public int? ListenPort { get; set; }

    [CommandOption("--remove")]
    [Description("Remove a TCP proxy")]
    public bool Remove { get; set; }
}
