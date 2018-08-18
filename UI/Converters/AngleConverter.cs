using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace BitnuaVideoPlayer
{
    public sealed class AngleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double))
                throw new NotSupportedException();

            return new RotateTransform(((double)value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is RotateTransform))
                throw new NotSupportedException();

            return ((RotateTransform)value).Angle;
        }
    }
}
