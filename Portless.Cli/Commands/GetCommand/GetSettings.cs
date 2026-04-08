using System.ComponentModel;
using Spectre.Console.Cli;

namespace Portless.Cli.Commands.GetCommand;

public class GetSettings : CommandSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("The name of the service to get the URL for")]
    public string Name { get; init; } = string.Empty;

    [CommandOption("--json")]
    [Description("Output as JSON")]
    [DefaultValue(false)]
    public bool Json { get; init; }
}
