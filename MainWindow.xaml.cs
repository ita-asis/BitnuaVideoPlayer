using BitnuaVideoPlayer.ViewModels;
using CefSharp;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml.Linq;
using System.Windows.Threading;
using System.Diagnostics;
using System.Reflection;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Text;
using Newtonsoft.Json;
using System.ComponentModel;
using BitnuaVideoPlayer.UI;

namespace BitnuaVideoPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowVM m_MainWindowVM;
        private Canvas m_PresentationCanvas;
        public MainViewModel VM { get; set; }

        private void Init()
        {
            MouseDown += Window_MouseDown;
            VersionLbl.Content = GetVersion();
            AppUpdateManager.VersionUpdateProgressChanged += p => VersionLbl.Dispatcher.BeginInvoke(new Action(() => VersionLbl.Content = p >= 100 ? GetVersion() : $"Update in progress: {(p / 100d).ToString("P0")}"));
        }

        private object GetVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return string.Format("Version {0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        #region Font And Color pickers
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

        private void btnSongInfoFontPicker(object sender, RoutedEventArgs e) =>
           VM.SongInfo01.Font = PickFont(VM.SongInfo01.Font);
        private void btnSongInfo2FontPicker(object sender, RoutedEventArgs e) =>
           VM.SongInfo02.Font = PickFont(VM.SongInfo02.Font);

        private void btnSongInfoBackColorPicker(object sender, RoutedEventArgs e) =>
          VM.SongInfo01.BackColor = PickColor(VM.SongInfo01.BackColor);

        private void btnSongInfoForeColorPicker(object sender, RoutedEventArgs e) =>
          VM.SongInfo01.ForeColor = PickColor(VM.SongInfo01.ForeColor);

        private void btnSongInfo2ForeColorPicker(object sender, RoutedEventArgs e) =>
          VM.SongInfo02.ForeColor = PickColor(VM.SongInfo02.ForeColor);
        

        private void btnBannerFontPicker(object sender, RoutedEventArgs e) =>
            VM.Banner.Font = PickFont(VM.Banner.Font);

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

        private static Color PickColor(Color? i_color = null)
        {
            var color = i_color ?? Color.Black;
            var colorDialog = new ColorDialog() { AnyColor = true, Color = color };
            var dr = colorDialog.ShowDialog();
            if (dr != System.Windows.Forms.DialogResult.Cancel)
            {
                return colorDialog.Color;
            }

            return color;
        }
        #endregion

        private void addPresentationItemClick(object sender, RoutedEventArgs e)
        {
            ePresentationKinds kind = (ePresentationKinds)presentaionItemCB.SelectedItem;
            
            var item = PresentationItem.Create(kind, persentaionItemPath.Text);
            item.X = (int)(m_PresentationCanvas.Width - item.Width)/ 2;
            item.Y = (int)(m_PresentationCanvas.Height - item.Height) / 2;
            VM.PresentationVM.PresentationItems.Add(item);
        }

        private void pItemKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                OnDeletePresentationItem();
            }
        }

        private void OnDeletePresentationItem()
        {
            if (PVM.SelectedPresentationItem != null)
            {
                VM.PresentationVM.PresentationItems.Remove(PVM.SelectedPresentationItem);
            }
        }

        private void pItemPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var ctrl = ((FrameworkElement)sender);
            PVM.SelectedPresentationItem = (PresentationItem)ctrl.DataContext;
            //var listItem = (System.Windows.Controls.ListViewItem)sender;

            //listItem.pa

            //listItem.IsSelected = !listItem.IsSelected;
        }

        public MainWindowVM PVM => m_MainWindowVM ?? (m_MainWindowVM = new MainWindowVM());

        public class MainWindowVM: ViewModelBase
        {

            private PresentationItem m_SelectedPresentationItem;

            public PresentationItem SelectedPresentationItem
            {
                get { return m_SelectedPresentationItem; }
                set { m_SelectedPresentationItem = value; OnPropertyChanged(() => SelectedPresentationItem); }
            }
        }

        private void btnPresentationPathBrowse(object sender, RoutedEventArgs e)
        {
            var selectedType = (ePresentationKinds)presentaionItemCB.SelectedItem;
            switch (selectedType)
            {
                case ePresentationKinds.PictureList:
                case ePresentationKinds.VideoList:
                    using (var fileDialog = new CommonOpenFileDialog()
                    {
                        IsFolderPicker = true,
                        EnsureFileExists = false
                    })
                    {
                        if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                            persentaionItemPath.Text = fileDialog.FileName;
                    }
                    break;
                case ePresentationKinds.Picture:
                case ePresentationKinds.Video:
                    using (var fileDialog = new OpenFileDialog())
                    {
                        if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            persentaionItemPath.Text = fileDialog.FileName;
                    }
                    break;
                case ePresentationKinds.YoutubeVideo:
                    break;
                default:
                    break;
            }
        }

        private void designerCanvasLoaded(object sender, RoutedEventArgs e)
        {
            m_PresentationCanvas = sender as Canvas;
        }

        private Window m_SongInfoWindow;
        private void btnShowSongInfoPopup(object sender, RoutedEventArgs e)
        {
            var dto = Properties.Settings.Default.songInfoWindow;
            var defualtHeight = 100 + SystemParameters.WindowCaptionHeight;
            var content = new SongInfoControl() { DataContext = VM };

            m_SongInfoWindow?.Close();
            m_SongInfoWindow = CreateUserCtrlWindow(dto, defualtHeight, content);
           
            m_SongInfoWindow.Show();
        }

        private Window m_BannerWindow;
        private void btnShowBannerPopup(object sender, RoutedEventArgs e)
        {
            var dto = Properties.Settings.Default.bannerWindow;
            var defualtHeight = VM.Banner.Height + SystemParameters.WindowCaptionHeight;
            var content = new BannerControl() { DataContext = VM };

            m_BannerWindow?.Close();
            m_BannerWindow = CreateUserCtrlWindow(dto, defualtHeight, content);
            m_BannerWindow.Show();
        }

        private Window CreateUserCtrlWindow(WindowStateDTO dto, double defualtHeight, object content)
        {
            var window = new Window()
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                Height = dto.Height > 0 ? dto.Height : defualtHeight,
                Width = dto.Width > 0 ? dto.Width : SystemParameters.PrimaryScreenWidth,
                Left = dto.Left,
                Top = dto.Top,
                Content = content,
                Topmost = true,
                WindowStyle = WindowStyle.ToolWindow,
            };

            window.Closing += (s, ee) =>
            {
                dto.Width = window.Width;
                dto.Height = window.Height;
                dto.Top = window.Top;
                dto.Left = window.Left;
                Properties.Settings.Default.Save();
            };

            window.MouseDown += (s, ee) =>
            {
                if (ee.ChangedButton == MouseButton.Left)
                    window.DragMove();
            };
            return window;
        }

        private void btnShowPresentaionWindow(object sender, RoutedEventArgs e)
        {
            App.Instance.ShowPresentaionWindow();
        }

        private void btnHidePresentaionWindow(object sender, RoutedEventArgs e)
        {
            App.Instance.HidePresentaionWindow();
        }

        private void btnHideBannerPopup(object sender, RoutedEventArgs e)
        {
            m_BannerWindow?.Close();
        }

        private void btnHideSongInfoPopup(object sender, RoutedEventArgs e)
        {
            m_SongInfoWindow?.Close();
        }

        private void btnRestoreSongInfoPopup(object sender, RoutedEventArgs e)
        {
            BringWindowIntoView(m_SongInfoWindow);
        }

        private void btnRestoreBannerPopup(object sender, RoutedEventArgs e)
        {
            BringWindowIntoView(m_BannerWindow);

        }
        private void btnRestorePresentaionWindow(object sender, RoutedEventArgs e)
        {
            BringWindowIntoView(App.Instance.m_PlayerWindow);
        }

        private void BringWindowIntoView(Window window)
        {
            if (window == null)
                return;

            var left = this.Left + this.Width + 10;
            var top = this.Top;
            window.Left = left;
            window.Top = top;
        }

        private void presentaionItemCBChecked(object sender, RoutedEventArgs e)
        {
            if (btnPresentationPath != null && persentaionItemPath != null)
                btnPresentationPath.IsEnabled = persentaionItemPath.IsEnabled = ((ePresentationKinds)presentaionItemCB.SelectedValue) != ePresentationKinds.AmpsLive;
        }

        private void presentaionItemCBContextChanged(object sender, DependencyPropertyChangedEventArgs e) => presentaionItemCBChecked(null, null);
    }

    public class ColorToSolidColorBrushValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (null == value)
            {
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

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // If necessary, here you can convert back. Check if which brush it is (if its one),
            // get its Color-value and return it.

            throw new NotImplementedException();
        }
    }

    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return source.Shuffle(new Random());
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (rng == null) throw new ArgumentNullException("rng");

            return source.ShuffleIterator(rng);
        }

        private static IEnumerable<T> ShuffleIterator<T>(
            this IEnumerable<T> source, Random rng)
        {
            List<T> buffer = source.ToList();
            for (int i = 0; i < buffer.Count; i++)
            {
                int j = rng.Next(i, buffer.Count);
                yield return buffer[j];

                buffer[j] = buffer[i];
            }
        }
    }
}
