using Microsoft.Win32;
using OpenUsage.Core.Enums;

namespace OpenUsage.Services;

public sealed class ThemeService : IDisposable
{
    private const string PersonalizeKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightTheme = "AppsUseLightTheme";

    private bool _disposed;

    public event EventHandler<ThemeMode>? SystemThemeChanged;

    public ThemeService()
    {
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public ThemeMode GetSystemTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey, writable: false);
        var value = key?.GetValue(AppsUseLightTheme);

        return value is int intValue && intValue == 0
            ? ThemeMode.Dark
            : ThemeMode.Light;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
        {
            var theme = GetSystemTheme();
            SystemThemeChanged?.Invoke(this, theme);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
    }
}
