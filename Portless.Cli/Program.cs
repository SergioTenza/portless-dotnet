using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using Portless.Cli.Commands.ProxyCommand;
using Portless.Cli.Commands.CertCommand;
using Portless.Cli.Commands.RunCommand;
using Portless.Cli.Commands.ListCommand;
using Portless.Cli.Commands.GetCommand;
using Portless.Cli.Commands.AliasCommand;
using Portless.Cli.Commands.HostsCommand;
using Portless.Cli.Commands.UpCommand;
using Portless.Cli.Commands.TcpCommand;
using Portless.Cli.Commands.CompletionCommand;
using Portless.Cli.DependencyInjection;
using Portless.Cli.Services;
using Portless.Core.Extensions;
using Portless.Core.Services;

// Create service collection
var services = new ServiceCollection();

// Register Core services (includes PortPool, PortAllocator, RouteStore, and CleanupService)
services.AddPortlessPersistence();

// Register certificate services (includes trust service)
services.AddPortlessCertificates();

// Register CLI services
services.AddSingleton<IProxyProcessManager, ProxyProcessManager>();
services.AddSingleton<IProxyRouteRegistrar, ProxyRouteRegistrar>();
services.AddHttpClient();

// Configure command app with dependency injection
var registrar = new TypeRegistrar(services);
#pragma warning disable IL3050 // Spectre.Console.Cli relies on reflection — not AOT-compatible by design
var app = new CommandApp(registrar);
#pragma warning restore IL3050

app.Configure(config =>
{
    config.AddCommand<RunCommand>("run")
        .WithAlias("r")
        .WithDescription("Run an app with a named URL")
        .WithExample("run", "testapi", "dotnet", "run", "--project", "TestApi/TestApi.csproj");

    config.AddCommand<ListCommand>("list")
        .WithAlias("ls")
        .WithDescription("List active routes");

    config.AddCommand<GetCommand>("get")
        .WithAlias("g")
        .WithDescription("Get the URL for a named service")
        .WithExample("get", "api");

    config.AddCommand<AliasCommand>("alias")
        .WithDescription("Manage static route aliases for Docker/external services")
        .WithExample("alias", "db", "5432")
        .WithExample("alias", "--remove", "db");

    config.AddCommand<HostsCommand>("hosts")
        .WithDescription("Manage /etc/hosts entries for portless routes")
        .WithExample("hosts", "sync")
        .WithExample("hosts", "clean");

    config.AddCommand<UpCommand>("up")
        .WithDescription("Start routes from portless.config.yaml")
        .WithExample("up")
        .WithExample("up", "-f", "./my-config.yaml");

    config.AddCommand<TcpCommand>("tcp")
        .WithDescription("Manage TCP proxy routes for databases and services")
        .WithExample("tcp", "redis", "localhost:6379", "--listen", "16379")
        .WithExample("tcp", "redis", "--remove");

    config.AddCommand<CompletionCommand>("completion")
        .WithDescription("Generate shell completion scripts")
        .WithExample("completion", "bash")
        .WithExample("completion", "zsh")
        .WithExample("completion", "fish");

    config.AddBranch("proxy", proxy =>
    {
        proxy.AddCommand<ProxyStartCommand>("start")
            .WithDescription("Start the proxy server");
        proxy.AddCommand<ProxyStopCommand>("stop")
            .WithDescription("Stop the proxy server");
        proxy.AddCommand<ProxyStatusCommand>("status")
            .WithDescription("Check proxy status");
    });

    config.AddBranch("cert", cert =>
    {
        cert.AddCommand<CertInstallCommand>("install")
            .WithDescription("Install CA certificate to system trust store");
        cert.AddCommand<CertStatusCommand>("status")
            .WithDescription("Display certificate trust status");
        cert.AddCommand<CertUninstallCommand>("uninstall")
            .WithDescription("Remove CA certificate from trust store");
        cert.AddCommand<CertCheckCommand>("check")
            .WithDescription("Check certificate expiration and validity");
        cert.AddCommand<CertRenewCommand>("renew")
            .WithDescription("Renew certificate (auto-renews if expiring soon)");
    });
});

// Run the app
return await app.RunAsync(args);
