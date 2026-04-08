namespace Portless.Plugin.SDK;

/// <summary>
/// Describes a plugin's metadata. Typically authored as a JSON or YAML manifest
/// file bundled alongside the plugin assembly.
/// </summary>
public class PluginManifest
{
    /// <summary>
    /// Human-readable plugin name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Semantic version of the plugin.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Short description of what the plugin does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Author or organisation name.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// List of hook names the plugin subscribes to
    /// (e.g. "BeforeProxy", "AfterProxy", "OnError").
    /// </summary>
    public List<string> Hooks { get; set; } = [];

    /// <summary>
    /// Default configuration key-value pairs.
    /// </summary>
    public Dictionary<string, string> Config { get; set; } = [];

    /// <summary>
    /// Filename of the entry assembly (e.g. "MyPlugin.dll").
    /// </summary>
    public string EntryAssembly { get; set; } = string.Empty;
}
