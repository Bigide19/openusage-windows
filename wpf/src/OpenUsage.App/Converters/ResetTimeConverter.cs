using System.Globalization;
using System.Windows.Data;

namespace OpenUsage.App.Converters;

/// <summary>
/// Converts a DateTimeOffset? (ResetsAt) to a human-readable relative time string
/// such as "Resets in 3d 5h" or "Resets in 2h 30m".
/// </summary>
public class ResetTimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DateTimeOffset resetsAt)
            return string.Empty;

        var remaining = resetsAt - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero)
            return "Resetting...";

        if (remaining.TotalDays >= 1)
            return $"Resets in {(int)remaining.TotalDays}d {remaining.Hours}h";
        if (remaining.TotalHours >= 1)
            return $"Resets in {(int)remaining.TotalHours}h {remaining.Minutes}m";
        if (remaining.TotalMinutes >= 1)
            return $"Resets in {(int)remaining.TotalMinutes}m";

        return "Resets soon";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
