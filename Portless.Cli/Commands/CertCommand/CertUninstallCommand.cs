using Spectre.Console;
using Spectre.Console.Cli;
using Portless.Core.Services;
using Microsoft.Extensions.Logging;

namespace Portless.Cli.Commands.CertCommand;

public class CertUninstallCommand : AsyncCommand<CertUninstallSettings>
{
    private readonly ICertificateManager _certificateManager;
    private readonly ICertificateTrustServiceFactory _trustServiceFactory;
    private readonly ILogger<CertUninstallCommand> _logger;

    public CertUninstallCommand(
        ICertificateManager certificateManager,
        ICertificateTrustServiceFactory trustServiceFactory,
        ILogger<CertUninstallCommand> logger)
    {
        _certificateManager = certificateManager;
        _trustServiceFactory = trustServiceFactory;
        _logger = logger;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CertUninstallSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Create platform-specific trust service
            var trustService = _trustServiceFactory.CreateTrustService();

            // Platform detection: certificate trust uninstallation is platform-specific
            var platformInfo = _trustServiceFactory.PlatformDetector.GetPlatformInfo();
            if (platformInfo.OSPlatform != System.Runtime.InteropServices.OSPlatform.Windows)
            {
                AnsiConsole.MarkupLine("[yellow]Warning:[/] Certificate trust uninstallation is Windows-only in v1.2.");
                AnsiConsole.MarkupLine("\n[bold]Manual uninstallation required for macOS/Linux:[/]");
                AnsiConsole.MarkupLine("\nmacOS: Run 'sudo security delete-certificate -c \"Portless Local CA\" /Library/Keychains/System.keychain'");
                AnsiConsole.MarkupLine("Linux: Remove ca.pfx from /usr/local/share/ca-certificates/ and run 'sudo update-ca-certificates --fresh'");
                return 1; // Exit code 1: Platform not supported
            }

            // Load CA certificate
            var cert = await _certificateManager.GetCertificateAuthorityAsync(cancellationToken);
            if (cert == null)
            {
                AnsiConsole.MarkupLine("[yellow]No certificate found.[/]");
                return 0; // Nothing to uninstall
            }

            // Uninstall certificate
            var removed = await trustService.UninstallCertificateAuthorityAsync(cert.Thumbprint, cancellationToken);

            if (removed)
            {
                AnsiConsole.MarkupLine("[green]✓[/] Certificate uninstalled successfully");
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Certificate not found in trust store.[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall certificate authority");
            AnsiConsole.MarkupLine("[red]Error:[/] {0}", ex.Message);
            return 1;
        }
    }
}
