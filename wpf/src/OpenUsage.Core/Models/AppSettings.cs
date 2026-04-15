using OpenUsage.Core.Enums;

namespace OpenUsage.Core.Models;

public sealed class AppSettings
{
    public PluginSettings Plugins { get; set; } = new();
    public AutoUpdateInterval AutoUpdateInterval { get; set; } = AutoUpdateInterval.Minutes15;
    public ThemeMode ThemeMode { get; set; } = ThemeMode.System;
    public DisplayMode DisplayMode { get; set; } = DisplayMode.Left;
    public ResetTimerDisplayMode ResetTimerDisplayMode { get; set; } = ResetTimerDisplayMode.Relative;
    public string? GlobalShortcut { get; set; }
    public bool StartOnLogin { get; set; }
}

public sealed class PluginSettings
{
    public List<string> Order { get; set; } = [];
    public List<string> Disabled { get; set; } = [];
}
