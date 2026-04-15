using System.Globalization;
using System.Windows.Data;
using OpenUsage.Core.Enums;
using OpenUsage.Core.Models;

namespace OpenUsage.App.Converters;

public class ProgressFormatConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3)
            return string.Empty;

        if (values[0] is not double used || values[1] is not double limit)
            return string.Empty;

        var format = values[2] as ProgressFormat;
        // Default to "left" (remaining) display mode, matching original macOS app
        var remaining = Math.Max(0, limit - used);

        if (format is null)
            return $"{remaining:N0} left";

        return format.Kind switch
        {
            ProgressFormatKind.Percent => $"{(limit > 0 ? remaining / limit * 100 : 0):F0}% left",
            ProgressFormatKind.Dollars => $"${remaining / 100:F2} left",
            ProgressFormatKind.Count => string.IsNullOrEmpty(format.Suffix)
                ? $"{remaining:N0} left"
                : $"{remaining:N0} {format.Suffix} left",
            _ => $"{remaining:N0} left"
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
