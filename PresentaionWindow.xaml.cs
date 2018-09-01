using CefSharp;
using CefSharp.Wpf;
using ColorFont;
using MongoDB.Driver;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace BitnuaVideoPlayer
{
    internal static class Ex
    {
        public static void LoadHtml(this ChromiumWebBrowser browser, string html)
        {
            browser.Dispatcher.BeginInvoke(new Action(() =>
            {
                var brws = ((IWebBrowser)browser);
                brws.LoadHtml(html);

            }), System.Windows.Threading.DispatcherPriority.DataBind);
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class PresentaionWindow : Window
    {
        private MainViewModel VM;

        public PresentaionWindow()
        {
            InitializeComponent();

            DataContextChanged += PresentaionWindow_DataContextChanged;
            Loaded += (s, e) => { InitAll(); };
            MouseDown += Window_MouseDown;
            MouseDoubleClick += PresentaionWindow_MouseDoubleClick;
        }

        private void PresentaionWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.WindowState != WindowState.Maximized)
                this.WindowState = WindowState.Maximized;
            else
                this.WindowState = WindowState.Normal;
        }

        private void PresentaionWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            VM = e.NewValue as MainViewModel;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

       
        private void InitAll()
        {
            InitLeftPic();
        }

        private void InitLeftPic()
        {

            VM.LeftPicSource = @"D:\Videos\Overwolf\Thumbnails\Desktop 01-23-2017 16-11-07-290.jpg";

            if (VM.LeftPicTitle == null)
            {
                VM.LeftPicTitle = new ViewModels.TextVM()
                {
                    Text = "Sample Composer Name",
                    Font = new Font("Times New Roman",
                                                26f,
                                                System.Drawing.FontStyle.Regular,
                                                GraphicsUnit.Pixel
                                    ),
                    BackColor = Color.AliceBlue,
                    ForeColor = Color.Goldenrod
                };
            }
            else
            {
                VM.LeftPicTitle.Text = "Sample Composer Name";
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            App.Current.Shutdown();
        }
    }
}
