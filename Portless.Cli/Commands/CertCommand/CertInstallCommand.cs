using Spectre.Console;
using Spectre.Console.Cli;
using Portless.Core.Services;
using Microsoft.Extensions.Logging;

namespace Portless.Cli.Commands.CertCommand;

public class CertInstallCommand : AsyncCommand<CertInstallSettings>
{
    private readonly ICertificateManager _certificateManager;
    private readonly ICertificateTrustServiceFactory _trustServiceFactory;
    private readonly ILogger<CertInstallCommand> _logger;

    public CertInstallCommand(
        ICertificateManager certificateManager,
        ICertificateTrustServiceFactory trustServiceFactory,
        ILogger<CertInstallCommand> logger)
    {
        _certificateManager = certificateManager;
        _trustServiceFactory = trustServiceFactory;
        _logger = logger;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CertInstallSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Create platform-specific trust service
            var trustService = _trustServiceFactory.CreateTrustService();

            // Check for admin privileges
            var isAdmin = await trustService.IsAdministratorAsync(cancellationToken);
            if (!isAdmin)
            {
                var platformInfo = _trustServiceFactory.PlatformDetector.GetPlatformInfo();
                AnsiConsole.MarkupLine("[yellow]Administrator privileges required[/]");
                AnsiConsole.MarkupLine($"[dim]Run: {platformInfo.ElevationCommand} portless cert install[/dim]");
                return 2; // Exit code for permissions denied
            }

            // Load certificate
            var cert = await _certificateManager.GetCertificateAuthorityAsync(cancellationToken);
            if (cert == null)
            {
                AnsiConsole.MarkupLine("[yellow]No certificate found.[/]");
                return 3; // Exit code for missing certificate
            }

            // Install certificate
            var result = await trustService.InstallCertificateAuthorityAsync(cert, cancellationToken);

            // Display result
            if (result.Success)
            {
                if (result.AlreadyInstalled)
                {
                    AnsiConsole.MarkupLine("[dim]Certificate already installed.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[green]✓ Certificate installed successfully[/]");
                }
                return 0;
            }
            else
            {
                if (result.StoreAccessDenied)
                {
                    AnsiConsole.MarkupLine("[red]✗ Access denied[/]");
                    if (result.ErrorMessage != null)
                    {
                        AnsiConsole.MarkupLine($"[dim]{result.ErrorMessage}[/dim]");
                    }
                    return 2;
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]✗ Installation failed[/]");
                    if (result.ErrorMessage != null)
                    {
                        AnsiConsole.MarkupLine($"[dim]Error: {result.ErrorMessage}[/dim]");
                    }
                    return 1;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install certificate");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
}
