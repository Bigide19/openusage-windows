using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OpenUsage.App.Converters;

/// <summary>
/// Converts used/limit values to a pace dot color:
/// &lt;50% green (ahead), 50-80% yellow (on track), &gt;80% red (behind).
/// </summary>
public class PaceDotColorConverter : IMultiValueConverter
{
    private static readonly SolidColorBrush GreenBrush = new(Color.FromRgb(0x4A, 0xDE, 0x80));
    private static readonly SolidColorBrush YellowBrush = new(Color.FromRgb(0xFA, 0xCC, 0x15));
    private static readonly SolidColorBrush RedBrush = new(Color.FromRgb(0xF8, 0x71, 0x71));

    static PaceDotColorConverter()
    {
        GreenBrush.Freeze();
        YellowBrush.Freeze();
        RedBrush.Freeze();
    }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return YellowBrush;

        if (values[0] is not double used || values[1] is not double limit || limit <= 0)
            return YellowBrush;

        var ratio = used / limit;

        if (ratio < 0.5)
            return GreenBrush;
        if (ratio <= 0.8)
            return YellowBrush;
        return RedBrush;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
