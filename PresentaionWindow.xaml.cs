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
                ((IWebBrowser)browser).LoadHtml(html);
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
            if (e.OldValue is MainViewModel)
                ((MainViewModel)e.OldValue).PropertyChanged -= VM_PropertyChanged;

            VM = e.NewValue as MainViewModel;
            VM.PropertyChanged += VM_PropertyChanged;
            VM.Banner.PropertyChanged += VM_PropertyChanged;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void VM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // register for prop change of non wpf binded controls
            if (sender == VM.Banner)
            {
                InitBanner();
            }
        }

        private void InitAll()
        {
            InitBanner();
            InitLeftPic();
        }

        private void InitBanner()
        {
            var html =
                $@"<html><head>
                <meta charset=utf-8>
                    <style>
                    body {{
                        overflow: -moz-scrollbars-vertical;
                        overflow-x: hidden;
                        overflow-y: auto;
                        background-color: rgba({VM.Banner.BackColor.R},{VM.Banner.BackColor.G},{VM.Banner.BackColor.B},{VM.Banner.BackColor.A});
                    }}
                    .parent {{
                        height: 40px;
                        text-align: center;
                    }}
                    .parent > .child {{
                        line-height: 50px;
                    }}
                    marquee {{
                        font-size: {VM.Banner.Font.Size}px;
                        font-family: {VM.Banner.Font.FontFamily.Name};
                        font-style: {(VM.Banner.Font.Italic ? "italic" :"normal")};
                        font-weight: {(VM.Banner.Font.Bold ? "bold" : "normal")};
                        text-decoration: {(VM.Banner.Font.Underline ? "underline" : "")} {(VM.Banner.Font.Strikeout ? "line-through" : "")};
                        color: rgba({VM.Banner.ForeColor.R},{VM.Banner.ForeColor.G},{VM.Banner.ForeColor.B},{VM.Banner.ForeColor.A});
                    }}

                </style>
                </head><body>
                        <div class=""parent"">
                            <marquee class=""child"" scrolldelay=""{VM.Banner.Speed}"" behavior=""scroll"" direction=""{VM.Banner.Direction}"">{ VM.Banner.Text}</marquee>
                        </div>
                </body></html>";

            browser.LoadHtml(html);
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
