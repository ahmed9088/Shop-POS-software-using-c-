using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PosApp.Converters;

public class EqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Equals(value, parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true) return parameter;
        return Binding.DoNothing;
    }
}
