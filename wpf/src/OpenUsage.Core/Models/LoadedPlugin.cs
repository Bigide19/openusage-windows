namespace OpenUsage.Core.Models;

public sealed class LoadedPlugin
{
    public required PluginManifest Manifest { get; init; }
    public required string PluginDir { get; init; }
    public required string EntryScript { get; init; }
    public required string IconDataUrl { get; init; }
}
