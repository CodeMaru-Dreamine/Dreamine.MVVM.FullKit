using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DreamineVMS.Views;

public sealed class BoolToBrushConverter : IValueConverter
{
    public static readonly BoolToBrushConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Brushes.ForestGreen : Brushes.Gray;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BoolToStatusTextConverter : IValueConverter
{
    public static readonly BoolToStatusTextConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "● 연결됨" : "○ 미연결";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
