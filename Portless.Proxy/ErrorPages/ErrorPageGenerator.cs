using System.Text;
using System.Text.Json;

namespace Portless.Proxy.ErrorPages;

/// <summary>
/// Generates styled HTML error pages for common proxy errors.
/// </summary>
public static class ErrorPageGenerator
{
    private const string BrandColor = "#0070f3";
    private const string DarkBg = "#000000";
    private const string DarkSurface = "#111111";
    private const string DarkText = "#ededed";
    private const string DarkTextDim = "#888888";

    public static string Generate404(string hostname, IEnumerable<string> activeRoutes)
    {
        var sb = new StringBuilder();
        var routes = activeRoutes.ToList();

        sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.Append("<meta name='viewport' content='width=device-width,initial-scale=1'>");
        sb.Append($"<title>404 - {hostname}</title>");
        sb.Append("<style>");
        sb.Append(GetBaseStyles());
        sb.Append("</style></head><body>");
        sb.Append("<div class='container'>");
        sb.Append("<div class='code'>404</div>");
        sb.Append("<h1>Route Not Found</h1>");
        sb.Append($"<p class='dim'>No route is registered for <code>{hostname}</code></p>");

        if (routes.Count > 0)
        {
            sb.Append("<div class='routes'>");
            sb.Append("<h2>Active Routes</h2>");
            sb.Append("<ul>");
            foreach (var route in routes.OrderBy(r => r))
            {
                var name = route.Replace(".localhost", "");
                sb.Append($"<li><a href='http://{route}'>{name}</a> <span class='dim'>({route})</span></li>");
            }
            sb.Append("</ul></div>");
        }

        sb.Append("<div class='hint'>");
        sb.Append("Start a service with <code>portless run &lt;name&gt; &lt;command&gt;</code>");
        sb.Append("</div>");
        sb.Append("</div></body></html>");

        return sb.ToString();
    }

    public static string Generate502(string hostname, int? backendPort, string? reason)
    {
        var sb = new StringBuilder();

        sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.Append("<meta name='viewport' content='width=device-width,initial-scale=1'>");
        sb.Append($"<title>502 - {hostname}</title>");
        sb.Append("<style>");
        sb.Append(GetBaseStyles());
        sb.Append("</style></head><body>");
        sb.Append("<div class='container'>");
        sb.Append("<div class='code'>502</div>");
        sb.Append("<h1>Bad Gateway</h1>");
        sb.Append($"<p class='dim'>Backend for <code>{hostname}</code> is not responding</p>");

        if (backendPort.HasValue)
        {
            sb.Append($"<p class='dim'>Expected backend at <code>localhost:{backendPort}</code></p>");
        }

        var errorMessage = reason switch
        {
            "process_dead" => "The backend process has terminated. It may have crashed or been stopped.",
            "connection_refused" => "The backend is not accepting connections. It may still be starting up.",
            _ => "The backend process may have crashed or is still starting up."
        };

        sb.Append($"<p class='error-detail'>{errorMessage}</p>");

        sb.Append("<div class='hint'>");
        sb.Append("Check your service logs or restart it with <code>portless run</code>");
        sb.Append("</div>");
        sb.Append("</div></body></html>");

        return sb.ToString();
    }

    public static string Generate508(string hostname, int hopCount)
    {
        var sb = new StringBuilder();

        sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.Append("<meta name='viewport' content='width=device-width,initial-scale=1'>");
        sb.Append($"<title>508 - Loop Detected</title>");
        sb.Append("<style>");
        sb.Append(GetBaseStyles());
        sb.Append("</style></head><body>");
        sb.Append("<div class='container'>");
        sb.Append("<div class='code'>508</div>");
        sb.Append("<h1>Loop Detected</h1>");
        sb.Append($"<p class='dim'>Request for <code>{hostname}</code> is looping ({hopCount} hops detected)</p>");

        sb.Append("<div class='error-detail'>");
        sb.Append("<p>This usually happens when:</p>");
        sb.Append("<ul>");
        sb.Append("<li>The proxy is forwarding to itself</li>");
        sb.Append("<li>A backend redirects back to the proxy</li>");
        sb.Append("<li>Multiple proxies are chained together</li>");
        sb.Append("</ul>");
        sb.Append("</div>");

        sb.Append("<div class='hint'>");
        sb.Append("Check your route configuration with <code>portless list</code>");
        sb.Append("</div>");
        sb.Append("</div></body></html>");

        return sb.ToString();
    }

    private static string GetBaseStyles()
    {
        var sb = new StringBuilder();
        sb.AppendLine("* { margin: 0; padding: 0; box-sizing: border-box; }");
        sb.AppendLine("body {");
        sb.AppendLine("    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;");
        sb.AppendLine($"    background: {DarkBg}; color: {DarkText}; min-height: 100vh;");
        sb.AppendLine("    display: flex; align-items: center; justify-content: center;");
        sb.AppendLine("}");
        sb.AppendLine(".container { text-align: center; max-width: 600px; padding: 2rem; }");
        sb.AppendLine(".code {");
        sb.AppendLine("    font-size: 6rem; font-weight: 800;");
        sb.AppendLine($"    background: linear-gradient(135deg, {BrandColor}, #00a8ff);");
        sb.AppendLine("    -webkit-background-clip: text; -webkit-text-fill-color: transparent;");
        sb.AppendLine("    line-height: 1;");
        sb.AppendLine("}");
        sb.AppendLine("h1 { font-size: 1.5rem; margin: 1rem 0 0.5rem; font-weight: 600; }");
        sb.AppendLine($".dim {{ color: {DarkTextDim}; }}");
        sb.AppendLine("code {");
        sb.AppendLine($"    background: {DarkSurface}; padding: 0.2rem 0.5rem;");
        sb.AppendLine("    border-radius: 4px; font-size: 0.9rem;");
        sb.AppendLine("}");
        sb.AppendLine(".routes { margin: 2rem 0; text-align: left; }");
        sb.AppendLine($".routes h2 {{ font-size: 1rem; color: {DarkTextDim}; margin-bottom: 0.5rem; text-align: center; }}");
        sb.AppendLine(".routes ul { list-style: none; }");
        sb.AppendLine(".routes li {");
        sb.AppendLine("    padding: 0.5rem 1rem; margin: 0.25rem 0;");
        sb.AppendLine($"    background: {DarkSurface}; border-radius: 6px;");
        sb.AppendLine("}");
        sb.AppendLine($".routes a {{ color: {BrandColor}; text-decoration: none; font-weight: 500; }}");
        sb.AppendLine(".routes a:hover { text-decoration: underline; }");
        sb.AppendLine(".error-detail {");
        sb.AppendLine("    margin: 1rem 0; padding: 1rem;");
        sb.AppendLine($"    background: {DarkSurface}; border-radius: 8px;");
        sb.AppendLine("    border-left: 3px solid #ff4444;");
        sb.AppendLine("    text-align: left; font-size: 0.9rem;");
        sb.AppendLine("}");
        sb.AppendLine(".error-detail ul { margin: 0.5rem 0 0 1.5rem; }");
        sb.AppendLine(".hint {");
        sb.AppendLine("    margin-top: 2rem; padding: 1rem;");
        sb.AppendLine($"    background: {DarkSurface}; border-radius: 8px;");
        sb.AppendLine($"    font-size: 0.85rem; color: {DarkTextDim};");
        sb.AppendLine("}");
        sb.AppendLine(".hint code { background: #1a1a1a; }");
        return sb.ToString();
    }
}
