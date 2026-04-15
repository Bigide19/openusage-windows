using Microsoft.Win32;

namespace OpenUsage.Services;

public sealed class AutoStartService
{
    private const string AppName = "OpenUsage";
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public void Enable()
    {
        var processPath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot determine process path.");

        var value = $"\"{processPath}\" --minimized";

        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.SetValue(AppName, value, RegistryValueKind.String);
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.DeleteValue(AppName, throwOnMissingValue: false);
    }

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue(AppName) is not null;
    }
}
