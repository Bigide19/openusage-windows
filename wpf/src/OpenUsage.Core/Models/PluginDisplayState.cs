namespace OpenUsage.Core.Models;

public sealed class PluginDisplayState
{
    public required PluginMeta Meta { get; init; }
    public PluginOutput? Data { get; set; }
    public bool Loading { get; set; }
    public string? Error { get; set; }
    public DateTime? LastManualRefreshAt { get; set; }
}
