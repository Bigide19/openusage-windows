using System.Globalization;
using System.Windows.Data;

namespace OpenUsage.App.Converters;

public class ProgressWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3) return 0.0;

        if (values[0] is not double used || values[1] is not double limit || values[2] is not double totalWidth)
            return 0.0;

        if (limit <= 0) return 0.0;

        // Display mode "left": fill bar proportional to REMAINING (matches "X% left" label)
        var remaining = Math.Max(0, limit - used);
        var ratio = Math.Min(remaining / limit, 1.0);
        return ratio * totalWidth;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
