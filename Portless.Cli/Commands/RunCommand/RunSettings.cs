using System.ComponentModel;
using Spectre.Console.Cli;

namespace Portless.Cli.Commands.RunCommand;

public class RunSettings : CommandSettings
{
    [CommandArgument(0, "[NAME]")]
    public string Name { get; set; } = string.Empty;

    [CommandOption("-p|--path <PATH>")]
    [Description("Path prefix for path-based routing (e.g. /api)")]
    public string? Path { get; set; }
}
