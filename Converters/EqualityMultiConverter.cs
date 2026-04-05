using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace PosApp.Converters
{
    public class EqualityMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return false;
            
            var val1 = values[0];
            var val2 = values[1];

            if (val1 == null && val2 == null) return true;
            if (val1 == null || val2 == null) return false;

            return val1.ToString() == val2.ToString();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
            {
                // This is a bit tricky for TwoWay. For RadioButtons, we usually return 
                // the "other" value that was passed in.
                // In our case, values[1] is the category itself.
                // But ConvertBack doesn't have the original values.
                // So we'll return the value that should become the property.
                return new object[] { Binding.DoNothing, Binding.DoNothing }; // handle in separate command if needed
            }
            return Array.Empty<object>();
        }
    }
}
