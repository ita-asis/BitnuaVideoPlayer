using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace BitnuaVideoPlayer
{
    public class DoubleMultiplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double d1, d2;
            if (!double.TryParse(System.Convert.ToString(value), out d1) || !double.TryParse(System.Convert.ToString(parameter), out d2))
                throw new ArgumentException("value or paramter arn't double type");

            return d1 * d2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
