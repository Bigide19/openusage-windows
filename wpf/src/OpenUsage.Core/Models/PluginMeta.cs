namespace OpenUsage.Core.Models;

public sealed class PluginMeta
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string IconUrl { get; init; }
    public string? BrandColor { get; init; }
    public required List<ManifestLine> Lines { get; init; }
    public List<PluginLink>? Links { get; init; }
    public required List<string> PrimaryCandidates { get; init; }
}
