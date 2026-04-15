namespace OpenUsage.Core.Models;

public sealed class PluginManifest
{
    public int SchemaVersion { get; init; } = 1;
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required string Entry { get; init; }
    public string? Icon { get; init; }
    public string? BrandColor { get; init; }
    public List<PluginLink>? Links { get; init; }
    public required List<ManifestLine> Lines { get; init; }
}
