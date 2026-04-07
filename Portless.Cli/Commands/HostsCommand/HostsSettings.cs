using System.ComponentModel;
using Spectre.Console.Cli;

namespace Portless.Cli.Commands.HostsCommand;

public class HostsSettings : CommandSettings
{
    [CommandArgument(0, "<ACTION>")]
    [Description("Action to perform: 'sync' or 'clean'")]
    public string Action { get; init; } = string.Empty;
}
