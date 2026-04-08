using System.ComponentModel;
using Spectre.Console.Cli;

namespace Portless.Cli.Commands.CompletionCommand;

public class CompletionSettings : CommandSettings
{
    [CommandArgument(0, "[SHELL]")]
    [Description("Shell type: bash, zsh, fish, or powershell")]
    public string? Shell { get; set; }
}
