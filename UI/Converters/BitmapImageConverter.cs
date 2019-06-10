using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace BitnuaVideoPlayer
{
    public class BitmapImageConverter : IValueConverter
    {
        public object Convert(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            string path = value?.ToString();
            if (string.IsNullOrEmpty(path))
                return null;

            // Create source.
            BitmapImage bi = new BitmapImage();
            // BitmapImage.UriSource must be in a BeginInit/EndInit block.
            bi.BeginInit();

            if (parameter != null && int.TryParse(parameter.ToString(), out int pixelWidth))
                bi.DecodePixelWidth = pixelWidth;

            bi.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            bi.EndInit();

            return bi;
        }

        public object ConvertBack(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
