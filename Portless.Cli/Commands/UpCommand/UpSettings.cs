using Spectre.Console.Cli;
using System.ComponentModel;

namespace Portless.Cli.Commands.UpCommand;

public class UpSettings : CommandSettings
{
    [CommandOption("-f|--file <PATH>")]
    [Description("Path to portless.config.yaml (auto-detected if not specified)")]
    public string? ConfigFile { get; set; }
}
