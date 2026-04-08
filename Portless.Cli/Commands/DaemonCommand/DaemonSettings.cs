using System.ComponentModel;
using Spectre.Console.Cli;

namespace Portless.Cli.Commands.DaemonCommand;

public class DaemonInstallSettings : CommandSettings
{
    [CommandOption("--https")]
    [Description("Enable HTTPS endpoint")]
    public bool EnableHttps { get; set; } = false;

    [CommandOption("--enable")]
    [Description("Enable the service to auto-start on boot")]
    public bool EnableNow { get; set; } = false;
}

public class DaemonUninstallSettings : CommandSettings
{
}

public class DaemonStatusSettings : CommandSettings
{
}

public class DaemonEnableSettings : CommandSettings
{
}

public class DaemonDisableSettings : CommandSettings
{
}
