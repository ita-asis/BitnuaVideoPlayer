using CefSharp;
using CefSharp.Wpf;
using ColorFont;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;

namespace BitnuaVideoPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string c_ConfFilePath = "conf.json";
        private readonly PresentaionWindow m_PlayerWindow;

        public MainViewModel VM { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            DataContext = VM = MainViewModel.Create(c_ConfFilePath);
            m_PlayerWindow = new PresentaionWindow() {DataContext = VM };
            Loaded += MainWindow_Loaded;
            MouseDown += Window_MouseDown;

            App.Current.Exit += App_Exit;
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            Properties.Settings.Default.Save();
            VM.SaveTo(c_ConfFilePath);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            m_PlayerWindow.WindowStyle = WindowStyle.None;
            m_PlayerWindow.Show();
            m_PlayerWindow.Owner = this;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }


        private void btnLeftPicHeaderFontPicker(object sender, RoutedEventArgs e) =>
            VM.LeftPicTitle.Font = PickFont(VM.LeftPicTitle.Font);

        private void btnLeftPicHeaderBackColorPicker(object sender, RoutedEventArgs e) =>
            VM.LeftPicTitle.BackColor = PickColor(VM.LeftPicTitle.BackColor);

        private void btnLeftPicHeaderForeColorPicker(object sender, RoutedEventArgs e) =>
            VM.LeftPicTitle.ForeColor = PickColor(VM.LeftPicTitle.ForeColor);

        private void btnLyricsFontPicker(object sender, RoutedEventArgs e) =>
            VM.Lyrics.Font = PickFont(VM.Lyrics.Font);

        private void btnLyricsBackColorPicker(object sender, RoutedEventArgs e) =>
            VM.Lyrics.BackColor = PickColor(VM.Lyrics.BackColor);

        private void btnLyricsForeColorPicker(object sender, RoutedEventArgs e) =>
            VM.Lyrics.ForeColor = PickColor(VM.Lyrics.ForeColor);

        private void btnBannerFontPicker(object sender, RoutedEventArgs e) =>
            VM.Banner.Font = PickFont(VM.Banner.Font, (int)m_PlayerWindow.browser.Height);

        private void btnBannerBackColorPicker(object sender, RoutedEventArgs e) =>
            VM.Banner.BackColor = PickColor(VM.Banner.BackColor);

        private void btnBannerForeColorPicker(object sender, RoutedEventArgs e) =>
            VM.Banner.ForeColor = PickColor(VM.Banner.ForeColor);

        private static Font PickFont(Font font, int maxHeight = 72)
        {
            var fntDialog = new FontDialog()
            {
                FontMustExist = true,
                ShowEffects = false,
                MaxSize = maxHeight,
                Font = font
            };
            var dr = fntDialog.ShowDialog();
            if (dr != System.Windows.Forms.DialogResult.Cancel)
            {
                return fntDialog.Font;
            }

            return font;
        }

        private static Color PickColor(Color color)
        {
            var colorDialog = new ColorDialog() { AnyColor = true, Color = color };
            var dr = colorDialog.ShowDialog();
            if (dr != System.Windows.Forms.DialogResult.Cancel)
            {
                return colorDialog.Color;
            }

            return color;
        }
    }

    public class ColorToSolidColorBrushValueConverter : IValueConverter {

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
        if (null == value) {
            return null;
        }
            Type type = value.GetType();

            if (value is System.Windows.Media.Color)
                return new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)value);
            else if (value is System.Drawing.Color)
            {
                var sdColor = (System.Drawing.Color)value;
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(sdColor.A, sdColor.R, sdColor.G, sdColor.B));
            }

            throw new InvalidOperationException("Unsupported type [" + type.Name + "]");
        }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
        // If necessary, here you can convert back. Check if which brush it is (if its one),
        // get its Color-value and return it.

        throw new NotImplementedException();
    }
}
}
