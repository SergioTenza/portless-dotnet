using Spectre.Console.Cli;
using System.ComponentModel;

namespace Portless.Cli.Commands.PluginCommand;

public sealed class PluginSettings : CommandSettings
{
    [CommandArgument(0, "[action]")]
    [Description("Action: list, install, uninstall, enable, disable, create")]
    public string? Action { get; set; }

    [CommandArgument(1, "[target]")]
    [Description("Plugin name or path (depends on action)")]
    public string? Target { get; set; }

    [CommandOption("--enable")]
    [Description("Enable plugin after install")]
    public bool Enable { get; set; }
}
