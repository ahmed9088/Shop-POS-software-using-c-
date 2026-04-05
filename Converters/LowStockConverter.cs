using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PosApp.Converters
{
    /// <summary>
    /// Returns Visible when stock is > 0 and < 20 (low stock warning).
    /// Returns Collapsed otherwise (healthy stock or zero/service items).
    /// </summary>
    public class LowStockConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return targetType == typeof(Visibility) ? Visibility.Collapsed : (object)false;

            double stock = 0;
            try { stock = System.Convert.ToDouble(value); } catch { return targetType == typeof(Visibility) ? Visibility.Collapsed : (object)false; }

            bool isLowStock = stock > 0 && stock < 20;

            if (targetType == typeof(Visibility))
            {
                return isLowStock ? Visibility.Visible : Visibility.Collapsed;
            }

            return isLowStock;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Returns Visible when value > 0 (used for Change Due display).
    /// </summary>
    public class PositiveToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Visibility.Collapsed;
            try
            {
                double num = System.Convert.ToDouble(value);
                return num > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { return Visibility.Collapsed; }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Returns Visible when string is not null/empty (used for error messages).
    /// </summary>
    public class StringNotEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
