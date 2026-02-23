using System.Diagnostics;
using Spectre.Console;
using Spectre.Console.Cli;
using Portless.Core.Services;
using Microsoft.Extensions.Logging;

namespace Portless.Cli.Commands.CertCommand;

public class CertInstallCommand : AsyncCommand<CertInstallSettings>
{
    private readonly ICertificateManager _certificateManager;
    private readonly ICertificateTrustService _trustService;
    private readonly ILogger<CertInstallCommand> _logger;

    public CertInstallCommand(
        ICertificateManager certificateManager,
        ICertificateTrustService trustService,
        ILogger<CertInstallCommand> logger)
    {
        _certificateManager = certificateManager;
        _trustService = trustService;
        _logger = logger;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CertInstallSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Platform detection: certificate trust installation is Windows-only in v1.2
            if (!OperatingSystem.IsWindows())
            {
                AnsiConsole.MarkupLine("[yellow]Warning:[/] Certificate trust installation is Windows-only in v1.2.");
                AnsiConsole.MarkupLine("\n[bold]Manual installation required for macOS/Linux:[/]");
                AnsiConsole.MarkupLine("\n1. Locate CA certificate: ~/.portless/ca.pfx");
                AnsiConsole.MarkupLine("2. macOS: Run 'sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain ~/.portless/ca.pfx'");
                AnsiConsole.MarkupLine("3. Linux: Copy ca.pfx to /usr/local/share/ca-certificates/ and run 'sudo update-ca-certificates' (distribution-specific)");
                return 1; // Exit code 1: Platform not supported
            }

            // Check admin status
            var isAdmin = await _trustService.IsAdministratorAsync(cancellationToken);
            if (!isAdmin)
            {
                AnsiConsole.MarkupLine("[yellow]Administrator privileges required. Attempting to elevate...[/]");
                
                try
                {
                    // Restart current process with admin privileges
                    var currentProcess = Process.GetCurrentProcess();
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = Environment.ProcessPath,
                        Arguments = "cert install",
                        Verb = "runas", // Triggers UAC prompt
                        UseShellExecute = true
                    };
                    
                    Process.Start(startInfo);
                    return 0; // Exit current process, elevated process continues
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to elevate privileges");
                    AnsiConsole.MarkupLine("[red]Error:[/] Administrator privileges required. UAC prompt was declined or failed.");
                    return 2; // Exit code 2: Insufficient permissions
                }
            }

            // Load CA certificate
            var cert = await _certificateManager.GetCertificateAuthorityAsync(cancellationToken);
            if (cert == null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No certificate found. Run 'portless proxy start' to generate.");
                return 3; // Exit code 3: Certificate file missing
            }

            // Install certificate
            var result = await _trustService.InstallCertificateAuthorityAsync(cert, cancellationToken);

            if (result.Success)
            {
                if (result.AlreadyInstalled)
                {
                    AnsiConsole.MarkupLine("[green]✓[/] Certificate already installed");
                }
                else
                {
                    AnsiConsole.MarkupLine("[green]✓[/] Certificate installed successfully");
                }
                return 0;
            }
            else if (result.StoreAccessDenied)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Certificate store access denied. Check permissions.");
                return 5; // Exit code 5: Certificate store access denied
            }
            else if (result.ErrorMessage != null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] {0}", result.ErrorMessage);
                return 1; // Exit code 1: Generic error
            }

            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install certificate authority");
            AnsiConsole.MarkupLine("[red]Error:[/] {0}", ex.Message);
            return 1;
        }
    }
}
