using Spectre.Console;
using Spectre.Console.Cli;
using Portless.Core.Services;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace Portless.Cli.Commands.CertCommand;

public class CertCheckCommand : AsyncCommand<CertCheckSettings>
{
    private readonly ICertificateManager _certificateManager;
    private readonly ICertificateStorageService _storageService;
    private readonly ILogger<CertCheckCommand> _logger;

    public CertCheckCommand(
        ICertificateManager certificateManager,
        ICertificateStorageService storageService,
        ILogger<CertCheckCommand> _logger)
    {
        _certificateManager = certificateManager;
        _storageService = storageService;
        this._logger = _logger;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CertCheckSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            AnsiConsole.MarkupLine("[bold]Certificate Status Check[/]");

            // Check if certificate files exist
            bool filesExist = await _storageService.CertificateFilesExistAsync(cancellationToken);

            if (!filesExist)
            {
                AnsiConsole.MarkupLine("[red]✗ No certificate found[/]");
                AnsiConsole.MarkupLine("\n[dim]Certificate files do not exist. Run 'portless proxy start --https' to generate certificates[/]");
                return 3; // Exit code 3 indicates certificate not found
            }

            // Load and check certificate
            X509Certificate2? cert = null;
            try
            {
                cert = await _certificateManager.GetServerCertificateAsync(cancellationToken);

                if (cert == null)
                {
                    AnsiConsole.MarkupLine("[red]✗ Failed to load certificate[/]");
                    AnsiConsole.MarkupLine("\n[dim]Certificate files may be corrupted. Run 'portless cert renew --force' to regenerate[/]");
                    return 3;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]✗ Certificate file is corrupted[/]");
                AnsiConsole.MarkupLine("[dim]Error: {0}[/]", ex.Message);
                AnsiConsole.MarkupLine("\n[dim]Run 'portless cert renew --force' to regenerate[/]");
                return 1;
            }

            // Calculate days until expiration
            var now = DateTimeOffset.UtcNow;
            var daysUntilExpiration = (cert.NotAfter - now).Days;
            var isExpired = now > cert.NotAfter;
            var isExpiringSoon = now > cert.NotAfter.AddDays(-30);

            // Display status with color coding
            string statusBadge;
            if (isExpired)
            {
                statusBadge = "[red on red] EXPIRED [/]";
            }
            else if (isExpiringSoon)
            {
                statusBadge = "[yellow on yellow] EXPIRING SOON [/]";
            }
            else
            {
                statusBadge = "[green on green] VALID [/]";
            }

            AnsiConsole.MarkupLine("\nStatus: {0}", statusBadge);

            // Display expiration information
            if (isExpired)
            {
                AnsiConsole.MarkupLine("[red]✗ Certificate expired on {0}[/]", cert.NotAfter.ToString("yyyy-MM-dd"));
                AnsiConsole.MarkupLine("\n[yellow]Action required:[/] Run 'portless cert renew' to generate a new certificate");
                return 2; // Exit code 2 indicates expired certificate
            }
            else if (isExpiringSoon)
            {
                AnsiConsole.MarkupLine("[yellow]⚠ Certificate expires in {0} days ({1})[/]",
                    daysUntilExpiration, cert.NotAfter.ToString("yyyy-MM-dd"));
                AnsiConsole.MarkupLine("\n[yellow]Recommended:[/] Run 'portless cert renew' to renew before expiration");
            }
            else
            {
                AnsiConsole.MarkupLine("[green]✓ Certificate valid for {0} days ({1})[/]",
                    daysUntilExpiration, cert.NotAfter.ToString("yyyy-MM-dd"));
            }

            // Display verbose information if requested
            if (settings.Verbose)
            {
                AnsiConsole.MarkupLine("\n[bold]Certificate Details:[/]");
                AnsiConsole.MarkupLine("  [dim]Subject:[/]");
                AnsiConsole.MarkupLine("    {0}", cert.Subject);
                AnsiConsole.MarkupLine("  [dim]Issuer:[/]");
                AnsiConsole.MarkupLine("    {0}", cert.Issuer);
                AnsiConsole.MarkupLine("  [dim]Valid From:[/]");
                AnsiConsole.MarkupLine("    {0}", cert.NotBefore.ToString("yyyy-MM-dd HH:mm:ss"));
                AnsiConsole.MarkupLine("  [dim]Valid To:[/]");
                AnsiConsole.MarkupLine("    {0}", cert.NotAfter.ToString("yyyy-MM-dd HH:mm:ss"));
                AnsiConsole.MarkupLine("  [dim]SHA-256 Thumbprint:[/]");
                AnsiConsole.MarkupLine("    {0}", cert.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256));
                AnsiConsole.MarkupLine("  [dim]Serial Number:[/]");
                AnsiConsole.MarkupLine("    {0}", cert.SerialNumber);
                AnsiConsole.MarkupLine("  [dim]Version:[/]");
                AnsiConsole.MarkupLine("    {0}", cert.Version);

                // Check file permissions
                try
                {
                    var stateDir = Portless.Core.Configuration.StateDirectoryProvider.GetStateDirectory();
                    var certFile = System.IO.Path.Combine(stateDir, "cert.pfx");
                    var caFile = System.IO.Path.Combine(stateDir, "ca.pfx");

                    if (System.IO.File.Exists(certFile))
                    {
                        var certInfo = new System.IO.FileInfo(certFile);
                        AnsiConsole.MarkupLine("  [dim]Certificate File:[/]");
                        AnsiConsole.MarkupLine("    {0} ({1} bytes)", certFile, certInfo.Length);
                    }

                    if (System.IO.File.Exists(caFile))
                    {
                        var caInfo = new System.IO.FileInfo(caFile);
                        AnsiConsole.MarkupLine("  [dim]CA File:[/]");
                        AnsiConsole.MarkupLine("    {0} ({1} bytes)", caFile, caInfo.Length);
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine("  [dim]File Information:[/]");
                    AnsiConsole.MarkupLine("    [yellow]Unable to read file information: {0}[/]", ex.Message);
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check certificate status");
            AnsiConsole.MarkupLine("[red]Error:[/] {0}", ex.Message);
            return 1;
        }
    }
}
