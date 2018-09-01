using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace BitnuaVideoPlayer
{
    [ValueConversion(typeof(bool), typeof(GridLength))]
    public class BoolToGridRowHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var len = new GridLength(1, GridUnitType.Star);
            if (parameter is string)
            {
                var conv = new System.Windows.GridLengthConverter();
                len = (GridLength)conv.ConvertFromString((string)parameter);
            }
            else if (parameter is GridLength)
                len = (GridLength)parameter;

            return ((bool)value == true) ? len : new GridLength(0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {    // Don't need any convert back
            return null;
        }
    }

    public class BoolToGridRowHeightMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(values[0], targetType, values[1], culture);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var len = new GridLength(1, GridUnitType.Star);
            if (parameter is string)
            {
                var conv = new System.Windows.GridLengthConverter();
                len = (GridLength)conv.ConvertFromString((string)parameter);
            }
            else if (parameter is GridLength)
                len = (GridLength)parameter;

            return ((bool)value == true) ? len : new GridLength(0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {    // Don't need any convert back
            return null;
        }
    }
}
