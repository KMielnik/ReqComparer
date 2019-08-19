using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

public class TestCaseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var tcIDsValue = value as HashSet<int>;
        return tcIDsValue.Contains((int)parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}