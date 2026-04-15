using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OpenUsage.App.Converters;

/// <summary>
/// Converts a hex color string (e.g. "#FF5733") to a SolidColorBrush.
/// Returns DependencyProperty.UnsetValue when the string is null/empty so that
/// fallback or default values apply.
/// </summary>
public class ColorStringToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string colorStr || string.IsNullOrWhiteSpace(colorStr))
            return System.Windows.DependencyProperty.UnsetValue;

        try
        {
            var color = (Color)ColorConverter.ConvertFromString(colorStr);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
        catch
        {
            return System.Windows.DependencyProperty.UnsetValue;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
