using System.ComponentModel;
using Spectre.Console.Cli;

namespace Portless.Cli.Commands.AliasCommand;

public class AliasSettings : CommandSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("The name for the alias (e.g. 'db', 'redis')")]
    public string Name { get; init; } = string.Empty;

    [CommandArgument(1, "[PORT]")]
    [Description("The target port number")]
    public int? Port { get; init; }

    [CommandOption("--remove")]
    [Description("Remove an existing alias")]
    [DefaultValue(false)]
    public bool Remove { get; init; }

    [CommandOption("--host")]
    [Description("Target host (default: localhost)")]
    [DefaultValue("localhost")]
    public string Host { get; init; } = "localhost";

    [CommandOption("--protocol")]
    [Description("Backend protocol (http or https)")]
    [DefaultValue("http")]
    public string Protocol { get; init; } = "http";

    [CommandOption("-p|--path <PATH>")]
    [Description("Path prefix for path-based routing (e.g. /api)")]
    public string? Path { get; init; }
}
