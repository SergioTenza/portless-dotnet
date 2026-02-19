using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using Portless.Cli.Commands.ProxyCommand;
using Portless.Cli.Commands.RunCommand;
using Portless.Cli.Commands.ListCommand;
using Portless.Cli.DependencyInjection;
using Portless.Cli.Services;
using Portless.Core.Extensions;

// Create service collection
var services = new ServiceCollection();

// Register Core services
services.AddPortlessPersistence();

// Register CLI services
services.AddSingleton<IProxyProcessManager, ProxyProcessManager>();
services.AddSingleton<IPortAllocator, PortAllocator>();
services.AddHttpClient();

// Configure command app with dependency injection
var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.AddCommand<RunCommand>("run")
        .WithAlias("r")
        .WithDescription("Run an app with a named URL");

    config.AddCommand<ListCommand>("list")
        .WithAlias("ls")
        .WithDescription("List active routes");

    config.AddBranch("proxy", proxy =>
    {
        proxy.AddCommand<ProxyStartCommand>("start")
            .WithDescription("Start the proxy server");
        proxy.AddCommand<ProxyStopCommand>("stop")
            .WithDescription("Stop the proxy server");
        proxy.AddCommand<ProxyStatusCommand>("status")
            .WithDescription("Check proxy status");
    });
});

// Run the app
return await app.RunAsync(args);
