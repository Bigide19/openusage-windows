using OpenUsage.Core.Enums;

namespace OpenUsage.Core.Models;

public sealed class ManifestLine
{
    public required MetricLineType Type { get; init; }
    public required string Label { get; init; }
    public required MetricLineScope Scope { get; init; }
    public int? PrimaryOrder { get; init; }
}
