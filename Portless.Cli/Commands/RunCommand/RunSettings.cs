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

    [CommandOption("-b|--backend <URL>")]
    [Description("Additional backend URL for load balancing (can be repeated). First backend is auto-assigned.")]
    public string[] Backends { get; set; } = Array.Empty<string>();
}
