using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenUsage.Core.Enums;
using OpenUsage.Core.Models;

namespace OpenUsage.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private bool _suppressSettingsModified;

    [ObservableProperty]
    private AutoUpdateInterval _autoUpdateInterval;

    [ObservableProperty]
    private ThemeMode _themeMode;

    [ObservableProperty]
    private DisplayMode _displayMode;

    [ObservableProperty]
    private ResetTimerDisplayMode _resetTimerDisplayMode;

    [ObservableProperty]
    private string? _globalShortcut;

    [ObservableProperty]
    private bool _startOnLogin;

    public ObservableCollection<PluginSettingItem> PluginItems { get; } = [];

    public event Action<AppSettings>? SettingsModified;

    public void LoadSettings(AppSettings settings, List<PluginMeta> allPlugins)
    {
        _suppressSettingsModified = true;
        try
        {
            AutoUpdateInterval = settings.AutoUpdateInterval;
            ThemeMode = settings.ThemeMode;
            DisplayMode = settings.DisplayMode;
            ResetTimerDisplayMode = settings.ResetTimerDisplayMode;
            GlobalShortcut = settings.GlobalShortcut;
            StartOnLogin = settings.StartOnLogin;

            PluginItems.Clear();

            var disabled = settings.Plugins.Disabled.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var ordered = settings.Plugins.Order;

            // Build ordered list: first items in Order, then remaining plugins
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in ordered)
            {
                var plugin = allPlugins.FirstOrDefault(
                    p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
                if (plugin is null) continue;

                seen.Add(id);
                var item = new PluginSettingItem
                {
                    Id = plugin.Id,
                    Name = plugin.Name,
                    IsEnabled = !disabled.Contains(plugin.Id)
                };
                item.EnabledChanged += () => RaiseSettingsModified();
                PluginItems.Add(item);
            }

            foreach (var plugin in allPlugins.Where(p => !seen.Contains(p.Id)))
            {
                var item = new PluginSettingItem
                {
                    Id = plugin.Id,
                    Name = plugin.Name,
                    IsEnabled = !disabled.Contains(plugin.Id)
                };
                item.EnabledChanged += () => RaiseSettingsModified();
                PluginItems.Add(item);
            }
        }
        finally
        {
            _suppressSettingsModified = false;
        }
    }

    public AppSettings BuildAppSettings()
    {
        return new AppSettings
        {
            AutoUpdateInterval = AutoUpdateInterval,
            ThemeMode = ThemeMode,
            DisplayMode = DisplayMode,
            ResetTimerDisplayMode = ResetTimerDisplayMode,
            GlobalShortcut = GlobalShortcut,
            StartOnLogin = StartOnLogin,
            Plugins = new PluginSettings
            {
                Order = PluginItems.Select(p => p.Id).ToList(),
                Disabled = PluginItems.Where(p => !p.IsEnabled).Select(p => p.Id).ToList()
            }
        };
    }

    [RelayCommand]
    private void MoveUp(PluginSettingItem item)
    {
        var index = PluginItems.IndexOf(item);
        if (index <= 0) return;

        PluginItems.Move(index, index - 1);
        RaiseSettingsModified();
    }

    [RelayCommand]
    private void MoveDown(PluginSettingItem item)
    {
        var index = PluginItems.IndexOf(item);
        if (index < 0 || index >= PluginItems.Count - 1) return;

        PluginItems.Move(index, index + 1);
        RaiseSettingsModified();
    }

    [RelayCommand]
    private void ToggleEnabled(PluginSettingItem item)
    {
        item.IsEnabled = !item.IsEnabled;
        RaiseSettingsModified();
    }

    partial void OnAutoUpdateIntervalChanged(AutoUpdateInterval value) => RaiseSettingsModified();
    partial void OnThemeModeChanged(ThemeMode value) => RaiseSettingsModified();
    partial void OnDisplayModeChanged(DisplayMode value) => RaiseSettingsModified();
    partial void OnResetTimerDisplayModeChanged(ResetTimerDisplayMode value) => RaiseSettingsModified();
    partial void OnGlobalShortcutChanged(string? value) => RaiseSettingsModified();
    partial void OnStartOnLoginChanged(bool value) => RaiseSettingsModified();

    private void RaiseSettingsModified()
    {
        if (_suppressSettingsModified) return;
        SettingsModified?.Invoke(BuildAppSettings());
    }
}

public partial class PluginSettingItem : ObservableObject
{
    public required string Id { get; init; }
    public required string Name { get; init; }

    [ObservableProperty]
    private bool _isEnabled;

    public event Action? EnabledChanged;

    partial void OnIsEnabledChanged(bool value)
    {
        EnabledChanged?.Invoke();
    }
}
