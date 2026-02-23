using Spectre.Console;
using Spectre.Console.Cli;
using Portless.Core.Services;
using Microsoft.Extensions.Logging;

namespace Portless.Cli.Commands.CertCommand;

public class CertUninstallCommand : AsyncCommand<CertUninstallSettings>
{
    private readonly ICertificateManager _certificateManager;
    private readonly ICertificateTrustService _trustService;
    private readonly ILogger<CertUninstallCommand> _logger;

    public CertUninstallCommand(
        ICertificateManager certificateManager,
        ICertificateTrustService trustService,
        ILogger<CertUninstallCommand> logger)
    {
        _certificateManager = certificateManager;
        _trustService = trustService;
        _logger = logger;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CertUninstallSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Load CA certificate
            var cert = await _certificateManager.GetCertificateAuthorityAsync(cancellationToken);
            if (cert == null)
            {
                AnsiConsole.MarkupLine("[yellow]No certificate found.[/]");
                return 0; // Nothing to uninstall
            }

            // Uninstall certificate
            var removed = await _trustService.UninstallCertificateAuthorityAsync(cert.Thumbprint, cancellationToken);

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
