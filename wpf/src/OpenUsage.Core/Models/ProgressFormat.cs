using OpenUsage.Core.Enums;

namespace OpenUsage.Core.Models;

public sealed class ProgressFormat
{
    public required ProgressFormatKind Kind { get; init; }
    public string? Suffix { get; init; }
}
