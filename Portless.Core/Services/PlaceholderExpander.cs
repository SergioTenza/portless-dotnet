namespace Portless.Core.Services;

/// <summary>
/// Expands placeholder tokens in command strings.
/// Supported: {PORT}, {HOST}, {URL}, {NAME}
/// </summary>
public static class PlaceholderExpander
{
    public static string Expand(string template, int port, string hostname)
    {
        var name = hostname.EndsWith(".localhost")
            ? hostname[..^".localhost".Length]
            : hostname;

        var url = $"http://{hostname}";

        return template
            .Replace("{PORT}", port.ToString())
            .Replace("{HOST}", "127.0.0.1")
            .Replace("{URL}", url)
            .Replace("{NAME}", name);
    }

    public static string[] ExpandArgs(string[] args, int port, string hostname)
    {
        return args.Select(arg => Expand(arg, port, hostname)).ToArray();
    }

    public static Dictionary<string, string> ExpandEnvVars(string[] templates, int port, string hostname)
    {
        var result = new Dictionary<string, string>();
        foreach (var template in templates)
        {
            var expanded = Expand(template, port, hostname);
            var eqIndex = expanded.IndexOf('=');
            if (eqIndex > 0)
            {
                result[expanded[..eqIndex]] = expanded[(eqIndex + 1)..];
            }
        }
        return result;
    }
}
