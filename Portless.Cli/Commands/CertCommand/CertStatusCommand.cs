using Spectre.Console;
using Spectre.Console.Cli;
using Portless.Core.Services;
using Microsoft.Extensions.Logging;

namespace Portless.Cli.Commands.CertCommand;

public class CertStatusCommand : AsyncCommand<CertStatusSettings>
{
    private readonly ICertificateManager _certificateManager;
    private readonly ICertificateTrustService _trustService;
    private readonly ILogger<CertStatusCommand> _logger;

    public CertStatusCommand(
        ICertificateManager certificateManager,
        ICertificateTrustService trustService,
        ILogger<CertStatusCommand> logger)
    {
        _certificateManager = certificateManager;
        _trustService = trustService;
        _logger = logger;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CertStatusSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Load CA certificate
            var cert = await _certificateManager.GetCertificateAuthorityAsync(cancellationToken);
            
            // Load certificate metadata
            var metadata = await _certificateManager.GetCertificateStatusAsync(cancellationToken);
            
            if (cert == null || metadata == null)
            {
                AnsiConsole.MarkupLine("[yellow]No certificate found.[/]");
                return 0;
            }

            // Get trust status
            var status = await _trustService.GetTrustStatusAsync(cert.Thumbprint, cancellationToken);

            // Display status with color coding
            switch (status)
            {
                case Portless.Core.Models.TrustStatus.Trusted:
                    AnsiConsole.MarkupLine("[green]✓ Trusted[/]");
                    break;
                case Portless.Core.Models.TrustStatus.NotTrusted:
                    AnsiConsole.MarkupLine("[red]✗ Not Trusted[/]");
                    break;
                case Portless.Core.Models.TrustStatus.ExpiringSoon:
                    AnsiConsole.MarkupLine("[yellow]⚠ Expiring Soon[/]");
                    break;
                case Portless.Core.Models.TrustStatus.Unknown:
                    AnsiConsole.MarkupLine("[yellow]Unknown (non-Windows platform)[/]");
                    break;
            }

            // Display verbose information if requested
            if (settings.Verbose)
            {
                AnsiConsole.MarkupLine("\n[bold]Certificate Details:[/]");
                AnsiConsole.MarkupLine("  SHA-256: {0}", metadata.Sha256Thumbprint ?? "N/A");
                AnsiConsole.MarkupLine("  Expires: {0}", metadata.ExpiresAt ?? "N/A");
                AnsiConsole.MarkupLine("  Store: Cert:\\LocalMachine\\Root");
                AnsiConsole.MarkupLine("  Subject: {0}", cert.Subject);
                AnsiConsole.MarkupLine("  Issuer: {0}", cert.Issuer);
                AnsiConsole.MarkupLine("  Serial: {0}", cert.SerialNumber);
            }

            // Show installation instructions if not trusted
            if (status == Portless.Core.Models.TrustStatus.NotTrusted)
            {
                AnsiConsole.MarkupLine("\n[yellow]To install:[/] Run as Administrator: portless cert install");
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get certificate status");
            AnsiConsole.MarkupLine("[red]Error:[/] {0}", ex.Message);
            return 1;
        }
    }
}
