using System.Globalization;
using System.Windows.Data;

namespace WindowsClockWidget;

public class RepeatTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is RepeatType r ? r switch
        {
            RepeatType.None => "Yok",
            RepeatType.Daily => "Günlük",
            RepeatType.Weekly => "Haftalık",
            RepeatType.Monthly => "Aylık",
            _ => ""
        } : "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
