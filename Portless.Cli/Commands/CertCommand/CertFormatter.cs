using Spectre.Console;

namespace Portless.Cli.Commands.CertCommand;

/// <summary>
/// Common formatting helpers for certificate display across
/// CertCheck, CertRenew, and CertStatus commands.
/// </summary>
internal static class CertFormatter
{
    /// <summary>
    /// Displays thumbprint and expiration info from a renewal status.
    /// Used by CertRenewCommand in multiple places.
    /// </summary>
    public static void WriteRenewalInfo(string? thumbprint, DateTimeOffset? expiresAt)
    {
        if (thumbprint != null)
        {
            AnsiConsole.MarkupLine("[dim]New thumbprint: {0}[/]", thumbprint);
        }
        if (expiresAt.HasValue)
        {
            AnsiConsole.MarkupLine("[dim]Expires: {0}[/]", expiresAt.Value.ToString("yyyy-MM-dd"));
        }
    }

    /// <summary>
    /// Displays SHA-256 and expiration from cert metadata.
    /// Used by CertStatusCommand.
    /// </summary>
    public static void WriteCertMetadata(string? sha256, string? expiresAt, string indent = "")
    {
        if (!string.IsNullOrEmpty(indent))
        {
            AnsiConsole.MarkupLine($"{indent}SHA-256: {{0}}", sha256 ?? "N/A");
            AnsiConsole.MarkupLine($"{indent}Expires: {{0}}", expiresAt ?? "N/A");
        }
        else
        {
            AnsiConsole.MarkupLine("SHA-256: {0}", sha256 ?? "N/A");
            AnsiConsole.MarkupLine("Expires: {0}", expiresAt ?? "N/A");
        }
    }

    /// <summary>
    /// Formats a date as yyyy-MM-dd for display.
    /// </summary>
    public static string FormatDate(DateTime date) => date.ToString("yyyy-MM-dd");

    /// <summary>
    /// Formats a date as yyyy-MM-dd HH:mm:ss for detailed display.
    /// </summary>
    public static string FormatDateTime(DateTime date) => date.ToString("yyyy-MM-dd HH:mm:ss");
}
