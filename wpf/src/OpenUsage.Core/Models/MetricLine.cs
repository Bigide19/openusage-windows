using System.Text.Json.Serialization;

namespace OpenUsage.Core.Models;

// Polymorphic type. The "type" field on the JSON wire (e.g., "progress") is the
// discriminator — handled by JsonPolymorphic, so we don't expose a Type property
// on the model itself (consumers pattern-match on subtype instead).
[JsonPolymorphic(
    TypeDiscriminatorPropertyName = "type",
    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
[JsonDerivedType(typeof(TextMetricLine), "text")]
[JsonDerivedType(typeof(ProgressMetricLine), "progress")]
[JsonDerivedType(typeof(BadgeMetricLine), "badge")]
public abstract class MetricLine
{
    public required string Label { get; init; }
    public string? Color { get; init; }
    public string? Subtitle { get; init; }
}

public sealed class TextMetricLine : MetricLine
{
    public required string Value { get; init; }
}

public sealed class ProgressMetricLine : MetricLine
{
    public required double Used { get; init; }
    public required double Limit { get; init; }
    public required ProgressFormat Format { get; init; }
    public DateTimeOffset? ResetsAt { get; init; }
    public long? PeriodDurationMs { get; init; }
}

public sealed class BadgeMetricLine : MetricLine
{
    public required string Text { get; init; }
}
