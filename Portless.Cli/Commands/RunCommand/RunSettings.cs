using Spectre.Console;
using Spectre.Console.Cli;

namespace Portless.Cli.Commands.RunCommand;

public class RunSettings : CommandSettings
{
    [CommandArgument(0, "[NAME]")]
    [Description("Hostname for the app (e.g., \"myapi\", \"chat\")")]
    public string Name { get; set; } = string.Empty;
}
