namespace Portless.Plugin.SDK;

/// <summary>
/// Injected context provided to the plugin during <see cref="PortlessPlugin.OnLoadAsync"/>.
/// Gives access to configuration and the plugin's deployment directory.
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// Key-value configuration pairs supplied to the plugin.
    /// </summary>
    IReadOnlyDictionary<string, string> Config { get; }

    /// <summary>
    /// Absolute path to the directory where the plugin assembly and assets live.
    /// </summary>
    string PluginDirectory { get; }
}
