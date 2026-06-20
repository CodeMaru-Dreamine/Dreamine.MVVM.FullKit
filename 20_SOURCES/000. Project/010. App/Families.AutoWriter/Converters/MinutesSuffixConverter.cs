using System.Globalization;
using System.Windows.Data;

namespace FamiliesAutoWriter.Converters;

public sealed class MinutesSuffixConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        $"{value}분";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
