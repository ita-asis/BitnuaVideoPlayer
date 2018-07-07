using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace BitnuaVideoPlayer
{
    public sealed class StringToEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value is string && !string.IsNullOrEmpty((string)value))
                    return Enum.Parse(targetType, (string)value);
            }
            catch
            {
            }

            return Enum.GetValues(targetType).Cast<object>().First();
        }


        // No need to implement converting back on a one-way binding 
        public object ConvertBack(object value, Type targetType,
          object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
