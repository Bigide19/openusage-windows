namespace OpenUsage.Core.Models;

public sealed class PluginOutput
{
    public required string ProviderId { get; init; }
    public required string DisplayName { get; init; }
    public string? Plan { get; init; }
    public required List<MetricLine> Lines { get; init; }
    public required string IconUrl { get; init; }
}
