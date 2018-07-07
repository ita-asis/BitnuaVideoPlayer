using CefSharp;
using CefSharp.Wpf;
using ColorFont;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml.Linq;

namespace BitnuaVideoPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string c_ConfFilePath = "conf.json";
        private readonly PresentaionWindow m_PlayerWindow;
        private readonly FileSystemWatcher m_FileSysWatcher;

        public MainViewModel VM { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            DataContext = VM = MainViewModel.Create(c_ConfFilePath);
            m_PlayerWindow = new PresentaionWindow() {DataContext = VM };

            m_FileSysWatcher = new FileSystemWatcher()
            {
                Path = VM.WatchDir,
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = "*.*",
                EnableRaisingEvents = true
            };
            m_FileSysWatcher.Changed += new FileSystemEventHandler(OnWatchDirChanged);

            Loaded += MainWindow_Loaded;
            MouseDown += Window_MouseDown;

            App.Current.Exit += App_Exit;
        }

        private async void OnWatchDirChanged(object sender, FileSystemEventArgs e)
        {
            await Task.Delay(100);
            await UpdateVM(VM, e.FullPath.Replace(@"//", @"/"));
        }

        private Task UpdateVM(MainViewModel vm, string filePath)
        {
            var fields = ReadSongXml(filePath);
            vm.LeftPicTitle.Text = fields[Fields.Title].Value;

            Field field;
            int year;
            var song = new ViewModels.Song();
            if (fields.TryGetValue(Fields.Title, out field))
                song.Title = field.Value;
            if (fields.TryGetValue(Fields.Performer, out field))
                song.Performer = field.Value;
            if (fields.TryGetValue(Fields.Creator, out field))
                song.Creator = field.Value;
            if (fields.TryGetValue(Fields.Composer, out field))
                song.Composer = field.Value;
            if (fields.TryGetValue(Fields.Writer, out field))
                song.Writer = field.Value;
            if (fields.TryGetValue(Fields.Year, out field) && int.TryParse(field.Value, out year))
                song.Year = year;

            vm.Song = song;

            return Task.CompletedTask;
        }

        private static Dictionary<string, Field> ReadSongXml(string filePath)
        {
            var fields = new Dictionary<string, Field>();
            var xml = File.ReadAllText(filePath);
            var doc = XDocument.Parse(xml);

            var song = doc.Element("Song");

            foreach (XElement current in song.Descendants("Field"))
            {
                var name = current.Attribute("Name").Value;
                var leng = current.Attribute("Language").Value;
                var value = current.Value;

                var field = new Field(name, value, leng);
                fields.Add(field.Name, field);
            }

            return fields;
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

        private struct Field
        {
            public readonly string Leng;
            public readonly string Name;
            public readonly string Value;

            public Field(string name, string value, string leng)
            {
                Name  = name;
                Value = value;
                Leng  = leng;
            }
        }

        public static class Fields
        {
            public const string Title = "Title";
            public const string Heb_Title = "שם הריקוד";
            public const string Type = "Type";
            public const string Year = "שנה";
            public const string Tab2 = "לשונית משנה";
            public const string Performer = "מבצע";
            public const string LastPlayed = "Last Played";
            public const string LastMarked = "LastMarked";
            public const string TotalPlays = "TotalPlays";
            public const string SongChanged = "SongChanged";
            public const string SongAdded = "SongAdded";
            public const string SongLastPlayed = "SongLastPlayed";
            public const string ID = "ID";
            public const string Valid = "Valid";
            public const string FileModified = "FileModified";
            public const string TypeID = "TypeID";
            public const string FileName = "FileName";
            public const string FilePath = "FilePath";
            public const string FileExt = "FileExt";
            public const string SongLength = "Song Length";
            public const string Flag = "Flag";
            public const string Show = "Show";
            public const string Speed = "Speed";
            public const string Pitch = "Pitch";
            public const string Volume = "Volume";
            public const string FullPath = "FullPath";
            public const string id3_Title = "id3 Title";
            public const string id3_Artist = "id3_Artist";
            public const string id3_Album = "id3_Album";
            public const string id3_Year = "id3 Year";
            public const string id3_Genre = "id3 Genre";
            public const string id3_Comment = "id3_Comment";
            public const string Composer = "לחן";
            public const string Creator = "יוצר";
            public const string Writer = "משורר";
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
