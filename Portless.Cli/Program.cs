using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using Portless.Cli.Commands.ProxyCommand;
using Portless.Cli.Commands.CertCommand;
using Portless.Cli.Commands.RunCommand;
using Portless.Cli.Commands.ListCommand;
using Portless.Cli.DependencyInjection;
using Portless.Cli.Services;
using Portless.Core.Extensions;

// Create service collection
var services = new ServiceCollection();

// Register Core services (includes PortPool, PortAllocator, RouteStore, and CleanupService)
services.AddPortlessPersistence();

// Register certificate services (includes trust service)
services.AddPortlessCertificates();

// Register CLI services
services.AddSingleton<IProxyProcessManager, ProxyProcessManager>();
services.AddHttpClient();

// Configure command app with dependency injection
var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.AddCommand<RunCommand>("run")
        .WithAlias("r")
        .WithDescription("Run an app with a named URL")
        .WithExample("run", "testapi", "dotnet", "run", "--project", "TestApi/TestApi.csproj");

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
