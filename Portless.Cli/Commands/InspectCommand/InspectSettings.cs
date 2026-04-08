using Spectre.Console.Cli;
using System.ComponentModel;

namespace Portless.Cli.Commands.InspectCommand;

public sealed class InspectSettings : CommandSettings
{
    [CommandOption("-f|--filter <filter>")]
    [Description("Filter: host:pattern, method:GET, status:5xx, path:/api")]
    public string? Filter { get; set; }

    [CommandOption("-s|--save <path>")]
    [Description("Export captured requests to JSONL file")]
    public string? SavePath { get; set; }

    [CommandOption("-n|--number <count>")]
    [Description("Number of recent requests to show")]
    [DefaultValue(50)]
    public int Count { get; set; } = 50;

    [CommandOption("--live")]
    [Description("Live-stream requests as they arrive")]
    public bool Live { get; set; }
}
