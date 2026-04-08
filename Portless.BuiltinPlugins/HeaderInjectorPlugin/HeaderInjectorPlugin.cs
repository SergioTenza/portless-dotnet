using Portless.Plugin.SDK;

namespace HeaderInjectorPlugin;

/// <summary>
/// Built-in plugin that injects custom headers into proxied responses.
/// Config via plugin.yaml config section: key-value pairs where key is header name, value is header value.
/// </summary>
public class HeaderInjectorPlugin : PortlessPlugin
{
    public override string Name => "header-injector";
    public override string Version => "1.0.0";

    private IReadOnlyDictionary<string, string> _headers = new Dictionary<string, string>();
    private IPluginContext? _context;

    public override Task OnLoadAsync(IPluginContext context)
    {
        _context = context;
        _headers = context.Config;
        return Task.CompletedTask;
    }

    // Note: This plugin injects headers into the ProxyResult's ResponseHeaders dictionary.
    // Since ResponseHeaders is a mutable Dictionary<string, string>, we can add headers
    // directly. In a real scenario with an immutable result type, header injection would
    // require a custom middleware or a future plugin capability for modifying HttpContext.
    public override Task AfterProxyAsync(ProxyContext context, ProxyResult result)
    {
        foreach (var header in _headers)
        {
            result.ResponseHeaders[header.Key] = header.Value;
        }

        return Task.CompletedTask;
    }
}
