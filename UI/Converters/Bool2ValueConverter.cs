using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace BitnuaVideoPlayer
{
    [ValueConversion(typeof(bool), typeof(object))]
    public class Value2BoolConverter : IValueConverter
    {
        public object TrueValue { get; set; }
        public object FalseValue { get; set; }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool))
                return null;
            return (bool)value ? TrueValue : FalseValue;
        }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (IsEqual(value, TrueValue))
                return true;
            if (IsEqual(value, FalseValue))
                return false;
            return null;
        }

        private static bool IsEqual(object x, object y)
        {
            if (Equals(x, y))
                return true;

            IComparable c = x as IComparable;
            if (c != null)
                return (c.CompareTo(y) == 0);

            return false;
        }
    }

    public class Bool2ValueConverter: Value2BoolConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) => base.ConvertBack(value, targetType, parameter, culture);
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => base.Convert(value, targetType, parameter, culture);
    }
}
