using Spectre.Console;
using Spectre.Console.Cli;
using Portless.Core.Services;
using Microsoft.Extensions.Logging;

namespace Portless.Cli.Commands.CertCommand;

public class CertStatusCommand : AsyncCommand<CertStatusSettings>
{
    private readonly ICertificateManager _certificateManager;
    private readonly ICertificateTrustServiceFactory _trustServiceFactory;
    private readonly ILogger<CertStatusCommand> _logger;

    public CertStatusCommand(
        ICertificateManager certificateManager,
        ICertificateTrustServiceFactory trustServiceFactory,
        ILogger<CertStatusCommand> logger)
    {
        _certificateManager = certificateManager;
        _trustServiceFactory = trustServiceFactory;
        _logger = logger;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CertStatusSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Create platform-specific trust service
            var trustService = _trustServiceFactory.CreateTrustService();

            // Load certificate metadata (works on all platforms)
            var metadata = await _certificateManager.GetCertificateStatusAsync(cancellationToken);

            if (metadata == null)
            {
                AnsiConsole.MarkupLine("[yellow]No certificate found.[/]");
                return 0;
            }

            // Platform detection: certificate trust status is platform-specific
            var platformInfo = _trustServiceFactory.PlatformDetector.GetPlatformInfo();
            if (platformInfo.OSPlatform != System.Runtime.InteropServices.OSPlatform.Windows)
            {
                // Display certificate file information (no trust status on non-Windows)
                AnsiConsole.MarkupLine("Certificate: [green]Valid[/]");
                CertFormatter.WriteCertMetadata(metadata.Sha256Thumbprint, metadata.ExpiresAt);
                AnsiConsole.MarkupLine("\n[yellow]Trust Status: Manual installation required[/]");
                AnsiConsole.MarkupLine("[dim]Certificate trust installation is Windows-only in v1.2.[/dim]");
                AnsiConsole.MarkupLine("\n[bold]Manual trust required:[/]");
                AnsiConsole.MarkupLine("macOS: sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain ~/.portless/ca.pfx");
                AnsiConsole.MarkupLine("Linux: Copy ca.pfx to /usr/local/share/ca-certificates/ and run 'sudo update-ca-certificates'");
                return 0; // Certificate is valid, trust is just manual
            }

            // Load CA certificate (Windows-specific trust status below)
            var cert = await _certificateManager.GetCertificateAuthorityAsync(cancellationToken);

            if (cert == null)
            {
                AnsiConsole.MarkupLine("[yellow]No certificate found.[/]");
                return 0;
            }

            // Get trust status
            var status = await trustService.GetTrustStatusAsync(cert.Thumbprint, cancellationToken);

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
                CertFormatter.WriteCertMetadata(metadata.Sha256Thumbprint, metadata.ExpiresAt, "  ");
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
