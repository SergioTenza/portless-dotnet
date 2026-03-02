using Spectre.Console;
using Spectre.Console.Cli;
using Portless.Core.Services;
using Microsoft.Extensions.Logging;

namespace Portless.Cli.Commands.CertCommand;

public class CertRenewCommand : AsyncCommand<CertRenewSettings>
{
    private readonly ICertificateManager _certificateManager;
    private readonly ILogger<CertRenewCommand> _logger;

    public CertRenewCommand(
        ICertificateManager certificateManager,
        ILogger<CertRenewCommand> logger)
    {
        _certificateManager = certificateManager;
        _logger = logger;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CertRenewSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            AnsiConsole.MarkupLine("[bold]Certificate Renewal[/]");

            // Check current certificate status
            var status = await _certificateManager.EnsureCertificatesAsync(
                forceRegeneration: false,
                cancellationToken
            );

            if (settings.Force)
            {
                // Force renewal requested
                AnsiConsole.MarkupLine("[yellow]Force renewal requested...[/]");

                await _certificateManager.RegenerateCertificatesAsync(cancellationToken);

                // Get new certificate status
                var newStatus = await _certificateManager.EnsureCertificatesAsync(
                    forceRegeneration: false,
                    cancellationToken
                );

                AnsiConsole.MarkupLine("[green]✓ Certificate renewed successfully[/]");
                if (newStatus.Thumbprint != null)
                {
                    AnsiConsole.MarkupLine("[dim]New thumbprint: {0}[/]", newStatus.Thumbprint);
                }
                if (newStatus.ExpiresAt.HasValue)
                {
                    AnsiConsole.MarkupLine("[dim]Expires: {0}[/]", newStatus.ExpiresAt.Value.ToString("yyyy-MM-dd"));
                }

                // Display restart warning
                AnsiConsole.MarkupLine("\n[yellow]⚠ Restart required:[/] The proxy must be restarted to use the new certificate");
                AnsiConsole.MarkupLine("[dim]Run: portless proxy stop && portless proxy start[/]");

                return 0;
            }

            // Check if renewal is needed
            if (!status.NeedsRegeneration)
            {
                AnsiConsole.MarkupLine("[green]✓ Certificate is valid and does not need renewal[/]");
                AnsiConsole.MarkupLine("[dim]Status: {0}[/]", status.Message ?? "No message");
                if (status.ExpiresAt.HasValue)
                {
                    AnsiConsole.MarkupLine("[dim]Expires: {0}[/]", status.ExpiresAt.Value.ToString("yyyy-MM-dd"));
                }

                if (status.Thumbprint != null)
                {
                    AnsiConsole.MarkupLine("[dim]Thumbprint: {0}[/]", status.Thumbprint);
                }

                AnsiConsole.MarkupLine("\n[dim]Use --force to renew anyway[/]");

                return 0;
            }

            // Certificate needs renewal
            AnsiConsole.MarkupLine("[yellow]Certificate needs renewal:[/]");
            AnsiConsole.MarkupLine("[dim]{0}[/]", status.Message ?? "No message");

            // Check if auto-renewal is disabled
            if (settings.DisableAutoRenew)
            {
                AnsiConsole.MarkupLine("\n[yellow]Auto-renewal is disabled for this invocation[/]");
                AnsiConsole.MarkupLine("[dim]Run without --disable-auto-renew to renew automatically[/]");
                return 2; // Exit code 2 indicates renewal needed but not performed
            }

            // Perform renewal
            AnsiConsole.MarkupLine("\n[yellow]Renewing certificate...[/]");

            await _certificateManager.RegenerateCertificatesAsync(cancellationToken);

            // Get new certificate status
            var renewedStatus = await _certificateManager.EnsureCertificatesAsync(
                forceRegeneration: false,
                cancellationToken
            );

            AnsiConsole.MarkupLine("[green]✓ Certificate renewed successfully[/]");
            if (renewedStatus.Thumbprint != null)
            {
                AnsiConsole.MarkupLine("[dim]New thumbprint: {0}[/]", renewedStatus.Thumbprint);
            }
            if (renewedStatus.ExpiresAt.HasValue)
            {
                AnsiConsole.MarkupLine("[dim]Expires: {0}[/]", renewedStatus.ExpiresAt.Value.ToString("yyyy-MM-dd"));
            }

            // Display restart warning
            AnsiConsole.MarkupLine("\n[yellow]⚠ Restart required:[/] The proxy must be restarted to use the new certificate");
            AnsiConsole.MarkupLine("[dim]Run: portless proxy stop && portless proxy start[/]");

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to renew certificate");
            AnsiConsole.MarkupLine("[red]Error:[/] {0}", ex.Message);
            return 1;
        }
    }
}
